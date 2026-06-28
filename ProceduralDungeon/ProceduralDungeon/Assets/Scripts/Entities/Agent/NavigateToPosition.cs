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
        this.navMeshAgent ??= GetComponent<NavMeshAgent>();
        this.enabled = this.navMeshAgent && this.navMeshAgent.enabled;
    }

    public void SetTargetPosition(Vector2? position)
    {
        this.targetPosition = position;
        if (this.targetPosition == null)
            return;

        this.navMeshAgent.enabled = true;
        try {
            this.navMeshAgent.SetDestination(this.targetPosition.Value); }
        catch (Exception) {
            // ignored
        }
    }

    public Vector2? GetTargetPosition() => this.targetPosition;
}