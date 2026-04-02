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
        public ListInstance[] tiles;
        
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
        public class ListInstance
        {
            public string key;
            public RoomTile tile;
        }
        
        [SerializeField]
        private ColorList tileColors;

        [Serializable]
        private class ColorList
        {
            public ColorListInstance[] list;
            
            [Serializable]
            public class ColorListInstance
            {
                public string key;
                public TypedColors[] colorList;
            }

            [Serializable]
            public class TypedColors
            {
                public bool areaBased;
                public bool roomTypeBased;
                public AreaType areaType;
                public RoomType roomType;
                public Color color;
            }

            public Color GetColor(AreaType areaType, RoomType roomType, string listInstanceKey)
            {
                var foundList = list.FirstOrDefault(a => a.key == listInstanceKey);
                if (foundList == null)
                    return Color.black;

                bool AreaOrRoomTypeMatch(TypedColors a) =>
                    (a.areaBased && a.areaType == areaType) || (a.roomTypeBased && a.roomType == roomType);
                var colorObj = foundList.colorList.FirstOrDefault(AreaOrRoomTypeMatch);
                return colorObj != null ? colorObj.color : Color.black;
            }
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
        
        public Color GetColor(AreaType areaType, RoomType roomType, string listInstanceKey) => tileColors.GetColor(areaType, roomType, listInstanceKey);
    }
}