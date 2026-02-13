using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
    [SerializeField] private Vector2 size = new Vector2(0.1f, 1.2f);
    [SerializeField] private Vector2 speed = new Vector2(1f, 2f);
    [SerializeField] private Vector2 neighborRadius = new Vector2(5f, 10f);
    private NativeArray<Boid> boidsNative;
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
        if (boidsNative.IsCreated) boidsNative.Dispose();
        if (boidInstances != null)
            foreach (var instance in boidInstances) Destroy(instance);
        
        // create
        boidsNative = new NativeArray<Boid>(spawnAmount, Allocator.Persistent);
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
        boidsNative[index] = new Boid
        {
            velocity = velocity,
            speed = UnityEngine.Random.Range(speed.x, speed.y),
            queryRadius = UnityEngine.Random.Range(neighborRadius.x, neighborRadius.y)
        };
        
        return new InstanceInSpatial
        {
            position = position,
            boundingRadius = foundSize
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
        var onUpdateJob = new UpdateJob
        {
            instances = spatialHash.instances,
            boids = boidsNative,
            boundsMax = boundsMax,
            boundsMin = boundsMin,
            boundsSize = boundsMax - boundsMin,
            dT = Time.deltaTime
        };
        spatialHash.AddOnUpdateJob(dep => onUpdateJob.Schedule(boidsNative.Length, 64, dep));

        var onQueryJob = new UpdateNeighborsJob
        {
            instances = spatialHash.instances,
            hashAndIndices = spatialHash.hashAndIndices,
            boids = boidsNative,
            cellSize = spatialHash.cellSize,
        };
        spatialHash.AddOnQueryJob(dep => onQueryJob.Schedule(boidsNative.Length, 64, dep));
    }

    public void UpdateInstance(int index)
    {
        boidInstances[index].transform.position = spatialHash.instances[index].position;
        boidInstances[index].transform.forward = boidsNative[index].velocity;
    }
}