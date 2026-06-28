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
    public float ViewDistance { get => this.viewDistance; private set => this.viewDistance = value; }
    [SerializeField, Range(0, 360)]
    private float fieldOfView;
    public float FieldOfView { get => this.fieldOfView; private set => this.fieldOfView = value; }
    [SerializeField]
    private float minDistance = 0.1f;

    public Vector3 DirFromAngle(float angleInDegrees, bool isGlobal)
    {
        if (!isGlobal) angleInDegrees -= this.transform.eulerAngles.z + this.offsetRotation.eulerAngles.z;
        return new Vector3(MathF.Sin(angleInDegrees * Mathf.Deg2Rad), MathF.Cos(angleInDegrees * Mathf.Deg2Rad), 0);
    }

    public GameObject[] GetCurrentlySeenObjects()
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(this.transform.position, this.viewDistance, Vector2.up, 0, this.targetLayerMask);
        return hits.Length == 0 ? Array.Empty<GameObject>() : AreTransformsInCone(hits.Select(hit => hit.collider.gameObject).ToArray());
    }

    public GameObject[] AreTransformsInCone(GameObject[] gameObjects)
    {
        GameObject[] visibleHits = gameObjects.Where(obj =>
        {
            if (!obj.activeInHierarchy) return false;
            
            Transform target = obj.transform;
            Vector3 dirToTarget = (target.position - this.transform.position).normalized;

            float angle = Vector3.Angle(this.transform.right, dirToTarget);
            if (!(angle < this.fieldOfView / 2)) return false;
            
            float distToTarget = Vector2.Distance(this.transform.position, target.transform.position);
            if (distToTarget < this.minDistance) return true;
            
            RaycastHit2D result = Physics2D.Raycast(this.transform.position, dirToTarget, distToTarget + 1, this.obstacleLayerMask + this.targetLayerMask);
            if (!result) return false;
            
            if ((this.obstacleLayerMask.value & (1 << result.collider.gameObject.layer)) != 0) return false;
            if ((this.targetLayerMask.value & (1 << result.collider.gameObject.layer)) != 0) return true;

            return false;
        }).ToArray();
        
        return visibleHits;
    }

    private void OnDrawGizmosSelected()
    {
        GameObject[] seen = GetCurrentlySeenObjects();
        
        Color color = Color.red;
        Gizmos.color = color;
        
        foreach (GameObject obj in seen)
        {
            if (!obj) continue;
            Gizmos.DrawSphere(obj.transform.position + Vector3.back, 0.5f);
        }
    }
}