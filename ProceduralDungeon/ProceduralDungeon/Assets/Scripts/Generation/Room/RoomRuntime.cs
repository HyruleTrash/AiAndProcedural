using System;
using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    /// <summary>
    /// a class for keeping track of a unique live room, using a room data as its reference
    /// </summary>
    public class RoomRuntime : IComparable
    {
        public AreaType areaType;
        public RoomType roomType;
        public Vector2Int position;
        public Room roomRef = null!;
        private readonly List<string> mutations = new();
                
        public int CompareTo(object obj)
        {
            if (obj is not RoomRuntime other) return 0;
            return (this.position.y.CompareTo(other.position.y) + this.position.x.CompareTo(other.position.x)) / 2;
        }

        public void RemoveDoorFromLayout(Room.DoorPointGroup doorway)
        {
            RoomTileLookup? lookup = RoomTileLookup.LookupInstance;
            RoomTile? emptyChar = lookup?.GetTile("Empty");
            if (emptyChar == null)
                throw new Exception("Empty char not found");
            
            char[] mutation = new char[this.roomRef.Size];
            Array.Fill(mutation, '-');
            
            foreach (Vector2Int point in doorway.points)
            {
                int targetIndex = point.y * this.roomRef.Width + point.x;
                mutation[targetIndex] = emptyChar.Value.text;
            }

            this.mutations.Add(new string(mutation));
        }

        private string GetLayout()
        {
            char[] layout = this.roomRef.Layout.ToCharArray();
            foreach (string mutation in this.mutations)
            {
                for (int i = 0; i < mutation.Length; i++)
                {
                    char c = mutation[i];
                    if (c == '-')
                        continue;
                    layout[i] = c;
                }
            }

            return new string(layout);
        }
        
        public void MutateRemoveLeftDoorPixels()
        {
            RoomTileLookup? lookup = RoomTileLookup.LookupInstance;
            if (!lookup) return;
            
            RoomTile? wallChar = lookup.GetTile("Wall");
            if (wallChar == null)
                throw new Exception("Empty char not found");
            RoomTile? doorChar = lookup.GetTile("Doorway");
            if (doorChar == null)
                throw new Exception("Doorway char not found");
            
            char[] existingLayout = GetLayout().ToCharArray();
            char[] mutation = new char[this.roomRef.Size];
            Array.Fill(mutation, '-');

            for (int i = 0; i < existingLayout.Length; i++)
            {
                char c = existingLayout[i];
                if (c == doorChar.Value.text)
                    mutation[i] = wallChar.Value.text;
            }

            this.mutations.Add(new string(mutation));
        }

        public Color[] GetPixels() => Room.GetPixels(GetLayout(), this.roomRef.Width, this.roomRef.Height, this.areaType, this.roomType);
    }
}