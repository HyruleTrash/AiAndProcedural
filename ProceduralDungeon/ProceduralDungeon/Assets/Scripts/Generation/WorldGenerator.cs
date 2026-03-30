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
        [SerializeField]
        private int walkDirectionRepetitionAllowance = 2;
        
        public static readonly Vector2Int[] CardinalDirections = new Vector2Int[]
        {
            Vector2Int.up,    // (0, 1)
            Vector2Int.down,  // (0, -1)
            Vector2Int.left,  // (-1, 0)
            Vector2Int.right  // (1, 0)
        };
        
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

        public class WorldGenData
        {
            public RoomType lastHadRoomType;
            public List<RoomData> hadRooms = new();
            public Vector2Int currentWalkDirection = Vector2Int.zero;
            public Vector2Int currentPosition = Vector2Int.zero;
            public List<AreaData> backlog;
            public List<AreaGenData> hadAreas = new();
            public Grid grid = new();
            public int walkDirRepeated = 0;

            public WorldGenData(List<AreaData> areaData) => backlog = new List<AreaData>(areaData);

            public void AddToHadRooms(RoomData foundRoom, int roomRepetitionAllowance)
            {
                hadRooms.Add(foundRoom);
                if (hadRooms.Count >= roomRepetitionAllowance)
                    hadRooms.RemoveRange(hadRooms.Count - roomRepetitionAllowance, hadRooms.Count);
            }
        }

        public Grid Generate(string seed)
        {
            currentSeed = seed;
            var genData = new WorldGenData(areaData);

            WalkthroughAreas(ref genData, seed);

            // TODO spawn bossroom
            
            return genData.grid;
        }

        private void WalkthroughAreas(ref WorldGenData genData, string seed)
        {
            while (genData.backlog.Count > 0)
            {
                var foundIndex = RNG.RandomRange(0, genData.backlog.Count, seed);
                seed = RNG.MutateNext(seed);
                
                var pickedArea = genData.backlog[foundIndex].GetAreaGenData(seed);
                seed = RNG.MutateNext(seed);
                
                genData.backlog.RemoveAt(foundIndex);
                WalkthroughArea(ref genData, ref pickedArea, ref seed);
                genData.hadAreas.Add(pickedArea);
            }
        }

        private void WalkthroughArea(ref WorldGenData genData, ref AreaGenData pickedArea, ref string seed)
        {
            while (pickedArea.Size > 0)
            {
                var foundRoom = genData.grid.GetRoomAtPosition(genData.currentPosition);

                if (foundRoom == null)
                {
                    foundRoom = AddRoom(ref genData, ref pickedArea, ref seed);
                    seed = RNG.MutateNext(seed);
                }
                genData.currentPosition = foundRoom.position;
                
                // TODO add doorway to last been to existing room

                if (pickedArea.Size <= 0)
                    break;
                
                Walk(ref genData, foundRoom, ref seed);
            }
        }

        private void Walk(ref WorldGenData genData, RoomInstance currentRoom, ref string seed)
        {
            while (true)
            {
                var index = RNG.RandomRange(0, CardinalDirections.Length, seed);
                seed = RNG.MutateNext(seed);

                if (CardinalDirections[index] == genData.currentWalkDirection)
                    genData.walkDirRepeated++;
                else
                    genData.walkDirRepeated = 0;

                if (genData.walkDirRepeated > walkDirectionRepetitionAllowance) continue;
                
                genData.currentWalkDirection = CardinalDirections[index];
                break;
            }
            
            // Move to edge of room
            genData.currentPosition += new Vector2Int(
                genData.currentWalkDirection.x * (currentRoom.dataRef.Width / 2),
                genData.currentWalkDirection.y * (currentRoom.dataRef.Height / 2)
            );
        }

        private RoomInstance AddRoom(ref WorldGenData genData, ref AreaGenData pickedArea, ref string seed)
        {
            var pickedTypeList = pickedArea.PickTypeList(ref genData, ref seed);
            pickedTypeList.OnPicked(pickedArea);
            seed = RNG.MutateNext(seed);

            RoomData foundRoom;
            Vector2Int center;
            while (true)
            {
                foundRoom = pickedTypeList.TryGetRoom(
                    RNG.RandomRange(pickedTypeList.smallestRoomSize, pickedArea.Size, seed), 
                    ref seed,
                    genData.hadRooms.ToArray());
                
                // TODO, foundRoom is null due to there being no rooms small enough to fill the needed area size
                // TODO add a fallback for this
                
                center = genData.currentPosition + new Vector2Int(
                    genData.currentWalkDirection.x * (foundRoom.Width / 2),
                    genData.currentWalkDirection.y * (foundRoom.Height / 2)
                );
                if (genData.grid.CheckRoomPossible(foundRoom, center))
                    break;
            }
            
            var placedRoom = genData.grid.PlaceRoom(foundRoom, center);

            genData.AddToHadRooms(foundRoom, roomRepetitionAllowance);
            
            pickedArea.Size -= foundRoom.Size;
            genData.lastHadRoomType = pickedTypeList.roomType; // TODO: make allowance unique per type per area?

            return placedRoom;
        }
    }
}