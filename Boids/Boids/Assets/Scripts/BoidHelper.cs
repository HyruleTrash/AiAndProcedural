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
        public float minSpeed;
        public float maxSpeed;
        public float visionThreshold;
        public float avoidanceRadius;
    }
    
    [BurstCompile]
    public struct UpdatePositionJob : IJobParallelFor
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
            
            var speed = Vector3.Magnitude(boid.velocity);

            if (speed < 0.0001f)
                boid.velocity = new float3(0,0,1) * boid.minSpeed;
            else
            {
                var dir = boid.velocity / speed;
                speed = math.clamp(speed, boid.minSpeed, boid.maxSpeed);
                boid.velocity = dir * speed;
            }
            
            instance.position += boid.velocity * dT;
            
            // Rule Four: Teleport at bounds
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
    public struct UpdateVelocitiesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<InstanceInSpatial> instances;
        [ReadOnly] public NativeArray<HashAndIndex> hashAndIndices;
        [ReadOnly] public NativeArray<Boid> boidsRead;
        public NativeArray<Boid> boidsWrite;
        [ReadOnly] public float cellSize;
        [ReadOnly] public float separationWeight;
        [ReadOnly] public float cohesionWeight;
        [ReadOnly] public float alignmentWeight;
        [ReadOnly] public float turnRate;
        [ReadOnly] public float dT; // DeltaTime
        
        public void Execute(int index)
        {
            var boid = boidsRead[index];
            
            QueryJob.TryQuery(instances[index].position, boid.queryRadius, cellSize, instances, hashAndIndices, out var neighborIndices);
            
            var steer = GetRuleBasedVelocity(index, ref neighborIndices, ref instances, ref boidsRead, boid, instances[index], separationWeight, cohesionWeight, alignmentWeight);
            var desiredVelocity = Vector3.ClampMagnitude(steer, boid.maxSpeed);
            boid.velocity = math.lerp(boid.velocity, desiredVelocity, turnRate * dT);
            
            boidsWrite[index] = boid;
            neighborIndices.Dispose();
        }
    }
    
    private static float3 GetRuleBasedVelocity(int index, ref NativeList<int> neighborIndices,
        ref NativeArray<InstanceInSpatial> instances,
        ref NativeArray<Boid> boids, Boid boid, InstanceInSpatial instance, float separationWeight,
        float cohesionWeight, float alignmentWeight)
    {
        if (instances.Length == 0)
            return float3.zero;
        
        var centerOfFlock = float3.zero;
        var separationVel = float3.zero;
        var velocityFlock = float3.zero;
        var count = 0;
        
        foreach (var neighbor in neighborIndices)
        {
            if (neighbor == index) continue;
            var neighborInstance = instances[neighbor];
            count++;
            
            // Rule One: Boids try to fly towards the center of mass of neighboring boids.
            centerOfFlock += neighborInstance.position;
            
            // Rule Three: Boids try to match velocity with near boids.
            velocityFlock += boids[neighbor].velocity;
            
            // Rule Two: Boids try to keep a small distance away from other objects (including other boids).
            #region visionCone
            var toNeighbor = math.normalizesafe(neighborInstance.position - instance.position);
            var forward = math.normalizesafe(boid.velocity);
            var dot = Vector3.Dot(forward, toNeighbor);
            if (!(dot < boid.visionThreshold)) continue;
            #endregion
            
            var diff = instance.position - neighborInstance.position;
            var dist = Vector3.Magnitude(diff);

            if (!(dist > 0f) || !(dist < boid.avoidanceRadius)) continue;
            var away = diff / dist;
            var strength = (boid.avoidanceRadius - dist) / boid.avoidanceRadius;
            separationVel += away * strength;
        }
        if (count <= 0)
            return separationVel * separationWeight;

        var center = centerOfFlock / count;
        var desired = math.normalizesafe(center - instance.position) * boid.maxSpeed;
        var cohesionVel = desired - boid.velocity;
            
        var avgHeading = math.normalizesafe(velocityFlock / count) * boid.maxSpeed;
        var alignmentVel = avgHeading - boid.velocity;

        return cohesionVel * cohesionWeight +
               alignmentVel * alignmentWeight +
               separationVel * separationWeight;
    }
}