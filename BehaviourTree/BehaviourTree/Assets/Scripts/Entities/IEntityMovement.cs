using UnityEngine.Events;

namespace DefaultNamespace
{
    public interface IEntityMovement
    {
        UnityEvent<bool> IsMovingChanged { get; }
    }
}