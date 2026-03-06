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
        private InputActionAsset inputActionAsset;
        private InputAction lookAction;
        private Vector2 inputDirection;

        private void OnValidate()
        {
            lookDirectionManager ??= GetComponent<LookDirectionManager>();
            enabled = lookDirectionManager;
        }

        public void Connect(InputActionAsset inputAsset)
        {
            inputActionAsset = inputAsset;
            lookAction = inputActionAsset.FindAction(actionNameLook);
            lookAction.performed += OnLookActionPerformed;
        }

        public void Disconnect() => lookAction.performed -= OnLookActionPerformed;
        private void OnLookActionPerformed(InputAction.CallbackContext ctx)
        {
            inputDirection = ctx.ReadValue<Vector2>();
            lookDirectionManager.SetLookAt(inputDirection.normalized + transform.position.xy());
        }
    }
}
