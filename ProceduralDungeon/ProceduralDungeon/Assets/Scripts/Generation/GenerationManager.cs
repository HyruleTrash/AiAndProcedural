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

        #if UNITY_EDITOR
        [Button]
        public void GenerateMainSeed()
        {
            // TODO
            Debug.Log($"Seed has been set: {mainSeed}");
        }
        #endif
    }
}