using System;
using UnityEngine;

namespace Util
{
    public class TagComponent : MonoBehaviour
    {
        [SerializeField] private string name = "";
        
        public bool Compare(string other) => string.Compare(this.name, other, StringComparison.InvariantCulture) == 0; 
    }
}