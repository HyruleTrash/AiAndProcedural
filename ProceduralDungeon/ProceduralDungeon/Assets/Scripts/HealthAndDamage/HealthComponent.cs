using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour, IDamageable
{
    public float MaxHealth => this.maxHealth;
    [SerializeField] private float maxHealth = 100f;
    
    [SerializeField] private float currentHealth = 100f;
    
    [Space]
    public UnityEvent<float> onHealthChange = new();
    public UnityEvent onHealthDepleted = new();
    
    [SerializeField] private float maxInvincibilityTime = 1f;
    private Timer invincibilityTimer = null!;
    private bool invincible;

    private void OnEnable() => this.invincibilityTimer = new Timer(this.maxInvincibilityTime, () => this.invincible = false);
    private void Update() => this.invincibilityTimer.Update(Time.deltaTime);

    public void TakeDamage(float amount)
    {
        float lastHealth = this.currentHealth;
        this.currentHealth = Mathf.Clamp(this.currentHealth - amount, 0f, this.maxHealth);
        if (Mathf.Approximately(lastHealth, this.currentHealth)) return;
        this.onHealthChange.Invoke(this.currentHealth);
        if (this.currentHealth <= 0f) this.onHealthDepleted.Invoke();

        this.invincible = true;
        this.invincibilityTimer.Reset();
    }

    public bool CanTakeDamage() => this.currentHealth > 0f && !this.invincible;
}