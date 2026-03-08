using UnityEngine;

namespace Guard
{
    [RequireComponent(typeof(HealthComponent), typeof(WeaponHandler), typeof(NavigateToPosition))]
    public class GuardManager : MonoBehaviour
    {
        [SerializeField]
        private HealthComponent healthComponent;
        [SerializeField]
        private WeaponHandler weaponHandler;
        private IDamager damager;
        [SerializeField] 
        private NavigateToPosition navigateToPosition;

        private void OnValidate()
        {
            healthComponent ??= GetComponent<HealthComponent>();
            weaponHandler ??= GetComponent<WeaponHandler>();
            navigateToPosition ??= GetComponent<NavigateToPosition>();
            enabled = healthComponent && weaponHandler && navigateToPosition;
        }
    }
}
