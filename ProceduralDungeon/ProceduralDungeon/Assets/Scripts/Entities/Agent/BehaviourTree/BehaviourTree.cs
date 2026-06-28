using UnityEngine;

namespace BehaviourTree
{
    public class BehaviourTree : MonoBehaviour
    {
        private INode node = null!;
        private bool initialized;

        public void Initialize(INode newNode)
        {
            this.node = newNode;
            this.initialized = true;
        }

        private void Update()
        {
            if (!this.initialized) return;
            this.node.Call();
        }
    }
}
