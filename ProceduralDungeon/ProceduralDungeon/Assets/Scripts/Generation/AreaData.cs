using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Generation
{
    /// <summary>
    /// Holds references and default data for an entire area
    /// </summary>
    [CreateAssetMenu(fileName = "AreaData", menuName = "Generation/AreaData")]
    public class AreaData : ScriptableObject
    {
        public AreaType areaType;
        [SerializeField]
        private Vector2Int minMaxSize; // x is min, y is max
        [SerializeField, Expandable]
        private List<RoomTypeDataList> roomTypes;

        public AreaGenData GetAreaGenData(string seed)
        {
            return new AreaGenData(areaType, RNG.RandomRange(minMaxSize.x, minMaxSize.y, seed), roomTypes);
        }
    }
}