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
        public int Size { get => size; set => size = value; }
        private List<RoomTypeDataList> roomTypes;
        
        public AreaGenData(AreaType areaType, int size, List<RoomTypeDataList> roomTypes)
        {
            this.areaType = areaType;
            this.size = size;
            this.roomTypes = new List<RoomTypeDataList>();
            foreach (var list in roomTypes) this.roomTypes.Add(new(list));
        }

        public RoomTypeDataList PickTypeList(ref WorldGenerator.WorldGenData genData, ref string seed)
        {
            RoomTypeDataList foundTypeList;
            while (true)
            {
                var foundIndex = RNG.RandomRange(0, roomTypes.Count, seed);
                seed = RNG.MutateNext(seed);
                foundTypeList = roomTypes[foundIndex];
                
                if (foundTypeList && foundTypeList.roomType != genData.lastHadRoomType) break;
            }
            return foundTypeList;
        }
    }
}