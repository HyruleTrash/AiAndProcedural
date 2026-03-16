using System;
using Guard;
using UnityEngine;

public class WeaponHandlerPickup : MonoBehaviour
{
    [SerializeField]
    private Weapon weaponRef;

    private void OnValidate() => enabled = weaponRef;
    private void OnTriggerEnter2D(Collider2D other)
    {
        var weaponHandler = other.gameObject.GetComponent<WeaponHandler>();
        if (!weaponHandler || weaponHandler.HasWeapon()) return;
        weaponHandler.SetWeapon(weaponRef);
        enabled = false;
        Destroy(gameObject);
    }
}