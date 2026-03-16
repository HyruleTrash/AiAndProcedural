using System;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Events;

namespace Guard
{
    [RequireComponent(typeof(Rigidbody2D),  typeof(LookDirectionManager), typeof(WalkAnimManager))]
    public class Movement : MonoBehaviour, IEntityMovement
    {
        [Header("Required Components")]
        [SerializeField] 
        private NavigateToPosition navigateToPosition;
        [SerializeField]
        private WalkAnimManager walkAnimManager;
        [SerializeField]
        private WaypointManager waypointManager;
        [SerializeField]
        private LookDirectionManager lookDirectionManager;
        [SerializeField]
        private Rigidbody2D rb;
        [Header("Events"), Space]
        public UnityEvent<bool> isMovingChanged;
        public UnityEvent<bool> IsMovingChanged => isMovingChanged;
        private bool isMoving = false;

        private void OnValidate()
        {
            lookDirectionManager ??= GetComponent<LookDirectionManager>();
            rb ??= GetComponent<Rigidbody2D>();
            navigateToPosition ??= GetComponentInChildren<NavigateToPosition>();
            
            enabled = navigateToPosition &&
                      waypointManager.OnValidate() && 
                      rb &&
                      lookDirectionManager;
            
            walkAnimManager ??= GetComponent<WalkAnimManager>();
        }
        
        private void FixedUpdate()
        {
            // move to navAgent, and remove this offset
            rb.MovePosition(rb.position + (Vector2)navigateToPosition.transform.localPosition);
            navigateToPosition.transform.localPosition = Vector3.zero;

            // set look direction
            var target = navigateToPosition.GetTargetPosition();
            var newIsMoving = false;
            if (target.HasValue && Vector2.Distance(target.Value, transform.position) > 0.1f)
            {
                newIsMoving = true;
                lookDirectionManager.SetLookAt(target.Value);
            }

            if (newIsMoving == isMoving) return;
            isMoving = newIsMoving;
            isMovingChanged.Invoke(newIsMoving);
        }
        
        private void OnDrawGizmosSelected() => waypointManager.OnDrawGizmosSelected();
        private void Start() => walkAnimManager?.Connect(this);
        private void OnDestroy() => walkAnimManager?.Disconnect();

        #region WaypointManager
        
        public bool IsCurrentWaypointNull() => navigateToPosition.GetTargetPosition() == null;
        
        public bool SetCurrentWaypoint(Vector2? getNearestWayPoint)
        {
            navigateToPosition.SetTargetPosition(getNearestWayPoint);
            return !IsCurrentWaypointNull();
        }
        
        public Vector2 GetNextWaypoint() => waypointManager.GetNextWaypoint(navigateToPosition.GetTargetPosition());
        
        public Vector2 GetNearestWayPoint() => waypointManager.GetNearestWayPoint(transform.position.xy());
        
        public bool IsAgentNearCurrentWaypoint()
        {
            var currentWaypoint = navigateToPosition.GetTargetPosition();
            if (currentWaypoint == null) return false;
            return Vector2.Distance(currentWaypoint.Value, transform.position) <= waypointManager.minimumDistanceToWaypoint;
        }
        
        public Vector2 GetCurrentWaypoint()
        {
            var target = navigateToPosition.GetTargetPosition();
            if (target == null) return transform.position;
            return target.Value;
        }
        
        #endregion
    }
}