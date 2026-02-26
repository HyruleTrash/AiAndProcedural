using UnityEngine;

public static class Vector2IntExtensions
{
    public static int ManhattanDistance(this Vector2Int a, Vector2Int b)
    {
        var dx = Mathf.Abs(a.x - b.x);
        var dy = Mathf.Abs(a.y - b.y);
        return dx + dy;
    }
}