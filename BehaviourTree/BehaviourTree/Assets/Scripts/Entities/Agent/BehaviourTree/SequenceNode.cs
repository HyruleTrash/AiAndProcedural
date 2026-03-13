namespace BehaviourTree
{
    public class SequenceNode : CompositeNode
    {
        public SequenceNode(INode[] toTrigger) : base(toTrigger) { }

        public override NodeState Call()
        {
            foreach (var child in toTrigger)
            {
                var result = child.Call();
                if (result == NodeState.Failed) return NodeState.Failed;
            }
            return NodeState.Success;
        }
    }
}