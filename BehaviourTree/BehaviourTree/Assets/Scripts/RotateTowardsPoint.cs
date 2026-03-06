using UnityEngine;

public class RotateTowardsPoint : MonoBehaviour
{
    public void UpdateRotation(Vector2 pointToRotateTowards)
    {
        var dir = (pointToRotateTowards - transform.position.xy()).normalized;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        var eulerAngles = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, angle);
    }
}
