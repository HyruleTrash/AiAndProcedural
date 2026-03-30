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
        private List<RoomDataList> roomTypes;
        
        public AreaGenData(AreaType areaType, int size, List<RoomDataList> roomTypes)
        {
            this.areaType = areaType;
            this.size = size;
            this.roomTypes = new List<RoomDataList>(roomTypes);
        }
    }
}