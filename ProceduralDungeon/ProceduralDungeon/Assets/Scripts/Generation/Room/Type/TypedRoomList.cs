using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Util;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Generation
{
    [CreateAssetMenu(fileName = "TypedRoomList", menuName = "Generation/TypedRoomList")]
    public class TypedRoomList : ScriptableObject
    {
        public RoomType roomType;
        [SerializeField]
        private float weight;
        [SerializeField]
        private RoomList rooms = null!;
        [SerializeField, HideInInspector]
        public int smallestRoomSize;

        public float Weight
        {
            get => this.weight;
            protected set => this.weight = value;
        }

        public RoomList Rooms
        {
            get => this.rooms;
            protected set => this.rooms = value;
        }

        public TypedRoomList Duplicate()
        {
            TypedRoomList? newInstance = CreateInstance(GetType()) as TypedRoomList;
            newInstance!.roomType = this.roomType;
            newInstance.weight = this.weight;
            newInstance.rooms = new RoomList(this.rooms.RoomData);
            newInstance.smallestRoomSize = this.smallestRoomSize;
            return newInstance;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            this.weight = Mathf.Clamp(this.weight, 0f, 1f);
            this.weight = (float)Math.Round(this.weight, 2);
            this.rooms.OnValidate();
            this.rooms.onRoomsChanged = OnRoomsChanged;
        }

        private void OnDestroy() => this.rooms.OnDestroy();

        private void OnRoomsChanged()
        {
            this.smallestRoomSize = this.rooms.RoomData.Select(room => room.Size).Prepend(int.MaxValue).Min();
            EditorUtility.SetDirty(this);
        }
        #endif

        public static Room? TryGetRoom(WorldGenRuntime genRuntime, List<Room> pool, Room[] lastRooms = null!)
        {
            string usedSeed = genRuntime.currentSeed;
            int index;
            
            while (true)
            {
                if (pool.Count == 0) return null;
                
                index = Rng.RandomRange(0, pool.Count, usedSeed);

                if (lastRooms == null) break;
                if (!lastRooms.Contains(pool[index])) break;

                usedSeed = Rng.MutateNext(usedSeed);
                pool.RemoveAt(index);
            }
            genRuntime.currentSeed = usedSeed;
            return pool[index];
        }

        public virtual void OnPicked(AreaRuntime pickedArea) {}
        public virtual void UndoPicked(AreaRuntime pickedArea) {}

        private static string RoomDataListToString(List<Room> data)
        {
            StringBuilder builder = new();
            foreach (Room room in data) builder.AppendLine(room.ToString());
            return builder.ToString();
        }

    }
}