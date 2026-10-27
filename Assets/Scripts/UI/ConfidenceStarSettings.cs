using UnityEngine;

namespace FirstJobber
{
    /// <summary>Shared presentation mapping only. Confidence still comes from ResumeChoiceData.</summary>
    [CreateAssetMenu(menuName = "First Jobber/UI/Confidence Star Settings", fileName = "ConfidenceStarSettings")]
    public class ConfidenceStarSettings : ScriptableObject
    {
        [Header("Sprites (original transparency is preserved)")]
        public Sprite starLevel1;
        public Sprite starLevel2;
        public Sprite starLevel3;
        public Sprite starLevel4;
        public Sprite starLevel5;
        [Tooltip("Optional fallback/header sprite. Used only when the mapped level has no sprite.")]
        public Sprite defaultStar;

        [Header("Thresholds (inclusive upper bounds, faintest to strongest)")]
        [Range(0f, 100f)] public float level5Max = 20f;
        [Range(0f, 100f)] public float level4Max = 40f;
        [Range(0f, 100f)] public float level3Max = 60f;
        [Range(0f, 100f)] public float level2Max = 80f;

        public Sprite GetSprite(float confidence)
        {
            float value = ResumeConfidenceCalculator.Clamp(confidence);
            Sprite result = value <= level5Max ? starLevel5
                : value <= level4Max ? starLevel4
                : value <= level3Max ? starLevel3
                : value <= level2Max ? starLevel2 : starLevel1;
            return result != null ? result : defaultStar;
        }

        void OnValidate()
        {
            level5Max = ResumeConfidenceCalculator.Clamp(level5Max);
            level4Max = Mathf.Max(level5Max, ResumeConfidenceCalculator.Clamp(level4Max));
            level3Max = Mathf.Max(level4Max, ResumeConfidenceCalculator.Clamp(level3Max));
            level2Max = Mathf.Max(level3Max, ResumeConfidenceCalculator.Clamp(level2Max));
        }
    }
}
