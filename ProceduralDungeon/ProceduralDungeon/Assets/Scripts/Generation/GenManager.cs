using System;
using System.Globalization;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using Util;

namespace Generation
{
    public class GenManager : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer tempWorldGenSprite;
        [SerializeField]
        private string mainSeed;
        [SerializeField] 
        private float minDistanceBossRoom;
        [SerializeField]
        private WorldGen worldGen;
        [SerializeField]
        private TMP_InputField inputFieldWaitTime;
        [SerializeField]
        private float waitTime;

        private Coroutine routine;

#if UNITY_EDITOR
        [Button]
        public void GenerateMainSeed()
        {
            this.mainSeed = Rng.ParseSeed((ulong)DateTime.Now.Ticks);
            this.mainSeed = Rng.MutateNext(this.mainSeed);
            Debug.Log($"Seed has been set: {this.mainSeed}");
        }
        #endif

        private void OnValidate()
        {
            WorldGen.WaitTime = this.waitTime;
            this.inputFieldWaitTime.text = this.waitTime.ToString(CultureInfo.InvariantCulture);
            this.enabled = this.inputFieldWaitTime;
        }

        private void Start()
        {
            WorldGen.WaitTime = this.waitTime;
            this.inputFieldWaitTime.text = this.waitTime.ToString(CultureInfo.InvariantCulture);
        }

        public void OnWaitTimeChanged(string time)
        {
            this.waitTime = float.TryParse(this.inputFieldWaitTime.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
            WorldGen.WaitTime = this.waitTime;
        }

        [Button]
        public void GenerateWorld()
        {
            if (!Application.isPlaying)
                return;
            if (this.routine != null)
                return;
            this.worldGen.SetOwner(this);
            WorldGen.GenerationResult result = new();

            void OnFinish()
            {
                this.routine = null;
                GenerateDebugTexture(result);
            }

            this.routine = StartCoroutine(this.worldGen.Generate(this.mainSeed, result, OnFinish, GenerateDebugTexture, this.minDistanceBossRoom));
        }

        private void GenerateDebugTexture(WorldGen.GenerationResult result)
        {
            Vector2Int size = result.grid.GetWorldSize(out Vector2Int offset);
            
            // Adding a border
            size += Vector2Int.one * 200;
            offset += Vector2Int.one * 100;
                
            Texture2D worldGenTex = new(size.x, size.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            Color[] pixels = result.grid.GetPixels(size, offset);
            int posIndex = (result.currentPosition.y + offset.y) * size.x + result.currentPosition.x + offset.x;
            pixels[posIndex] = Color.blueViolet;
            
            worldGenTex.SetPixels(pixels);
            worldGenTex.Apply();
                
            Rect rect = new(0, 0, worldGenTex.width, worldGenTex.height);
            Sprite newSprite = Sprite.Create(worldGenTex, rect, new Vector2(0.5f, 0.5f), 32f);

            this.tempWorldGenSprite.sprite = newSprite;
        }
    }
}