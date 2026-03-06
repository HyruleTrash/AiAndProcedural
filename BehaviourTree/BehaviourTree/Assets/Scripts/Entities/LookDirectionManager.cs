using System;
using UnityEngine;
using UnityEngine.Events;

public class LookDirectionManager : MonoBehaviour
{
    public Vector2 LookDirection { get; private set; }
    private Vector2 toLookAt;
    public UnityEvent<Vector2> onDirectionChanged;

    public void SetLookAt(Vector2 lookAt)
    {
        toLookAt = lookAt;
        var newLookDirection = (toLookAt - transform.position.xy()).normalized;
        if (Vector2.Dot(newLookDirection, LookDirection) > 0.999f) return;
        LookDirection = newLookDirection;
        onDirectionChanged.Invoke(newLookDirection);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.position + (Vector3)LookDirection);
    }
}