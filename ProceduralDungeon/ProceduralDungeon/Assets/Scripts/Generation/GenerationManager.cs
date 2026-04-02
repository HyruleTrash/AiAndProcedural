using System;
using System.Globalization;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace Generation
{
    public class GenerationManager : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer tempWorldGenSprite;
        [SerializeField]
        private string mainSeed;
        [SerializeField]
        private WorldGenerator worldGenerator;
        [SerializeField]
        private TMP_InputField inputFieldWaitTime;
        [SerializeField]
        private float waitTime;

        private Coroutine routine;

        #if UNITY_EDITOR
        [Button]
        public void GenerateMainSeed()
        {
            mainSeed = RNG.ParseSeed((ulong)DateTime.Now.Ticks);
            mainSeed = RNG.MutateNext(mainSeed);
            Debug.Log($"Seed has been set: {mainSeed}");
        }
        #endif

        private void OnValidate()
        {
            WorldGenerator.WaitTime = waitTime;
            inputFieldWaitTime.text = waitTime.ToString(CultureInfo.InvariantCulture);
            enabled = inputFieldWaitTime;
        }

        private void Start()
        {
            WorldGenerator.WaitTime = waitTime;
            inputFieldWaitTime.text = waitTime.ToString(CultureInfo.InvariantCulture);
        }

        public void OnWaitTimeChanged(string time)
        {
            waitTime = float.TryParse(inputFieldWaitTime.text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f;
            WorldGenerator.WaitTime = waitTime;
        }

        [Button]
        public void GenerateWorld()
        {
            if (routine != null)
                return;
            worldGenerator.SetOwner(this);
            var result = new WorldGenerator.GenerationResult();

            void OnFinish()
            {
                routine = null;
                GenerateDebugTexture(result);
            }

            routine = StartCoroutine(worldGenerator.Generate(mainSeed, result, OnFinish, GenerateDebugTexture));
        }

        public void GenerateDebugTexture(WorldGenerator.GenerationResult result)
        {
            var size = result.grid.GetWorldSize(out Vector2Int offset);
            
            // Adding a border
            size += Vector2Int.one * 16;
            offset += Vector2Int.one * 8;
                
            var worldGenTex = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            worldGenTex.filterMode = FilterMode.Point;
            worldGenTex.wrapMode = TextureWrapMode.Clamp;
            worldGenTex.anisoLevel = 0;
                
            var pixels = result.grid.GetPixels(size, offset);
            var posIndex = (result.currentPosition.y + offset.y) * size.x + result.currentPosition.x + offset.x;
            pixels[posIndex] = Color.blueViolet;
            
            worldGenTex.SetPixels(pixels);
            worldGenTex.Apply();
                
            var rect = new Rect(0, 0, worldGenTex.width, worldGenTex.height);
            var newSprite = Sprite.Create(worldGenTex, rect, new Vector2(0.5f, 0.5f), 32f);
                
            tempWorldGenSprite.sprite = newSprite;
        }
    }
}