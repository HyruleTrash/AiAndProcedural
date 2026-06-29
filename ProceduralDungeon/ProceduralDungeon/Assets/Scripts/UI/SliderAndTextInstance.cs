using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;
using Object = UnityEngine.Object;

[Serializable]
public class SliderAndTextInstance
{
    public string name = null!;
    [NonSerialized] public TextMeshProUGUI text = null!;
    [NonSerialized] public Slider slider = null!;
    [NonSerialized] public TextMeshProUGUI totalText = null!;

    public void Initialize(GameObject uiInstance)
    {
        TextMeshProUGUI[]? textComponents = uiInstance.GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents?.Length >= 2)
        {
            this.text = textComponents[0];
            this.totalText = textComponents[1];
        }

        this.slider = uiInstance.GetComponentInChildren<Slider>();
    }
    
    /// <summary>
    /// Using reflection connects ui sliders to set values
    /// </summary>
    public static void ConnectSlidersToWorldGenData(SliderAndTextInstance[] instances, Type type, object obj, GameObject sliderAndTextPrefab, Transform parent)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        foreach (SliderAndTextInstance instance in instances)
        {
            instance.Initialize(Object.Instantiate(sliderAndTextPrefab, parent));
            
            FieldInfo? field = type.GetField(instance.name, flags);
            if (field == null)
            {
                Debug.LogWarning($"Could not find field '{instance.name}' on WorldGen.");
                continue;
            }

            RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();
            if (range != null)
            {
                instance.slider.minValue = range.min;
                instance.slider.maxValue = range.max;
                instance.slider.wholeNumbers = field.FieldType == typeof(int);
            }
            
            UpdateSliderUIInitialState(obj, instance, field);

            instance.slider.onValueChanged.RemoveAllListeners();
            instance.slider.onValueChanged.AddListener((val) =>
            {
                if (field.FieldType == typeof(int))
                {
                    int intVal = Mathf.RoundToInt(val);
                    field.SetValue(obj, intVal);
                    instance.text.text = $"{instance.name}:".ToReadableString();
                    instance.totalText.text = $"{intVal}";
                }
                else if (field.FieldType == typeof(float))
                {
                    field.SetValue(obj, val);
                    instance.text.text = $"{instance.name}:".ToReadableString();
                    instance.totalText.text = $"{val:F2}";
                }
            });
        }
    }
    
    private static void UpdateSliderUIInitialState(object obj, SliderAndTextInstance instance, FieldInfo field)
    {
        object value = field.GetValue(obj);

        switch (value)
        {
            case int intVal:
                instance.slider.value = intVal;
                instance.text.text = $"{instance.name}:".ToReadableString();
                instance.totalText.text = $"{intVal}";
                break;
            case float floatVal:
                instance.slider.value = floatVal;
                instance.text.text = $"{instance.name}:".ToReadableString();
                instance.totalText.text = $"{floatVal:F2}";
                break;
        }
    }
}