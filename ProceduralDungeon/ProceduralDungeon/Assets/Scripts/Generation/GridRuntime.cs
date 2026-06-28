using System;
using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    /// <summary>
    /// A container for runtime rooms
    /// </summary>
    public class GridRuntime
    {
        private readonly List<RoomRuntime> rooms = new();

        /// <summary>
        /// Gets the room that the position is currently in
        /// </summary>
        /// <param name="position">position inside bounds</param>
        /// <returns>null or the room its inside</returns>
        public RoomRuntime? GetRoomAtPosition(Vector2Int position)
        {
            foreach (RoomRuntime instance in this.rooms)
            {
                Vector2Int center = instance.position;
                Room room = instance.roomRef;

                Vector2 min = new(
                    center.x - room.Width / 2f + 1f,
                    center.y - room.Height / 2f + 1f
                );
                Vector2 max = new(
                    center.x + room.Width / 2f - 1f,
                    center.y + room.Height / 2f - 1f
                );

                bool inside =
                    position.x >= min.x &&
                    position.x <= max.x &&
                    position.y >= min.y &&
                    position.y <= max.y;

                if (inside) return instance;
            }

            return null;
        }

        public bool CheckRoomPossible(Room roomToCheck, Vector2Int center, out RoomRuntime? firstHit)
        {
            Vector2Int newMin = new(
                center.x - roomToCheck.Width / 2,
                center.y - roomToCheck.Height / 2
            );
            Vector2Int newMax = new(
                center.x + roomToCheck.Width / 2,
                center.y + roomToCheck.Height / 2
            );

            foreach (RoomRuntime instance in this.rooms)
            {
                Vector2Int otherCenter = instance.position;
                Room other = instance.roomRef;

                Vector2Int otherMin = new(
                    otherCenter.x - other.Width / 2,
                    otherCenter.y - other.Height / 2
                );

                Vector2Int otherMax = new(
                    otherCenter.x + other.Width / 2,
                    otherCenter.y + other.Height / 2
                );

                bool overlaps =
                    newMin.x < otherMax.x &&
                    newMax.x > otherMin.x &&
                    newMin.y < otherMax.y &&
                    newMax.y > otherMin.y;

                if (!overlaps) continue;
                firstHit = instance;
                return false;
            }

            firstHit = null;
            return true;
        }

        public RoomRuntime PlaceRoom(Room room, Vector2Int center)
        {
            RoomRuntime roomRuntime = new()
            {
                position = center,
                roomRef = room
            };
            this.rooms.Add(roomRuntime);
            return roomRuntime;
        }

        public Vector2Int GetWorldSize(out Vector2Int offset)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (RoomRuntime roomInstance in this.rooms)
            {
                int halfW = roomInstance.roomRef.Width / 2;
                int halfH = roomInstance.roomRef.Height / 2;

                Vector2Int min = new(
                    roomInstance.position.x - halfW,
                    roomInstance.position.y - halfH
                );

                Vector2Int max = new(
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
            Color[] pixels = new Color[size.x * size.y];
            foreach (RoomRuntime roomInstance in this.rooms)
            {
                int roomWidth = roomInstance.roomRef.Width;
                int halfW = roomInstance.roomRef.Width / 2;
                int halfH = roomInstance.roomRef.Height / 2;
                Color[] roomPixels = roomInstance.GetPixels();
                for (int i = 0; i < roomPixels.Length; i++)
                {
                    Color roomPixel = roomPixels[i];
                    Vector2Int pixelPositionInRoom = new(i % roomWidth, i / roomWidth); // using room width, and height, translate flat i to vector2
                    
                    Vector2Int pixelPositionInWorld = new(
                        roomInstance.position.x - halfW + pixelPositionInRoom.x,
                        roomInstance.position.y - halfH + pixelPositionInRoom.y
                    );
                    Vector2Int pixelPositionOnTexture = pixelPositionInWorld + offset;

                    if (pixelPositionOnTexture.x >= 0 && pixelPositionOnTexture.x < size.x &&
                        pixelPositionOnTexture.y >= 0 && pixelPositionOnTexture.y < size.y)
                    {
                        int targetIndex =
                            pixelPositionOnTexture.y * size.x +
                            pixelPositionOnTexture.x; // flatten pixelPositionOnTexture
                        pixels[targetIndex] = roomPixel;
                    }
                }
            }
            return pixels;
        }

        public void RemoveUnusedDoorways()
        {
            foreach (RoomRuntime roomInstance in this.rooms) roomInstance.MutateRemoveLeftDoorPixels();
        }

        public void Clear() => this.rooms.Clear();
    }
}