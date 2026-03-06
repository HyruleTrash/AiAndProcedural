using System;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Animator)), RequireComponent(typeof(ScaleBasedOnLookDirection))]
    public class WalkAnimManager : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private ScaleBasedOnLookDirection scaleBasedOnLookDirection;
        private static readonly int IsWalkingIndex = Animator.StringToHash("Walking");
        private static readonly int WalkSpeedIndex = Animator.StringToHash("WalkingSpeed");
        private Movement movementComponent;

        private void OnValidate()
        {
            animator ??= GetComponent<Animator>();
            scaleBasedOnLookDirection ??= GetComponent<ScaleBasedOnLookDirection>();
            enabled = animator && scaleBasedOnLookDirection;
        }
        
        public void Connect(Movement movement)
        {
            movementComponent = movement;
            movementComponent.isMovingChanged.AddListener(SetWalkingAnimation);
            scaleBasedOnLookDirection.directionChanged += ChangeWalkingSpeed;
        }
        
        public void Disconnect() => movementComponent.isMovingChanged.RemoveListener(SetWalkingAnimation);
        private void SetWalkingAnimation(bool isWalking) => animator.SetBool(IsWalkingIndex, isWalking);
        private void ChangeWalkingSpeed() => animator.SetFloat(WalkSpeedIndex, scaleBasedOnLookDirection.flipped ? -1f : 1f);
    }
}