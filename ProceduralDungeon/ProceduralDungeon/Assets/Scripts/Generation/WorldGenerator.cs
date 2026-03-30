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
        
        public struct Grid
        {
            private List<RoomInstance> rooms;

            private class RoomInstance : IComparable
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

            public RoomData GetRoomAtPosition(Vector2Int position)
            {
                var found = rooms.FirstOrDefault(x => x.position == position);
                return found?.dataRef;
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

            return grid;
        }

        private void WalkthroughAreas(ref List<AreaData> backlog, ref List<AreaGenData> hadAreas, ref Grid grid,
            ref Vector2Int position, string seed)
        {
            while (backlog.Count > 0)
            {
                var foundIndex = RNG.RandomRange(0, backlog.Count, seed);
                var pickedArea = backlog[foundIndex].GetAreaGenData(seed);
                backlog.RemoveAt(foundIndex);

                WalkthroughArea(ref pickedArea, ref grid, ref position, seed);
                
                hadAreas.Add(pickedArea);
            }
        }

        private void WalkthroughArea(ref AreaGenData pickedArea, ref Grid grid,
            ref Vector2Int position, string seed)
        {
            while (pickedArea.Size > 0)
            {
                var foundRoom = grid.GetRoomAtPosition(position);

                if (foundRoom == null)
                {
                    
                }
                else
                {
                        
                }
            }
        }
    }
}