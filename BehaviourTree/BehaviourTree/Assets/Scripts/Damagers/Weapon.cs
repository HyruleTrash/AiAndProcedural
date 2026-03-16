using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAsset", menuName = "Weapons/BaseWeapon")]
public class Weapon : ScriptableObject
{
    [SerializeField]
    private GameObject prefab;
    public GameObject Prefab { 
        get => prefab;
        private set => prefab = value;
    }
    public float attackRange = 1f;
    public float damage = 1f;

    public bool IsInRange(Vector2 origin, GameObject other) => Vector2.Distance(origin, other.transform.position) <= attackRange;
}