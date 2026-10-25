#if UNITY_EDITOR
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <remarks>
    /// Code created by Gaskellgames: https://gaskellgames.com
    /// </remarks>
    
    // [CreateAssetMenu(fileName = "Sample_Attribute_Preview", menuName = "Gaskellgames/Samples/GgSamplePage")]
    public class Sample_Attribute_Preview : ScriptableObject
    {
        // ---------- Preview ----------

        [SerializeField, Preview]
        private Texture2D preview;

        [field: SerializeField, Preview]
        private Texture2D Preview { get; set; }

    } // class end
}

#endif