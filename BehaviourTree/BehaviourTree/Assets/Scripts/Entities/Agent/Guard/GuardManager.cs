using System;
using UnityEngine;

namespace Guard
{
    [RequireComponent(typeof(HealthComponent), typeof(WeaponHandler), typeof(VisionCone))]
    public class GuardManager : MonoBehaviour
    {
        [SerializeField]
        private HealthComponent healthComponent;
        [SerializeField]
        private WeaponHandler weaponHandler;
        private IDamager damager;
        [SerializeField] 
        private NavigateToPosition navigateToPosition;
        [SerializeField]
        private VisionCone visionCone;

        private void OnValidate()
        {
            healthComponent ??= GetComponent<HealthComponent>();
            weaponHandler ??= GetComponent<WeaponHandler>();
            visionCone ??= GetComponent<VisionCone>();
            navigateToPosition ??= GetComponentInChildren<NavigateToPosition>();
            enabled = healthComponent && weaponHandler && navigateToPosition;
        }

        private void Update()
        {
            transform.position += navigateToPosition.transform.localPosition;
            navigateToPosition.transform.localPosition = Vector3.zero;
        }
    }
}
