using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Astar
{
    private Vector2Int startPos;
    private Vector2Int endPos;
    
    private List<Node> nodes = new();
    private List<Vector2Int> openNodes = new();
    private List<Vector2Int> closedNodes = new();
    private Cell[,] grid;
    
    /// <summary>
    /// This readonly IComparer class is used for sorting the openNodesList, based on node F cost
    /// </summary>
    private readonly struct CompareNodeOrder : IComparer<Vector2Int>
    {
        private readonly List<Node> openNodes;
        public CompareNodeOrder(List<Node> openNodes) => this.openNodes = openNodes;
        public int Compare(Vector2Int x, Vector2Int y) => GetNodeWithPos(x).FScore.CompareTo(GetNodeWithPos(y).FScore);
        private Node GetNodeWithPos(Vector2Int pos) => openNodes.First(node => node.position == pos);
    }

    private void Init(Vector2Int sP, Vector2Int eP, Cell[,] g)
    {
        startPos = sP;
        endPos = eP;
        grid = g;
        
        nodes.Clear();
        openNodes.Clear();
        closedNodes.Clear();
    }

    /// <summary>
    /// Triggers the A star Algorithm
    /// </summary>
    /// <param name="sP">StartPoint</param>
    /// <param name="eP">EndPoint</param>
    /// <param name="g">Grid</param>
    /// <returns>List of points along the grid towards the end point, starting from the start point</returns>
    public List<Vector2Int> FindPathToTarget(Vector2Int sP, Vector2Int eP, Cell[,] g)
    {
        Init(sP, eP, g);
        var resultPath = TryFindPath();
        if (resultPath == null || resultPath.Count <= 0) return new List<Vector2Int>();
        return resultPath;
    }

    private List<Vector2Int> TryFindPath()
    {
        AddStartNode();
        if (openNodes.Count <= 0)
            return null;

        Node current = null;
        var tries = -1;
        var maxTries = grid.LongLength;
        while (openNodes.Count > 0 && tries < maxTries)
        {
            tries++;
            current = GetLowestFAndRemoveFromOpen();
            
            // target has been found
            if (current.position.x == endPos.x && current.position.y == endPos.y)
                break;
            
            var cell = grid[current.position.x, current.position.y];
            var neighbors = cell.GetNeighbours(grid);
            ViableNeighborsToOpenNodes(neighbors, current);
        }
        return current == null ? null : GetPathFromEndNode(current);
    }

    private void AddStartNode()
    {
        var startNode = new Node(startPos,0, startPos.ManhattanDistance(endPos));
        
        nodes.Add(startNode);
        closedNodes.Add(startNode.position);
        
        var cell = grid[startNode.position.x, startNode.position.y];
        var neighbors = cell.GetNeighbours(grid);
        ViableNeighborsToOpenNodes(neighbors, startNode);
    }
    
    private void AddToOpen(Node toAdd)
    {
        nodes.Add(toAdd);
        openNodes.Add(toAdd.position);
        openNodes.Sort(new CompareNodeOrder(nodes));
    }

    private void ViableNeighborsToOpenNodes(List<Cell> neighbors, Node center)
    {
        foreach (var neighborCell in neighbors)
        {
            if (!CheckTraversable(center, neighborCell, grid) || closedNodes.Any(pos => neighborCell.gridPosition == pos)) continue;
            if (openNodes.Any(pos => neighborCell.gridPosition == pos)) continue;

            var neighbor = new Node(neighborCell.gridPosition,
                center.gScore + 1f,
                neighborCell.gridPosition.ManhattanDistance(endPos), center);
            
            AddToOpen(neighbor);
            for (var i = 0; i < nodes.Count; i++)
                if (nodes[i].position == neighbor.position && neighbor.FScore < nodes[i].FScore)
                    nodes[i] = neighbor;
        }
    }

    private static bool CheckTraversable(Node current, Cell neighbor, Cell[,] grid)
    {
        var direction = neighbor.gridPosition - current.position;
        var currentCell = grid[current.position.x, current.position.y];
        
        // ignore node if there's a wall
        if ((direction.x < 0 && currentCell.HasWall(Wall.LEFT))
            || (direction.x > 0 && currentCell.HasWall(Wall.RIGHT))
            || (direction.y < 0 && currentCell.HasWall(Wall.DOWN))
            || (direction.y > 0 && currentCell.HasWall(Wall.UP)))
            return false;
        return true;
    }

    private Node GetLowestFAndRemoveFromOpen()
    {
        var toReturn = nodes.First(node => node.position == openNodes[0]);
        openNodes.RemoveAt(0);
        closedNodes.Add(toReturn.position);
        return toReturn;
    }

    private static List<Vector2Int> GetPathFromEndNode(Node found)
    {
        var current = found;
        var resultPath = new List<Vector2Int> { current.position };

        while (true)
        {
            if (current.parent == null)
                break;
            resultPath.Add(current.parent.position);
            current = current.parent;
        }
        
        resultPath.Reverse(0, resultPath.Count);
        return resultPath;
    }
}