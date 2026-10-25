using UnityEngine;

namespace FirstJobber
{
    [CreateAssetMenu(menuName = "First Jobber/Resume Choice", fileName = "Choice_New")]
    public class ResumeChoiceData : ScriptableObject
    {
        [Tooltip("Stable unique id.")]
        public string id;
        public ResumeCategoryData category;
        public string displayText;
        [TextArea] public string description;
        public string[] tags;
        [Header("Dialogue")]
        [Tooltip("Player's thoughts when this choice is clicked.")]
        public DialogueLine[] playerReaction;
        [Tooltip("What HR says about this choice when reviewing the resume (speaker = HR, plus an emotion).")]
        public DialogueLine[] hrReactions;

        public bool HasTag(string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < tags.Length; i++) if (tags[i] == tag) return true;
            return false;
        }
    }
}
