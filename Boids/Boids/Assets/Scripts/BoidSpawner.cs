using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(SpatialHash))]
public class BoidSpawner : MonoBehaviour, IForSpatialHash
{
    [SerializeField] private Transform boidHolder;
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private int boidAmount = 100;
    [SerializeField] private SpatialHash spatialHash;
    
    [SerializeField] private float minSize = 0.1f;
    [SerializeField] private float maxSize = 2f;
    private NativeArray<Boid> boidsNative;
    private GameObject[] boidInstances;
    private Renderer[] boidRenderers;

    private struct Boid
    {
        public float3 velocity;
    }

    private void OnValidate()
    {
        if (!spatialHash)
            spatialHash = GetComponent<SpatialHash>();
        
        if (!spatialHash || !boidPrefab || !boidHolder)
            enabled = false;
        else
            enabled = true;
    }

    private void Start() => spatialHash.dependantComponents.Add(this);

    public void PrepareBoidContainersAndInitBoids()
    {
        // clean
        if (boidsNative.IsCreated) boidsNative.Dispose();
        if (boidInstances != null)
            foreach (var instance in boidInstances) Destroy(instance);
        
        // create
        boidsNative = new NativeArray<Boid>(boidAmount, Allocator.Persistent);
        boidInstances = new GameObject[boidAmount];
        boidRenderers = new Renderer[boidAmount];
        
        // init
        spatialHash.InitInstances(boidAmount, CreateBoidInstance);
    }

    private SpatialHash.InstanceInSpatial CreateBoidInstance(int index)
    {
        // defining initial data
        Vector3 position = spatialHash.GetRandomInBounds();
        var size = UnityEngine.Random.Range(minSize, maxSize);
        
        Vector3 velocity = new(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        );
        velocity = velocity.normalized;

        // Creating gameObject
        var instance = Instantiate(boidPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * size * 2f;
        instance.transform.SetParent(boidHolder);
        // saving
        boidInstances[index] = instance;
        boidRenderers[index] = instance.GetComponent<Renderer>();
        // saving data instance
        boidsNative[index] = new Boid
        {
            velocity = velocity
        };
        
        return new SpatialHash.InstanceInSpatial
        {
            position = position,
            boundingRadius = size
        };
    }

    private void OnDestroy()
    {
        if (boidsNative.IsCreated) boidsNative.Dispose();
    }

    public void PrepareJobs()
    {
        var boundsMax = spatialHash.GetBoundsMax();
        var boundsMin = spatialHash.GetBoundsMin();
        var onUpdateJob = new UpdateBoidJob
        {
            instances = spatialHash.instances,
            boids = boidsNative,
            boundsMax = boundsMax,
            boundsMin = boundsMin,
            boundsSize = boundsMax - boundsMin,
            dT = Time.deltaTime
        };
        
        spatialHash.AddOnUpdateJob(dep => onUpdateJob.Schedule(boidsNative.Length, 64, dep));
    }

    public void UpdateInstance(int index)
    {
        boidInstances[index].transform.position = spatialHash.instances[index].position;
        boidInstances[index].transform.forward = boidsNative[index].velocity;
    }

    [BurstCompile]
    private struct UpdateBoidJob : IJobParallelFor
    {
        public NativeArray<SpatialHash.InstanceInSpatial> instances;
        public NativeArray<Boid> boids;
        public float3 boundsMin;
        public float3 boundsMax;
        public float3 boundsSize;
        public float dT; // DeltaTime
        
        public void Execute(int index)
        {
            var instance = instances[index];
            var boid = boids[index];

            instance.position += boid.velocity * dT;
            
            if (instance.position.x - instance.boundingRadius > boundsMax.x)
                instance.position.x -= boundsSize.x;
            else if (instance.position.x + instance.boundingRadius < boundsMin.x)
                instance.position.x += boundsSize.x;

            if (instance.position.y - instance.boundingRadius > boundsMax.y)
                instance.position.y -= boundsSize.y;
            else if (instance.position.y + instance.boundingRadius < boundsMin.y)
                instance.position.y += boundsSize.y;

            if (instance.position.z - instance.boundingRadius > boundsMax.z)
                instance.position.z -= boundsSize.z;
            else if (instance.position.z + instance.boundingRadius < boundsMin.z)
                instance.position.z += boundsSize.z;

            instances[index] = instance;
            boids[index] = boid;
        }
    }
}