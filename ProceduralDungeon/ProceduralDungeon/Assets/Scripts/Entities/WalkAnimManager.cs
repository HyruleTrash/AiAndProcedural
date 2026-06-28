using DefaultNamespace;
using UnityEngine;

[RequireComponent(typeof(Animator)), RequireComponent(typeof(ScaleBasedOnLookDirection))]
public class WalkAnimManager : MonoBehaviour
{
    [SerializeField] private Animator animator = null!;
    [SerializeField] private ScaleBasedOnLookDirection scaleBasedOnLookDirection = null!;
    private static readonly int IsWalkingIndex = Animator.StringToHash("Walking");
    private static readonly int WalkSpeedIndex = Animator.StringToHash("WalkingSpeed");
    private IEntityMovement movementComponent = null!;

    private void OnValidate()
    {
        this.animator ??= GetComponent<Animator>();
        this.scaleBasedOnLookDirection ??= GetComponent<ScaleBasedOnLookDirection>();
        this.enabled = this.animator && this.scaleBasedOnLookDirection;
    }
        
    public void Connect(IEntityMovement movement)
    {
        this.movementComponent = movement;
        this.movementComponent.IsMovingChanged.AddListener(SetWalkingAnimation);
        this.scaleBasedOnLookDirection.directionChanged += ChangeWalkingSpeed;
    }
        
    public void Disconnect() => this.movementComponent.IsMovingChanged.RemoveListener(SetWalkingAnimation);
    private void SetWalkingAnimation(bool isWalking) => this.animator.SetBool(IsWalkingIndex, isWalking);
    private void ChangeWalkingSpeed() => this.animator.SetFloat(WalkSpeedIndex, this.scaleBasedOnLookDirection.flipped ? -1f : 1f);
}