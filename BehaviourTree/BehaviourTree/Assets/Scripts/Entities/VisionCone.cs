using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VisionCone : MonoBehaviour
{
    [SerializeField]
    private Quaternion offsetRotation;
    [SerializeField]
    private LayerMask targetLayerMask;
    [SerializeField]
    private LayerMask obstacleLayerMask;
    [SerializeField]
    private float viewDistance;
    public float ViewDistance { get => viewDistance; private set => viewDistance = value; }
    [SerializeField, Range(0, 360)]
    private float fieldOfView;
    public float FieldOfView { get => fieldOfView; private set => fieldOfView = value; }

    public Vector3 DirFromAngle(float angleInDegrees, bool isGlobal)
    {
        if (!isGlobal) angleInDegrees -= transform.eulerAngles.z + offsetRotation.eulerAngles.z;
        return new Vector3(MathF.Sin(angleInDegrees * Mathf.Deg2Rad), MathF.Cos(angleInDegrees * Mathf.Deg2Rad), 0);
    }
    
    public GameObject[] GetCurrentlySeenObjects()
    {
        var hits = Physics2D.CircleCastAll(transform.position, viewDistance, Vector2.up, 0, targetLayerMask);
        if (hits.Length == 0)
            return Array.Empty<GameObject>();
        
        var threshold = -MathF.Cos(fieldOfView * Mathf.Deg2Rad);
        var visibleHits = hits.Where(raycastHit =>
        {
            var target = raycastHit.collider.transform;
            var dirToTarget = (target.position - transform.position).normalized;

            if (!(Vector3.Dot(transform.forward, dirToTarget) < threshold)) return false;
            
            var distToTarget = Vector2.Distance(transform.position, target.transform.position);
            return Physics2D.Raycast(transform.position, dirToTarget, distToTarget, obstacleLayerMask);
        }).ToArray();
        
        var objects = visibleHits.Select(raycastHit => raycastHit.collider?.gameObject).ToArray();
        return objects;
    }

    private void OnDrawGizmosSelected()
    {
        var seen = GetCurrentlySeenObjects();
        var color = Color.red;
        color.a = 0.5f;
        Gizmos.color = color;
        Debug.Log(seen.Length);
        foreach (var obj in seen)
        {
            if (!obj) continue;
            Gizmos.DrawSphere(obj.transform.position, 0.5f);
        }
    }
}