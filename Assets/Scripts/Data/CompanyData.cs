using UnityEngine;

namespace FirstJobber
{
    [CreateAssetMenu(menuName = "First Jobber/Company", fileName = "Company_New")]
    public class CompanyData : ScriptableObject
    {
        public string id;
        public string companyName;
        public string jobTitle;
        public string salary;
        public string industry;
        [TextArea] public string description;
        public Sprite logo;
        public string[] tags;
        [Header("Dialogue")]
        public DialogueLine[] playerReaction;
        [Tooltip("Optional. Falls back to GameDatabase.defaultHrIntro.")]
        public DialogueLine[] hrIntro;
        [Tooltip("Optional. Falls back to GameDatabase.rejectionHR.")]
        public DialogueLine[] rejection;

        public bool HasTag(string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < tags.Length; i++) if (tags[i] == tag) return true;
            return false;
        }
    }
}
