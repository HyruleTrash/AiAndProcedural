using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A debug camera movement script made by Gemini
/// </summary>
[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    private enum MouseButton { Left, Right, Middle }

    [Header("Zoom Settings")]
    [Tooltip("How fast the camera zooms in and out.")]
    [SerializeField] private float zoomSpeed = 5f;
    [Tooltip("The closest the camera can get.")]
    [SerializeField] private float minZoom = 2f;
    [Tooltip("The furthest the camera can pull back.")]
    [SerializeField] private float maxZoom = 30f;

    [Header("Mouse Button")]
    [Tooltip("Which mouse button triggers the drag/pan.")]
    [SerializeField] private MouseButton[] dragButtons = {MouseButton.Left, MouseButton.Middle, MouseButton.Right};

    private Camera cam = null!;
    private Vector3 dragOrigin;

    private void Start()
    {
        this.cam = GetComponent<Camera>();

        if (this.cam.orthographic) return;
        Debug.LogWarning("MapCameraController: Camera is not set to Orthographic. Switching to Orthographic mode.");
        this.cam.orthographic = true;
    }

    private void LateUpdate()
    {
        if (Mouse.current == null) return;

        PanCamera();
        ZoomCamera();
    }

    private void PanCamera()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        if (GetButtonJustPressed()) this.dragOrigin = this.cam.ScreenToWorldPoint(mouseScreenPos);

        if (!GetButtonPressed()) return;
        Vector3 difference = this.dragOrigin - this.cam.ScreenToWorldPoint(mouseScreenPos);
        difference.z = 0; // Lock Z axis for 2D
        this.transform.position += difference;
    }

    private void ZoomCamera()
    {
        // New Input System scroll returns a Vector2 (typically Y is ~120 or -120 per notch)
        float scrollValue = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scrollValue, 0f)) return;

        // Normalize the scroll value to match standard expected speeds (approx 0.01 to 0.1)
        float scroll = scrollValue * 0.001f;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseBeforeZoom = this.cam.ScreenToWorldPoint(mouseScreenPos);

        // Adjust zoom
        this.cam.orthographicSize -= scroll * this.zoomSpeed;
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize, this.minZoom, this.maxZoom);

        Vector3 mouseAfterZoom = this.cam.ScreenToWorldPoint(mouseScreenPos);

        // Shift camera to anchor zoom directly on mouse pointer
        Vector3 difference = mouseBeforeZoom - mouseAfterZoom;
        difference.z = 0;
        this.transform.position += difference;
    }

    // Helper method to check if the chosen button was clicked this frame
    private bool GetButtonJustPressed()
    {
        if (this.dragButtons.Contains(MouseButton.Left) && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (this.dragButtons.Contains(MouseButton.Right) && Mouse.current.rightButton.wasPressedThisFrame) return true;
        if (this.dragButtons.Contains(MouseButton.Middle) && Mouse.current.middleButton.wasPressedThisFrame) return true;
        return false;
    }

    // Helper method to check if the chosen button is currently being held down
    private bool GetButtonPressed()
    {
        if (this.dragButtons.Contains(MouseButton.Left) && Mouse.current.leftButton.isPressed) return true;
        if (this.dragButtons.Contains(MouseButton.Right) && Mouse.current.rightButton.isPressed) return true;
        if (this.dragButtons.Contains(MouseButton.Middle) && Mouse.current.middleButton.isPressed) return true;
        return false;
    }
}