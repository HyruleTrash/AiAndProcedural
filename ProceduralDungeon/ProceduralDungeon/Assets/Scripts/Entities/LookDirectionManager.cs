using UnityEngine;
using UnityEngine.Events;
using Util;

public class LookDirectionManager : MonoBehaviour
{
    public Vector2 LookDirection { get; private set; }
    private Vector2 toLookAt;
    public UnityEvent<Vector2> onDirectionChanged = null!;

    public void SetLookAt(Vector2 lookAt)
    {
        this.toLookAt = lookAt;
        Vector2 newLookDirection = (this.toLookAt - this.transform.position.xy()).normalized;
        if (Vector2.Dot(newLookDirection, this.LookDirection) > 0.999f) return;
        this.LookDirection = newLookDirection;
        this.onDirectionChanged.Invoke(newLookDirection);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(this.transform.position, this.transform.position + (Vector3)this.LookDirection);
    }
}