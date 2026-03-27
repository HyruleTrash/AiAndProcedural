using System;
using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    // 1 = wall
    // 0 = openSpace
    // n = next line
    // x = content
    
    [Serializable]
    public class RoomData
    {
        [SerializeField]
        private string layout;
        
        [SerializeField] 
        private int size;
        public int Size { get => size; private set => size = value; }
        
        [SerializeField]
        private Vector2[] contentPoints;
        public Vector2[] ContentPoints { get => contentPoints; private set => contentPoints = value; }

        public RoomData(string layout)
        {
            this.layout = layout;
            Size = CalcSize(layout);
            contentPoints = GetContentPoints(layout);
        }

        private static int CalcSize(string layout)
        {
            var height = 0;
            var width = 0;
            
            var widthCount = 0;
            foreach (var c in layout)
            {
                if (c != 'n')
                {
                    widthCount++;
                    continue;
                }

                height++;
                if (widthCount > width)
                    width = widthCount;
                widthCount = 0;
            }

            return height * width;
        }

        private static Vector2[] GetContentPoints(string layout)
        {
            var contentPoints = new List<Vector2>();
            
            var height = 0;
            var width = -1;
            
            var widthCount = -1;
            foreach (var c in layout)
            {
                switch (c)
                {
                    case 'x':
                        contentPoints.Add(new Vector2(width, height));
                        goto case '1';
                    case '0':
                    case '1':
                        widthCount++;
                        continue;
                    case 'n':
                    {
                        height++;
                        if (widthCount > width)
                            width = widthCount;
                        widthCount = -1;
                        break;
                    }
                }
            }
            
            return contentPoints.ToArray();
        }
    }
}