using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class RoomList
    {
        #if UNITY_EDITOR
        [SerializeField] 
        private List<Texture2D> rooms;
        #endif
        [SerializeField, HideInInspector]
        private List<Room> roomData;
        public List<Room> RoomData { get => this.roomData; private set => this.roomData = value; }

        public RoomList(List<Room> givenData) => this.roomData = givenData;
        public RoomList(RoomList givenData) => this.roomData = givenData.RoomData;

        #if UNITY_EDITOR
        private bool onRoomsChangedRegistered = false;
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
    }
}