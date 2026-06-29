using System;
using System.Globalization;
using System.Reflection;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Generation
{
    /// <summary>
    /// Connects the world gen to unity, made to hold the references and timing
    /// </summary>
    public class GenManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField, Expandable] private WorldGen worldGen = null!;
        [SerializeField] private string mainSeed = null!;

        [Header("UI")] 
        [SerializeField] private TextMeshProUGUI seedTextElement = null!;
        [SerializeField] private SpriteRenderer tempWorldGenSprite = null!;
        [SerializeField] private TMP_InputField inputFieldWaitTime = null!;
        [SerializeField] private SliderAndTextInstance[] sliderAndTextInstances = Array.Empty<SliderAndTextInstance>();
        [SerializeField] private float waitTime;

        private Coroutine? genRoutine; // used to make use of unity's wait functions during generation. to create visual

        [Serializable]
        private class SliderAndTextInstance
        {
            public string name = null!;
            public TextMeshProUGUI text = null!;
            public Slider slider = null!;
            public TextMeshProUGUI totalText = null!;
        }
        
        /// <summary>
        /// Generates the seed used throughout the generated world
        /// </summary>
        [Button]
        public void GenerateMainSeed()
        {
            this.mainSeed = Rng.ParseSeed((ulong)DateTime.Now.Ticks);
            this.mainSeed = Rng.MutateNext(this.mainSeed);
            NotificationManager.Log($"Seed has been set: {this.mainSeed}", this.worldGen.GetAnimWaitTime());
            this.seedTextElement.text = this.mainSeed;
        }
        
        /// <summary>
        /// Triggers the world gen algorithm connected by button, and connects a coroutine animation
        /// </summary>
        public void TriggerGenWorld()
        {
            if (!Application.isPlaying || this.genRoutine != null) return;
            this.worldGen.SetOwner(this);
            WorldGenSnapshot snapshotRef = new();

            this.genRoutine = StartCoroutine(this.worldGen.InitiateGen(
                this.mainSeed,
                OnFinish,
                ss => WorldGenSnapshot.GenDebugTex(ss, this.tempWorldGenSprite)));
            return;

            void OnFinish()
            {
                this.genRoutine = null;
                WorldGenSnapshot.GenDebugTex(snapshotRef, this.tempWorldGenSprite);
            }
        }

        public void StopGen()
        {
            if (this.genRoutine == null) return;
            StopCoroutine(this.genRoutine);
            this.worldGen.StopGen();
        }

        private void OnValidate()
        {
            if (this.worldGen) UpdateWaitTime();
            this.enabled = this.inputFieldWaitTime && this.worldGen;
        }

        private void Start()
        {
            this.seedTextElement.text = $"Seed: {this.mainSeed}";
            this.worldGen = Instantiate(this.worldGen);
            UpdateWaitTime();
            ConnectSlidersToWorldGenData();
        }
        
        /// <summary>
        /// Using reflection connects ui sliders to set values
        /// </summary>
        private void ConnectSlidersToWorldGenData()
        {
            Type worldGenType = typeof(WorldGen);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            foreach (SliderAndTextInstance instance in this.sliderAndTextInstances)
            {
                FieldInfo? field = worldGenType.GetField(instance.name, flags);
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
                
                UpdateSliderUIInitialState(instance, field);

                instance.slider.onValueChanged.RemoveAllListeners();
                instance.slider.onValueChanged.AddListener((val) =>
                {
                    if (field.FieldType == typeof(int))
                    {
                        int intVal = Mathf.RoundToInt(val);
                        field.SetValue(this.worldGen, intVal);
                        instance.text.text = $"{instance.name}:";
                        instance.totalText.text = $"{intVal}";
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        field.SetValue(this.worldGen, val);
                        instance.text.text = $"{instance.name}:";
                        instance.totalText.text = $"{val:F2}";
                    }
                });
            }
        }
        
        private void UpdateSliderUIInitialState(SliderAndTextInstance instance, FieldInfo field)
        {
            object value = field.GetValue(this.worldGen);

            switch (value)
            {
                case int intVal:
                    instance.slider.value = intVal;
                    instance.text.text = $"{instance.name}:";
                    instance.totalText.text = $"{intVal}";
                    break;
                case float floatVal:
                    instance.slider.value = floatVal;
                    instance.text.text = $"{instance.name}:";
                    instance.totalText.text = $"{floatVal:F2}";
                    break;
            }
        }

        private void UpdateWaitTime()
        {
            this.worldGen.SetAnimWaitTime(this.waitTime);
            this.inputFieldWaitTime.text = this.waitTime.ToString(CultureInfo.InvariantCulture);
        }

        public void OnWaitTimeChanged(string time)
        {
            this.waitTime = float.TryParse(this.inputFieldWaitTime.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
            this.worldGen.SetAnimWaitTime(this.waitTime);
        }
    }
}