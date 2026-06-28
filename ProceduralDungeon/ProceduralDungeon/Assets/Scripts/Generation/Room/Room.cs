using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class Room
    {
        [SerializeField]
        private string layout;
        public string Layout { get => this.layout; private set => this.layout = value; }
        
        [SerializeField] 
        private int size;
        [SerializeField] 
        private int width;
        [SerializeField] 
        private int height;
        public int Size { get => this.size; private set => this.size = value; }
        public int Width { get => this.width; private set => this.width = value; }
        public int Height { get => this.height; private set => this.height = value; }
        
        [SerializeField]
        private Vector2[] contentPoints;
        public Vector2[] ContentPoints { get => this.contentPoints; private set => this.contentPoints = value; }

        [SerializeField]
        private List<DoorPointListItem> doorPoints;
        public List<DoorPointListItem> DoorPoints { get => this.doorPoints; private set => this.doorPoints = value; }

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

        public Room(Texture2D texture)
        {
            this.layout = GetStringLayout(texture);
            this.Size = texture.height * texture.width;
            this.width = texture.width;
            this.height = texture.height;
            this.contentPoints = GetContentPoints(this.layout, this.width);
            this.doorPoints = FillDoorPoints(this.layout, this.width, this.height);
        }

        /// <summary>
        /// Translates a texture to a internally used layout text, based on a lookup table singleton
        /// </summary>
        private string GetStringLayout(Texture2D texture)
        {
            StringBuilder result = new();
            Color32[] pixels = texture.GetPixels32();

            const float tolerance = 0.1f;
            bool CheckColor(Color a, Color b) => 
                Math.Abs(a.r - b.r) < tolerance && 
                Math.Abs(a.g - b.g) < tolerance && 
                Math.Abs(a.b - b.b) < tolerance && 
                Math.Abs(a.a - b.a) < tolerance;
            
            RoomTileLookup? lookupInstance = RoomTileLookup.LookupInstance;
            if (lookupInstance == null || lookupInstance.tiles == null) return result.ToString();
            
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Color32 pixel = pixels[y * texture.width + x];

                    bool found = false;
                    foreach (RoomTileLookup.ListInstance lookupInstanceTile in lookupInstance.tiles)
                    {
                        if (!CheckColor(pixel, lookupInstanceTile.tile.color))
                            continue;
                        result.Append(lookupInstanceTile.tile.text);
                        found = true;
                        break;
                    }
                    if (!found) 
                        NotificationManager.Log($"Unexpected color: {pixel}");
                }
            }
    
            return result.ToString();
        }

        private static Vector2[] GetContentPoints(string layout, int width)
        {
            List<Vector2> contentPoints = new();
            
            RoomTileLookup? lookupInstance = RoomTileLookup.LookupInstance;
            char? contentChar = lookupInstance?.GetTile("Content")?.text;
            
            for (int i = 0; i < layout.Length; i++)
            {
                char c = layout[i];
                if (c  == contentChar)
                    contentPoints.Add(new Vector2Int(i % width, i / width));
            }

            return contentPoints.ToArray();
        }
        
        private static List<DoorPointListItem> FillDoorPoints(string layout, int width, int height)
        {
            RoomTileLookup? lookupInstance = RoomTileLookup.LookupInstance;
            
            List<DoorPointListItem> result = new()
            {
                new DoorPointListItem {key = Vector2Int.up, value = new List<DoorPointGroup>()},
                new DoorPointListItem {key = Vector2Int.down, value = new List<DoorPointGroup>()},
                new DoorPointListItem {key = Vector2Int.left, value = new List<DoorPointGroup>()},
                new DoorPointListItem {key = Vector2Int.right, value = new List<DoorPointGroup>()}
            };

            List<int> hadIndices = new();
            char? doorwayChar = lookupInstance?.GetTile("Doorway")?.text;
            if (doorwayChar == null)
                throw new Exception("Doorway character not found");

            for (int i = 0; i < layout.Length; i++)
            {
                char c = layout[i];
                if (c != doorwayChar.Value || hadIndices.Contains(i))
                    continue;

                DoorPointGroup doorPointGroup = new();
                doorPointGroup.points.Add(new Vector2Int(i % width, i / width));
                hadIndices.Add(i);
                GetDoorPointNeighbours(doorPointGroup, i, layout, ref hadIndices, doorwayChar.Value, width);
                Vector2Int key = GetDirectionGetDoorPointGroup(doorPointGroup, width, height);
                result.First(a => a.key == key).value.Add(doorPointGroup);
            }

            return result;
        }

        private static void GetDoorPointNeighbours(DoorPointGroup doorPointGroup, int originIndex, string layout,
            ref List<int> hadIndeces, char doorwayChar, int width)
        {
            List<int> indecesToCheck = new()
            {
                originIndex + width,
                originIndex - width,
            };
            
            int x = originIndex % width; // check x to prevent wrapping to the next row
            if (x < width - 1) indecesToCheck.Add(originIndex + 1);
            if (x > 0) indecesToCheck.Add(originIndex - 1);

            while (indecesToCheck.Count > 0)
            {
                int current = indecesToCheck.Last();
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
            Vector2 average = doorPointGroup.points.Aggregate(Vector2.zero, (current, point) => current + point);
            average /= doorPointGroup.points.Count;
            
            Vector2 dir = (average - new Vector2(width / 2f, height / 2f)).normalized;

            float smallestDot = -2f;
            Vector2Int foundDir = Vector2Int.zero;
            foreach (Vector2Int cardinalDirection in Util.Extensions.CardinalDirections)
            {
                float dot = Vector2.Dot(cardinalDirection, dir);
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
            builder.AppendLine($"Size: {this.size}");
            builder.AppendLine($"Width: {this.width}");
            builder.AppendLine($"Height: {this.height}");
            builder.AppendLine(this.layout);
            builder.AppendLine("}");
            return builder.ToString();
        }

        public static Color[] GetPixels(string layout, int width, int height, AreaType areaType, RoomType roomType)
        {
            Color[] pixels = new Color[width * height];
            RoomTileLookup? lookupInstance = RoomTileLookup.LookupInstance;
            if (!lookupInstance ||  lookupInstance.tiles == null) return pixels;
            
            int offset = 0;
            for (int i = 0; i < layout.Length; i++)
            {
                char c = layout[i];

                if (c == lookupInstance.GetTile("NextLine")?.text)
                {
                    offset++;
                    continue;
                }
                
                foreach (RoomTileLookup.ListInstance listInstance in lookupInstance.tiles)
                {
                    if (listInstance.tile.text == c)
                        pixels[i - offset] = lookupInstance.GetColor(areaType, roomType, listInstance.key);
                }
            }

            return pixels;
        }
    }
}