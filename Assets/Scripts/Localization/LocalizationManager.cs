using System;
using UnityEngine;

namespace FirstJobber
{
    public enum Language { English, Thai }

    /// <summary>
    /// Current language + string lookup. English is the source language: the English text stored in the
    /// ScriptableObjects / code is always the fallback, so a missing or empty translation shows English.
    /// Other languages live in a LocalizationTable loaded from Resources (see LocalizationKeys for the key scheme).
    /// </summary>
    public static class LocalizationManager
    {
        const string PrefKey = "FirstJobber.Language";
        const string ThaiTablePath = "Localization/LocalizationTable_TH";

        public static event Action LanguageChanged;

        static Language? current;
        static LocalizationTable thaiTable;
        static bool thaiTableLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            thaiTable = null;
            thaiTableLoaded = false;
            LanguageChanged = null;
        }

        public static Language Current
        {
            get
            {
                if (current == null)
                {
                    int saved = PlayerPrefs.GetInt(PrefKey, (int)Language.English);
                    current = Enum.IsDefined(typeof(Language), saved) ? (Language)saved : Language.English;
                }
                return current.Value;
            }
        }

        public static void SetLanguage(Language language)
        {
            if (language == Current) return;
            current = language;
            PlayerPrefs.SetInt(PrefKey, (int)language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        /// <summary>The table of the active language, or null for English / when no table asset exists.</summary>
        public static LocalizationTable Table => Current == Language.Thai ? ThaiTable : null;

        /// <summary>The Thai table whatever the current language is (its font is used for every language).</summary>
        public static LocalizationTable ThaiTable
        {
            get
            {
                if (!thaiTableLoaded)
                {
                    thaiTableLoaded = true;
                    thaiTable = Resources.Load<LocalizationTable>(ThaiTablePath);
                }
                return thaiTable;
            }
        }

        /// <summary>Translation of 'key' in the current language, or 'english' when there is none. Never empties a non-empty English text.</summary>
        public static string Get(string key, string english)
        {
            if (string.IsNullOrEmpty(english) || string.IsNullOrEmpty(key)) return english;
            var table = Table;
            if (table != null && table.TryGet(key, out var translated)) return translated;
            return english;
        }

        /// <summary>Same as Get, with the key built from a data asset: see LocalizationKeys.Of.</summary>
        public static string Get(ScriptableObject owner, string field, string english)
        {
            if (string.IsNullOrEmpty(english) || owner == null) return english;
            return Get(LocalizationKeys.Of(owner, field), english);
        }
    }
}
