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
        [Header("Confidence")]
        [Tooltip("The applicant's own confidence in this choice (0-100, decimals allowed). Fixed content: the same choice always contributes the same value. It does NOT affect hiring, rejection or endings.")]
        [Range(0f, 100f)] public float confidencePercentage = 50f;

        [Header("Dialogue")]
        [Tooltip("Player's thoughts when this choice is clicked.")]
        public DialogueLine[] playerReaction;
        [Tooltip("Sequential: play every block. RandomOne: pick ONE block (chunk) at random and play all its pages. Once per choice per playthrough either way.")]
        public DialoguePlaybackMode playerReactionMode;
        [Tooltip("What HR says about this choice when reviewing the resume (speaker = HR, plus an emotion).")]
        public DialogueLine[] hrReactions;

        /// <summary>Confidence clamped to 0-100 (NaN counts as 0).</summary>
        public float Confidence => ResumeConfidenceCalculator.Clamp(confidencePercentage);

        void OnValidate() { confidencePercentage = ResumeConfidenceCalculator.Clamp(confidencePercentage); }

        public bool HasTag(string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            for (int i = 0; i < tags.Length; i++) if (tags[i] == tag) return true;
            return false;
        }
    }
}
