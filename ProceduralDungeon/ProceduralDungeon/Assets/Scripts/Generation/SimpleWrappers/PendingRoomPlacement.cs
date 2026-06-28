using UnityEngine;

namespace Generation
{
    /// <summary>
    /// Container classes to deal with coroutines not allowing out parameters
    /// Represents a possible room
    /// </summary>
    public class PendingRoomPlacement
    {
        public Room? possibleRoom;
        public RoomType possibleRoomType;
        public Vector2Int? center;
        public Room.DoorPointGroup? doorGroup;
    }
}