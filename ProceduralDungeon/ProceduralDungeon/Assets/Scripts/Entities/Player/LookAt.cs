using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
            lookDirectionManager ??= GetComponent<LookDirectionManager>();
            enabled = lookDirectionManager;
        }

        public void Connect(InputActionAsset inputAsset, Camera newCamera)
        {
            inputActionAsset = inputAsset;
            playerCamera = newCamera;
            lookAction = inputActionAsset.FindAction(actionNameLook);
            lookAction.performed += OnLookActionPerformed;
            lookAction.Enable();
        }

        public void Disconnect() => lookAction.performed -= OnLookActionPerformed;
        private void OnLookActionPerformed(InputAction.CallbackContext ctx)
        {
            if (ctx.control.device is Mouse)
            {
                var mouseScreen = Mouse.current.position.ReadValue();
                var mouseWorld = playerCamera.ScreenToWorldPoint(mouseScreen);
                lookDirectionManager.SetLookAt(mouseWorld);
                gunRotation?.UpdateRotation(mouseWorld);
                inputDirection = lookDirectionManager.LookDirection;
            }
            else
            {
                lookDirectionManager.SetLookAt(inputDirection.normalized + transform.position.xy());
                gunRotation?.UpdateRotation(inputDirection.normalized + gunRotation.transform.position.xy());
                inputDirection = ctx.ReadValue<Vector2>();
            }
        }
    }
}
