using System;
using NaughtyAttributes;
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

        public void GenerateWorld()
        {
            if (routine != null)
                return;
            worldGenerator.SetOwner(this);
            var result = new WorldGenerator.GenerationResult();

            void OnFinish()
            {
                routine = null;

                var size = result.grid.GetWorldSize(out Vector2Int offset);
                
                var worldGenTex = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                worldGenTex.filterMode = FilterMode.Point;
                worldGenTex.wrapMode = TextureWrapMode.Clamp;
                worldGenTex.anisoLevel = 0;
                
                var pixels = result.grid.GetPixels(size, offset);
                worldGenTex.SetPixels(pixels);
                worldGenTex.Apply();
                
                var rect = new Rect(0, 0, worldGenTex.width, worldGenTex.height);
                var newSprite = Sprite.Create(worldGenTex, rect, new Vector2(0.5f, 0.5f), 32f);
                
                tempWorldGenSprite.sprite = newSprite;
            }

            routine = StartCoroutine(worldGenerator.Generate(mainSeed, result, OnFinish));
        }
    }
}