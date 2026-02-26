using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class Astar
{
    public List<int2> FindPathToTarget(int2 startPos, int2 endPos, Cell[,] grid)
    {
        // turn grid to a job usable one
        var currentGrid = GetCellData(grid);
        
        // start astar job
        var calculatePathJob = new CalculateAStarPathJob(
            startPos,
            endPos,
            currentGrid,
            grid.GetLength(0),
            grid.GetLength(1),
            new NativeList<int2>(Allocator.TempJob));
        var handle = calculatePathJob.Schedule();
        handle.Complete();

        calculatePathJob.nodes.Dispose();
        calculatePathJob.openNodes.Dispose();
        calculatePathJob.closedNodes.Dispose();
        
        // check and handle result
        if (calculatePathJob.resultPath.Length <= 0) return new List<int2>();
        
        Debug.Log($"Path found {startPos}, {endPos}");
        var result = new List<int2>();
        foreach (var pos in calculatePathJob.resultPath) result.Add(pos);
        calculatePathJob.resultPath.Dispose();
        
        result.Reverse(0, result.Count);
        return result;
    }
    
    public static int GetGridIndex(int2 pos, int gridWidth) => pos.y * gridWidth + pos.x;

    private NativeArray<CellData> GetCellData(Cell[,] grid)
    {
        var currentGrid = new NativeArray<CellData>(grid.GetLength(0) * grid.GetLength(1), Allocator.TempJob);
        for (var y = 0; y < grid.GetLength(1); y++)
        {
            for (var x = 0; x < grid.GetLength(0); x++)
            {
                var original = grid[x, y];
                currentGrid[GetGridIndex(new int2(x, y), grid.GetLength(0))] = new CellData{ walls = original.walls };
            }
        }
        return currentGrid;
    }
}


public struct Node
{
    public readonly int2 position;        //Position on the grid
    public int id;
    public int parentId;                  //Parent Node of this node

    public float FScore => gScore + hScore;
    private readonly float gScore;        //Current Traveled Distance
    private readonly float hScore;        //Distance estimated based on Heuristic

    public Node(int2 position, int gScore, int hScore, int id, int parentId = -1)
    {
        this.position = position;
        this.parentId = parentId;
        this.gScore = gScore;
        this.hScore = hScore;
        this.id = id;
    }
}

public struct CellData
{
    public Wall walls;
    public bool HasWall(Wall wallDirection) => (walls & wallDirection) != 0;
}

[BurstCompile]
public struct CalculateAStarPathJob : IJob
{
    public NativeList<int2> resultPath;

    private readonly int2 startPos;
    private readonly int2 endPos;
    private readonly NativeArray<CellData> grid;
    private readonly int gridWidth;
    private readonly int gridHeight;
    
    public NativeList<Node> nodes;
    public NativeList<int> openNodes;
    public NativeList<int> closedNodes;

    public CalculateAStarPathJob(
        int2 startPos,
        int2 endPos,
        NativeArray<CellData> grid,
        int gridWidth,
        int gridHeight,
        NativeList<int2> resultPath)
    {
        this.startPos = startPos;
        this.endPos = endPos;
        this.grid = grid;
        this.gridWidth = gridWidth;
        this.gridHeight = gridHeight;
        this.resultPath = resultPath;
        
        nodes = new NativeList<Node>(Allocator.TempJob);
        openNodes = new NativeList<int>(Allocator.TempJob);
        closedNodes = new NativeList<int>(Allocator.TempJob);
    }

    public void Execute()
    {
        AddStartNode();
        if (openNodes.Length <= 0)
            return;

        Node? current = null;
        var tries = -1;
        var maxTries = gridWidth * gridHeight;
        while (openNodes.Length > 0 && tries < maxTries)
        {
            tries++;
            current = GetLowestFAndRemoveFromOpen();
            
            if (current.Value.position.x == endPos.x && current.Value.position.y == endPos.y) // target has been found
                break;
            
            var neighbors = GetNeighbors(current.Value);
            CheckNeighborsAndAddToOpen(neighbors, current.Value);
        }
        if (current == null)
            return;

        CalcResultPath(current.Value);
    }

    private void AddStartNode()
    {
        var startNode = new Node(startPos, 0,
            startPos.ManhattanDistance(endPos), 0);
        nodes.Add(startNode);
        closedNodes.Add(startNode.id);
        var neighbors = GetNeighbors(startNode);
        CheckNeighborsAndAddToOpen(neighbors, startNode);
    }
    
    private Node GetLowestFAndRemoveFromOpen()
    {
        var index = openNodes[0];
        var toReturn = nodes[index];
        openNodes.RemoveAt(0);
        closedNodes.Add(toReturn.id);
        return toReturn;
    }

    private struct CompareNodeOrder : IComparer<int>
    {
        private NativeList<Node> openNodes;

        public CompareNodeOrder(NativeList<Node> openNodes) => this.openNodes = openNodes;
        public int Compare(int x, int y) => openNodes[x].FScore.CompareTo(openNodes[y].FScore);
    }
    
    private void AddToOpen(Node toAdd)
    {
        toAdd.id = nodes.Length;
        nodes.Add(toAdd);
        openNodes.Add(toAdd.id);
        openNodes.Sort(new CompareNodeOrder(nodes));
    }
    
    private NativeList<Node> GetNeighbors(Node current)
    {
        var result = new NativeList<Node>(Allocator.TempJob);
        for (var x = -1; x < 2; x++)
        {
            for (var y = -1; y < 2; y++)
            {
                var cellX = current.position.x + x;
                var cellY = current.position.y + y;
                if (cellX < 0 || cellX >= gridWidth || cellY < 0 || cellY >= gridHeight || Mathf.Abs(x) == Mathf.Abs(y))
                    continue;
                
                var neighborPos = new int2(cellX, cellY);
                
                // check if cell exists or not
                if (NodeListContainsPos(neighborPos, nodes))
                    continue;
                
                var candidateCell = new Node(neighborPos,
                    neighborPos.ManhattanDistance(startPos),
                    neighborPos.ManhattanDistance(endPos),
                    -1,
                    current.id);
                result.Add(candidateCell);
            }
        }
        return result;
    }
    
    private void CheckNeighborsAndAddToOpen(NativeList<Node> neighbors, Node center)
    {
        for (var i = 0; i < neighbors.Length; i++)
        {
            var neighbor = neighbors[i];
                
            if (!CheckTraversable(center, neighbor) || NodeIdListContains(neighbor.id, closedNodes))
                continue;

            if (!NodeIdListContains(neighbor.id, openNodes) || neighbor.FScore < center.FScore)
            {
                neighbor.parentId = center.id;
                if (!NodeIdListContains(neighbor.id, openNodes))
                    AddToOpen(neighbor);
            }
        }
        neighbors.Dispose();
    }

    private bool CheckTraversable(Node current, Node neighbor)
    {
        var cell = grid[Astar.GetGridIndex(current.position, gridWidth)];
        var direction = neighbor.position - current.position;
        
        // ignore node if there's a wall
        Debug.Log($"dir {direction}\nleft: {cell.HasWall(Wall.LEFT)}, right: {cell.HasWall(Wall.RIGHT)}, up: {cell.HasWall(Wall.UP)}, down: {cell.HasWall(Wall.DOWN)}");
        if ((direction.x < 0 && cell.HasWall(Wall.LEFT))
            || (direction.x > 0 && cell.HasWall(Wall.RIGHT))
            || (direction.y < 0 && cell.HasWall(Wall.DOWN))
            || (direction.y > 0 && cell.HasWall(Wall.UP)))
            return false;
        return true;
    }

    private static bool NodeIdListContains(int toFind, NativeList<int> list)
    {
        foreach (var id in list)
        {
            if (id == toFind)
                return true;
        }
        return false;
    }
    
    private static bool NodeListContainsPos(int2 toFind, NativeList<Node> list)
    {
        foreach (var node in list)
        {
            if (node.position.x == toFind.x && node.position.y == toFind.y)
                return true;
        }
        return false;
    }

    private void CalcResultPath(Node found)
    {
        var current = found;
        
        resultPath.Add(current.position);
        
        while (true)
        {
            if (current.parentId == -1)
                break;
            var parent = nodes[current.parentId];
            resultPath.Add(parent.position);
            current = parent;
        }
    }
}

public static class Int2Extensions
{
    public static int ManhattanDistance(this int2 a, int2 b)
    {
        
        var dx = Mathf.Abs(a.x - b.x);
        var dy = Mathf.Abs(a.y - b.y);
        return dx + dy;
    }
}
