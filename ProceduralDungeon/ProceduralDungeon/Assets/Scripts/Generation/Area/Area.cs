using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using Util;

namespace Generation
{
    /// <summary>
    /// Holds references and default data for an entire area
    /// </summary>
    [CreateAssetMenu(fileName = "Area", menuName = "Generation/Area")]
    public class Area : ScriptableObject
    {
        public AreaType areaType;
        [SerializeField] private Vector2Int minMaxSize; // x is min, y is max
        [SerializeField, Expandable] private List<TypedRoomList> roomTypes = new();
        [SerializeField] private RoomList endRooms = null!;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            int smallestRoom = this.roomTypes.Select(roomTypeList => roomTypeList.smallestRoomSize).Prepend(int.MaxValue).Min();
            if (this.minMaxSize.x < smallestRoom * 2)
            {
                NotificationManager.Log($"Area {this.name}, min size value was less then two rooms, min value has been adjusted");
                this.minMaxSize.x = smallestRoom * 2;
            }
            if (this.minMaxSize.y < this.minMaxSize.x) this.minMaxSize.y = this.minMaxSize.x;
            
            // make sure no endRooms are in weighted list
            foreach (TypedRoomList roomTypeDataList in this.roomTypes)
            {
                if (roomTypeDataList.roomType == RoomType.EndRoom)
                    roomTypeDataList.roomType = RoomType.HostileRoom;
            }

            this.endRooms.OnValidate();
            this.endRooms.onRoomsChanged = OnRoomsChanged;
        }

        private void OnRoomsChanged() => EditorUtility.SetDirty(this);

        private void OnDestroy() => this.endRooms.OnDestroy();
        #endif

        public AreaRuntime GetAreaGenData(string seed) =>
            new(this.areaType, Rng.RandomRange(this.minMaxSize.x, this.minMaxSize.y, seed), this.roomTypes, this.endRooms);
    }
}