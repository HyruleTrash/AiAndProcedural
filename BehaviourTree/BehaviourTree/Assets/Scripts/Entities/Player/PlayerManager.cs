using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(Movement), typeof(HealthComponent), typeof(LookAt))]
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField]
        private HealthComponent healthComponent;
        [SerializeField]
        private Movement movementComponent;
        [SerializeField]
        private WalkAnimManager walkAnimManager;
        [SerializeField]
        private LookAt lookAtComponent;
        [SerializeField]
        private InputActionAsset inputActionAsset;
        [SerializeField]
        private Camera playerCamera;

        private void OnValidate()
        {
            healthComponent ??= GetComponent<HealthComponent>();
            movementComponent ??= GetComponent<Movement>();
            lookAtComponent ??= GetComponent<LookAt>();
            enabled = healthComponent && lookAtComponent&& movementComponent && playerCamera && inputActionAsset != null;
            walkAnimManager ??= GetComponent<WalkAnimManager>();
        }

        private void Start()
        {
            movementComponent.Connect(inputActionAsset);
            lookAtComponent.Connect(inputActionAsset, playerCamera);
            walkAnimManager?.Connect(movementComponent);
        }

        private void OnDestroy()
        {
            movementComponent.Disconnect();
            lookAtComponent.Disconnect();
            walkAnimManager?.Disconnect();
        }
    }
}
