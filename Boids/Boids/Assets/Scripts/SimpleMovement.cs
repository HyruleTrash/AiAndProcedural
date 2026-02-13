using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleMovement : MonoBehaviour
{
    [SerializeField]
    private float speed = 2f;
    [SerializeField]
    private float sprintingSpeed = 2f;
    [SerializeField]
    private InputActionAsset inputMap;
    private InputAction move;
    private InputAction crouch;
    private InputAction jump;
    private InputAction sprint;
    
    private void Start()
    {
        move = inputMap.FindAction("Move");
        crouch = inputMap.FindAction("Crouch");
        jump = inputMap.FindAction("Jump");
        sprint = inputMap.FindAction("Sprint");
    }

    private void Update()
    {
        var usedSpeed = (sprint.IsPressed() ? sprintingSpeed : speed) * Time.deltaTime;
        var input = move.ReadValue<Vector2>();
        
        var forward = transform.forward;
        var right = transform.right;
        
        var horizontalMove = (forward * input.y + right * input.x) * usedSpeed;
        
        var vertical = jump.ReadValue<float>() + -crouch.ReadValue<float>();
        var verticalMove = Vector3.up * (vertical * usedSpeed);
        
        transform.position += horizontalMove + verticalMove;
    }
}