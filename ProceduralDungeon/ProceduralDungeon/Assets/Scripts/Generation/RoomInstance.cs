using System;
using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    /// <summary>
    /// a class for keeping track of a unique live room, using a room data as its reference
    /// </summary>
    public class RoomInstance : IComparable
    {
        public Vector2Int position;
        public RoomData dataRef;
        private List<string> mutations = new();
                
        public int CompareTo(object obj)
        {
            if (obj is RoomInstance other)
                return (position.y.CompareTo(other.position.y) + position.x.CompareTo(other.position.x)) / 2;
            return 0;
        }

        public void RemoveDoorFromLayout(RoomData.DoorPointGroup doorway)
        {
            var lookup = RoomTileLookup.LookupInstance;
            var emptyChar = lookup.GetTile("Empty");
            if (emptyChar == null)
                throw new Exception("Empty char not found");
            
            var mutation = new char[dataRef.Size];
            Array.Fill(mutation, '-');
            
            foreach (var point in doorway.points)
            {
                var targetIndex = point.y * dataRef.Width + point.x;
                mutation[targetIndex] = emptyChar.Value.text;
            }
            
            mutations.Add(new string(mutation));
        }

        private string GetLayout()
        {
            var layout = dataRef.Layout.ToCharArray();
            foreach (var mutation in mutations)
            {
                for (var i = 0; i < mutation.Length; i++)
                {
                    var c = mutation[i];
                    if (c == '-')
                        continue;
                    layout[i] = c;
                }
            }

            return new string(layout);
        }
        
        public void MutateRemoveLeftDoorPixels()
        {
            var lookup = RoomTileLookup.LookupInstance;
            
            var wallChar = lookup.GetTile("Wall");
            if (wallChar == null)
                throw new Exception("Empty char not found");
            var doorChar = lookup.GetTile("Doorway");
            if (doorChar == null)
                throw new Exception("Doorway char not found");
            
            var existingLayout = GetLayout().ToCharArray();
            var mutation = new char[dataRef.Size];
            Array.Fill(mutation, '-');

            for (var i = 0; i < existingLayout.Length; i++)
            {
                var c = existingLayout[i];
                if (c == doorChar.Value.text)
                    mutation[i] = wallChar.Value.text;
            }

            mutations.Add(new string(mutation));
        }

        public Color[] GetPixels() => RoomData.GetPixels(GetLayout(), dataRef.Width, dataRef.Height);
    }
}