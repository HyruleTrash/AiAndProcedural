using System.Collections.Generic;
using UnityEngine;

public class MazeGeneration : MonoBehaviour
{
    public int width = 10, height = 10;
    public Cell[,] grid;
    public float scaleFactor = 1;
    public CellPrefab cellPrefab;
    public float desiredWallpercentage = 0.4f;
    private List<GameObject> allCellObjects = new List<GameObject>();
    public int seed = 1234;

    // Start is called before the first frame update
    private void Awake()
    {
        Random.InitState(seed);
        GenerateMaze();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            seed = Random.Range(0, int.MaxValue);
            Random.InitState(seed);
            width = Random.Range(10, 100);
            height = Random.Range(10, 100);
            desiredWallpercentage = Random.Range(0.2f, 1.0f);
            DestroyMazeObjects();
            GenerateMaze();
        }
    }

    private void DestroyMazeObjects()
    {
        allCellObjects.Clear();
        foreach (Transform t in transform)
        {
            Destroy(t.gameObject);
        }
    }

    public void GenerateMaze()
    {
        grid = new Cell[width, height];
        grid.Initialize();
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                grid[x, y] = new Cell();
                grid[x, y].gridPosition = new Vector2Int(x, y);
                grid[x, y].walls = Wall.DOWN | Wall.LEFT | Wall.RIGHT | Wall.UP;
            }
        }

        var cellStack = new Stack<Cell>();
        var visitedCells = new List<Cell>();
        cellStack.Push(grid[0, 0]);
        Cell currentCell;
        while (cellStack.Count > 0)
        {
            currentCell = cellStack.Pop();
            var neighbours = GetUnvisitedNeighbours(currentCell, visitedCells, cellStack);
            if (neighbours.Count > 1)
            {
                cellStack.Push(currentCell);
            }

            if (neighbours.Count != 0)
            {
                var randomUnvisitedNeighbour = neighbours[Random.Range(0, neighbours.Count)];
                RemoveWallBetweenCells(currentCell, randomUnvisitedNeighbour);
                visitedCells.Add(randomUnvisitedNeighbour);
                cellStack.Push(randomUnvisitedNeighbour);
            }
        }

        //Remove a couple random walls to make the maze more 'open'
        var totalWallsInMaze = GetWallCount(grid);
        var totalPossibleWallsInmaze = 4 * width * height;
        var wallPercentage = totalWallsInMaze / (float)totalPossibleWallsInmaze;
        Debug.Log("Wall Percentage: " + wallPercentage);
        while (wallPercentage > desiredWallpercentage)
        {
            var randomX = Random.Range(0, width);
            var randomY = Random.Range(0, height);
            var randomCell = grid[randomX, randomY];
            var neighbours = randomCell.GetNeighbours(grid);
            if (neighbours.Count > 0)
            {
                var randomNeighbour = neighbours[Random.Range(0, neighbours.Count)];
                var wallsRemoved = RemoveWallBetweenCells(randomCell, randomNeighbour);
                if (wallsRemoved)
                {
                    totalWallsInMaze -= 2;
                    wallPercentage = totalWallsInMaze / (float)totalPossibleWallsInmaze;
                }
            }
        }

        //Generate Objects
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var cellObject = Instantiate(cellPrefab, new Vector3(x * scaleFactor, 0, y * scaleFactor), Quaternion.identity, transform);
                cellObject.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                cellObject.SpawnWalls(grid[x, y]);
                allCellObjects.Add(cellObject.gameObject);
            }
        }
    }
    private int GetWallCount(Cell[,] grid)
    {
        var walls = 0;
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var cell = grid[x, y];
                walls += Cell.GetNumWalls(cell.walls);
            }
        }
        return walls;
    }



    /// <summary>
    /// Gets the unvisited neighbours for a cell
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="visitedCells"></param>
    /// <param name="cellstack"></param>
    /// <returns></returns>
    private List<Cell> GetUnvisitedNeighbours(Cell cell, List<Cell> visitedCells, Stack<Cell> cellstack)
    {
        var result = new List<Cell>();
        for (var x = -1; x < 2; x++)
        {
            for (var y = -1; y < 2; y++)
            {
                var cellX = cell.gridPosition.x + x;
                var cellY = cell.gridPosition.y + y;
                if (cellX < 0 || cellX >= width || cellY < 0 || cellY >= height || Mathf.Abs(x) == Mathf.Abs(y))
                {
                    continue;
                }
                var canditateCell = grid[cellX, cellY];
                if (!visitedCells.Contains(canditateCell) && !cellstack.Contains(canditateCell))
                {
                    result.Add(canditateCell);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// This function assumes the two inputcells are next to each other
    /// </summary>
    /// <param name="cellOne"></param>
    /// <param name="cellTwo"></param>
    private bool RemoveWallBetweenCells(Cell cellOne, Cell cellTwo)
    {
        var numWallCellOne = Cell.GetNumWalls(cellOne.walls);
        var dirVector = cellTwo.gridPosition - cellOne.gridPosition;
        if (dirVector.x != 0)
        {
            cellOne.RemoveWall(dirVector.x > 0 ? Wall.RIGHT : Wall.LEFT);
            cellTwo.RemoveWall(dirVector.x > 0 ? Wall.LEFT : Wall.RIGHT);
        }
        if (dirVector.y != 0)
        {
            cellOne.RemoveWall(dirVector.y > 0 ? Wall.UP : Wall.DOWN);
            cellTwo.RemoveWall(dirVector.y > 0 ? Wall.DOWN : Wall.UP);
        }

        //Is a wall succesfully removed?
        if (numWallCellOne != Cell.GetNumWalls(cellOne.walls)) { return true; }
        return false;
    }

    public Cell GetCellForWorldPosition(Vector3 worldPos)
    {
        return grid[(int)(Mathf.RoundToInt(worldPos.x) / scaleFactor), (int)(Mathf.RoundToInt(worldPos.z) / scaleFactor)];
    }
}

