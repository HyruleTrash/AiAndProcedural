using System;

namespace BehaviourTree
{
    public class TaskNode : INode
    {
        private Func<bool> toExecute;
        public TaskNode(Func<bool> toExecute) => this.toExecute = toExecute;
        public NodeState Call() => toExecute.Invoke() ? NodeState.Success : NodeState.Failed;
    }
}