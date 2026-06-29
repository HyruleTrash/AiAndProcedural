using DefaultNamespace;
using UnityEngine;
using UnityEngine.Events;
using Util;

namespace Guard
{
    [RequireComponent(typeof(Rigidbody2D),  typeof(LookDirectionManager), typeof(WalkAnimManager))]
    public class Movement : MonoBehaviour, IEntityMovement
    {
        [Header("Required Components")]
        [SerializeField] private NavigateToPosition navigateToPosition = null!;
        [SerializeField] private WalkAnimManager walkAnimManager = null!;
        [SerializeField] private WaypointManager waypointManager = null!;
        [SerializeField] private LookDirectionManager lookDirectionManager = null!;
        [SerializeField] private Rigidbody2D rb = null!;
        [Header("Events"), Space]
        public UnityEvent<bool> isMovingChanged = null!;
        public UnityEvent<bool> IsMovingChanged => this.isMovingChanged;
        private bool isMoving;

        private void OnValidate()
        {
            this.lookDirectionManager ??= GetComponent<LookDirectionManager>();
            this.rb ??= GetComponent<Rigidbody2D>();
            this.navigateToPosition ??= GetComponentInChildren<NavigateToPosition>();

            this.enabled = this.navigateToPosition && this.waypointManager.OnValidate() && this.rb && this.lookDirectionManager;

            this.walkAnimManager ??= GetComponent<WalkAnimManager>();
        }
        
        private void FixedUpdate()
        {
            // move to navAgent, and remove this offset
            this.rb.MovePosition(this.rb.position + (Vector2)this.navigateToPosition.transform.localPosition);
            this.navigateToPosition.transform.localPosition = Vector3.zero;

            // set look direction
            Vector2? target = this.navigateToPosition.GetTargetPosition();
            bool newIsMoving = false;
            if (target.HasValue && Vector2.Distance(target.Value, this.transform.position) > 0.1f)
            {
                newIsMoving = true;
                this.lookDirectionManager.SetLookAt(target.Value);
            }

            if (newIsMoving == this.isMoving) return;
            this.isMoving = newIsMoving;
            this.isMovingChanged.Invoke(newIsMoving);
        }
        
        private void OnDrawGizmosSelected() => this.waypointManager.OnDrawGizmosSelected();
        private void Start() => this.walkAnimManager?.Connect(this);
        private void OnDestroy() => this.walkAnimManager?.Disconnect();

        #region WaypointManager
        
        public bool IsCurrentWaypointNull() => this.navigateToPosition.GetTargetPosition() == null;
        
        public bool SetCurrentWaypoint(Vector2? getNearestWayPoint)
        {
            this.navigateToPosition.SetTargetPosition(getNearestWayPoint);
            return !IsCurrentWaypointNull();
        }
        
        public Vector2 GetNextWaypoint() => this.waypointManager.GetNextWaypoint(this.navigateToPosition.GetTargetPosition());
        
        public Vector2 GetNearestWayPoint() => this.waypointManager.GetNearestWayPoint(this.transform.position.XY());
        
        public bool IsAgentNearCurrentWaypoint()
        {
            Vector2? currentWaypoint = this.navigateToPosition.GetTargetPosition();
            if (currentWaypoint == null) return false;
            return Vector2.Distance(currentWaypoint.Value, this.transform.position) <= this.waypointManager.minimumDistanceToWaypoint;
        }
        
        public Vector2 GetCurrentWaypoint()
        {
            Vector2? target = this.navigateToPosition.GetTargetPosition();
            if (target == null) return this.transform.position;
            return target.Value;
        }
        
        #endregion
    }
}