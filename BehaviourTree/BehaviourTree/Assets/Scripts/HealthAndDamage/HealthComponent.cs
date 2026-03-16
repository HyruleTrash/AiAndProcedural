using System;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] 
    private float maxHealth = 100f;
    public float MaxHealth => maxHealth;
    [SerializeField] 
    private float currentHealth = 100f;
    [Space]
    public UnityEvent<float> onHealthChange;
    public UnityEvent onHealthDepleted;
    [SerializeField]
    private float maxInvincibilityTime = 1f;
    private Timer invincibilityTimer;
    private bool invincible = false;

    private void OnEnable() => invincibilityTimer = new Timer(maxInvincibilityTime, () => invincible = false);
    private void Update() => invincibilityTimer.Update(Time.deltaTime);

    public void TakeDamage(float amount)
    {
        var lastHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        if (Mathf.Approximately(lastHealth, currentHealth)) return;
        onHealthChange.Invoke(currentHealth);
        if (currentHealth <= 0f)
            onHealthDepleted.Invoke();
        
        invincible = true;
        invincibilityTimer.Reset();
    }

    public bool CanTakeDamage() => currentHealth > 0f && !invincible;
}