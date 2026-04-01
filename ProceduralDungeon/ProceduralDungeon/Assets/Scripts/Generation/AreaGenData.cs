using System.Collections.Generic;

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

            for (var i = 0; i < roomTypes.Count; i++) // TODO pool creation from weight can be turned into a interface abstract method
            {
                var list = roomTypes[i];
                var counter = 0f;
                while (true)
                {
                    indexPool.Add(i);
                    counter += list.Weight;
                    if (counter >= 1f)
                        break;
                }
            }

            while (true)
            {
                var foundIndex = indexPool[RNG.RandomRange(0, indexPool.Count, genData.currentSeed)];
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                foundTypeList = roomTypes[foundIndex];
                
                if (foundTypeList && foundTypeList.roomType != genData.lastHadRoomType) break;
            }
            return foundTypeList;
        }
    }
}