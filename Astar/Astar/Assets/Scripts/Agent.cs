using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
public class Agent : MonoBehaviour
{
    public int moveButton = 0;
    public float moveSpeed = 3;
    private Astar Astar = new();
    private List<int2> path = new();
    private readonly Plane ground = new(Vector3.up, 0f);
    private MeshRenderer agentRenderer;
    private GameObject targetVisual;
    private MazeGeneration maze;
    private LineRenderer line;
    
    private void Awake()
    {
        maze = FindFirstObjectByType<MazeGeneration>();
        agentRenderer = GetComponentInChildren<MeshRenderer>();
        targetVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetVisual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        targetVisual.GetComponent<MeshRenderer>().material.color = agentRenderer.material.color;
        line = GetComponent<LineRenderer>();
        line.material.color = agentRenderer.material.color;
        line.material.color = agentRenderer.material.color;
    }

    private void FindPathToTarget(int2 startPos, int2 endPos, Cell[,] grid)
    {
        path = Astar.FindPathToTarget(startPos, endPos, grid);
        DrawPath();
    }

    private void DrawPath()
    {
        if (path != null && path.Count > 0)
        {
            line.positionCount = path.Count;
            for (var i = 0; i < path.Count; i++)
            {
                line.SetPosition(i, Vector2IntToVector3(path[i], 0.1f));
            }
        }
    }

    //Move to clicked position
    public void Update()
    {
        if (Input.GetMouseButtonDown(moveButton))
        {
            Debug.Log("Click");
            var r = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -10));

            var mousePos = MouseToWorld();
            var targetPos = Vector3ToVector2Int(mousePos);
            targetVisual.transform.position = Vector2IntToVector3(targetPos);
            FindPathToTarget(Vector3ToVector2Int(transform.position), targetPos, maze.grid);
        }

        if (path != null && path.Count > 0)
        {
            if (transform.position != Vector2IntToVector3(path[0]))
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Vector2IntToVector3(path[0]) - transform.position), 360f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, Vector2IntToVector3(path[0]), moveSpeed * Time.deltaTime);
            }
            else
            {
                path.RemoveAt(0);
                DrawPath();
            }
        }

    }

    private Vector3 MouseToWorld()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        ground.Raycast(ray, out var distToGround);
        var worldPos = ray.GetPoint(distToGround);

        return worldPos;
    }

    private int2 Vector3ToVector2Int(Vector3 pos) => new(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));
    private Vector3 Vector2IntToVector3(int2 pos, float YPos = 0) => new(Mathf.RoundToInt(pos.x), YPos, Mathf.RoundToInt(pos.y));

    private void OnDrawGizmos()
    {
        if (path != null && path.Count > 0)
        {
            for (var i = 0; i < path.Count - 1; i++)
            {
                Gizmos.color = agentRenderer.material.color;
                Gizmos.DrawLine(Vector2IntToVector3(path[i], 0.5f), Vector2IntToVector3(path[i + 1], 0.5f));
            }

        }
    }
}
