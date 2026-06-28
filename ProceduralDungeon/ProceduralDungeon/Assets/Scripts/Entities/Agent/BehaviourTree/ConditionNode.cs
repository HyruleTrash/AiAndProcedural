using System;

namespace BehaviourTree
{
    public class ConditionNode : INode
    {
        private Func<bool> condition;
        private readonly bool isInverted = false;
        private INode toExecute;
        
        public ConditionNode(Func<bool> condition, INode toExecute, bool isInverted = false)
        {
            this.condition = condition;
            this.toExecute = toExecute;
            this.isInverted = isInverted;
        }

        public NodeState Call()
        {
            if (this.isInverted)
                return !this.condition.Invoke() ? this.toExecute.Call() : NodeState.Failed;
            return this.condition.Invoke() ? this.toExecute.Call() : NodeState.Failed;
        }
    }
}