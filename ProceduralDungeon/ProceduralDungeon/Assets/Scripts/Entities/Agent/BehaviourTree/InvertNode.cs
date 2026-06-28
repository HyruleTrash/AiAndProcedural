namespace BehaviourTree
{
    public class InvertNode : INode
    {
        private INode toInvert;
        
        public InvertNode(INode toInvert) => this.toInvert = toInvert;

        public NodeState Call()
        {
            NodeState result = this.toInvert.Call();
            return result switch
            {
                NodeState.Success => NodeState.Failed,
                NodeState.Failed => NodeState.Success,
                _ => result
            };
        }
    }
}