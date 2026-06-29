using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// A simple ui element that's used in the static notif manager
/// </summary>
public class NotificationItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent = null!;
    [SerializeField] private float visibleDuration = 2.0f;
    private float fadeDuration;

    public void Initialize(string message, float fadeOutSpeed)
    {
        if (fadeOutSpeed == 0.0f)
        {
            this.gameObject.SetActive(false);
            return;
        }

        this.fadeDuration = this.visibleDuration / fadeOutSpeed;
        if (!this.textComponent) this.textComponent = GetComponent<TextMeshProUGUI>();

        this.textComponent.text = message;
        StartCoroutine(FadeAndDestroyRoutine());
    }

    private IEnumerator FadeAndDestroyRoutine()
    {
        yield return new WaitForSeconds(this.visibleDuration);

        Color originalColor = this.textComponent.color;
        float time = 0f;

        while (time < this.fadeDuration)
        {
            time += Time.deltaTime;
            
            float t = Mathf.Lerp(1f, 0f, time / this.fadeDuration);
            this.textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, t);
            yield return null;
        }

        Destroy(this.gameObject);
    }
}