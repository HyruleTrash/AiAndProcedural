using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static SpatialHash;

public static class BoidHelper
{
    public struct Boid
    {
        public float queryRadius;
        public float3 velocity;
        public float speed;
    }
    
    [BurstCompile]
    public struct UpdateJob : IJobParallelFor
    {
        public NativeArray<InstanceInSpatial> instances;
        public NativeArray<Boid> boids;
        public float3 boundsMin;
        public float3 boundsMax;
        public float3 boundsSize;
        public float dT; // DeltaTime
        
        public void Execute(int index)
        {
            var instance = instances[index];
            var boid = boids[index];
            
            instance.position += boid.velocity * (dT * boid.speed);
            
            // Teleport at bounds
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
            
            // save
            instances[index] = instance;
            boids[index] = boid;
        }
    }
    
    [BurstCompile]
    public struct UpdateNeighborsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<InstanceInSpatial> instances;
        [ReadOnly] public NativeArray<HashAndIndex> hashAndIndices;
        public NativeArray<Boid> boids;
        public float cellSize;
        
        public void Execute(int index)
        {
            var boid = boids[index];
            
            QueryJob.TryQuery(instances[index].position, boid.queryRadius, cellSize, instances, hashAndIndices, out var neighborIndices);

            float3 result = RuleOne(index, neighborIndices, instances);
            boid.velocity += result;
            
            boids[index] = boid;
        }
    }
    
    /// <summary>
    /// Boids try to fly towards the center of mass of neighboring boids.
    /// </summary>
    private static float3 RuleOne(int index, NativeList<int> neighborIndices, NativeArray<InstanceInSpatial> instances)
    {
        if (instances.Length == 0)
            return float3.zero;
        
        var centerOfNeighbors = new float3(0,0,0);
        var count = 0;
        foreach (var neighbor in neighborIndices)
        {
            if (neighbor == index) continue;
            centerOfNeighbors += instances[neighbor].position;
            count++;
        }

        if (count <= 0)
            return float3.zero;
        centerOfNeighbors /= count;
        
        return (centerOfNeighbors - instances[index].position) / 100;
    }
}