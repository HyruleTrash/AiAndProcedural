using System;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace Generation
{
    public class RoomTileLookup : MonoBehaviour
    {
        public static Action? RoomDataHasBeenUpdated;
        public static RoomTileLookup? LookupInstance
        {
            get
            {
                if (!lookupInstance)
                    lookupInstance = FindFirstObjectByType<RoomTileLookup>();
                return lookupInstance;
            }
            private set => lookupInstance = value;
        }

        private static RoomTileLookup? lookupInstance;
        public ListInstance[] tiles = null!;
        [SerializeField] private ColorList tileColors = new();

        [Serializable]
        public class ListInstance
        {
            public string key = null!;
            public RoomTile tile;
        }
        

        private void OnEnable()
        {
            if (LookupInstance && LookupInstance != this) Debug.LogWarning("Only one lookup instance should be active");
            LookupInstance = this;
        }

        private void OnDisable()
        {
            if (LookupInstance == this) LookupInstance = null;
        }

        public RoomTile? GetTile(string key) => this.tiles.FirstOrDefault(x => x.key == key)?.tile;

        #if UNITY_EDITOR
        [Button] // This function is used within the editor to forcefully update references
        private void UpdateRoomData() => RoomDataHasBeenUpdated?.Invoke();
        #endif
        
        public Color GetColor(AreaType areaType, RoomType roomType, string listInstanceKey) => this.tileColors.GetColor(areaType, roomType, listInstanceKey);
    }
}