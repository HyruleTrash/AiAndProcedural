using System.Text.RegularExpressions;
using UnityEngine;

namespace Util
{
    public static class Extensions
    {
        public static Vector2 XY(this Vector3 v) => (Vector2)v;
        // public static Vector2 xz(this Vector3 v) => new(v.x, v.z); in-case needed later
        // public static Vector2 yx(this Vector3 v) => new(v.y, v.x);
        // public static Vector2 yz(this Vector3 v) => new(v.y, v.z);
        // public static Vector2 zx(this Vector3 v) => new(v.z, v.x);
        // public static Vector2 zy(this Vector3 v) => new(v.z, v.y);
    
        public static Vector3 RandomPoint(this Bounds bounds, Transform transform)
        {
            // Random pos in bounds
            Vector3 local = new(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // Apply scale
            Vector3 scaled = Vector3.Scale(local, transform.lossyScale);
            return transform.position + transform.rotation * scaled;
        }
    
        public static readonly Vector2Int[] CardinalDirections = {
            Vector2Int.up,    // (0, 1)
            Vector2Int.down,  // (0, -1)
            Vector2Int.left,  // (-1, 0)
            Vector2Int.right  // (1, 0)
        };
        
        public static string ToReadableString(this string input) => string.IsNullOrEmpty(input) ? input : Regex.Replace(input, @"(\p{Ll})(\p{Lu})", "$1 $2");
    }
}