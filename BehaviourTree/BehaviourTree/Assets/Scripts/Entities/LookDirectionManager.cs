using UnityEngine;
using UnityEngine.Events;

public class LookDirectionManager : MonoBehaviour
{
    private Vector2 toLookAt;
    private Vector2 lookDirection;
    public UnityEvent<Vector2> onDirectionChanged;

    public void SetLookAt(Vector2 lookAt)
    {
        toLookAt = lookAt;
        var newLookDirection = (toLookAt - transform.position.xy()).normalized;
        if (newLookDirection != lookDirection)
        {
            lookDirection = newLookDirection;
            onDirectionChanged.Invoke(lookDirection);
        }
    }
}