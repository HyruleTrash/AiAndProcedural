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

    private struct HashAndIndex : IComparable<HashAndIndex>
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
    
    private NativeArray<Particle> particlesNative;
    private NativeArray<HashAndIndex> hashAndIndices;
    private NativeList<int> resultIndices;
    
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
        queryRadiusSlider.onValueChanged.AddListener(val =>
        {
            queryRadius = val;
            queryRadiusText.text = val.ToString("0.0");;
        });
        
        InitializeParticles();
        CreateQuerySphereVisual();
    }

    private void CreateQuerySphereVisual()
    {
        if (!querySphereVisual)
        {
            querySphereVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            querySphereVisual.transform.SetParent(querySphere);
            querySphereVisual.GetComponent<Renderer>().material = querySphereMaterial;
            querySphereVisual.GetComponent<Collider>().enabled = false;
        }
        UpdateQuerySphereVisual();
    }

    private void UpdateQuerySphereVisual()
    {
        if (!querySphereVisual) return;
        querySphereVisual.transform.localScale = Vector3.one * (queryRadius * 2);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (querySphere) Gizmos.DrawWireSphere(querySphere.position, queryRadius);

        if (showGrid)
            DrawGrid();
    }

    private void DrawGrid()
    {
        var gridXCount = Mathf.CeilToInt(particleBounds.size.x / cellSize);
        var gridYCount = Mathf.CeilToInt(particleBounds.size.y / cellSize);
        var gridZCount = Mathf.CeilToInt(particleBounds.size.z / cellSize);

        void DrawCell(Vector3 position)
        {
            var cellCenter = particleBounds.min + position * cellSize + Vector3.one * (cellSize / 2);
            Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize);
        }
        
        for (var x = 0; x < gridXCount; x++)
        for (var y = 0; y < gridYCount; y++)
        for (var z = 0; z < gridZCount; z++) DrawCell(new Vector3(x, y, z));
        
        // Gizmos.DrawWireCube(particleBounds.center + transform.position, particleBounds.size);
    }

    private GameObject[] particleInstances;
    private Renderer[] particleRenderers;

    private void InitializeParticles()
    {
        if (particlesNative.IsCreated) particlesNative.Dispose();
        if (hashAndIndices.IsCreated) hashAndIndices.Dispose();

        if (particleInstances != null)
            foreach (var instance in particleInstances) Destroy(instance);
        
        particleInstances = new GameObject[particleCount];
        particleRenderers = new Renderer[particleCount];
        
        particlesNative = new NativeArray<Particle>(particleCount, Allocator.Persistent);
        hashAndIndices = new NativeArray<HashAndIndex>(particleCount, Allocator.Persistent);

        for (var i = 0; i < particleCount; i++)
            InitializeParticle(i);
    }

    private void InitializeParticle(int i)
    {
        Vector3 position = new(
            UnityEngine.Random.Range(particleBounds.min.x, particleBounds.max.x),
            UnityEngine.Random.Range(particleBounds.min.y, particleBounds.max.y),
            UnityEngine.Random.Range(particleBounds.min.z, particleBounds.max.z)
        );
        var radius = UnityEngine.Random.Range(0.5f, maxRadius);
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

        UpdateQuerySphereVisual();

        var updateJob = new UpdateParticleJob
        {
            dT = Time.deltaTime,
            particles = particlesNative,
            boundsMax = particleBounds.max,
            boundsMin = particleBounds.min
        };
        var updateHandle =  updateJob.Schedule(particlesNative.Length, 64);

        var hashJob = new HashParticleJob
        {
            particles = particlesNative,
            hashAndIndices = hashAndIndices,
            cellSize = cellSize
        };
        var hashHandle = hashJob.Schedule(particlesNative.Length, 64, updateHandle);

        var sortingJob = new SortHashCodesJob { hashAndIndices = hashAndIndices };
        var sortHandle = sortingJob.Schedule(hashHandle);

        var queryJob = new QueryJob
        {
            particles = particlesNative,
            hashAndIndices = hashAndIndices,
            queryPosition = querySphere.position,
            queryRadius = queryRadius,
            cellSize = cellSize,
            resultIndices = new NativeList<int>(Allocator.TempJob)
        };
        var queryJobHandle = queryJob.Schedule(sortHandle);
        
        queryJobHandle.Complete();
        
        if (resultIndices.IsCreated) resultIndices.Dispose();
        resultIndices = queryJob.resultIndices;
        
        // Debug.Log(resultIndices.Length);

        foreach (var pr in particleRenderers) pr.material.color = Color.white;
        foreach (var i in resultIndices) particleRenderers[i].material.color = Color.red;
        for (var i = 0; i < particlesNative.Length; i++)
            particleInstances[i].transform.position = particlesNative[i].position;
    }

    private void OnDestroy()
    {
        if (particlesNative.IsCreated) particlesNative.Dispose();
        if (hashAndIndices.IsCreated) hashAndIndices.Dispose();
        if (resultIndices.IsCreated) resultIndices.Dispose();
    }
    
    private static int3 GridPosition(float3 position, float cellSize) => new(math.floor(position / cellSize));

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

    [BurstCompile]
    private struct HashParticleJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Particle> particles;
        public NativeArray<HashAndIndex> hashAndIndices;
        public float cellSize;
        
        public void Execute(int index)
        {
            var particle = particles[index];
            var hash = Hash(GridPosition(particle.position, cellSize));
            
            hashAndIndices[index] = new HashAndIndex() { hash = hash, index = index };
        }
    }

    [BurstCompile]
    private struct SortHashCodesJob : IJob
    {
        public NativeArray<HashAndIndex> hashAndIndices;
        
        public void Execute()
        {
            hashAndIndices.Sort();
        }
    }

    [BurstCompile]
    private struct QueryJob : IJob
    {
        [ReadOnly] public NativeArray<Particle> particles;
        [ReadOnly] public NativeArray<HashAndIndex> hashAndIndices;
        public float3 queryPosition;
        public float queryRadius;
        public float cellSize;
        public NativeList<int> resultIndices;
        
        public void Execute()
        {
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
                            var particle = particles[particleIndex];
                            var toParticle = particle.position - queryPosition;

                            if (math.lengthsq(toParticle) <= radiusSquared) {
                                resultIndices.Add(particleIndex);
                            }
                        }
                    }
                }
            }
        }

        private int BinarySearchFirst(NativeArray<HashAndIndex> array, int hash)
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
}
