using UnityEngine;

namespace Generation
{
    /// <summary>
    /// Container classes to deal with coroutines not allowing out parameters
    /// Represents a getter room result
    /// </summary>
    public class GetRoomResult
    {
        public Room? foundRoom;
        public Vector2Int? center;
        public Room.DoorPointGroup? doorGroup;
    }
}