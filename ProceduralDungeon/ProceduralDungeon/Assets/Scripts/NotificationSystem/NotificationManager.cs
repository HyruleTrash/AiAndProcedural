using System;
using UnityEngine;

/// <summary>
/// Made with gemini, a simple console within the game
/// </summary>
public class NotificationManager : MonoBehaviour
{
    private static NotificationManager? instance;

    [SerializeField] private GameObject notificationPrefab = null!;
    [SerializeField] private int maxNotifications = 5;

    private void Awake()
    {
        // Set up the singleton instance
        if (instance && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// Globally accessible function to trigger a notification.
    /// </summary>
    [HideInCallstack]
    public static void Log(string message, float fadeOutSpeed)
    {
        if (!instance)
        {
            Debug.LogError("NotificationManager is missing from the scene! Put one on a GameObject in your UI Canvas.");
            return;
        }

        if (!instance.notificationPrefab)
        {
            Debug.LogError("NotificationManager is missing its Notification Prefab reference!");
            return;
        }

        Debug.Log(message);
        if (fadeOutSpeed == 0) return;

        GameObject spawnedObj = Instantiate(instance.notificationPrefab, instance.transform);

        // Initialize the fade logic
        NotificationItem item = spawnedObj.GetComponent<NotificationItem>();
        if (item)
            item.Initialize(message, fadeOutSpeed);
        else
            Debug.LogWarning("The Notification Prefab is missing the NotificationItem script!");
    }

    private void OnTransformChildrenChanged()
    {
        if (this.transform.childCount > this.maxNotifications)
            Destroy(this.transform.GetChild(0).gameObject);
    }
}