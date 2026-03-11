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
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            node.Call();
        }
    }
}
