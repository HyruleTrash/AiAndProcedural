using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Util;

namespace Generation
{
    [Serializable]
    public class RoomList
    {
        #if UNITY_EDITOR
        [SerializeField] private List<Texture2D> rooms;
        #endif
        
        [SerializeField, HideInInspector] private List<Room> roomData;
        public List<Room> RoomData { get => this.roomData; private set => this.roomData = value; }

        public RoomList(List<Room> givenData)
        {
            this.rooms = new List<Texture2D>();
            this.onRoomsChanged = null!;
            this.roomData = givenData;
        }

        public RoomList(RoomList givenData)
        {
            this.rooms = new List<Texture2D>();
            this.onRoomsChanged = null!;
            this.roomData = givenData.RoomData;
        }

#if UNITY_EDITOR
        private bool onRoomsChangedRegistered;
        public Action onRoomsChanged;
        
        public void OnValidate()
        {
            RoomTileLookup.RoomDataHasBeenUpdated += OnRoomDataUpdated;
            this.onRoomsChangedRegistered = true;
        }

        public void OnDestroy()
        {
            if (this.onRoomsChangedRegistered)
                RoomTileLookup.RoomDataHasBeenUpdated -= OnRoomDataUpdated;
        }

        private void OnRoomDataUpdated()
        {
            if (this.roomData == null)
                return;
            this.roomData = new List<Room>();
            foreach (Texture2D room in this.rooms.Where(room => room != null)) this.roomData.Add(new Room(room));
            this.onRoomsChanged.Invoke();
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
    }
}