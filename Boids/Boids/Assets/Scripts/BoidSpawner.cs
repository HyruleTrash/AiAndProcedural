using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(SpatialHash))]
public class BoidSpawner : MonoBehaviour, IForSpatialHash
{
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private int boidAmount = 100;
    [SerializeField] private SpatialHash spatialHash;
    
    [SerializeField] private float maxSize = 2;
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
        
        if (!spatialHash || !boidPrefab)
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
        Vector3 position = new(
            UnityEngine.Random.Range(spatialHash.spatialBounds.min.x, spatialHash.spatialBounds.max.x),
            UnityEngine.Random.Range(spatialHash.spatialBounds.min.y, spatialHash.spatialBounds.max.y),
            UnityEngine.Random.Range(spatialHash.spatialBounds.min.z, spatialHash.spatialBounds.max.z)
        );
        var size = UnityEngine.Random.Range(0.5f, maxSize);
        Vector3 velocity = new(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        );
        velocity = velocity.normalized;

        // Creating gameObject
        var instance = Instantiate(boidPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * size * 2f;
        instance.transform.SetParent(transform);
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
        var onUpdateJob = new UpdateBoidJob
        {
            instances = spatialHash.instances,
            boids = boidsNative,
            boundsMax = spatialHash.spatialBounds.max,
            boundsMin = spatialHash.spatialBounds.min,
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
        public float dT; // DeltaTime
        
        public void Execute(int index)
        {
            var instance = instances[index];
            var boid = boids[index];

            instance.position += boid.velocity * dT;
            
            if (instance.position.x - instance.boundingRadius < boundsMin.x && boid.velocity.x < 0 ||
                instance.position.x + instance.boundingRadius > boundsMax.x && boid.velocity.x > 0) 
                instance.position.x = -instance.position.x;
            if (instance.position.y - instance.boundingRadius < boundsMin.y && boid.velocity.y < 0 ||
                instance.position.y + instance.boundingRadius > boundsMax.y && boid.velocity.y > 0) 
                instance.position.y = -instance.position.y;
            if (instance.position.z - instance.boundingRadius < boundsMin.z && boid.velocity.z < 0 ||
                instance.position.z + instance.boundingRadius > boundsMax.z && boid.velocity.z > 0) 
                instance.position.z = -instance.position.z;

            instances[index] = instance;
            boids[index] = boid;
        }
    }
}