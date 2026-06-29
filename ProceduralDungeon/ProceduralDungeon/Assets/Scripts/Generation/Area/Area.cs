using System;
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
        [SerializeField, Range(25, 4000)] private int minSize;
        [SerializeField, Range(25, 4000)] private int maxSize;
        public int WalkDirectionRepetitionAllowance => this.walkDirectionRepetitionAllowance;
        [SerializeField, Range(0, 20)] private int walkDirectionRepetitionAllowance = 2;
        
        [SerializeField, Expandable] private List<TypedRoomList> roomTypes = new();
        [SerializeField] private RoomList endRooms = null!;
        [SerializeField, HideInInspector] public int smallestEndRoomSize;
        [SerializeField] private SliderAndTextInstance[] sliderAndTextInstances = Array.Empty<SliderAndTextInstance>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            int smallestRoom = this.roomTypes.Select(roomTypeList => roomTypeList.smallestRoomSize).Prepend(int.MaxValue).Min();
            if (this.minSize < smallestRoom * 2)
            {
                Debug.Log($"Area {this.name}, min size value was less then two rooms, min value has been adjusted");
                this.minSize = smallestRoom * 2;
            }
            if (this.maxSize < this.minSize) this.maxSize = this.minSize;
            
            // make sure no endRooms are in weighted list
            foreach (TypedRoomList roomTypeDataList in this.roomTypes)
            {
                if (roomTypeDataList.roomType == RoomType.EndRoom)
                    roomTypeDataList.roomType = RoomType.HostileRoom;
            }

            this.endRooms.OnValidate();
            this.endRooms.onRoomsChanged = OnRoomsChanged;
            OnRoomsChanged();
        }

        private void OnRoomsChanged()
        {
            int newSmallest = this.endRooms.RoomData.Select(room => room.Size).Prepend(int.MaxValue).Min();
            if (this.smallestEndRoomSize == newSmallest) return;
            this.smallestEndRoomSize = newSmallest;
            EditorUtility.SetDirty(this);
        }

        private void OnDestroy() => this.endRooms.OnDestroy();
#endif

        public AreaRuntime GetAreaGenData(string seed) =>
            new(this.areaType, Rng.RandomRange(this.minSize, this.maxSize, seed), this.roomTypes, this.endRooms, this.smallestEndRoomSize);

        public void CreatUI(Transform parent, GameObject sliderAndTextPrefab) => 
            SliderAndTextInstance.ConnectSlidersToWorldGenData(this.sliderAndTextInstances, typeof(Area), this, sliderAndTextPrefab, parent);
    }
}