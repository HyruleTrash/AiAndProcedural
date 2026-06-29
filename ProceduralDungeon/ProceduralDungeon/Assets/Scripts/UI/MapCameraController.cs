using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
    private bool isDragging;

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

        if (GetButtonJustPressed()) 
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
                this.isDragging = false;
            else
            {
                this.isDragging = true;
                this.dragOrigin = this.cam.ScreenToWorldPoint(mouseScreenPos);
            }
        }
        
        if (!this.isDragging || !GetButtonPressed())
        {
            this.isDragging = false;
            return;
        }

        Vector3 difference = this.dragOrigin - this.cam.ScreenToWorldPoint(mouseScreenPos);
        difference.z = 0;
        this.transform.position += difference;
    }

    private void ZoomCamera()
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        float scrollValue = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scrollValue, 0f)) return;

        float scroll = scrollValue * 0.001f;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseBeforeZoom = this.cam.ScreenToWorldPoint(mouseScreenPos);

        this.cam.orthographicSize -= scroll * this.zoomSpeed;
        this.cam.orthographicSize = Mathf.Clamp(this.cam.orthographicSize, this.minZoom, this.maxZoom);

        Vector3 mouseAfterZoom = this.cam.ScreenToWorldPoint(mouseScreenPos);

        Vector3 difference = mouseBeforeZoom - mouseAfterZoom;
        difference.z = 0;
        this.transform.position += difference;
    }

    private bool GetButtonJustPressed()
    {
        if (this.dragButtons.Contains(MouseButton.Left) && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (this.dragButtons.Contains(MouseButton.Right) && Mouse.current.rightButton.wasPressedThisFrame) return true;
        if (this.dragButtons.Contains(MouseButton.Middle) && Mouse.current.middleButton.wasPressedThisFrame) return true;
        return false;
    }

    private bool GetButtonPressed()
    {
        if (this.dragButtons.Contains(MouseButton.Left) && Mouse.current.leftButton.isPressed) return true;
        if (this.dragButtons.Contains(MouseButton.Right) && Mouse.current.rightButton.isPressed) return true;
        if (this.dragButtons.Contains(MouseButton.Middle) && Mouse.current.middleButton.isPressed) return true;
        return false;
    }
}