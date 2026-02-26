using UnityEngine;

public class Node
{
    public readonly Vector2Int position;
    public readonly Node parent;

    public float FScore => gScore + hScore;
    public readonly float gScore;
    private readonly float hScore;

    public Node(Vector2Int position, float gScore, float hScore, Node parent = null)
    {
        this.position = position;
        this.parent = parent;
        this.gScore = gScore;
        this.hScore = hScore;
    }
}