using UnityEngine;

public static class Vector2Extensions
{
    public static Vector2 xy(this Vector3 v) => (Vector2)v;
    public static Vector2 xz(this Vector3 v) => new(v.x, v.z);
    public static Vector2 yx(this Vector3 v) => new(v.y, v.x);
    public static Vector2 yz(this Vector3 v) => new(v.y, v.z);
    public static Vector2 zx(this Vector3 v) => new(v.z, v.x);
    public static Vector2 zy(this Vector3 v) => new(v.z, v.y);
}