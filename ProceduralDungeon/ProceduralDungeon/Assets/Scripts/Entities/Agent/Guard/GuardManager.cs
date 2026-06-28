using System;
using System.Linq;
using BehaviourTree;
using TMPro;
using UnityEngine;
using Util;

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
            this.behaviourTree ??= GetComponent<BehaviourTree.BehaviourTree>();
            this.healthComponent ??= GetComponent<HealthComponent>();
            this.weaponHandler ??= GetComponent<WeaponHandler>();
            this.visionCone ??= GetComponentInChildren<VisionCone>();
            this.visionConeRotation ??= GetComponentInChildren<RotateTowardsPoint>();
            this.movementComponent ??= GetComponent<Movement>();
            this.animator ??= GetComponent<Animator>();
            this.lookAtSpeed = Mathf.Clamp(this.lookAtSpeed, 1, float.PositiveInfinity);

            this.enabled = this.healthComponent && this.weaponHandler && this.visionCone && this.movementComponent && this.animator && this.behaviourTree;

            this.weaponSpawnerRef = FindFirstObjectByType<WeaponSpawner>();
        }

        private void OnEnable()
        {
            // Initialize behaviourTree
            SelectorNode sawPlayer = new(new INode[]
            {
                new ConditionNode(HasWeapon, isInverted: true, toExecute:
                    new ParallelNode(new INode[]
                    {
                        new TaskNode(() => SetStateText("Searching 4 weapon")),
                        new TaskNode(() => this.movementComponent.SetCurrentWaypoint(GetNearestWeaponPosition()))
                    })),
                new SequenceNode(new INode[]
                {
                    new TaskNode(HasWeapon),
                    new TaskNode(() => SetStateText("Attacking player")),
                    new SelectorNode(new INode[]
                    {
                        new ConditionNode(IsPlayerInAttackRange, new TaskNode(TryAttackPlayer)),
                        new ConditionNode(IsPlayerInAttackRange, isInverted: true, toExecute:
                            new TaskNode(() => this.movementComponent.SetCurrentWaypoint(GetPlayerPosition())))
                    })
                })
            });

            ParallelNode cantSeePlayer = new(new INode[]
            {
                new ConditionNode(this.movementComponent.IsCurrentWaypointNull, 
                    new TaskNode(() => this.movementComponent.SetCurrentWaypoint(this.movementComponent.GetNearestWayPoint()))),
                new ConditionNode(this.movementComponent.IsAgentNearCurrentWaypoint, 
                    new TaskNode(() => this.movementComponent.SetCurrentWaypoint(this.movementComponent.GetNextWaypoint())))
            });

            this.behaviourTree.Initialize(new SelectorNode(new INode[]
            {
                new SequenceNode(new INode[]
                {
                    new TaskNode(CanSeePlayer),
                    sawPlayer
                }),
                new SequenceNode(new INode[]
                {
                    new InvertNode(new TaskNode(CanSeePlayer)),
                    new ConditionNode(() => this.movementComponent.GetCurrentWaypoint() != GetNearestWeaponPosition(),
                        new TaskNode(() => SetStateText("Patrolling"))),
                    cantSeePlayer
                })
            }));
        }

        private void FixedUpdate()
        {
            this.lastLookAtPosition = Vector2.Lerp(this.lastLookAtPosition, this.movementComponent.GetCurrentWaypoint(), Time.fixedDeltaTime * this.lookAtSpeed);
            this.visionConeRotation.UpdateRotation(this.lastLookAtPosition);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(this.lastLookAtPosition, 0.1f);
        }

        /// <summary>
        /// Sets the non required state text to sometheing
        /// </summary>
        private bool SetStateText(string text)
        {
            if (this.stateText) this.stateText.text = $"State: {text}";
            return true;
        }

        #region Weapon management
        
        private bool HasWeapon()
        {
            bool state = this.weaponHandler.HasWeapon();
            this.animator.SetBool(HasWeapon1, state);
            return state;
        }

        private Vector2 GetNearestWeaponPosition() => !this.weaponSpawnerRef
            ? this.transform.position.xy()
            : this.weaponSpawnerRef.GetNearest(this.transform.position.xy()).instance.transform.position.xy();
        
        #endregion
        
        #region Player related
        
        /// <summary>
        /// Registers seen players, and returns true if it saw at least one
        /// </summary>
        private bool CanSeePlayer()
        {
            if (this.currentlyRegisteredPlayers.Length > 0 && this.visionCone.AreTransformsInCone(new[] { this.currentlyRegisteredPlayers.First() }).Length > 0)
                return true;
            this.currentlyRegisteredPlayers = this.visionCone.GetCurrentlySeenObjects();
            return this.currentlyRegisteredPlayers.Length > 0;
        }
        
        private Vector2? GetPlayerPosition()
        {
            if (this.currentlyRegisteredPlayers.Length <= 0)
                return null;
            return this.currentlyRegisteredPlayers.First().transform.position.xy();
        }
        
        private bool TryAttackPlayer()
        {
            if (this.currentlyRegisteredPlayers.Length <= 0)
                return false;
            GameObject player = this.currentlyRegisteredPlayers.First();
            if (Vector2.Distance(player.transform.position, this.transform.position) > this.weaponHandler.Weapon.attackRange)
                return false;
            
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable == null || !damageable.CanTakeDamage()) return false;

            this.animator.SetTrigger(Attack);
            damageable.TakeDamage(this.weaponHandler.GetDamage());
            return true;
        }
        
        private bool IsPlayerInAttackRange()
        {
            Vector2? playerPos = GetPlayerPosition();
            if (playerPos.HasValue)
                return Vector2.Distance(playerPos.Value, this.transform.position) <= this.weaponHandler.Weapon.attackRange;
            return false;
        }

        #endregion
    }
}
