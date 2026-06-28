using TMPro;
using UnityEngine;

public class SetHealthText : MonoBehaviour
{
    [SerializeField] private TextMeshPro healthText = null!;
    [SerializeField] private HealthComponent healthComponent = null!;

    private void OnValidate()
    {
        this.healthText ??= GetComponent<TextMeshPro>();
        this.healthComponent ??= GetComponentInParent<HealthComponent>();
    }

    private void OnEnable()
    {
        this.healthComponent.onHealthChange.AddListener(OnHealthChange);
        OnHealthChange(this.healthComponent.MaxHealth);
    }

    private void OnDisable() => this.healthComponent.onHealthChange.RemoveListener(OnHealthChange);
    private void OnHealthChange(float health) => this.healthText.text = $"HP: {health}/{this.healthComponent.MaxHealth}";
}
