using System;
using BehaviourTree;
using UnityEngine;

namespace Guard
{
    [RequireComponent(typeof(HealthComponent), typeof(WeaponHandler), typeof(VisionCone)),
     RequireComponent(typeof(BehaviourTree.BehaviourTree), typeof(NavigateToPosition))]
    public class GuardManager : MonoBehaviour
    {
        [SerializeField]
        private BehaviourTree.BehaviourTree behaviourTree;
        [SerializeField]
        private HealthComponent healthComponent;
        [SerializeField]
        private WeaponHandler weaponHandler;
        private IDamager damager;
        [SerializeField] 
        private NavigateToPosition navigateToPosition;
        [SerializeField]
        private VisionCone visionCone;

        private void OnValidate()
        {
            behaviourTree ??= GetComponent<BehaviourTree.BehaviourTree>();
            healthComponent ??= GetComponent<HealthComponent>();
            weaponHandler ??= GetComponent<WeaponHandler>();
            visionCone ??= GetComponent<VisionCone>();
            navigateToPosition ??= GetComponentInChildren<NavigateToPosition>();
            enabled = healthComponent && weaponHandler && navigateToPosition;
        }

        private void Update()
        {
            transform.position += navigateToPosition.transform.localPosition;
            navigateToPosition.transform.localPosition = Vector3.zero;
        }

        private void OnEnable()
        {
            behaviourTree.Initialize(new SelectorNode(new []
            {
                new SequenceNode(new INode[]
                {
                    new TaskNode(CanSeePlayer),
                    new ParallelNode(new INode[]
                    {
                        new InvertNode(new TaskNode(IsCurrentWaypointNull)) // this is wrong
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

        private object GetNextWaypoint()
        {
            throw new NotImplementedException();
        }

        private object GetNearestWayPoint()
        {
            throw new NotImplementedException();
        }

        private bool SetCurrentWaypoint(object getNearestWayPoint)
        {
            throw new NotImplementedException();
        }

        private bool IsAgentNearCurrentWaypoint()
        {
            throw new NotImplementedException();
        }

        private bool IsCurrentWaypointNull()
        {
            throw new NotImplementedException();
        }

        private bool CanSeePlayer()
        {
            throw new NotImplementedException();
        }
    }
}
