using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshRebaker : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMesh = null!;

    private void OnValidate()
    {
        this.navMesh ??= GetComponent<NavMeshSurface>();
        this.enabled = this.navMesh;
    }

    private void FixedUpdate() => this.navMesh.BuildNavMesh();
}
