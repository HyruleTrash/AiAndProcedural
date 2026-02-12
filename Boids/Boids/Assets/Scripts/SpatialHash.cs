using System;
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
    [Header("Particles")]
    public GameObject particlePrefab;
    public int particleCount = 500;
    public Bounds particleBounds = new Bounds(Vector3.zero, new Vector3(20, 20, 20));
    public float maxRadius = 1.5f;
    public float cellSize = 2f;
    
    [Header("Query settings")]
    public Transform querySphere;
    public float queryRadius = 5f;
    
    [Header("UI controls")]
    public Slider particleCountSlider;
    public Slider cellSizeSlider;
    public Slider queryRadiusSlider;
    public TextMeshProUGUI queryRadiusText;
    public Toggle showGridToggle;
    private bool showGrid;

    [Space(10)]
    private GameObject querySphereVisual;
    public Material querySphereMaterial;
    #endregion

    private struct Particle
    {
        public float3 position;
        public float3 velocity;
        public float radius;
    }

    // private struct HashAndIndex : IComparable<HashAndIndex>
    // {
    //     public int CompareTo(HashAndIndex other)
    //     {
    //         
    //     }
    // }
    
    private NativeArray<Particle> particlesNative;
    
    private void Start()
    {
        particleCountSlider.value = particleCount;
        particleCountSlider.onValueChanged.AddListener(amount =>
        {
            particleCount = Mathf.RoundToInt(amount);
            InitializeParticles();
        });
        showGridToggle.onValueChanged.AddListener(val => showGrid = val);
        
        cellSizeSlider.value = cellSize;
        cellSizeSlider.onValueChanged.AddListener(val => cellSize = val);
        
        queryRadiusSlider.value = queryRadius;
        queryRadiusSlider.onValueChanged.AddListener(val => queryRadius = val);
        
        InitializeParticles();
    }

    private GameObject[] particleInstances;
    private Renderer[] particleRenderers;

    private void InitializeParticles()
    {
        if (particlesNative.IsCreated) particlesNative.Dispose();

        if (particleInstances != null)
            foreach (var instance in particleInstances) Destroy(instance);
        
        particleInstances = new GameObject[particleCount];
        particleRenderers = new Renderer[particleCount];
        
        particlesNative = new NativeArray<Particle>(particleCount, Allocator.Persistent);

        for (int i = 0; i < particleCount; i++)
            InitializeParticle(i);
    }

    private void InitializeParticle(int i)
    {
        Vector3 position = new(
            UnityEngine.Random.Range(particleBounds.min.x, particleBounds.max.x),
            UnityEngine.Random.Range(particleBounds.min.y, particleBounds.max.y),
            UnityEngine.Random.Range(particleBounds.min.z, particleBounds.max.z)
        );
        float radius = UnityEngine.Random.Range(0.5f, maxRadius);
        Vector3 velocity = new(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        );
        
        particlesNative[i] = new Particle()
        {
            position = position,
            velocity = velocity,
            radius = radius
        };
            
        var instance = Instantiate(particlePrefab, position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * radius * 2f;
        instance.transform.SetParent(transform);
        
        particleInstances[i] = instance;
        particleRenderers[i] = instance.GetComponent<Renderer>();
    }

    private void Update()
    {
        if (!particlesNative.IsCreated) return;

        var updateJob = new UpdateParticleJob()
        {
            dT = Time.deltaTime,
            particles = particlesNative,
            boundsMax = particleBounds.max,
            boundsMin = particleBounds.min
        };
        
        JobHandle updateHandle =  updateJob.Schedule(particlesNative.Length, 64);
        
        updateHandle.Complete();

        for (int i = 0; i < particlesNative.Length; i++)
            particleInstances[i].transform.position = particlesNative[i].position;
    }

    private void OnDestroy()
    {
        if (particlesNative.IsCreated) {
            particlesNative.Dispose();
        }
    }

    [BurstCompile]
    private struct UpdateParticleJob : IJobParallelFor
    {
        public NativeArray<Particle> particles;
        public float3 boundsMin;
        public float3 boundsMax;
        public float dT; // DeltaTime
        
        public void Execute(int index)
        {
            var particle = particles[index];

            particle.position += particle.velocity * dT;
            
            // Bounce of bounds temp TODO
            if (particle.position.x - particle.radius < boundsMin.x && particle.velocity.x < 0 ||
                particle.position.x + particle.radius > boundsMax.x && particle.velocity.x > 0) 
                particle.velocity.x = -particle.velocity.x;
            if (particle.position.y - particle.radius < boundsMin.y && particle.velocity.y < 0 ||
                particle.position.y + particle.radius > boundsMax.y && particle.velocity.y > 0) 
                particle.velocity.y = -particle.velocity.y;
            if (particle.position.z - particle.radius < boundsMin.z && particle.velocity.z < 0 ||
                particle.position.z + particle.radius > boundsMax.z && particle.velocity.z > 0) 
                particle.velocity.z = -particle.velocity.z;

            particles[index] = particle;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(particleBounds.center + transform.position, particleBounds.size);
    }
}
