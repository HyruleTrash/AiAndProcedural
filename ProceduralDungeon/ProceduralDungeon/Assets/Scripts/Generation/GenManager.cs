using System;
using System.Globalization;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using Util;

namespace Generation
{
    /// <summary>
    /// Connects the world gen to unity, made to hold the references and timing
    /// </summary>
    public class GenManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WorldGen worldGen = null!;
        [SerializeField] private string mainSeed = null!;

        [Header("UI")] 
        [SerializeField] private TextMeshProUGUI seedTextElement = null!;
        [SerializeField] private SpriteRenderer tempWorldGenSprite = null!;
        [SerializeField] private TMP_InputField inputFieldWaitTime = null!;
        [SerializeField] private Transform worldGenUiBox = null!;
        [SerializeField] private Transform mainUiBox = null!;
        [SerializeField] private GameObject areaUIPrefab = null!;
        [SerializeField] private GameObject sliderAndTextPrefab = null!;
        [SerializeField] private SliderAndTextInstance[] sliderAndTextInstances = Array.Empty<SliderAndTextInstance>();
        [SerializeField] private float waitTime;

        private Coroutine? genRoutine; // used to make use of unity's wait functions during generation. to create visual
        
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
            
            // Create a duplicate so that ui sliders may safely edit
            this.worldGen = Instantiate(this.worldGen);
            this.worldGen.InstantiateAreaData();
            
            UpdateWaitTime();
            SliderAndTextInstance.ConnectSlidersToWorldGenData(this.sliderAndTextInstances, typeof(WorldGen), this.worldGen, this.sliderAndTextPrefab, this.worldGenUiBox);
            this.worldGen.CreateAreaUI(this.mainUiBox, this.areaUIPrefab, this.sliderAndTextPrefab);
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