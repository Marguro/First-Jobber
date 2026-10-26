using System;
using System.Collections.Generic;
using PhEngine.ThaiTextCare;
using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Translations for one language (key -> text) plus the font used to show them.
    /// Created and filled by Tools > First Jobber > 12. Export Translation Template; the Thai table is loaded
    /// from Resources/Localization/LocalizationTable_TH.asset. 'english' is only a reference copy for translators.
    /// </summary>
    [CreateAssetMenu(menuName = "First Jobber/Localization Table", fileName = "LocalizationTable_TH")]
    public class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            [TextArea] public string english;
            [TextArea] public string thai;
        }

        [Tooltip("Font asset used for every text while this language is active. Must contain the Thai glyphs (use Tools > First Jobber > 14. Setup Thai Font).")]
        public TMP_FontAsset font;
        [Tooltip("ThaiTextNurse glyph correction. FullC90 / YoorYingAndToorTaan only work if the font has the C90 glyphs U+F700-U+F71A.")]
        public ThaiGlyphCorrection correction = ThaiGlyphCorrection.None;
        public List<Entry> entries = new List<Entry>();

        [NonSerialized] Dictionary<string, string> lookup;

        void OnEnable() { lookup = null; }
        void OnValidate() { lookup = null; }

        public bool TryGet(string key, out string translated)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, string>(entries.Count);
                foreach (var e in entries)
                {
                    if (e == null || string.IsNullOrEmpty(e.key) || string.IsNullOrWhiteSpace(e.thai)) continue;
                    lookup[e.key] = e.thai;
                }
            }
            return lookup.TryGetValue(key, out translated);
        }
    }
}
