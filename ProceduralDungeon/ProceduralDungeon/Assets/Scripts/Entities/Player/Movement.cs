using DefaultNamespace;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Movement : MonoBehaviour, IEntityMovement
    {
        [Header("Required Components")]
        [SerializeField] 
        private string actionNameMovement = "Move";
        [SerializeField]
        private Rigidbody2D rb = null!;
        
        [Header("Events"), Space]
        public UnityEvent<bool> isMovingChanged = null!;
        public UnityEvent<bool> IsMovingChanged => this.isMovingChanged;

        [Header("Config")]
        [SerializeField]
        private float speed = 5f;
        [SerializeField]
        private float velocityLimit = 10f;
        [SerializeField]
        private float turningStrength = 5f;
        [SerializeField]
        private float applyTurnMin = 0.1f;
        [SerializeField]
        private float drag = 5f;
        [SerializeField]
        private float applyDragMin;
        
        private InputActionAsset inputActionAsset = null!;
        private InputAction moveAction = null!;
        private Vector2 inputDirection;

        private bool isWalking;

        private void OnValidate()
        {
            this.rb ??= GetComponent<Rigidbody2D>();
            this.enabled = this.rb;
        }

        public void Connect(InputActionAsset inputAsset)
        {
            this.inputActionAsset = inputAsset;
            this.moveAction = this.inputActionAsset.FindAction(this.actionNameMovement);
            this.moveAction.performed += OnMoveActionPerformed;
            this.moveAction.canceled += OnMoveActionStopped;
            this.moveAction.Enable();
        }
        
        public void Disconnect()
        {
            this.moveAction.performed -= OnMoveActionPerformed;
            this.moveAction.canceled -= OnMoveActionStopped;
        }

        private void OnMoveActionPerformed(InputAction.CallbackContext ctx) => UpdateMovementData(ctx);
        private void OnMoveActionStopped(InputAction.CallbackContext ctx) => UpdateMovementData(ctx);

        private void UpdateMovementData(InputAction.CallbackContext ctx)
        {
            this.inputDirection = ctx.ReadValue<Vector2>();
            this.isWalking = this.inputDirection != Vector2.zero;
            this.isMovingChanged.Invoke(this.isWalking);
        }

        private void Update()
        {
            Vector2 currentDirection = this.rb.linearVelocity.normalized;
            if (this.isWalking)
            {
                float changingDirStrength = (-Vector2.Dot(this.inputDirection, currentDirection) + 1) * this.turningStrength;
                if (changingDirStrength <= this.applyTurnMin)
                    changingDirStrength = 1;

                this.rb.AddForce(this.inputDirection * (this.speed * Time.deltaTime * changingDirStrength), ForceMode2D.Impulse);
            }
            else
            {
                if (this.rb.linearVelocity.magnitude >= this.applyDragMin)
                    this.rb.AddForce(-currentDirection * (this.drag * Time.deltaTime), ForceMode2D.Impulse);
                else
                    this.rb.linearVelocity = Vector3.zero;
            }

            // Limit velocity
            this.rb.linearVelocity = new Vector2(Mathf.Clamp(this.rb.linearVelocity.x, -this.velocityLimit, this.velocityLimit),
                Mathf.Clamp(this.rb.linearVelocity.y, -this.velocityLimit, this.velocityLimit));
        }
    }
}
