using Guard;
using UnityEngine;

public class WeaponHandlerPickup : MonoBehaviour
{
    [SerializeField] private Weapon weaponRef = null!;

    private void OnValidate() => this.enabled = this.weaponRef;
    private void OnTriggerEnter2D(Collider2D other)
    {
        WeaponHandler weaponHandler = other.gameObject.GetComponent<WeaponHandler>();
        if (!weaponHandler || weaponHandler.HasWeapon()) return;
        weaponHandler.SetWeapon(this.weaponRef);
        this.enabled = false;
        Destroy(this.gameObject);
    }
}