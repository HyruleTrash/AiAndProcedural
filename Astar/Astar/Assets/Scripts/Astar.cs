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
    /// <summary>
    /// TODO: Implement this function so that it returns a list of int2 positions which describes a path from the startPos to the endPos
    /// Note that you will probably need to add some helper functions
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <param name="grid"></param>
    /// <returns></returns>
    public List<int2> FindPathToTarget(int2 startPos, int2 endPos, Cell[,] grid)
    {
        // turn grid to a job usable one
        var currentGrid = new NativeArray<CellData>(grid.GetLength(0) * grid.GetLength(1), Allocator.Temp);
        var index = 0;
        foreach (var cell in grid)
        {
            currentGrid[index] = new CellData
            {
                gridPosition = new int2(cell.gridPosition.x, cell.gridPosition.y),
                walls = cell.walls,
            };
            index++;
        }
        
        // start astar job
        var calculatePathJob = new CalculateAStarPathJob
        {
            startPos = startPos,
            endPos = endPos,
            grid = currentGrid,
            gridHeight = grid.GetLength(1),
        };
        var handle = calculatePathJob.Schedule();
        handle.Complete();

        // handle result
        var result = calculatePathJob.resultPath.ToList();
        calculatePathJob.resultPath.Dispose();
        return result;
    }

    /// <summary>
    /// This is the Node class you can use this class to store calculated FScores for the cells of the grid, you can leave this as it is
    /// </summary>
    private struct Node
    {
        public int2 position;       //Position on the grid
        public int parentIndex;     //Parent Node of this node

        public float FScore => gScore + hScore;
        public float gScore;        //Current Traveled Distance
        public float hScore;        //Distance estimated based on Heuristic

        public Node(int2 position, int parentIndex, int gScore, int hScore)
        {
            this.position = position;
            this.parentIndex = parentIndex;
            this.gScore = gScore;
            this.hScore = hScore;
        }
    }

    private struct CellData
    {
        public int2 gridPosition;
        public Wall walls;
    }

    [BurstCompile]
    private struct CalculateAStarPathJob : IJob
    {
        public int2 startPos;
        public int2 endPos;
        public NativeArray<int2> resultPath;
        public NativeArray<CellData> grid;
        public int gridHeight;
        private NativeList<Node> openNodes;
        private NativeList<Node> closedNodes;

        public void Execute()
        {
            openNodes.Add(GetNodeFromPos(startPos));

            NativeList<Node> neighbors = default;
            while (true)
            {
                var node = openNodes.First();
                openNodes.RemoveAt(0);
                closedNodes.Add(node);

                // End target reached
                if (node.position.x == endPos.x && node.position.y == endPos.y)
                    break;

                GetNeighbors(ref neighbors, node, closedNodes.Length - 1);
                foreach (var neighbor in neighbors)
                {
                    
                }
            }
            neighbors.Dispose();
            
            resultPath = new NativeArray<int2>(10, Allocator.TempJob);
        }

        private void GetNeighbors(ref NativeList<Node> neighbors, Node node, int indexNode)
        {
            neighbors.Clear();
            var foundCell = grid[PosToIndex(node.position)];
            for (var i = 0; i < Cell.GetNumWalls(foundCell.walls); i++)
            {
                
            }
        }

        private Node GetNodeFromPos(int2 position, int parentIndex = 0)
        {
            if (parentIndex == 0)
            {
                var parentCell = grid[PosToIndex(position)];
                // TODO no clue
            }
            
            return new Node
            {
                position = position,
                parentIndex = parentIndex,
                gScore = startPos.Distance(position),
                hScore = endPos.Distance(position),
            };
        }

        private int PosToIndex(int2 pos) => pos.x * gridHeight + pos.y;
    }
}

public static class Int2Extensions
{
    public static float Distance(this int2 a, int2 b)
    {
        int dx = a.x - b.x;
        int dy = a.y - b.y;
        return math.sqrt(dx * dx + dy * dy);
    }
}
