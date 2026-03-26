using System;
using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class WorldGenerator
    {
        private string currentSeed;
        [SerializeField]
        private List<AreaData> areaData;

        // public RoomData[,] Generate(string seed)
        // {
        //     
        // }
    }
}