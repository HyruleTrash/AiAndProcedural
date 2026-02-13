using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [SerializeField] 
    private Camera cam;
    [SerializeField]
    private InputActionAsset inputMap;
    private InputAction mouseCaptureToggle;
    private bool mouseCaptured;
    
    private InputAction look;
    [SerializeField]
    private float mouseSensitivity = 0.15f;
    
    private float pitch;
    private float yaw;

    private void OnValidate()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (!cam) enabled = false;
    }
    
    private void OnEnable()
    {
        mouseCaptureToggle = inputMap.FindAction("MouseCaptureToggle");
        look = inputMap.FindAction("Look");
        mouseCaptureToggle.Enable();
        look.Enable();
        mouseCaptureToggle.performed += HandleMouseCapture;
    }

    private void OnDisable()
    {
        mouseCaptureToggle.performed -= HandleMouseCapture;
        mouseCaptureToggle.Disable();
        look.Disable();
    }

    private void Update()
    {
        if (!mouseCaptured) return;
        var mouseDelta = look.ReadValue<Vector2>();
        
        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMouseCapture(InputAction.CallbackContext _ = default)
    {
        mouseCaptured = !mouseCaptured;
        if (mouseCaptured)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}