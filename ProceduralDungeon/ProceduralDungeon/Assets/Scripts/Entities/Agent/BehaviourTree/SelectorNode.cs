namespace BehaviourTree
{
    public class SelectorNode : CompositeNode
    {
        public SelectorNode(INode[] children) : base(children) { }

        public override NodeState Call()
        {
            foreach (var child in children)
            {
                var result = child.Call();
                if (result == NodeState.Success) return NodeState.Success;
            }
            return NodeState.Failed;
        }
    }
}