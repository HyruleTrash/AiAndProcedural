using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class WorldGenerator
    {
        private string currentSeed;
        [SerializeField]
        private List<AreaData> areaData;
        [SerializeField]
        private int roomRepetitionAllowance = 2;
        
        public static RoomType lastHadRoomType;
        public static List<RoomData> hadRooms = new();
        public static Vector2Int currentWalkDirection = Vector2Int.zero;
        
        public class Grid
        {
            private List<RoomInstance> rooms = new();

            public RoomData GetRoomAtPosition(Vector2Int position)
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
                        return room;
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

        public Grid Generate(string seed)
        {
            currentSeed = seed;
            List<AreaData> backlog = new(areaData);
            List<AreaGenData> hadAreas = new();
            var grid = new Grid();
            var position = Vector2Int.zero;

            WalkthroughAreas(ref backlog, ref hadAreas, ref grid, ref position, seed);

            // TODO spawn bossroom
            
            return grid;
        }

        private void WalkthroughAreas(ref List<AreaData> backlog, ref List<AreaGenData> hadAreas, ref Grid grid,
            ref Vector2Int position, string seed)
        {
            while (backlog.Count > 0)
            {
                var foundIndex = RNG.RandomRange(0, backlog.Count, seed);
                seed = RNG.MutateNext(seed);
                
                var pickedArea = backlog[foundIndex].GetAreaGenData(seed);
                seed = RNG.MutateNext(seed);
                
                backlog.RemoveAt(foundIndex);

                WalkthroughArea(ref pickedArea, ref grid, ref position, ref seed);
                
                hadAreas.Add(pickedArea);
            }
        }

        private void WalkthroughArea(ref AreaGenData pickedArea, ref Grid grid,
            ref Vector2Int position, ref string seed)
        {
            while (pickedArea.Size > 0)
            {
                var foundRoom = grid.GetRoomAtPosition(position);

                if (foundRoom == null)
                {
                    var createdRoom = AddRoom(ref pickedArea, ref grid, position, ref seed);
                    // foundRoom = createdRoom.dataRef;
                    seed = RNG.MutateNext(seed);
                    
                    position = createdRoom.position;
                }
                // TODO add doorway to last been to existing room

                if (pickedArea.Size <= 0)
                    break;
                
                // TODO implement walk
            }
        }

        private RoomInstance AddRoom(ref AreaGenData pickedArea, ref Grid grid, Vector2Int position, ref string seed)
        {
            var pickedTypeList = pickedArea.PickTypeList(ref seed);
            pickedTypeList.OnPicked(pickedArea);
            seed = RNG.MutateNext(seed);

            RoomData foundRoom;
            Vector2Int center;
            while (true)
            {
                foundRoom = pickedTypeList.TryGetRoom(
                    RNG.RandomRange(pickedTypeList.smallestRoomSize, pickedArea.Size, seed), 
                    ref seed,
                    hadRooms.ToArray());
                
                center = position + new Vector2Int(
                    currentWalkDirection.x * (foundRoom.Width / 2),
                    currentWalkDirection.y * (foundRoom.Height / 2)
                );
                if (grid.CheckRoomPossible(foundRoom, center))
                    break;
            }
            
            var placedRoom = grid.PlaceRoom(foundRoom, center);
            
            hadRooms.Add(foundRoom);
            if (hadRooms.Count >= roomRepetitionAllowance)
                hadRooms.RemoveRange(hadRooms.Count - roomRepetitionAllowance, hadRooms.Count);
            
            pickedArea.Size -= foundRoom.Size;
            lastHadRoomType = pickedTypeList.roomType; // TODO: make allowance unique per type per area?

            return placedRoom;
        }
    }
}