using System.Collections.Generic;
using System.Linq;

namespace BehaviourTree
{
    public class ParallelNode : CompositeNode
    {
        public ParallelNode(INode[] children) : base(children) { }

        public override NodeState Call()
        {
            var states = new List<NodeState>();
            foreach (var child in children) states.Add(child.Call());
            return states.Count(state => state == NodeState.Success) > 0 ? NodeState.Success : NodeState.Failed;
        }
    }
}