using System;
using System.Linq;
using BehaviourTree;
using TMPro;
using UnityEngine;

namespace Guard
{
    /// <summary>
    /// Manages a guard instance their components and connects its functions
    /// </summary>
    [RequireComponent(typeof(HealthComponent), typeof(WeaponHandler)),
     RequireComponent(typeof(BehaviourTree.BehaviourTree), typeof(Animator), typeof(Movement))]
    public class GuardManager : MonoBehaviour
    {
        private static readonly int HasWeapon1 = Animator.StringToHash("HasWeapon");
        private static readonly int Attack = Animator.StringToHash("Attack");

        [Header("Required Components")]
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private BehaviourTree.BehaviourTree behaviourTree;
        [SerializeField]
        private HealthComponent healthComponent;
        [SerializeField]
        private WeaponHandler weaponHandler;
        [SerializeField]
        private Movement movementComponent;
        [SerializeField]
        private VisionCone visionCone;
        [SerializeField]
        private RotateTowardsPoint visionConeRotation;
        [Space, Header("Config")]
        [SerializeField]
        private TextMeshPro stateText;
        [SerializeField] 
        private float lookAtSpeed = 1;
        [HideInInspector, SerializeReference] 
        private WeaponSpawner weaponSpawnerRef;
        private GameObject[] currentlyRegisteredPlayers = {};
        private Vector2 lastLookAtPosition;

        private void OnValidate()
        {
            // Get all required references
            behaviourTree ??= GetComponent<BehaviourTree.BehaviourTree>();
            healthComponent ??= GetComponent<HealthComponent>();
            weaponHandler ??= GetComponent<WeaponHandler>();
            visionCone ??= GetComponentInChildren<VisionCone>();
            visionConeRotation ??= GetComponentInChildren<RotateTowardsPoint>();
            movementComponent ??= GetComponent<Movement>();
            animator ??= GetComponent<Animator>();
            lookAtSpeed = Mathf.Clamp(lookAtSpeed, 1, float.PositiveInfinity);
            
            enabled = healthComponent &&
                      weaponHandler &&
                      visionCone &&
                      movementComponent &&
                      animator &&
                      behaviourTree;

            weaponSpawnerRef = FindFirstObjectByType<WeaponSpawner>();
        }

        private void OnEnable()
        {
            // Initialize behaviourTree
            behaviourTree.Initialize(new SelectorNode(new INode[]
            {
                new SequenceNode(new INode[]
                {
                    new TaskNode(CanSeePlayer),
                    new SelectorNode(new INode[]
                    {
                        new ConditionNode(HasWeapon, isInverted: true, toExecute:
                            new ParallelNode(new INode[]
                            {
                                new TaskNode(() => SetStateText("Searching 4 weapon")),
                                new TaskNode(() => movementComponent.SetCurrentWaypoint(GetNearestWeaponPosition()))
                            })),
                        new SequenceNode(new INode[]
                        {
                            new TaskNode(HasWeapon),
                            new TaskNode(() => SetStateText("Attacking player")),
                            new SelectorNode(new INode[]
                            {
                                new ConditionNode(IsPlayerInAttackRange, new TaskNode(TryAttackPlayer)),
                                new ConditionNode(IsPlayerInAttackRange, isInverted: true, toExecute: 
                                    new TaskNode(() => movementComponent.SetCurrentWaypoint(GetPlayerPosition())))
                            })
                        })
                    })
                }),
                new SequenceNode(new INode[]
                {
                    new InvertNode(new TaskNode(CanSeePlayer)),
                    new ConditionNode(() => movementComponent.GetCurrentWaypoint() != GetNearestWeaponPosition(),
                        new TaskNode(() => SetStateText("Patrolling"))),
                    new ParallelNode(new INode[]
                    {
                        new ConditionNode(movementComponent.IsCurrentWaypointNull, 
                            new TaskNode(() => movementComponent.SetCurrentWaypoint(movementComponent.GetNearestWayPoint()))),
                        new ConditionNode(movementComponent.IsAgentNearCurrentWaypoint, 
                            new TaskNode(() => movementComponent.SetCurrentWaypoint(movementComponent.GetNextWaypoint())))
                    })
                })
            }));
        }

        private void FixedUpdate()
        {
            lastLookAtPosition = Vector2.Lerp(lastLookAtPosition, movementComponent.GetCurrentWaypoint(), Time.fixedDeltaTime * lookAtSpeed);
            visionConeRotation.UpdateRotation(lastLookAtPosition);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastLookAtPosition, 0.1f);
        }

        /// <summary>
        /// Sets the non required state text to sometheing
        /// </summary>
        private bool SetStateText(string text)
        {
            if (stateText) stateText.text = $"State: {text}";
            return true;
        }

        #region Weapon management
        
        private bool HasWeapon()
        {
            var state = weaponHandler.HasWeapon();
            animator.SetBool(HasWeapon1, state);
            return state;
        }

        private Vector2 GetNearestWeaponPosition() => !weaponSpawnerRef
            ? transform.position.xy()
            : weaponSpawnerRef.GetNearest(transform.position.xy()).instance.transform.position.xy();
        
        #endregion
        
        #region Player related
        
        /// <summary>
        /// Registers seen players, and returns true if it saw at least one
        /// </summary>
        private bool CanSeePlayer()
        {
            if (currentlyRegisteredPlayers.Length > 0 &&
                visionCone.AreTransformsInCone(new[] { currentlyRegisteredPlayers.First() }).Length > 0)
                return true;
            currentlyRegisteredPlayers = visionCone.GetCurrentlySeenObjects();
            return currentlyRegisteredPlayers.Length > 0;
        }
        
        private Vector2? GetPlayerPosition()
        {
            if (currentlyRegisteredPlayers.Length <= 0)
                return null;
            return currentlyRegisteredPlayers.First().transform.position.xy();
        }
        
        private bool TryAttackPlayer()
        {
            if (currentlyRegisteredPlayers.Length <= 0)
                return false;
            var player = currentlyRegisteredPlayers.First();
            if (Vector2.Distance(player.transform.position, transform.position) > weaponHandler.Weapon.attackRange)
                return false;
            
            var damageable = player.GetComponent<IDamageable>();
            if (damageable == null || !damageable.CanTakeDamage()) return false;
            
            animator.SetTrigger(Attack);
            damageable.TakeDamage(weaponHandler.GetDamage());
            return true;
        }
        
        private bool IsPlayerInAttackRange()
        {
            var playerPos = GetPlayerPosition();
            if (playerPos.HasValue)
                return Vector2.Distance(playerPos.Value, transform.position) <= weaponHandler.Weapon.attackRange;
            return false;
        }

        #endregion
    }
}
