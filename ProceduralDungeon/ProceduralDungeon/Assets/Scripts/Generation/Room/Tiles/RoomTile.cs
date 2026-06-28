using System;
using UnityEngine;

namespace Generation
{
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
}