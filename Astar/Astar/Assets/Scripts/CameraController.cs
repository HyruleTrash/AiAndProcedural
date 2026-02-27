using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 1;
    public float yPos = 15;
    public float zoomSpeed = 2;
    public float lerpSpeed = 1;
    private Vector3 targetPos;

    void Start() => targetPos = new Vector3(0, yPos, 0);

    void Update()
    {
        var vert = 0f;
        var hor = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) vert += 1;
            if (Keyboard.current.sKey.isPressed) vert -= 1;
            if (Keyboard.current.dKey.isPressed) hor += 1;
            if (Keyboard.current.aKey.isPressed) hor -= 1;
        }

        if (vert != 0 || hor != 0)
        {
            targetPos += (Vector3.forward * vert + Vector3.right * hor).normalized * moveSpeed;
        }

        var scroll = 0f;
        if (Mouse.current != null) scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0)
        {
            yPos += -Mathf.Sign(scroll) * zoomSpeed;
            yPos = Mathf.Clamp(yPos, 1, 100);
            targetPos = new Vector3(targetPos.x, yPos, targetPos.z);
        }

        // transform.position = targetPos;
        transform.position = Vector3.Lerp(transform.position, targetPos, lerpSpeed * Time.deltaTime);
    }
}