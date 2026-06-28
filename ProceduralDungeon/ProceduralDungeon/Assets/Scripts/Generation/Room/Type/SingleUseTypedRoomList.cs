using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(fileName = "SingleUse", menuName = "Generation/SingleUse")]
    public class SingleUseTypedRoomList : TypedRoomList
    {
        public override void OnPicked(AreaRuntime pickedArea) => this.Weight = 0;
    }
}