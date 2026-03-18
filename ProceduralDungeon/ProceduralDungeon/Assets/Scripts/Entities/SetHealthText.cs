using System;
using TMPro;
using UnityEngine;

public class SetHealthText : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro healthText;
    [SerializeField]
    private HealthComponent healthComponent;

    private void OnValidate()
    {
        healthText ??= GetComponent<TextMeshPro>();
        healthComponent ??= GetComponentInParent<HealthComponent>();
    }

    private void OnEnable()
    {
        healthComponent.onHealthChange.AddListener(OnHealthChange);
        OnHealthChange(healthComponent.MaxHealth);
    }

    private void OnDisable() => healthComponent.onHealthChange.RemoveListener(OnHealthChange);
    private void OnHealthChange(float health) => healthText.text = $"HP: {health}/{healthComponent.MaxHealth}";
}
