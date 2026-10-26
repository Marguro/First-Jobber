using System;

namespace FirstJobber
{
    /// <summary>One entry of the applicant gender dropdown. Edit the list in GameDatabase - no code change needed.</summary>
    [Serializable]
    public class GenderOption
    {
        [UnityEngine.Tooltip("Stable id stored in the save and matched by dialogue (e.g. male, female). Never rename after testing.")]
        public string id;
        [UnityEngine.Tooltip("Text shown in the dropdown and in the {gender} token.")]
        public string displayName;

        public GenderOption() { }
        public GenderOption(string id, string displayName) { this.id = id; this.displayName = displayName; }
    }

    public static class Genders
    {
        /// <summary>Used for old saves and for missing data. Dialogue falls back to its 'any gender' version.</summary>
        public const string Unspecified = "unspecified";
    }
}
