using UnityEngine;

namespace Guard
{
    public class WeaponHandler : MonoBehaviour, IDamager
    {
        [SerializeReference]
        private Weapon? weaponRef;
        public Weapon? Weapon => this.weaponRef;

        public void SetWeapon(Weapon toSet) => this.weaponRef = toSet;

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            this.weaponRef = null;
        }

        public float GetDamage()
        {
            if (this.weaponRef) return this.weaponRef.damage;
            return 0;
        }
        
        public bool HasWeapon() => this.weaponRef;
    }
}