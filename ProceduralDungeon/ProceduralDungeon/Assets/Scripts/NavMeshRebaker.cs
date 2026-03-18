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
        navMesh ??= GetComponent<NavMeshSurface>();
        enabled = navMesh;
    }

    private void FixedUpdate()
    {
        navMesh.BuildNavMesh();
    }
}
