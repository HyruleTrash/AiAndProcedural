using DefaultNamespace;
using UnityEngine;

[RequireComponent(typeof(Animator)), RequireComponent(typeof(ScaleBasedOnLookDirection))]
public class WalkAnimManager : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private ScaleBasedOnLookDirection scaleBasedOnLookDirection;
    private static readonly int IsWalkingIndex = Animator.StringToHash("Walking");
    private static readonly int WalkSpeedIndex = Animator.StringToHash("WalkingSpeed");
    private IEntityMovement movementComponent;

    private void OnValidate()
    {
        animator ??= GetComponent<Animator>();
        scaleBasedOnLookDirection ??= GetComponent<ScaleBasedOnLookDirection>();
        enabled = animator && scaleBasedOnLookDirection;
    }
        
    public void Connect(IEntityMovement movement)
    {
        movementComponent = movement;
        movementComponent.IsMovingChanged.AddListener(SetWalkingAnimation);
        scaleBasedOnLookDirection.directionChanged += ChangeWalkingSpeed;
    }
        
    public void Disconnect() => movementComponent.IsMovingChanged.RemoveListener(SetWalkingAnimation);
    private void SetWalkingAnimation(bool isWalking) => animator.SetBool(IsWalkingIndex, isWalking);
    private void ChangeWalkingSpeed() => animator.SetFloat(WalkSpeedIndex, scaleBasedOnLookDirection.flipped ? -1f : 1f);
}