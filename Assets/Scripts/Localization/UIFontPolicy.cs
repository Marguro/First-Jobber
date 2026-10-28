using UnityEngine;

namespace FirstJobber
{
    /// <summary>Optional font ownership for independently authored scene buttons or text groups.</summary>
    [DisallowMultipleComponent]
    public class UIFontPolicy : MonoBehaviour
    {
        [Tooltip("Off keeps each child TMP text's authored font. Choose a font containing English and Thai. On uses the shared localization-table font.")]
        public bool useProjectFont;
        [Tooltip("Thai word-break preprocessing. Disable for short single-line labels such as speaker names; glyph correction still applies.")]
        public bool tokenizeThai = true;
    }
}
