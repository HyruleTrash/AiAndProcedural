using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static BoidHelper;
using static SpatialHash;

[RequireComponent(typeof(SpatialHash))]
public class BoidSpawner : MonoBehaviour, IForSpatialHash
{
    [Header("Required Components")]
    [SerializeField] private Transform boidHolder;
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private SpatialHash spatialHash;
    [Header("Boid data")]
    [SerializeField] private int spawnAmount = 100;
    [SerializeField] private Vector2 size = new(0.1f, 1.2f);
    [SerializeField] private Vector2 maxSpeedDefaults = new(1f, 2f);
    [SerializeField] private Vector2 minSpeedDefaults = new(1f, 2f);
    [SerializeField] private Vector2 visionThreshold = new(0f, -0.8f);
    [SerializeField] private Vector2 neighborCheckRadius = new(5f, 10f);
    [SerializeField] private Vector2 avoidanceRadius = new(2f, 5f);
    [SerializeField] private float turningSpeed;
    [Header("Temp Flock data")]
    [SerializeField] private float separationWeight = 1f;
    [SerializeField] private float cohesionWeight = 0.5f;
    [SerializeField] private float alignmentWeight = 0.125f;
    private NativeArray<Boid> boidsNativeRead;
    private NativeArray<Boid> boidsNativeWrite;
    private GameObject[] boidInstances;
    private Renderer[] boidRenderers;

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
        if (boidsNativeRead.IsCreated) boidsNativeRead.Dispose();
        if (boidsNativeWrite.IsCreated) boidsNativeRead.Dispose();
        if (boidInstances != null)
            foreach (var instance in boidInstances) Destroy(instance);
        
        // create
        boidsNativeRead = new NativeArray<Boid>(spawnAmount, Allocator.Persistent);
        boidsNativeWrite = new NativeArray<Boid>(spawnAmount, Allocator.Persistent);
        boidInstances = new GameObject[spawnAmount];
        boidRenderers = new Renderer[spawnAmount];
        
        // init
        spatialHash.InitInstances(spawnAmount, CreateBoidInstance);
    }

    private InstanceInSpatial CreateBoidInstance(int index)
    {
        // defining initial data
        Vector3 position = spatialHash.GetRandomInBounds();
        var foundSize = UnityEngine.Random.Range(size.x, size.y);
        
        Vector3 velocity = new(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        );
        velocity = velocity.normalized;

        // Creating gameObject
        var instance = Instantiate(boidPrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * foundSize * 2f;
        instance.transform.SetParent(boidHolder);
        // saving
        boidInstances[index] = instance;
        boidRenderers[index] = instance.GetComponent<Renderer>();
        // saving data instance
        var maxSpeed = UnityEngine.Random.Range(maxSpeedDefaults.x, maxSpeedDefaults.y);
        var minSpeed = UnityEngine.Random.Range(minSpeedDefaults.x, minSpeedDefaults.y < maxSpeed ? maxSpeed : minSpeedDefaults.y);
        Boid newBoid = new Boid
        {
            velocity = velocity,
            minSpeed = minSpeed,
            maxSpeed = maxSpeed,
            visionThreshold = UnityEngine.Random.Range(visionThreshold.x, visionThreshold.y),
            queryRadius = UnityEngine.Random.Range(neighborCheckRadius.x, neighborCheckRadius.y),
            avoidanceRadius = UnityEngine.Random.Range(avoidanceRadius.x, avoidanceRadius.y),
        };
        boidsNativeRead[index] = newBoid;
        
        return new InstanceInSpatial
        {
            position = position,
            boundingRadius = foundSize
        };
    }

    private void OnDestroy()
    {
        if (boidsNativeRead.IsCreated) boidsNativeRead.Dispose();
        if (boidsNativeWrite.IsCreated) boidsNativeWrite.Dispose();
        spatialHash.dependantComponents.Remove(this);
    }

    public void PrepareJobs()
    {
        var boundsMax = spatialHash.GetBoundsMax();
        var boundsMin = spatialHash.GetBoundsMin();
        
        var onUpdateVelocityJob = new UpdateVelocitiesJob
        {
            instances = spatialHash.instances,
            hashAndIndices = spatialHash.hashAndIndices,
            boidsRead = boidsNativeRead,
            boidsWrite = boidsNativeWrite,
            cellSize = spatialHash.cellSize,
            separationWeight = separationWeight,
            cohesionWeight = cohesionWeight,
            alignmentWeight = alignmentWeight,
            turnRate = turningSpeed,
            dT = Time.deltaTime
        };
        spatialHash.AddBeforeHashJob(dep => onUpdateVelocityJob.Schedule(boidsNativeRead.Length, 64, dep));

        var onSwitchBuffersJob = new SwitchBuffersJob
        {
            boidsNativeRead = boidsNativeRead,
            boidsNativeWrite = boidsNativeWrite,
        };
        spatialHash.AddBeforeHashJob(dep => onSwitchBuffersJob.Schedule(dep));
        
        var onUpdateJob = new UpdatePositionJob
        {
            instances = spatialHash.instances,
            boids = boidsNativeWrite,
            boundsMax = boundsMax,
            boundsMin = boundsMin,
            boundsSize = boundsMax - boundsMin,
            dT = Time.deltaTime
        };
        spatialHash.AddBeforeHashJob(dep => onUpdateJob.Schedule(boidsNativeRead.Length, 64, dep));
    }

    public void UpdateInstance(int index)
    {
        var diff = (float3)boidInstances[index].transform.position - spatialHash.instances[index].position;
        var lookDir = -Vector3.Normalize(diff);
        if (lookDir != Vector3.zero)
            boidInstances[index].transform.forward = Vector3.Lerp(boidInstances[index].transform.forward, lookDir, Time.deltaTime * turningSpeed);
        boidInstances[index].transform.position = spatialHash.instances[index].position;
    }

    private struct SwitchBuffersJob : IJob
    {
        public NativeArray<Boid> boidsNativeRead;
        public NativeArray<Boid> boidsNativeWrite;
        
        public void Execute() => (boidsNativeRead, boidsNativeWrite) = (boidsNativeWrite, boidsNativeRead);
    }
}