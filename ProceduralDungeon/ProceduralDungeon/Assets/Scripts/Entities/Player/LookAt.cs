using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;

namespace Player
{
    [RequireComponent(typeof(LookDirectionManager))]
    public class LookAt : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] 
        private string actionNameLook = "Look";
        [SerializeField]
        private LookDirectionManager lookDirectionManager;
        [SerializeField]
        private RotateTowardsPoint gunRotation;
        private Camera playerCamera;
        
        private InputActionAsset inputActionAsset;
        private InputAction lookAction;
        private Vector2 inputDirection;

        private void OnValidate()
        {
            this.lookDirectionManager ??= GetComponent<LookDirectionManager>();
            this.enabled = this.lookDirectionManager;
        }

        public void Connect(InputActionAsset inputAsset, Camera newCamera)
        {
            this.inputActionAsset = inputAsset;
            this.playerCamera = newCamera;
            this.lookAction = this.inputActionAsset.FindAction(this.actionNameLook);
            this.lookAction.performed += OnLookActionPerformed;
            this.lookAction.Enable();
        }

        public void Disconnect() => this.lookAction.performed -= OnLookActionPerformed;
        private void OnLookActionPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Mouse)
            {
                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                Vector3 mouseWorld = this.playerCamera.ScreenToWorldPoint(mouseScreen);
                this.lookDirectionManager.SetLookAt(mouseWorld);
                this.gunRotation?.UpdateRotation(mouseWorld);
                this.inputDirection = this.lookDirectionManager.LookDirection;
            }
            else
            {
                this.lookDirectionManager.SetLookAt(this.inputDirection.normalized + this.transform.position.xy());
                this.gunRotation?.UpdateRotation(this.inputDirection.normalized + this.gunRotation.transform.position.xy());
                this.inputDirection = ctx.ReadValue<Vector2>();
            }
        }
    }
}
