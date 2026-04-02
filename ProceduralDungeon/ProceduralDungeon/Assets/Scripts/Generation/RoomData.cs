using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class RoomData
    {
        [SerializeField]
        private string layout;
        public string Layout { get => layout; private set => layout = value; }
        
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

        [SerializeField]
        private List<DoorPointListItem> doorPoints;
        public List<DoorPointListItem> DoorPoints { get => doorPoints; private set => doorPoints = value; }

        [Serializable]
        public struct DoorPointListItem
        {
            public Vector2Int key;
            public List<DoorPointGroup> value;
        }
        
        [Serializable]
        public class DoorPointGroup
        {
            public List<Vector2Int> points = new();
            public Vector2Int roomPoint;
        }

        public RoomData(Texture2D texture)
        {
            layout = GetStringLayout(texture);
            Size = texture.height * texture.width;
            width = texture.width;
            height = texture.height;
            contentPoints = GetContentPoints(layout, width);
            doorPoints = FillDoorPoints(layout, width, height);
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
            }
    
            return result.ToString();
        }

        private static Vector2[] GetContentPoints(string layout, int width)
        {
            var contentPoints = new List<Vector2>();
            
            var lookupInstance = RoomTileLookup.LookupInstance;
            var contentChar = lookupInstance.GetTile("Content")?.text;
            
            for (var i = 0; i < layout.Length; i++)
            {
                var c = layout[i];
                if (c  == contentChar)
                    contentPoints.Add(new Vector2Int(i % width, i / width));
            }

            return contentPoints.ToArray();
        }
        
        private static List<DoorPointListItem> FillDoorPoints(string layout, int width, int height)
        {
            var lookupInstance = RoomTileLookup.LookupInstance;
            
            var result = new List<DoorPointListItem>()
            {
                new(){key = Vector2Int.up, value = new()},
                new(){key = Vector2Int.down, value = new()},
                new(){key = Vector2Int.left, value = new()},
                new(){key = Vector2Int.right, value = new()}
            };

            var hadIndeces = new List<int>();
            var doorwayChar = lookupInstance.GetTile("Doorway")?.text;
            if (doorwayChar == null)
                throw new Exception("Doorway character not found");

            for (var i = 0; i < layout.Length; i++)
            {
                var c = layout[i];
                if (c != doorwayChar.Value || hadIndeces.Contains(i))
                    continue;

                var doorPointGroup = new DoorPointGroup();
                doorPointGroup.points.Add(new Vector2Int(i % width, i / width));
                hadIndeces.Add(i);
                GetDoorPointNeighbours(doorPointGroup, i, layout, ref hadIndeces, doorwayChar.Value, width);
                var key = GetDirectionGetDoorPointGroup(doorPointGroup, width, height);
                result.First(a => a.key == key).value.Add(doorPointGroup);
            }

            return result;
        }

        private static void GetDoorPointNeighbours(DoorPointGroup doorPointGroup, int originIndex, string layout,
            ref List<int> hadIndeces, char doorwayChar, int width)
        {
            var indecesToCheck = new List<int>()
            {
                originIndex + width,
                originIndex - width,
            };
            
            var x = originIndex % width; // check x to prevent wrapping to the next row
            if (x < width - 1) indecesToCheck.Add(originIndex + 1);
            if (x > 0) indecesToCheck.Add(originIndex - 1);

            while (indecesToCheck.Count > 0)
            {
                var current = indecesToCheck.Last();
                if (hadIndeces.Contains(current) || current < 0 || current > layout.Length - 1 || layout[current] != doorwayChar)
                {
                    hadIndeces.Add(current);
                    indecesToCheck.RemoveAt(indecesToCheck.Count - 1);
                    continue;
                }

                doorPointGroup.points.Add(new Vector2Int(current % width, current / width));
                hadIndeces.Add(current);
                indecesToCheck.RemoveAt(indecesToCheck.Count - 1);
                
                x = current % width; // check x to prevent wrapping to the next row
                if (x < width - 1) indecesToCheck.Add(current + 1);
                if (x > 0) indecesToCheck.Add(current - 1);
                
                indecesToCheck.Add(current + width);
                indecesToCheck.Add(current - width);
            }
        }
        
        private static Vector2Int GetDirectionGetDoorPointGroup(DoorPointGroup doorPointGroup, int width, int height)
        {
            var average = doorPointGroup.points.Aggregate(Vector2.zero, (current, point) => current + point);
            average /= doorPointGroup.points.Count;
            
            var dir = (average - new Vector2(width / 2f, height / 2f)).normalized;

            var smallestDot = -2f;
            var foundDir = Vector2Int.zero;
            foreach (var cardinalDirection in WorldGenerator.CardinalDirections)
            {
                var dot = Vector2.Dot(cardinalDirection, dir);
                if (!(dot > smallestDot)) continue;
                smallestDot = dot;
                foundDir = cardinalDirection;
            }
            
            // save point where next room could be generated
            // abusing the fact that we use cardinal 4 way directions, we can fill in the middle point of the 0 value of the direction
            // (so 1,0 is right, meaning y = 0, meaning we can replace y with doorway center)
            // -1 to account for null indexing
            doorPointGroup.roomPoint = new Vector2Int(foundDir.x * width - 1, foundDir.y * height - 1);
            if (doorPointGroup.roomPoint.x < 1) doorPointGroup.roomPoint.x = (int)Mathf.Floor(average.x);
            if (doorPointGroup.roomPoint.y < 1) doorPointGroup.roomPoint.y = (int)Mathf.Floor(average.y);
            doorPointGroup.roomPoint -= new Vector2Int(width / 2, height / 2);
            doorPointGroup.roomPoint += Vector2Int.one; // +1 to account for null indexing

            return foundDir;
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

        public static Color[] GetPixels(string layout, int width, int height, AreaType areaType, RoomType roomType)
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
                        pixels[i - offset] = lookupInstance.GetColor(areaType, roomType, listInstance.key);
                }
            }

            return pixels;
        }
    }
}