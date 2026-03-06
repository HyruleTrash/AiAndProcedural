using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] 
    private float maxHealth = 100f;
    [SerializeField] 
    private float currentHealth = 100f;
    public UnityEvent<float> onHealthChange;
    public UnityEvent onHealthDepleted;
    
    public void TakeDamage(float amount)
    {
        var lastHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        if (Mathf.Approximately(lastHealth, currentHealth)) return;
        onHealthChange.Invoke(currentHealth);
        if (currentHealth <= 0f)
            onHealthDepleted.Invoke();
    }

    public bool CanTakeDamage() => currentHealth > 0f;
}