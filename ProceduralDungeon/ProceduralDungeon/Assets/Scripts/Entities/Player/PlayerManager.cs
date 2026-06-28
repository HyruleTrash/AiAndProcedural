using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(Movement), typeof(HealthComponent), typeof(LookAt))]
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField] private HealthComponent healthComponent = null!;
        [SerializeField] private Movement movementComponent = null!;
        [SerializeField] private WalkAnimManager walkAnimManager = null!;
        [SerializeField] private LookAt lookAtComponent = null!;
        [SerializeField] private InputActionAsset inputActionAsset = null!;
        [SerializeField] private Camera playerCamera = null!;

        private void OnValidate()
        {
            this.healthComponent ??= GetComponent<HealthComponent>();
            this.movementComponent ??= GetComponent<Movement>();
            this.lookAtComponent ??= GetComponent<LookAt>();
            this.enabled = this.healthComponent && this.lookAtComponent&& this.movementComponent && this.playerCamera && this.inputActionAsset != null;
            this.walkAnimManager ??= GetComponent<WalkAnimManager>();
        }

        private void Start()
        {
            this.movementComponent.Connect(this.inputActionAsset);
            this.lookAtComponent.Connect(this.inputActionAsset, this.playerCamera);
            this.walkAnimManager?.Connect(this.movementComponent);
        }

        private void OnDestroy()
        {
            this.movementComponent.Disconnect();
            this.lookAtComponent.Disconnect();
            this.walkAnimManager?.Disconnect();
        }
    }
}
