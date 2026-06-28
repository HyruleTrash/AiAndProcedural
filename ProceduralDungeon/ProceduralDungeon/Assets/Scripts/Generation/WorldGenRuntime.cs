using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    public class WorldGenRuntime
    {
        public string currentSeed;
        public RoomType? lastHadRoomType;
        public readonly List<Room> hadRooms = new();
        public Vector2Int currentWalkDirection = Vector2Int.zero;
        public Vector2Int currentPosition = Vector2Int.zero;
        public readonly List<Area> backlog;
        public readonly List<AreaRuntime> hadAreas = new();
        public readonly Grid grid = new();
        public int walkDirRepeated = 0;
        public readonly float minDistanceBossRoom;

        public WorldGenRuntime(List<Area> areaData, string seed, float minDistanceBossRoom)
        {
            this.currentSeed = seed;
            this.backlog = new List<Area>(areaData);
            this.minDistanceBossRoom = minDistanceBossRoom;
        }

        public void AddToHadRooms(Room foundRoom, int roomRepetitionAllowance)
        {
            this.hadRooms.Add(foundRoom);
            if (this.hadRooms.Count >= roomRepetitionAllowance) this.hadRooms.RemoveRange(this.hadRooms.Count - roomRepetitionAllowance, this.hadRooms.Count);
        }
    }
}