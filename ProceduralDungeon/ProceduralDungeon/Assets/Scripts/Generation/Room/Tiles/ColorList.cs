using System;
using System.Linq;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class ColorList
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
            ColorListInstance foundList = this.list.FirstOrDefault(a => a.key == listInstanceKey);
            if (foundList == null) return Color.black;

            TypedColors colorObj = foundList.colorList.FirstOrDefault(AreaOrRoomTypeMatch);
            return colorObj?.color ?? Color.black;

            bool AreaOrRoomTypeMatch(TypedColors a) =>
                (a.areaBased && a.areaType == areaType) || (a.roomTypeBased && a.roomType == roomType);
        }
    }
}