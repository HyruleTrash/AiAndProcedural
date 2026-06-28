namespace BehaviourTree
{
    public class SequenceNode : CompositeNode
    {
        public SequenceNode(INode[] children) : base(children) { }

        public override NodeState Call()
        {
            foreach (INode child in this.children)
            {
                NodeState result = child.Call();
                if (result == NodeState.Failed) return NodeState.Failed;
            }
            return NodeState.Success;
        }
    }
}