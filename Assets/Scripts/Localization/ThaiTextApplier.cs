using System.Collections.Generic;
using PhEngine.ThaiTextCare;
using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// While the language is Thai: every TMP_Text uses the table's Thai font and gets a ThaiTextNurse (word-break
    /// tokenizing + the glyph correction the font supports). Switching back restores the original font and disables
    /// the nurse. Creates itself at startup. TMP_Texts created later (pooled choice buttons) call Register, and a
    /// 1 s safety scan catches anything else. Per-text material overrides (outline etc.) are replaced by the Thai font's material.
    /// </summary>
    public class ThaiTextApplier : MonoBehaviour
    {
        const float ScanInterval = 1f;

        static ThaiTextApplier instance;

        readonly Dictionary<TMP_Text, TMP_FontAsset> originalFonts = new Dictionary<TMP_Text, TMP_FontAsset>();
        float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoCreate()
        {
            instance = null;
            var go = new GameObject("ThaiTextApplier");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ThaiTextApplier>();
        }

        void OnEnable() { LocalizationManager.LanguageChanged += ApplyAll; }
        void OnDisable() { LocalizationManager.LanguageChanged -= ApplyAll; }

        void Start() { ApplyAll(); }

        void Update()
        {
            if (LocalizationManager.Current != Language.Thai || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + ScanInterval;
            ApplyAll();
        }

        /// <summary>Applies the current language's font to one text (call after creating TMP_Texts at runtime).</summary>
        public static void Register(TMP_Text text)
        {
            if (instance != null) instance.Apply(text);
        }

        void ApplyAll()
        {
            // Forget texts that were destroyed.
            var dead = new List<TMP_Text>();
            foreach (var pair in originalFonts) if (pair.Key == null) dead.Add(pair.Key);
            foreach (var key in dead) originalFonts.Remove(key);

            foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Apply(t);
        }

        void Apply(TMP_Text t)
        {
            if (t == null) return;
            var table = LocalizationManager.Table; // null unless Thai
            var thaiFont = table != null ? table.font : null;
            var nurse = t.GetComponent<ThaiTextNurse>();

            if (thaiFont != null)
            {
                if (!originalFonts.ContainsKey(t)) originalFonts[t] = t.font;
                if (t.font != thaiFont) t.font = thaiFont;

                // The nurse's zero-width spaces break the caret of input fields, so only the font is changed there.
                if (t.GetComponentInParent<TMP_InputField>() != null) return;
                // Inactive objects never run Awake, so the nurse would have no TMP_Text yet (NullReference). The periodic scan adds it once the object is active.
                if (!t.gameObject.activeInHierarchy) return;
                if (nurse == null)
                {
                    nurse = t.gameObject.AddComponent<ThaiTextNurse>();
                    nurse.IsTokenize = true;
                    nurse.NotifyChange();
                }
                if (nurse.Correction != table.correction) nurse.Correction = table.correction;
                if (!nurse.enabled)
                {
                    nurse.enabled = true;
                    nurse.NotifyChange();
                }
                return;
            }

            if (originalFonts.TryGetValue(t, out var original))
            {
                originalFonts.Remove(t);
                if (original != null) t.font = original;
            }
            if (nurse != null && nurse.enabled)
            {
                nurse.enabled = false; // OnDisable detaches the preprocessor
                t.SetAllDirty();
            }
        }
    }
}
