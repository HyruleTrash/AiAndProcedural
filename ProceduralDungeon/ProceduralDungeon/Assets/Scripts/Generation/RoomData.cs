using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class RoomData
    {
        [SerializeField]
        private string layout;
        
        [SerializeField] 
        private int size;
        [SerializeField] 
        private int width;
        [SerializeField] 
        private int height;
        public int Size { get => size; private set => size = value; }
        public int Width { get => width; private set => width = value; }
        public int Height { get => height; private set => height = value; }
        
        [SerializeField]
        private Vector2[] contentPoints;
        public Vector2[] ContentPoints { get => contentPoints; private set => contentPoints = value; }

        public RoomData(Texture2D texture)
        {
            layout = GetStringLayout(texture);
            Size = texture.height * texture.width;
            width = texture.width;
            height = texture.height;
            contentPoints = GetContentPoints(layout);
        }

        /// <summary>
        /// Translates a texture to a internally used layout text, based on a lookup table singleton
        /// </summary>
        private string GetStringLayout(Texture2D texture)
        {
            var result = new StringBuilder();
            var pixels = texture.GetPixels32();

            const float tolerance = 0.1f;
            bool CheckColor(Color a, Color b) => 
                Math.Abs(a.r - b.r) < tolerance && 
                Math.Abs(a.g - b.g) < tolerance && 
                Math.Abs(a.b - b.b) < tolerance && 
                Math.Abs(a.a - b.a) < tolerance;
            
            var lookupInstance = RoomTileLookup.LookupInstance;
            
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var pixel = pixels[y * texture.width + x];

                    var found = false;
                    foreach (var lookupInstanceTile in lookupInstance.tiles)
                    {
                        if (!CheckColor(pixel, lookupInstanceTile.tile.color))
                            continue;
                        result.Append(lookupInstanceTile.tile.text);
                        found = true;
                        break;
                    }
                    if (!found) 
                        Debug.Log($"Unexpected color: {pixel}");
                }
                if (y < texture.height - 1) result.Append(lookupInstance.GetTile("NextLine")?.text);
            }
    
            return result.ToString();
        }

        // TODO delegate this logic to the lookup table to make it more abstract
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

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine("room: {\n");
            builder.AppendLine($"Size: {size}");
            builder.AppendLine($"Width: {width}");
            builder.AppendLine($"Height: {height}");
            builder.AppendLine(layout);
            builder.AppendLine("}");
            return builder.ToString();
        }

        public Color[] GetPixels()
        {
            var pixels = new Color[width * height];
            var lookupInstance = RoomTileLookup.LookupInstance;

            var offset = 0;
            for (var i = 0; i < layout.Length; i++)
            {
                var c = layout[i];

                if (c == lookupInstance.GetTile("NextLine")?.text)
                {
                    offset++;
                    continue;
                }
                
                foreach (var listInstance in lookupInstance.tiles)
                {
                    if (listInstance.tile.text == c)
                        pixels[i - offset] = listInstance.tile.color;
                }
            }

            return pixels;
        }
    }
}