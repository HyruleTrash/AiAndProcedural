using UnityEngine;

namespace NaughtyAttributes.Test
{
    public class SortingLayerTest : MonoBehaviour
    {
        [SortingLayer]
        public int layerNumber0;

        [SortingLayer]
        public string layerName0;

        public SortingLayerNest1 nest1;

        [Button]
        public void DebugLog()
        {
            Debug.LogFormat("{0} = {1}", nameof(this.layerNumber0), this.layerNumber0);
            Debug.LogFormat("{0} = {1}", nameof(this.layerName0), this.layerName0);
            Debug.LogFormat("LayerToName({0}) = {1}", this.layerNumber0, SortingLayer.IDToName(this.layerNumber0));
            Debug.LogFormat("NameToLayer({0}) = {1}", this.layerName0, SortingLayer.NameToID(this.layerName0));
        }
    }

    [System.Serializable]
    public class SortingLayerNest1
    {
        [SortingLayer]
        public int layerNumber1;

        [SortingLayer]
        public string layerName1;

        public SortingLayerNest2 nest2;
    }

    [System.Serializable]
    public struct SortingLayerNest2
    {
        [SortingLayer]
        public int layerNumber2;

        [SortingLayer]
        public string layerName2;
    }
}
