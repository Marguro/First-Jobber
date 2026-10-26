using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstJobber
{
    /// <summary>
    /// Localizes a static TMP label (button caption, header, placeholder) that is baked into a scene.
    /// Added automatically at runtime to every suitable TMP_Text of a loaded scene (no scene editing needed):
    /// the English text is captured from the label, the key is "ui/label/" + hierarchy path.
    /// If code ever changes the label's text, the label is dynamic: this component removes itself and leaves it alone.
    /// </summary>
    public class LocalizedLabel : MonoBehaviour
    {
        public string key;
        [TextArea] public string english;

        TMP_Text label;
        string applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Runs after Awake/OnEnable of the scene objects and before their Start, so the baked English is still there.
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                    if (ShouldLocalize(t) && t.GetComponent<LocalizedLabel>() == null)
                    {
                        var component = t.gameObject.AddComponent<LocalizedLabel>();
                        component.Init(t);
                    }
        }

        void Init(TMP_Text text)
        {
            label = text;
            key = KeyFor(text);
            english = text.text;
            applied = text.text;
            if (isActiveAndEnabled) Apply();
        }

        void OnEnable()
        {
            LocalizationManager.LanguageChanged += Apply;
            if (label != null) Apply();
        }

        void OnDisable() { LocalizationManager.LanguageChanged -= Apply; }

        void Apply()
        {
            if (label == null) return;
            if (label.text != applied) { Destroy(this); return; } // code owns this text now
            string shown = LocalizationManager.Get(key, english);
            if (shown != label.text) label.text = shown;
            applied = label.text;
        }

        /// <summary>A label is translatable when it contains a letter (skips empty, numeric and zero-width placeholders).</summary>
        public static bool ShouldLocalize(TMP_Text t)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) return false;
            foreach (char c in t.text) if (char.IsLetter(c)) return true;
            return false;
        }

        public static string KeyFor(TMP_Text t)
        {
            var sb = new StringBuilder("ui/label");
            var parts = new System.Collections.Generic.List<string>();
            for (var tr = t.transform; tr != null; tr = tr.parent) parts.Add(tr.name);
            for (int i = parts.Count - 1; i >= 0; i--) sb.Append('/').Append(parts[i]);
            return sb.ToString();
        }
    }
}
