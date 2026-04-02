using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
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
        [SerializeField]
        private RoomList endRooms;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            var smallestRoom = roomTypes.Select(roomTypeList => roomTypeList.smallestRoomSize).Prepend(int.MaxValue).Min();
            if (minMaxSize.x < smallestRoom * 2)
            {
                Debug.Log($"Area {name}, min size value was less then two rooms, min value has been adjusted");
                minMaxSize.x = smallestRoom * 2;
            }
            if (minMaxSize.y < minMaxSize.x)
                minMaxSize.y = minMaxSize.x;
            
            // make sure no endRooms are in weighted list
            foreach (var roomTypeDataList in roomTypes)
            {
                if (roomTypeDataList.roomType == RoomType.EndRoom)
                    roomTypeDataList.roomType = RoomType.HostileRoom;
            }
            
            endRooms.OnValidate();
            endRooms.OnRoomsChanged = OnRoomsChanged;
        }

        private void OnRoomsChanged() => EditorUtility.SetDirty(this);

        private void OnDestroy() => endRooms.OnDestroy();
        #endif

        public AreaGenData GetAreaGenData(string seed) =>
            new(areaType, RNG.RandomRange(minMaxSize.x, minMaxSize.y, seed), roomTypes, endRooms);
    }
}