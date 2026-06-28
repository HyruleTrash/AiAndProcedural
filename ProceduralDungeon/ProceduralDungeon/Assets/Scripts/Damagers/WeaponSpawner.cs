using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Util;
using Random = UnityEngine.Random;

public class WeaponSpawner : MonoBehaviour
{
    [Serializable]
    public class WeaponInstance
    {
        public GameObject instance = null!;
        public Weapon weapon = null!;
    }
    
    [SerializeField]
    public Tilemap tilemap = null!;
    [SerializeField]
    public List<Weapon> spawnableWeapons = new();
    [SerializeField]
    public List<WeaponInstance> liveWeapons = new();
    [SerializeField]
    private Bounds bounds;
    [SerializeField]
    private int spawnAmount;

    private void OnValidate() => this.enabled = this.tilemap && this.spawnAmount > 0 && this.bounds.extents.magnitude > 0;
    private void Start() => Spawn();

    public void Spawn()
    {
        for (int attempt = 0; attempt < this.spawnAmount; attempt++)
        {
            Weapon weapon = this.spawnableWeapons[Random.Range(0, this.spawnableWeapons.Count)];
            while (true)
            {
                Vector3 spawnPosition = this.bounds.RandomPoint(this.transform);
                Tuple<bool, Vector3> posCheck = AlignPositionToTilemap(spawnPosition);
                if (!posCheck.Item1)
                    continue;

                this.liveWeapons.Add(new WeaponInstance
                {
                    weapon = weapon,
                    instance = Instantiate(weapon.Prefab, posCheck.Item2, Quaternion.identity)
                });
                break;
            }
        }
    }

    public WeaponInstance GetNearest(Vector2 posToCheck)
    {
        CleanWeaponInstances();
        
        WeaponInstance closestInstance = this.liveWeapons.First();
        float distance = Vector3.Distance(posToCheck, closestInstance.instance.transform.position);
        foreach (WeaponInstance weaponInstance in this.liveWeapons)
        {
            float newDist = Vector2.Distance(weaponInstance.instance.transform.position.xy(), posToCheck);
            if (!(newDist < distance)) continue;
            distance = newDist;
            closestInstance = weaponInstance;
        }
        return closestInstance;
    }

    private void CleanWeaponInstances()
    {
        for (int i = this.liveWeapons.Count - 1; i >= 0; i--)
        {
            WeaponInstance x = this.liveWeapons[i];
            if (!x.instance) this.liveWeapons.RemoveAt(i);
        }
    }

    /// <summary>
    /// Aligns position to tilemap
    /// </summary>
    /// <returns>false if position invalid, and a position aligned to the grid</returns>
    public Tuple<bool, Vector3> AlignPositionToTilemap(Vector3 worldPosition)
    {
        Vector3Int cellPosition = this.tilemap.WorldToCell(worldPosition);

        if (this.tilemap.HasTile(cellPosition))
            return new Tuple<bool, Vector3>(false, Vector3.zero);

        Vector3 newPos = this.tilemap.CellToWorld(cellPosition) + (Vector3)(new Vector2(0.5f, 0.5f));
        return new Tuple<bool, Vector3>(true, newPos);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(this.bounds.center + (this.transform.rotation * this.transform.position), this.bounds.size);
    }
}