using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation
{
    /// <summary>
    /// Class used for holding live area gen data, has mutable data without breaking default editor data
    /// </summary>
    public class AreaGenData
    {
        private AreaType areaType;
        private int size;
        private List<RoomTypeDataList> roomTypes;
        public AreaType AreaType { get => areaType; private set => areaType = value; }
        public int Size { get => size; set => size = value; }
        
        public AreaGenData(AreaType areaType, int size, List<RoomTypeDataList> roomTypes)
        {
            this.areaType = areaType;
            this.size = size;
            this.roomTypes = new List<RoomTypeDataList>();
            foreach (var list in roomTypes) this.roomTypes.Add(list.Duplicate());
        }

        public RoomTypeDataList PickTypeList(WorldGenerator.WorldGenData genData)
        {
            RoomTypeDataList foundTypeList;
            var indexPool = new List<int>();
            const int resolution = 100;

            var guaranteedFound = false;

            for (var i = 0; i < roomTypes.Count; i++)
            {
                if (!Mathf.Approximately(roomTypes[i].Weight, 1f)) continue;
                indexPool.Add(i);
                guaranteedFound = true;
            }

            if (!guaranteedFound)
            {
                for (var i = 0; i < roomTypes.Count; i++)
                {
                    var count = Mathf.RoundToInt(roomTypes[i].Weight * resolution);

                    for (var j = 0; j < count; j++)
                        indexPool.Add(i);
                }
            }

            if (indexPool.Count == 0)
                throw new Exception("indexPool is empty");
            
            if (genData.lastHadRoomType != null)
            {
                var hasValid = indexPool.Any(i => roomTypes[i].roomType != genData.lastHadRoomType.Value);
                if (!hasValid)
                    throw new Exception("only non valid room types are left");
            }
            while (true)
            {
                var foundIndex = indexPool[RNG.RandomRange(0, indexPool.Count, genData.currentSeed)];
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                foundTypeList = roomTypes[foundIndex];
                
                if (foundTypeList && (genData.lastHadRoomType == null || foundTypeList.roomType != genData.lastHadRoomType.Value)) break;
            }
            return foundTypeList;
        }
    }
}