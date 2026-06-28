using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Util;

namespace Generation
{
    /// <summary>
    /// Class used for holding live area gen data, has mutable data without breaking default editor data
    /// </summary>
    public class AreaRuntime
    {
        private readonly List<TypedRoomList> roomTypes;
        private readonly RoomList endRooms;
        public AreaType AreaType { get; private set; }
        public int Size { get; set; }

        public int RoomCount => this.roomTypes.Count;
        public int EndRoomCount => this.endRooms.RoomData.Count;
        public List<Room> EndRooms => this.endRooms.RoomData;

        public AreaRuntime(AreaType areaType, int size, List<TypedRoomList> roomTypes, RoomList endRooms)
        {
            this.AreaType = areaType;
            this.Size = size;
            this.roomTypes = new List<TypedRoomList>();
            this.endRooms = new RoomList(endRooms);
            foreach (TypedRoomList list in roomTypes) this.roomTypes.Add(list.Duplicate());
        }

        public TypedRoomList PickTypeList(WorldGenRuntime genRuntime)
        {
            TypedRoomList foundTypeList;
            List<int> indexPool = new();
            const int resolution = 100;

            bool guaranteedFound = false;

            for (int i = 0; i < this.roomTypes.Count; i++)
            {
                if (!Mathf.Approximately(this.roomTypes[i].Weight, 1f)) continue;
                indexPool.Add(i);
                guaranteedFound = true;
            }

            if (!guaranteedFound)
            {
                for (int i = 0; i < this.roomTypes.Count; i++)
                {
                    int count = Mathf.RoundToInt(this.roomTypes[i].Weight * resolution);

                    for (int j = 0; j < count; j++)
                        indexPool.Add(i);
                }
            }

            if (indexPool.Count == 0)
                throw new Exception("indexPool is empty");
            
            if (genRuntime.lastHadRoomType != null)
            {
                bool hasValid = indexPool.Any(i => this.roomTypes[i].roomType != genRuntime.lastHadRoomType.Value);
                if (!hasValid)
                    throw new Exception("only non valid room types are left");
            }
            while (true)
            {
                int foundIndex = indexPool[Rng.RandomRange(0, indexPool.Count, genRuntime.currentSeed)];
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                foundTypeList = this.roomTypes[foundIndex];
                
                if (foundTypeList && (genRuntime.lastHadRoomType == null || foundTypeList.roomType != genRuntime.lastHadRoomType.Value)) break;
            }
            return foundTypeList;
        }
    }
}