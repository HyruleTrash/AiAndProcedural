namespace BehaviourTree
{
    public class SelectorNode : CompositeNode
    {
        public SelectorNode(INode[] toTrigger) : base(toTrigger) { }

        public override NodeState Call()
        {
            foreach (var child in toTrigger)
            {
                var result = child.Call();
                if (result == NodeState.Success) return NodeState.Success;
            }
            return NodeState.Failed;
        }
    }
}