using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshRebaker : MonoBehaviour
{
    [SerializeField] 
    private NavMeshSurface navMesh;

    private void OnValidate()
    {
        this.navMesh ??= GetComponent<NavMeshSurface>();
        this.enabled = this.navMesh;
    }

    private void FixedUpdate()
    {
        this.navMesh.BuildNavMesh();
    }
}
