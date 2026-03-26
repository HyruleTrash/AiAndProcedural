using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(fileName = "AreaData", menuName = "Generation/AreaData")]
    public class AreaData : ScriptableObject
    {
        public AreaType areaType;
        [SerializeField]
        private Vector2Int minMaxSize; // x is min, y is max
        [SerializeField, Expandable]
        private List<RoomDataList> roomTypes;
    }
}