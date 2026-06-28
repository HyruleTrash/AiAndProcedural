using System;
using UnityEngine;

namespace BehaviourTree
{
    public class BehaviourTree : MonoBehaviour
    {
        private INode node;
        private bool initialized = false;

        public void Initialize(INode node)
        {
            this.node = node;
            this.initialized = true;
        }

        private void Update()
        {
            if (!this.initialized) return;
            this.node.Call();
        }
    }
}
