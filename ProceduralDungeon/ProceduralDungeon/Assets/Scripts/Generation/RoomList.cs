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
        private List<RoomData> roomData;
        public List<RoomData> RoomData { get => roomData; private set => roomData = value; }

        public RoomList(List<RoomData> givenData) => roomData = givenData;
        public RoomList(RoomList givenData) => roomData = givenData.RoomData;

        #if UNITY_EDITOR
        private bool onRoomsChangedRegistered = false;
        public Action OnRoomsChanged;
        
        public void OnValidate()
        {
            RoomTileLookup.roomDataHasBeenUpdated += OnRoomDataUpdated;
            onRoomsChangedRegistered = true;
        }

        public void OnDestroy()
        {
            if (onRoomsChangedRegistered)
                RoomTileLookup.roomDataHasBeenUpdated -= OnRoomDataUpdated;
        }

        private void OnRoomDataUpdated()
        {
            if (roomData == null)
                return;
            roomData = new List<RoomData>();
            foreach (var room in rooms.Where(room => room != null)) roomData.Add(new RoomData(room));
            OnRoomsChanged.Invoke();
        }
        #endif
    }
}