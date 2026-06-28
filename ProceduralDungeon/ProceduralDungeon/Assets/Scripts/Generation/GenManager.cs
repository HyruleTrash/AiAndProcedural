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
        [SerializeField] private WorldGen worldGen = new();
        [SerializeField] private string mainSeed = null!;
        [SerializeField] private float minDistToBossRoom;
        
        [SerializeField] private SpriteRenderer tempWorldGenSprite = null!;
        [SerializeField] private TMP_InputField inputFieldWaitTime = null!;
        [SerializeField] private float waitTime;

        private Coroutine? genRoutine; // used to make use of unity's wait functions during generation. to create visual

#if UNITY_EDITOR
        /// <summary>
        /// Generates the seed used throughout the generated world
        /// </summary>
        [Button]
        public void GenerateMainSeed()
        {
            this.mainSeed = Rng.ParseSeed((ulong)DateTime.Now.Ticks);
            this.mainSeed = Rng.MutateNext(this.mainSeed);
            NotificationManager.Log($"Seed has been set: {this.mainSeed}", this.worldGen.GetAnimWaitTime());
        }
#endif

        private void OnValidate()
        {
            UpdateWaitTime();
            this.enabled = this.inputFieldWaitTime;
        }

        private void Start() => UpdateWaitTime();

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
                this.minDistToBossRoom,
                OnFinish,
                snapshotRef,
                ss => WorldGenSnapshot.GenDebugTex(ss, this.tempWorldGenSprite)));
            return;

            void OnFinish()
            {
                this.genRoutine = null;
                WorldGenSnapshot.GenDebugTex(snapshotRef, this.tempWorldGenSprite);
            }
        }
    }
}