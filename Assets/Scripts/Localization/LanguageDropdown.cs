using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// English / ไทย selector for a TMP_Dropdown. It only forwards to LocalizationManager (the single source of
    /// language state) and mirrors the saved preference, so it never owns a language of its own.
    /// Option order = Language enum order (0 English, 1 Thai). The option captions are native names and are
    /// intentionally NOT translated. Re-skin the dropdown freely: only the TMP_Dropdown reference is used.
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LanguageDropdown : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown dropdown;
        [Tooltip("Captions in Language enum order: English, Thai.")]
        [SerializeField] string[] captions = { "English", "ไทย" };

        void Reset() { dropdown = GetComponent<TMP_Dropdown>(); }

        void Awake()
        {
            if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        }

        void OnEnable()
        {
            if (dropdown == null) return;
            dropdown.onValueChanged.RemoveListener(OnValueChanged); // never register twice
            Populate();
            dropdown.onValueChanged.AddListener(OnValueChanged);
            LocalizationManager.LanguageChanged += Sync;
        }

        void OnDisable()
        {
            if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnValueChanged);
            LocalizationManager.LanguageChanged -= Sync;
        }

        void Populate()
        {
            dropdown.ClearOptions();
            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            var languages = (Language[])System.Enum.GetValues(typeof(Language));
            for (int i = 0; i < languages.Length; i++)
                options.Add(new TMP_Dropdown.OptionData(captions != null && i < captions.Length && !string.IsNullOrEmpty(captions[i]) ? captions[i] : languages[i].ToString()));
            dropdown.AddOptions(options);
            Sync();
        }

        // Reflect the current language without raising onValueChanged (no recursion).
        void Sync()
        {
            if (dropdown == null) return;
            dropdown.SetValueWithoutNotify((int)LocalizationManager.Current);
            dropdown.RefreshShownValue();
        }

        void OnValueChanged(int index)
        {
            if (!System.Enum.IsDefined(typeof(Language), index)) return;
            LocalizationManager.SetLanguage((Language)index);
        }
    }
}
