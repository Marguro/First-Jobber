using System;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </summary>
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PreviewAttribute : PropertyAttribute
    {
        public float lines;
        
        public PreviewAttribute()
        {
            lines = 4;
        }
        
        public PreviewAttribute(float lines)
        {
            this.lines = Mathf.Max(1, lines);
        }
        
    } // class end
}