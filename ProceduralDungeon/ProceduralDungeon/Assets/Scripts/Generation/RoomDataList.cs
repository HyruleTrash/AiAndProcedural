using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(fileName = "RoomDataList", menuName = "Generation/RoomDataList")]
    public class RoomDataList : ScriptableObject
    {
        public RoomType roomType;
        [SerializeField]
        private float weight;
        [SerializeField] 
        private List<string> rooms;
    }
}