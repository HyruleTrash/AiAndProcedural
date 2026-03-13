using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavigateToPosition : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent navMeshAgent;
    private Vector2? targetPosition;

    private void OnValidate()
    {
        navMeshAgent ??= GetComponent<NavMeshAgent>();
        enabled = navMeshAgent && navMeshAgent.enabled;
    }

    public void SetTargetPosition(Vector2? position)
    {
        targetPosition = position;
        if (targetPosition == null)
            return;
        
        navMeshAgent.enabled = true;
        try { navMeshAgent.SetDestination(targetPosition.Value); }
        catch (Exception _) {
            // ignored
        }
    }

    public Vector2? GetTargetPosition() => targetPosition;
}