using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] 
    private string actionNameMovement = "Move";
    [SerializeField]
    private Rigidbody2D rb;
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
    private float applyDragMin = 0f;
    
    private InputActionAsset inputActionAsset;
    private InputAction moveAction;
    private Vector2 inputDirection;

    private void OnValidate()
    {
        rb = GetComponent<Rigidbody2D>();
        enabled = rb;
    }

    public void Connect(InputActionAsset inputAsset)
    {
        inputActionAsset = inputAsset;
        moveAction = inputActionAsset.FindAction(actionNameMovement);
        moveAction.performed += OnMoveActionPerformed;
        moveAction.canceled += OnMoveActionStopped;
    }
    
    public void Disconnect()
    {
        moveAction.performed -= OnMoveActionPerformed;
        moveAction.canceled -= OnMoveActionStopped;
    }

    private void OnMoveActionPerformed(InputAction.CallbackContext ctx) => inputDirection = ctx.ReadValue<Vector2>();
    private void OnMoveActionStopped(InputAction.CallbackContext ctx) => inputDirection = ctx.ReadValue<Vector2>();

    private void Update()
    {
        var currentDirection = rb.linearVelocity.normalized;
        if (inputDirection != Vector2.zero)
        {
            var changingDirStrength = (-Vector2.Dot(inputDirection, currentDirection) + 1) * turningStrength;
            if (changingDirStrength <= applyTurnMin)
                changingDirStrength = 1;
            
            rb.AddForce(inputDirection * (speed * Time.deltaTime * changingDirStrength), ForceMode2D.Impulse);
        }
        else
        {
            if (rb.linearVelocity.magnitude >= applyDragMin)
                rb.AddForce(-currentDirection * (drag * Time.deltaTime), ForceMode2D.Impulse);
        }

        // Limit velocity
        rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -velocityLimit, velocityLimit),
            Mathf.Clamp(rb.linearVelocity.y, -velocityLimit, velocityLimit));
    }
}