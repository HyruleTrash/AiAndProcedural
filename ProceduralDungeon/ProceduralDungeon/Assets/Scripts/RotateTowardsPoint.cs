using UnityEngine;

public class RotateTowardsPoint : MonoBehaviour
{
    public void UpdateRotation(Vector2 pointToRotateTowards)
    {
        var delta = pointToRotateTowards - transform.position.xy();
        if (delta.sqrMagnitude < 0.0001f)
            return;

        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        var euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(euler.x, euler.y, angle);
    }
}
