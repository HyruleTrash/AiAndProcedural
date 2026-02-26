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
    private NativeList<Node> nodes;
    private NativeList<int> openNodes;
    private NativeList<int> closedNodes;

    private NativeArray<CellData> flatGrid;
    
    public List<int2> FindPathToTarget(int2 startPos, int2 endPos, Cell[,] grid)
    {
        InitData(grid);
        
        // start astar job
        var pathFinder = new AStarPathFind(startPos, endPos, grid, nodes, openNodes, closedNodes, flatGrid);
        var resultPath = pathFinder.StartJobLoop();
        CleanupData();
        
        // check and handle result
        if (resultPath.Length <= 0) return new List<int2>();
        var result = ResultToUsableData(resultPath);
        resultPath.Dispose();
        
        return result;
    }
    
    public static int GetGridIndex(int2 pos, int gridWidth) => pos.y * gridWidth + pos.x;

    private void InitData(Cell[,] grid)
    {
        flatGrid = GetCellData(grid);
        nodes = new NativeList<Node>(Allocator.TempJob);
        openNodes = new NativeList<int>(Allocator.TempJob);
        closedNodes = new NativeList<int>(Allocator.TempJob);
    }
    
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

    private void CleanupData()
    {
        flatGrid.Dispose();
        nodes.Dispose();
        openNodes.Dispose();
        closedNodes.Dispose();
    }
    
    
    private List<int2> ResultToUsableData(NativeList<int2> native)
    {
        var result = new List<int2>();
        foreach (var pos in native) result.Add(pos);
        result.Reverse(0, result.Count);
        return result;
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

public class AStarPathFind
{
    private NativeList<Node> nodes;
    private NativeList<int> openNodes;
    private NativeList<int> closedNodes;
    private NativeArray<CellData> flatGrid;
    
    private readonly int2 startPos;
    private readonly int2 endPos;
    private readonly Cell[,] grid;

    public AStarPathFind(int2 startPos, int2 endPos, Cell[,] grid, NativeList<Node> nodes, NativeList<int> openNodes,
        NativeList<int> closedNodes, NativeArray<CellData> flatGrid)
    {
        this.startPos = startPos;
        this.endPos = endPos;
        this.grid = grid;
        this.nodes = nodes;
        this.openNodes = openNodes;
        this.closedNodes = closedNodes;
        this.flatGrid = flatGrid;
    }

    public NativeList<int2> StartJobLoop()
    {
        var addStartNodeJob = new AddNodeJob(
            startPos,
            nodes,
            openNodes,
            closedNodes,
            startPos,
            endPos,
            grid.GetLength(0),
            grid.GetLength(1),
            flatGrid);
        var handle = addStartNodeJob.Schedule();
        handle.Complete();
        
        if (openNodes.Length <= 0)
            return default;
        
        var tries = -1;
        var maxTries = grid.LongLength;
        Node? found = null;
        
        while (openNodes.Length > 0 && tries < maxTries)
        {
            var breadthFirstJob = new AStarBreadthFirstJob(
                nodes, openNodes, closedNodes,
                startPos, endPos,
                flatGrid, grid.GetLength(0), grid.GetLength(1)
                );
            handle = breadthFirstJob.Schedule(openNodes.Length, 64);
            handle.Complete();
            
            tries += openNodes.Length;
            // TODO handle nodesToAdd

            if (breadthFirstJob.found) 
                found = nodes[breadthFirstJob.foundIndex];
        }

        if (found == null)
            return default;
        
        return CalcResultPath(found.Value);
    }
    
    private NativeList<int2> CalcResultPath(Node found)
    {
        var current = found;
        var resultPath = new NativeList<int2>(Allocator.TempJob);
        resultPath.Add(current.position);
        
        while (true)
        {
            if (current.parentId == -1)
                break;
            var parent = nodes[current.parentId];
            resultPath.Add(parent.position);
            current = parent;
        }
        
        return resultPath;
    }
}

[BurstCompile]
public struct AddNodeJob : IJob
{
    private int2 posToAdd;
    
    private NativeList<Node> nodes;
    private NativeList<int> openNodes;
    private NativeList<int> closedNodes;
    
    private readonly int2 startPos;
    private readonly int2 endPos;
    
    private readonly int gridWidth;
    private readonly int gridHeight;
    private readonly NativeArray<CellData> grid;

    private struct CompareNodeOrder : IComparer<int>
    {
        private NativeList<Node> openNodes;

        public CompareNodeOrder(NativeList<Node> openNodes) => this.openNodes = openNodes;
        public int Compare(int x, int y) => openNodes[x].FScore.CompareTo(openNodes[y].FScore);
    }
    
    public AddNodeJob(int2 posToAdd, NativeList<Node> nodes, NativeList<int> openNodes,
        NativeList<int> closedNodes, int2 startPos, int2 endPos, int gridWidth, int gridHeight, NativeArray<CellData> grid)
    {
        this.posToAdd = posToAdd;
        
        this.nodes = nodes;
        this.openNodes = openNodes;
        this.closedNodes = closedNodes;
        
        this.startPos = startPos;
        this.endPos = endPos;
        
        this.gridWidth = gridWidth;
        this.gridHeight = gridHeight;
        this.grid = grid;
    }
    
    public void Execute() => AddNode(posToAdd, 0);

    private void AddNode(int2 pos, int id)
    {
        var node = new Node(pos, pos.ManhattanDistance(startPos),
            pos.ManhattanDistance(endPos), id);
        nodes.Add(node);
        closedNodes.Add(node.id);
        
        var neighbors = GetNeighbors(nodes, node, startPos, endPos, gridWidth, gridHeight);
        
        var nodesToAdd = new NativeList<Node>(Allocator.Temp);
        var openNodesWrite = new NativeList<int>(Allocator.Temp);
        CheckNeighborsAndAddToOpen(neighbors, node, 
            nodes, nodesToAdd, 
            openNodes, openNodesWrite, closedNodes, 
            gridWidth, grid);
        
        nodes.AddRange(nodesToAdd.AsArray());
        openNodes.AddRange(openNodesWrite.AsArray());
        openNodes.Sort(new CompareNodeOrder(nodes));
    }

    #region NeighborLogic
    public static NativeList<Node> GetNeighbors(NativeList<Node> nodes,
        Node current,
        int2 startPos,
        int2 endPos,
        int gridWidth,
        int gridHeight)
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
    
    public static void CheckNeighborsAndAddToOpen(NativeList<Node> neighbors,
        Node center,
        NativeList<Node> nodes, NativeList<Node> nodesToAdd,
        NativeList<int> openNodesRead, NativeList<int> openNodesWrite,
        NativeList<int> closedNodesRead,
        int gridWidth, NativeArray<CellData> grid)
    {
        for (var i = 0; i < neighbors.Length; i++)
        {
            var neighbor = neighbors[i];
                
            if (!CheckTraversable(center, neighbor, gridWidth, grid) || NodeIdListContains(neighbor.id, closedNodesRead))
                continue;

            if (NodeIdListContains(neighbor.id, openNodesRead) && !(neighbor.FScore < center.FScore)) continue;
            neighbor.parentId = center.id;
            
            if (NodeIdListContains(neighbor.id, openNodesRead)) continue;
            neighbor.id = nodes.Length + nodesToAdd.Length;
            nodesToAdd.Add(neighbor);
            openNodesWrite.Add(neighbor.id);
        }
        neighbors.Dispose();
    }

    public static bool CheckTraversable(Node current, Node neighbor, int gridWidth, NativeArray<CellData> grid)
    {
        var cell = grid[Astar.GetGridIndex(current.position, gridWidth)];
        var direction = neighbor.position - current.position;
        
        // ignore node if there's a wall
        if ((direction.x < 0 && cell.HasWall(Wall.LEFT))
            || (direction.x > 0 && cell.HasWall(Wall.RIGHT))
            || (direction.y < 0 && cell.HasWall(Wall.DOWN))
            || (direction.y > 0 && cell.HasWall(Wall.UP)))
            return false;
        return true;
    }
    #endregion

    #region ListExtentions
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
    #endregion
}

[BurstCompile]
public struct AStarBreadthFirstJob : IJobParallelFor
{
    private readonly NativeList<Node> existingNodes;
    public NativeList<Node> nodesToAdd;
    private readonly NativeList<int> openNodesRead;
    private NativeList<int> openNodesWrite;
    private readonly NativeList<int> closedNodesRead;
    private NativeList<int> closedNodesWrite;
    
    private readonly int2 startPos;
    private readonly int2 endPos;
    
    private readonly NativeArray<CellData> grid;
    private readonly int gridWidth;
    private readonly int gridHeight;
    
    public bool found;
    public int foundIndex;

    public AStarBreadthFirstJob(NativeList<Node> nodes,
        NativeList<int> openNodes, NativeList<int> closedNodes,
        int2 startPos, int2 endPos, NativeArray<CellData> grid, int gridWidth, int gridHeight)
    {
        nodesToAdd = new NativeList<Node>(Allocator.TempJob);
        existingNodes = nodes;
        
        openNodesWrite = new NativeList<int>(Allocator.TempJob);
        openNodesRead = openNodes;
        
        closedNodesWrite = new NativeList<int>(Allocator.TempJob);
        closedNodesRead = closedNodes;
        
        this.startPos = startPos;
        this.endPos = endPos;
        
        this.grid = grid;
        this.gridWidth = gridWidth;
        this.gridHeight = gridHeight;
        
        found = false;
        foundIndex = -1;
    }

    public void Execute(int index)
    {
        var current = existingNodes[index];
        openNodes.RemoveAt(0);
        closedNodes.Add(current.id);

        // target has been found
        if (current.position.x == endPos.x && current.position.y == endPos.y)
        {
            found = true;
            foundIndex = index;
            return;
        }
            
        var neighbors = AddNodeJob.GetNeighbors(existingNodes, current, startPos, endPos, gridWidth, gridHeight);
        AddNodeJob.CheckNeighborsAndAddToOpen(neighbors, current, existingNodes, openNodes, closedNodes, gridWidth, grid);
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
