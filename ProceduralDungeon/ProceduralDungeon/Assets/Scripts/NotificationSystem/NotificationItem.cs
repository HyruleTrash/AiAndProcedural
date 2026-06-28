using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Made with gemini, a simple console text instance, with fading logic
/// </summary>
public class NotificationItem : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI textComponent = null!;

    [Header("Settings")]
    [SerializeField] private float visibleDuration = 2.0f; // How long it stays fully visible
    [SerializeField] private float fadeDuration = 1.0f;    // How long the fade-out takes

    public void Initialize(string message)
    {
        // Fail-safe if the reference wasn't dragged into the inspector
        if (!this.textComponent) this.textComponent = GetComponent<TextMeshProUGUI>();

        this.textComponent.text = message;
        StartCoroutine(FadeAndDestroyRoutine());
    }

    private IEnumerator FadeAndDestroyRoutine()
    {
        // 1. Wait out the solid text lifetime
        yield return new WaitForSeconds(this.visibleDuration);

        // 2. Smoothly fade the alpha channel
        Color originalColor = this.textComponent.color;
        float elapsedTime = 0f;

        while (elapsedTime < this.fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Calculate new alpha
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / this.fadeDuration);
            this.textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            
            yield return null; // Wait for the next frame
        }

        // 3. Goodbye, cruel world!
        Destroy(this.gameObject);
    }
}