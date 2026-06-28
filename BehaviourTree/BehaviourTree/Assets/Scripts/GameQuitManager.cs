using UnityEngine;
using UnityEngine.InputSystem;

public class GameQuitManager : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) 
            QuitGame();
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}