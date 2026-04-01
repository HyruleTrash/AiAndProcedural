using System;
using System.Collections.Generic;
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

                var min = new Vector2Int(
                    center.x - room.Width / 2,
                    center.y - room.Height / 2
                );
                var max = new Vector2Int(
                    center.x + room.Width / 2,
                    center.y + room.Height / 2
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
    }
}