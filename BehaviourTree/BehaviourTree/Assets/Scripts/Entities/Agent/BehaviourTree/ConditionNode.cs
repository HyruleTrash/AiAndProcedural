using System;

namespace BehaviourTree
{
    public class ConditionNode : INode
    {
        private Func<bool> condition;
        private INode toExecute;
        
        public ConditionNode(Func<bool> condition, INode toExecute)
        {
            this.condition = condition;
            this.toExecute = toExecute;
        }

        public NodeState Call() => condition.Invoke() ? toExecute.Call() : NodeState.Failed;
    }
}