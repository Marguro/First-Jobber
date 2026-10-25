#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <remarks>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </remarks>
    
    //[CreateAssetMenu(fileName = "Sample_Attribute_ListLength", menuName = "Gaskellgames/Samples/GgSamplePage")]
    public class Sample_Attribute_ListLength : ScriptableObject
    {
        // ---------- ListLength ----------
        
        [SerializeField, ListLength(4)]
        private List<GameObject> listLength = new List<GameObject>();
        
        [field: SerializeField, ListLength(4, true)]
        private List<GameObject> ListLengthForced { get; set; }
        
    } // class end
}

#endif