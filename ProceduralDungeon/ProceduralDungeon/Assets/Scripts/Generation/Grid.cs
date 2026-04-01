using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Generation
{
    public class RoomInstance : IComparable
    {
        public Vector2Int position;
        public RoomData dataRef;
                
        public int CompareTo(object obj)
        {
            if (obj is RoomInstance other)
                return (position.y.CompareTo(other.position.y) + position.x.CompareTo(other.position.x)) / 2;
            return 0;
        }
    }
    
    /// <summary>
    /// Used contain room instances, upon world generation
    /// </summary>
    public class Grid
    {
        private List<RoomInstance> rooms = new();

        public RoomInstance GetRoomAtPosition(Vector2Int position)
        {
            foreach (var instance in rooms)
            {
                var center = instance.position;
                var room = instance.dataRef;

                // limit bounding box to allow spawning of rooms at edge
                var min = new Vector2(
                    center.x - room.Width / 2f + 0.25f,
                    center.y - room.Height / 2f + 0.25f
                );
                var max = new Vector2(
                    center.x + room.Width / 2f - 0.25f,
                    center.y + room.Height / 2f - 0.25f
                );

                var inside =
                    position.x >= min.x &&
                    position.x <= max.x &&
                    position.y >= min.y &&
                    position.y <= max.y;

                if (inside)
                    return instance;
            }

            return null;
        }

        public bool CheckRoomPossible(RoomData roomToCheck, Vector2Int center)
        {
            var newMin = new Vector2Int(
                center.x - roomToCheck.Width / 2,
                center.y - roomToCheck.Height / 2
            );
            var newMax = new Vector2Int(
                center.x + roomToCheck.Width / 2,
                center.y + roomToCheck.Height / 2
            );

            foreach (var instance in rooms)
            {
                var otherCenter = instance.position;
                var other = instance.dataRef;

                var otherMin = new Vector2Int(
                    otherCenter.x - other.Width / 2,
                    otherCenter.y - other.Height / 2
                );

                var otherMax = new Vector2Int(
                    otherCenter.x + other.Width / 2,
                    otherCenter.y + other.Height / 2
                );

                var overlaps =
                    newMin.x < otherMax.x &&
                    newMax.x > otherMin.x &&
                    newMin.y < otherMax.y &&
                    newMax.y > otherMin.y;

                if (overlaps)
                    return false;
            }

            return true;
        }

        public RoomInstance PlaceRoom(RoomData room, Vector2Int center)
        {
            var roomInstance = new RoomInstance
            {
                position = center,
                dataRef = room
            };
            rooms.Add(roomInstance);
            return roomInstance;
        }

        public Vector2Int GetWorldSize(out Vector2Int offset)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;

            Debug.Log(rooms.Count);
            foreach (var roomInstance in rooms)
            {
                var halfW = roomInstance.dataRef.Width / 2;
                var halfH = roomInstance.dataRef.Height / 2;

                var min = new Vector2Int(
                    roomInstance.position.x - halfW,
                    roomInstance.position.y - halfH
                );

                var max = new Vector2Int(
                    roomInstance.position.x + halfW,
                    roomInstance.position.y + halfH
                );

                if (min.x < minX) minX = min.x;
                if (min.y < minY) minY = min.y;
                if (max.x > maxX) maxX = max.x;
                if (max.y > maxY) maxY = max.y;
            }

            offset = new Vector2Int(-minX, -minY);
            return new Vector2Int(maxX - minX, maxY - minY);
        }

        public Color[] GetPixels(Vector2Int size, Vector2Int offset)
        {
            var pixels = new Color[size.x * size.y];
            foreach (var roomInstance in rooms)
            {
                var roomWidth = roomInstance.dataRef.Width;
                var halfW = roomInstance.dataRef.Width / 2;
                var halfH = roomInstance.dataRef.Height / 2;
                var roomPixels = roomInstance.dataRef.GetPixels();
                for (var i = 0; i < roomPixels.Length; i++)
                {
                    var roomPixel = roomPixels[i];
                    var pixelPositionInRoom = new Vector2Int(i % roomWidth, i / roomWidth); // using room width, and height, translate flat i to vector2
                    
                    var pixelPositionInWorld = new Vector2Int(
                        roomInstance.position.x - halfW + pixelPositionInRoom.x,
                        roomInstance.position.y - halfH + pixelPositionInRoom.y
                    );
                    var pixelPositionOnTexture = pixelPositionInWorld + offset;

                    if (pixelPositionOnTexture.x >= 0 && pixelPositionOnTexture.x < size.x &&
                        pixelPositionOnTexture.y >= 0 && pixelPositionOnTexture.y < size.y)
                    {
                        var targetIndex =
                            pixelPositionOnTexture.y * size.x +
                            pixelPositionOnTexture.x; // flatten pixelPositionOnTexture
                        pixels[targetIndex] = roomPixel;
                    }
                }
            }
            return pixels;
        }
    }
}