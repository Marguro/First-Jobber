using System;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </summary>

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ListLengthAttribute : PropertyAttribute
    {
        public int capacity;
        public bool forceLength;
        
        public ListLengthAttribute(int capacity, bool forceLength = false)
        {
            this.capacity = Mathf.Max(0, capacity);
            this.forceLength = forceLength;
        }
        
    } // class end
}

