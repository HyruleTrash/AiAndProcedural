using System;
using UnityEngine;

namespace Guard
{
    public class WeaponHandler : MonoBehaviour, IDamager
    {
        [SerializeReference]
        private Weapon weaponRef;
        public void SetWeapon(Weapon toSet) => weaponRef = toSet;

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            weaponRef = null;
        }

        public float GetDamage()
        {
            if (weaponRef) return weaponRef.damage;
            return 0;
        }
        
        public bool HasWeapon() => weaponRef;
    }
}