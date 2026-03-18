namespace BehaviourTree
{
    public abstract class CompositeNode : INode
    {
        protected readonly INode[] children;

        protected CompositeNode(INode[] children)
        {
            this.children = children;
        }

        public abstract NodeState Call();
    }
}