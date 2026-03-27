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

        public RoomData(Texture2D texture)
        {
            layout = GetStringLayout(texture);
            Size = texture.height * texture.width;
            contentPoints = GetContentPoints(layout);
        }

        private static readonly Color Wall = new(1, 1, 1, 1);
        private static readonly Color Empty = new(0, 0, 0, 1);
        private static readonly Color Content = new(1, 0, 0, 1);

        private string GetStringLayout(Texture2D texture)
        {
            var result = new System.Text.StringBuilder();
            var pixels = texture.GetPixels();

            const float tolerance = 0.1f;
            bool CheckColor(Color a, Color b) => 
                Math.Abs(a.r - b.r) < tolerance && 
                Math.Abs(a.g - b.g) < tolerance && 
                Math.Abs(a.b - b.b) < tolerance;
            
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var pixel = pixels[y * texture.width + x];

                    if (CheckColor(pixel, Wall)) result.Append("1");
                    else if (CheckColor(pixel, Empty)) result.Append("0");
                    else if (CheckColor(pixel, Content)) result.Append("x");
                    else Debug.Log($"Unexpected color: {pixel}");
                }
                if (y < texture.height - 1) result.Append("n");
            }
    
            return result.ToString();
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