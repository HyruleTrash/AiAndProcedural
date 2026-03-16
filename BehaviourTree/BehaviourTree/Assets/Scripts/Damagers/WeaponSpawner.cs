using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WeaponSpawner : MonoBehaviour
{
    [Serializable]
    public class WeaponInstance
    {
        public GameObject instance;
        public Weapon weapon;
    }
    
    [SerializeField]
    public Tilemap tilemap;
    [SerializeField]
    public List<Weapon> spawnableWeapons;
    [SerializeField]
    public List<WeaponInstance> liveWeapons;
    [SerializeField]
    private Bounds bounds;
    [SerializeField]
    private int spawnAmount;
    [SerializeField]
    private int cellSize;

    private void OnValidate() => enabled = tilemap && spawnAmount > 0 && cellSize > 0 && bounds.extents.magnitude > 0;
    private void Start() => Spawn();

    public void Spawn()
    {
        for (var attempt = 0; attempt < spawnAmount; attempt++)
        {
            var weapon = spawnableWeapons[Random.Range(0, spawnableWeapons.Count)];
            while (true)
            {
                var spawnPosition = bounds.RandomPoint(transform);
                if (AlignPositionToTilemap(ref spawnPosition))
                    continue;
                
                liveWeapons.Add(new WeaponInstance
                {
                    weapon = weapon,
                    instance = Instantiate(weapon.Prefab, spawnPosition, Quaternion.identity)
                });
                break;
            }
        }
    }

    public WeaponInstance GetNearest(Vector2 posToCheck)
    {
        CleanWeaponInstances();
        
        var closestInstance = liveWeapons.First();
        var distance = Vector3.Distance(posToCheck, closestInstance.instance.transform.position);
        foreach (var weaponInstance in liveWeapons)
        {
            var newDist = Vector2.Distance(weaponInstance.instance.transform.position.xy(), posToCheck);
            if (!(newDist < distance)) continue;
            distance = newDist;
            closestInstance = weaponInstance;
        }
        return closestInstance;
    }

    private void CleanWeaponInstances()
    {
        for (var i = liveWeapons.Count - 1; i >= 0; i--)
        {
            var x = liveWeapons[i];
            if (!x.instance)
                liveWeapons.RemoveAt(i);
        }
    }

    /// <summary>
    /// Aligns position to tilemap
    /// </summary>
    /// <returns>false if position invalid</returns>
    public bool AlignPositionToTilemap(ref Vector3 worldPosition)
    {
        var cellPosition = tilemap.WorldToCell(worldPosition);

        if (!tilemap.HasTile(cellPosition))
            return false;

        worldPosition = tilemap.GetCellCenterWorld(cellPosition);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center + (transform.rotation * transform.position), bounds.size);
    }
}