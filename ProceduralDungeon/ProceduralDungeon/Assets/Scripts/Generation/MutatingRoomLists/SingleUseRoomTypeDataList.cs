using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(fileName = "SingleUse", menuName = "Generation/SingleUse")]
    public class SingleUseRoomTypeDataList : RoomTypeDataList
    {
        public override void OnPicked(AreaGenData pickedArea)
        {
            Weight = 0;
        }
    }
}