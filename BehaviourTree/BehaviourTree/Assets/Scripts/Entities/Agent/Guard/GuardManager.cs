using System;
using System.Linq;
using BehaviourTree;
using UnityEngine;

namespace Guard
{
    [RequireComponent(typeof(HealthComponent), typeof(WeaponHandler), typeof(VisionCone)),
     RequireComponent(typeof(BehaviourTree.BehaviourTree), typeof(NavigateToPosition))]
    public class GuardManager : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField]
        private BehaviourTree.BehaviourTree behaviourTree;
        [SerializeField]
        private HealthComponent healthComponent;
        [SerializeField]
        private WeaponHandler weaponHandler;
        [SerializeField] 
        private NavigateToPosition navigateToPosition;
        [SerializeField]
        private VisionCone visionCone;
        [Space]
        [SerializeField]
        private WaypointManager waypointManager;
        [SerializeField] 
        private float attackRange;

        [HideInInspector, SerializeReference] 
        private WeaponSpawner weaponSpawnerRef;
        private GameObject[] currentlyRegisteredPlayers;

        private void OnValidate()
        {
            behaviourTree ??= GetComponent<BehaviourTree.BehaviourTree>();
            healthComponent ??= GetComponent<HealthComponent>();
            weaponHandler ??= GetComponent<WeaponHandler>();
            visionCone ??= GetComponent<VisionCone>();
            navigateToPosition ??= GetComponentInChildren<NavigateToPosition>();
            enabled = healthComponent && weaponHandler && navigateToPosition && waypointManager.OnValidate();

            weaponSpawnerRef = FindFirstObjectByType<WeaponSpawner>();
        }

        private void Update()
        {
            transform.position += navigateToPosition.transform.localPosition;
            navigateToPosition.transform.localPosition = Vector3.zero;
        }
        
        private void OnDrawGizmosSelected() => waypointManager.OnDrawGizmosSelected();

        private void OnEnable()
        {
            behaviourTree.Initialize(new SelectorNode(new INode[]
            {
                new SequenceNode(new INode[]
                {
                    new TaskNode(CanSeePlayer),
                    new ParallelNode(new INode[]
                    {
                        new ConditionNode(IsCurrentWaypointNull, isInverted: true, toExecute:
                            new TaskNode(() => SetCurrentWaypoint(null))),
                        new SelectorNode(new INode[]
                        {
                            new ConditionNode(HasWeapon, isInverted: true, toExecute:
                                new TaskNode(() => SetCurrentWaypoint(GetNearestWeaponPosition()))),
                            new SequenceNode(new INode[]
                            {
                                new InvertNode(new TaskNode(HasWeapon)),
                                new SelectorNode(new INode[]
                                {
                                    new ConditionNode(IsPlayerInAttackRange, new TaskNode(TryAttackPlayer)),
                                    new ConditionNode(IsPlayerInAttackRange, isInverted: true, toExecute: 
                                        new TaskNode(() => SetCurrentWaypoint(GetPlayerPosition())))
                                })
                            })
                        })
                    })
                }),
                new SequenceNode(new INode[]
                {
                    new InvertNode(new TaskNode(CanSeePlayer)),
                    new ParallelNode(new INode[]
                    {
                        new ConditionNode(IsCurrentWaypointNull, 
                            new TaskNode(() => SetCurrentWaypoint(GetNearestWayPoint()))),
                        new ConditionNode(IsAgentNearCurrentWaypoint, 
                            new TaskNode(() => SetCurrentWaypoint(GetNextWaypoint())))
                    })
                })
            }));
        }
        
        private bool CanSeePlayer()
        {
            currentlyRegisteredPlayers = visionCone.GetCurrentlySeenObjects();
            return currentlyRegisteredPlayers.Length > 0;
        }

        #region Waypoint
        private bool IsCurrentWaypointNull() => navigateToPosition.GetTargetPosition() == null;
        private bool SetCurrentWaypoint(Vector2? getNearestWayPoint)
        {
            navigateToPosition.SetTargetPosition(getNearestWayPoint);
            return !IsCurrentWaypointNull();
        }
        private Vector2 GetNextWaypoint() => waypointManager.GetNextWaypoint(navigateToPosition.GetTargetPosition());
        private Vector2 GetNearestWayPoint() => waypointManager.GetNearestWayPoint(transform.position.xy());
        private bool IsAgentNearCurrentWaypoint()
        {
            var currentWaypoint = navigateToPosition.GetTargetPosition();
            if (currentWaypoint == null) return false;
            return Vector2.Distance(currentWaypoint.Value, transform.position) <= waypointManager.minimumDistanceToWaypoint;
        }
        #endregion

        #region Weapon
        private bool HasWeapon() => weaponHandler.HasWeapon();
        private Vector2 GetNearestWeaponPosition() => !weaponSpawnerRef
            ? transform.position.xy()
            : weaponSpawnerRef.GetNearest(transform.position.xy()).instance.transform.position.xy();
        #endregion

        #region Player
        private Vector2 GetPlayerPosition()
        {
            if (currentlyRegisteredPlayers.Length <= 0)
                return transform.position;
            return currentlyRegisteredPlayers.First().transform.position;
        }
        private bool TryAttackPlayer()
        {
            if (currentlyRegisteredPlayers.Length <= 0)
                return false;
            var player = currentlyRegisteredPlayers.First();
            
            if (Vector2.Distance(player.transform.position, transform.position) > attackRange)
                return false;
            
            var damageable = player.GetComponent<IDamageable>();
            if (damageable == null || !damageable.CanTakeDamage()) return false;
            damageable.TakeDamage(weaponHandler.GetDamage());
            return true;
        }
        private bool IsPlayerInAttackRange()
        {
            return Vector2.Distance(GetPlayerPosition(), transform.position) <= attackRange;
        }
        #endregion
    }
}
