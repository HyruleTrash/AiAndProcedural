namespace BehaviourTree
{
    public abstract class CompositeNode : INode
    {
        protected INode[] toTrigger;

        protected CompositeNode(INode[] toTrigger)
        {
            this.toTrigger = toTrigger;
        }

        public abstract NodeState Call();
    }
}