using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flips the transform x scale based on where the look direction is
/// </summary>
public class ScaleBasedOnLookDirection : MonoBehaviour
{
    [Serializable]
    public class ToFlip
    {
        public Transform transform;
        public bool flipX;
        public bool flipY;
    }
    
    [SerializeField]
    private List<ToFlip> childrenToFlip;
    public bool flipped = false;
    public Action directionChanged;

    private void OnValidate() => enabled = childrenToFlip is { Count: > 0 };

    public void SetScaleBasedOnLookDirection(Vector2 lookDirection)
    {
        var flip = Mathf.Sign(childrenToFlip[0].transform.localScale.x) != Mathf.Sign(lookDirection.x);
        if (!flip) return;
        foreach (var child in childrenToFlip)
        {
            var scale = child.transform.localScale;
            if (child.flipX) scale.x *= -1;
            if (child.flipY) scale.y *= -1;
            
            child.transform.localScale = scale;
        }

        flipped = childrenToFlip[0].transform.localScale.x < 0;
        directionChanged?.Invoke();
    }
}