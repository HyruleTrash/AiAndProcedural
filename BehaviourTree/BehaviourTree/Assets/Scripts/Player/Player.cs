using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement), typeof(HealthComponent))]
public class Player : MonoBehaviour
{
    [SerializeField]
    private HealthComponent healthComponent;
    [SerializeField]
    private PlayerMovement movementComponent;
    [SerializeField]
    private InputActionAsset inputActionAsset;
    [SerializeField]
    private Camera playerCamera;

    private void OnValidate()
    {
        healthComponent ??= GetComponent<HealthComponent>();
        movementComponent ??= GetComponent<PlayerMovement>();
        enabled = healthComponent && movementComponent && playerCamera && inputActionAsset != null;
    }

    private void Start() => movementComponent.Connect(inputActionAsset);

    private void OnDestroy() => movementComponent.Disconnect();
}
