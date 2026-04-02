using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
#endif

namespace Generation
{
    [CreateAssetMenu(fileName = "RoomTypeDataList", menuName = "Generation/RoomTypeDataList")]
    public class RoomTypeDataList : ScriptableObject
    {
        public RoomType roomType;
        [SerializeField]
        private float weight;
        [SerializeField]
        private RoomList rooms;
        [SerializeField, HideInInspector]
        public int smallestRoomSize;

        public float Weight
        {
            get => weight;
            protected set => weight = value;
        }

        public RoomList Rooms
        {
            get => rooms;
            protected set => rooms = value;
        }

        public RoomTypeDataList Duplicate()
        {
            var newInstance = ScriptableObject.CreateInstance(GetType()) as RoomTypeDataList;;
            newInstance.roomType = roomType;
            newInstance.weight = weight;
            newInstance.rooms = new (rooms.RoomData);
            newInstance.smallestRoomSize = smallestRoomSize;
            return newInstance;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            weight = Mathf.Clamp(weight, 0f, 1f);
            weight = (float)Math.Round(weight, 2);
            rooms.OnValidate();
            rooms.OnRoomsChanged = OnRoomsChanged;
        }

        private void OnDestroy() => rooms.OnDestroy();

        private void OnRoomsChanged()
        {
            smallestRoomSize = rooms.RoomData.Select(room => room.Size).Prepend(int.MaxValue).Min();
            EditorUtility.SetDirty(this);
        }
        #endif

        public RoomData TryGetRoom(WorldGenerator.WorldGenData genData, List<RoomData> pool, RoomData[] lastRooms = null)
        {
            var usedSeed = genData.currentSeed;
            int index;
            
            while (true)
            {
                if (pool.Count == 0)
                    return null;
                
                index = RNG.RandomRange(0, pool.Count, usedSeed);

                if (lastRooms == null)
                    break;
                if (!lastRooms.Contains(pool[index]))
                    break;

                usedSeed = RNG.MutateNext(usedSeed);
                pool.RemoveAt(index);
            }
            genData.currentSeed = usedSeed;
            return pool[index];
        }

        public virtual void OnPicked(AreaGenData pickedArea) {}
        public virtual void UndoPicked(AreaGenData pickedArea) {}

        private static string RoomDataListToString(List<RoomData> data)
        {
            StringBuilder builder = new();
            foreach (var room in data) builder.AppendLine(room.ToString());
            return builder.ToString();
        }

    }
}