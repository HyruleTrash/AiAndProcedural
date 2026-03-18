using UnityEngine;

public static class Extensions
{
    public static Vector2 xy(this Vector3 v) => (Vector2)v;
    // public static Vector2 xz(this Vector3 v) => new(v.x, v.z); in-case needed later
    // public static Vector2 yx(this Vector3 v) => new(v.y, v.x);
    // public static Vector2 yz(this Vector3 v) => new(v.y, v.z);
    // public static Vector2 zx(this Vector3 v) => new(v.z, v.x);
    // public static Vector2 zy(this Vector3 v) => new(v.z, v.y);
    
    public static Vector3 RandomPoint(this Bounds bounds, Transform transform)
    {
        // Random pos in bounds
        var local = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        // Apply scale
        var scaled = Vector3.Scale(local, transform.lossyScale);
        return transform.position + transform.rotation * scaled;
    }
}