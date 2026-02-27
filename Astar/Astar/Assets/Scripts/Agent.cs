using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Agent : MonoBehaviour
{
    public int moveButton = 0;
    public float moveSpeed = 3;
    private Astar Astar = new();
    private List<Vector2Int> path = new();
    private readonly Plane ground = new(Vector3.up, 0f);
    private MeshRenderer agentRenderer;
    private GameObject targetVisual;
    [NonSerialized]
    public MazeGeneration maze;
    private LineRenderer line;
    private Camera cam;
    
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
        cam = Camera.main;
    }

    public void CallAStar(Vector2Int startPos, Vector2Int endPos, Cell[,] grid) => path = Astar.FindPathToTarget(startPos, endPos, grid);

    private void FindPathToTarget(Vector2Int startPos, Vector2Int endPos, Cell[,] grid)
    {
        CallAStar(startPos, endPos, grid);
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
        if (moveButton == 0 && Mouse.current.leftButton.isPressed || moveButton == 1 && Mouse.current.rightButton.isPressed)
        {
            cam.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -10));

            var mousePos = MouseToWorld();
            var targetPos = Vector3ToVector2Int(mousePos);
            targetVisual.transform.position = Vector2IntToVector3(targetPos);
            FindPathToTarget(Vector3ToVector2Int(transform.position), targetPos, maze.grid);
        }

        if (!IsMoving()) return;
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

    public bool IsMoving() => !(path == null || path.Count <= 0);

    private Vector3 MouseToWorld()
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);

        ground.Raycast(ray, out var distToGround);
        var worldPos = ray.GetPoint(distToGround);

        return worldPos;
    }

    public Vector2Int Vector3ToVector2Int(Vector3 pos) => new(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));
    private Vector3 Vector2IntToVector3(Vector2Int pos, float YPos = 0) => new(Mathf.RoundToInt(pos.x), YPos, Mathf.RoundToInt(pos.y));

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
