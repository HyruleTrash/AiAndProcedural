using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField, HideInInspector]
        private List<RoomData> roomData;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            roomData = new List<RoomData>();
            foreach (var room in rooms) roomData.Add(new RoomData(room));
            EditorUtility.SetDirty(this);
        }
        #endif

        public RoomData TryGetRoom(int size, string randomSeed, RoomData[] lastRooms = null)
        {
            var pool = roomData.Where(room => room.Size <= size).ToList();
            var usedSeed = randomSeed;
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
            return pool[index];
        }
    }
}