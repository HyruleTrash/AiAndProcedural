using System;
using System.Linq;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

namespace Generation
{
    public class RoomTileLookup : MonoBehaviour
    {
        public static Action roomDataHasBeenUpdated;
        public static RoomTileLookup LookupInstance
        {
            get
            {
                if (lookupInstance == null)
                    lookupInstance = FindFirstObjectByType<RoomTileLookup>();
                return lookupInstance;
            }
            private set => lookupInstance = value;
        }

        private static RoomTileLookup lookupInstance;
        public listInstance[] tiles;
        
        [Serializable]
        public struct RoomTile
        {
            public char text;
            public Color color;

            public RoomTile(char text, Color color)
            {
                this.text = text;
                this.color = color;
            }
        }

        [Serializable]
        public class listInstance
        {
            public string key;
            public RoomTile tile;
        }

        private void OnEnable()
        {
            if (LookupInstance)
                Debug.LogWarning("Only one lookup instance should be active");
            LookupInstance = this;
        }

        public RoomTile? GetTile(string key) => tiles.FirstOrDefault(x => x.key == key)?.tile;

        #if UNITY_EDITOR
        [Button]
        private void UpdateRoomData() => roomDataHasBeenUpdated?.Invoke();
        #endif
    }
}