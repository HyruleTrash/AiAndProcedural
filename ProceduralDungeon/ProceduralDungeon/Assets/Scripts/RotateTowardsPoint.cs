using UnityEngine;
using Util;

public class RotateTowardsPoint : MonoBehaviour
{
    public void UpdateRotation(Vector2 pointToRotateTowards)
    {
        Vector2 delta = pointToRotateTowards - this.transform.position.xy();
        if (delta.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        Vector3 euler = this.transform.eulerAngles;
        this.transform.rotation = Quaternion.Euler(euler.x, euler.y, angle);
    }
}
