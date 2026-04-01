using System;
using NaughtyAttributes;
using UnityEngine;

namespace Generation
{
    public class GenerationManager : MonoBehaviour
    {
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
            routine = StartCoroutine(worldGenerator.Generate(mainSeed, result, () => routine = null));
        }
    }
}