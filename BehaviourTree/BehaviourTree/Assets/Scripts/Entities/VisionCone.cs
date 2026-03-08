using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VisionCone : MonoBehaviour
{
    [SerializeField]
    private LayerMask layerMask;
    [SerializeField]
    private float viewDistance;
    [SerializeField]
    private float fieldOfView;
    
    public GameObject[] GetCurrentlySeenObjects()
    {
        var hits = Array.Empty<RaycastHit>();
        Physics.SphereCastNonAlloc(transform.position, viewDistance, Vector3.zero, hits, viewDistance, layerMask);
        
        var objects = hits.Select(raycastHit => raycastHit.collider.gameObject).ToArray();

        return objects;
    }

    private void OnDrawGizmos()
    {
        var seen = GetCurrentlySeenObjects();
        Gizmos.color = Color.red;
        foreach (var obj in seen)
        {
            Gizmos.DrawSphere(obj.transform.position, 1f);
        }
    }
}