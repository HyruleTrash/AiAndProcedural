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
        #if UNITY_EDITOR
        [SerializeField] 
        private List<Texture2D> rooms;
        #endif
        [SerializeField, HideInInspector]
        private List<RoomData> roomData;
        [SerializeField, HideInInspector]
        public int smallestRoomSize;

        public float Weight
        {
            get => weight;
            private set => weight = value;
        }

        public RoomTypeDataList Duplicate()
        {
            var newInstance = CreateInstance<RoomTypeDataList>();
            newInstance.roomType = roomType;
            newInstance.weight = weight;
            newInstance.roomData = roomData;
            newInstance.smallestRoomSize = smallestRoomSize;
            return newInstance;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (roomData == null)
                return;
            roomData = new List<RoomData>();
            foreach (var room in rooms.Where(room => room != null)) roomData.Add(new RoomData(room));
            smallestRoomSize = roomData.Select(room => room.Size).Prepend(int.MaxValue).Min();
            EditorUtility.SetDirty(this);
        }
        #endif

        public RoomData TryGetRoom(WorldGenerator.WorldGenData genData, int size, RoomData[] lastRooms = null)
        {
            var pool = roomData.Where(room => room.Size <= size).ToList();
            var usedSeed = genData.currentSeed;
            int index;
            
            while (true)
            {
                if (pool.Count == 0)
                {
                    // Debug.Log($"Size requested: {size}\n{RoomDataListToString(roomData)}");
                    return null;
                }
                
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

        public void OnPicked(AreaGenData pickedArea)
        {
            // TODO implement areagen mutation logic here
        }

        private static string RoomDataListToString(List<RoomData> data)
        {
            StringBuilder builder = new();
            foreach (var room in data) builder.AppendLine(room.ToString());
            return builder.ToString();
        }
    }
}