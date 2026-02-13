using System;
using System.Collections.Generic;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class SpatialHash : MonoBehaviour
{
    #region Settings and Preferences
    [SerializeField]
    private Bounds spatialBounds = new(Vector3.zero, new Vector3(20, 20, 20));
    public float cellSize = 2f;
    [SerializeField] 
    private bool showGrid = true;
    #endregion
    
    public NativeArray<InstanceInSpatial> instances;
    public NativeArray<HashAndIndex> hashAndIndices;

    public readonly List<IForSpatialHash> dependantComponents = new ();
    private List<Func<JobHandle, JobHandle>> onUpdateSchedulers = new();
    private List<Func<JobHandle, JobHandle>> onQuerySchedulers = new();
    
    public struct InstanceInSpatial
    {
        public float3 position;
        public float boundingRadius;
    }

    #region Hashing

    public struct HashAndIndex : IComparable<HashAndIndex>
    {
        public int hash;
        public int index;
        
        public int CompareTo(HashAndIndex other)
        {
            return hash.CompareTo(other.hash);
        }
    }

    public static int Hash(int3 gridPos)
    {
        unchecked {
            return gridPos.x * 73856093 ^ gridPos.y * 19349663 ^ gridPos.z * 83492791;
        }
    }
    #endregion

    #region GizmoDraw
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (showGrid)
            DrawGrid();
    }

    private void DrawGrid()
    {
        var gridXCount = Mathf.CeilToInt(spatialBounds.size.x / cellSize);
        var gridYCount = Mathf.CeilToInt(spatialBounds.size.y / cellSize);
        var gridZCount = Mathf.CeilToInt(spatialBounds.size.z / cellSize);

        void DrawCell(Vector3 position)
        {
            var cellCenter = (Vector3)GetBoundsMin() + position * cellSize + Vector3.one * (cellSize / 2);
            Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize);
        }
        
        for (var x = 0; x < gridXCount; x++)
        for (var y = 0; y < gridYCount; y++)
        for (var z = 0; z < gridZCount; z++) DrawCell(new Vector3(x, y, z));
    }
    #endregion

    public void InitInstances(int instanceAmount, Func<int, InstanceInSpatial> createInstance)
    {
        if (instances.IsCreated) instances.Dispose();
        if (hashAndIndices.IsCreated) hashAndIndices.Dispose();
        
        instances = new NativeArray<InstanceInSpatial>(instanceAmount, Allocator.Persistent);
        hashAndIndices = new NativeArray<HashAndIndex>(instanceAmount, Allocator.Persistent);

        for (var i = 0; i < instanceAmount; i++)
        {
            instances[i] = createInstance.Invoke(i);
        }
    }

    private void Update()
    {
        if (!instances.IsCreated) return;
        
        dependantComponents.ForEach(comp => comp.PrepareJobs());

        JobHandle onUpdateDependancy = default;
        foreach (var scheduler in onUpdateSchedulers)
            onUpdateDependancy = scheduler(onUpdateDependancy);
        onUpdateSchedulers.Clear();

        var hashJob = new HashInstanceJob
        {
            instances = instances,
            hashAndIndices = hashAndIndices,
            cellSize = cellSize
        };
        var hashHandle = hashJob.Schedule(instances.Length, 64, onUpdateDependancy);

        var sortingJob = new SortHashCodesJob { hashAndIndices = hashAndIndices };
        var sortHandle = sortingJob.Schedule(hashHandle);
        
        var onQueryDependancy = sortHandle;
        foreach (var scheduler in onQuerySchedulers)
            onQueryDependancy = scheduler(onQueryDependancy);
        onQuerySchedulers.Clear();
        
        onQueryDependancy.Complete();
        
        for (var i = 0; i < instances.Length; i++)
            dependantComponents.ForEach(comp => comp.UpdateInstance(i));
    }

    public void AddOnUpdateJob(Func<JobHandle, JobHandle> scheduler) => onUpdateSchedulers.Add(scheduler);
    public void AddOnQueryJob(Func<JobHandle, JobHandle> scheduler) => onQuerySchedulers.Add(scheduler);

    private void OnDestroy()
    {
        if (instances.IsCreated) instances.Dispose();
        if (hashAndIndices.IsCreated) hashAndIndices.Dispose();
    }
    
    private static int3 GridPosition(float3 position, float cellSize) => new(math.floor(position / cellSize));
    
    [BurstCompile]
    private struct HashInstanceJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<InstanceInSpatial> instances;
        public NativeArray<HashAndIndex> hashAndIndices;
        public float cellSize;
        
        public void Execute(int index)
        {
            var particle = instances[index];
            var hash = Hash(GridPosition(particle.position, cellSize));
            
            hashAndIndices[index] = new HashAndIndex() { hash = hash, index = index };
        }
    }

    [BurstCompile]
    private struct SortHashCodesJob : IJob
    {
        public NativeArray<HashAndIndex> hashAndIndices;
        public void Execute() => hashAndIndices.Sort();
    }

    [BurstCompile]
    public struct QueryJob : IJob
    {
        [ReadOnly] public NativeArray<InstanceInSpatial> instances;
        [ReadOnly] public NativeArray<HashAndIndex> hashAndIndices;
        public float3 queryPosition;
        public float queryRadius;
        public float cellSize;
        public NativeList<int> resultIndices;
        
        public void Execute() => TryQuery(queryPosition, queryRadius, cellSize, instances, hashAndIndices, out resultIndices);

        public static void TryQuery(float3 queryPosition, float queryRadius, float cellSize, NativeArray<InstanceInSpatial> instances, NativeArray<HashAndIndex> hashAndIndices, out NativeList<int> resultIndices)
        {
            resultIndices = new NativeList<int>(Allocator.TempJob);
            var radiusSquared = queryRadius * queryRadius;
            var minGridPos = GridPosition(queryPosition - queryRadius, cellSize);
            var maxGridPos = GridPosition(queryPosition + queryRadius, cellSize);

            for (var x = minGridPos.x; x <= maxGridPos.x; x++) {
                for (var y = minGridPos.y; y <= maxGridPos.y; y++) {
                    for (var z = minGridPos.z; z <= maxGridPos.z; z++) {
                        var gridPos = new int3(x, y, z);
                        var hash = Hash(gridPos);

                        var startIndex = BinarySearchFirst(hashAndIndices, hash);
                        
                        if (startIndex < 0) continue;

                        for (var i = startIndex; i < hashAndIndices.Length && hashAndIndices[i].hash == hash; i++) {
                            var particleIndex = hashAndIndices[i].index;
                            var particle = instances[particleIndex];
                            var toParticle = particle.position - queryPosition;

                            if (math.lengthsq(toParticle) <= radiusSquared) {
                                resultIndices.Add(particleIndex);
                            }
                        }
                    }
                }
            }
        }
        
        private static int BinarySearchFirst(NativeArray<HashAndIndex> array, int hash)
        {
            var left = 0;
            var right = array.Length - 1;
            var result = -1;

            while (left <= right) {
                var mid = (left + right) / 2;
                var midHash = array[mid].hash;

                if (midHash == hash)
                    result = mid;
                else if (midHash < hash)
                {
                    left = mid + 1;
                    continue;
                }
                right = mid - 1;
            }
            return result;
        }
    }

    public float3 GetBoundsMax() => spatialBounds.max + transform.position;
    public float3 GetBoundsMin() => spatialBounds.min + transform.position;

    public float3 GetRandomInBounds() =>
        new(
            UnityEngine.Random.Range(spatialBounds.min.x, spatialBounds.max.x),
            UnityEngine.Random.Range(spatialBounds.min.y, spatialBounds.max.y),
            UnityEngine.Random.Range(spatialBounds.min.z, spatialBounds.max.z)
        );
}
