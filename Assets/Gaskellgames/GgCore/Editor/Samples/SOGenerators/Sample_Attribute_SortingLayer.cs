#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;

namespace Gaskellgames.EditorOnly
{
    /// <remarks>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </remarks>
    
    // [CreateAssetMenu(fileName = "Sample_Attribute_SortingLayer", menuName = "Gaskellgames/Samples/GgSamplePage")]
    public class Sample_Attribute_SortingLayer : ScriptableObject
    {
        // ---------- SortingLayer ----------
        
        [SerializeField]
        private SortingGroup playerSortingGroup;
        
        [SerializeField, SortingLayer]
        private int sortingLayer;
        
        private void ExampleMethod()
        {
            playerSortingGroup.sortingLayerID = sortingLayer;
        }
        
    } // class end
}

#endif