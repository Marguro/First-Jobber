using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>One random candidate: a normal dialogue block (speaker, emotion, pages, ...) with a stable id used for its localization key.</summary>
    [Serializable]
    public class ConfidenceCandidate
    {
        [Tooltip("Stable id used in the localization key (confidence/apply/day_01/high/<id>/text). Keep it when reordering. Empty = candidate_<position>.")]
        public string id;
        public DialogueLine dialogue = new DialogueLine();
    }

    /// <summary>Reaction candidates of ONE day: one pool for High confidence, one for Low.</summary>
    [Serializable]
    public class ConfidenceDayConfig
    {
        [Min(1)] public int day = 1;
        [Tooltip("Confidence above the threshold. ONE candidate is picked at random and played (all its pages).")]
        public List<ConfidenceCandidate> high = new List<ConfidenceCandidate>();
        [Tooltip("Confidence equal to or below the threshold.")]
        public List<ConfidenceCandidate> low = new List<ConfidenceCandidate>();
    }

    /// <summary>
    /// Reaction played after the player confirms a company, chosen by game day and finalized resume confidence.
    /// Any number of days; add / remove / reorder them in the Inspector. Production and demo databases can use different assets.
    /// </summary>
    [CreateAssetMenu(menuName = "First Jobber/Confidence Dialogue Database", fileName = "ConfidenceDialogue_New")]
    public class ConfidenceDialogueDatabase : ScriptableObject
    {
        [Tooltip("Confidence ABOVE this value = High. Equal or below = Low.")]
        [Range(0f, 100f)] public float threshold = 50f;
        public List<ConfidenceDayConfig> days = new List<ConfidenceDayConfig>();
        [Header("General fallback (used when no configured day has a usable pool)")]
        public List<ConfidenceCandidate> fallbackHigh = new List<ConfidenceCandidate>();
        public List<ConfidenceCandidate> fallbackLow = new List<ConfidenceCandidate>();

        public static string DayKey(int day) { return "day_" + day.ToString("00"); }

        /// <summary>
        /// Candidates for this day and confidence, as blocks to play with RandomOne. Day lookup: the exact day, then the nearest
        /// PREVIOUS configured day, then later days, then the general fallback. A day whose chosen pool is empty falls through
        /// to the next one. Returns null when nothing is usable (the caller then uses the legacy confirmed-resume lines).
        /// </summary>
        public DialogueLine[] Resolve(int day, float confidence)
        {
            bool high = confidence > threshold;
            var ordered = new List<ConfidenceDayConfig>();
            foreach (var d in days) if (d != null && d.day <= day) ordered.Add(d);
            ordered.Sort((a, b) => b.day.CompareTo(a.day));           // exact day first, then nearest previous
            var later = new List<ConfidenceDayConfig>();
            foreach (var d in days) if (d != null && d.day > day) later.Add(d);
            later.Sort((a, b) => a.day.CompareTo(b.day));
            ordered.AddRange(later);

            foreach (var d in ordered)
            {
                var result = Usable(high ? d.high : d.low);
                if (result != null) return result;
            }
            return Usable(high ? fallbackHigh : fallbackLow);
        }

        static DialogueLine[] Usable(List<ConfidenceCandidate> candidates)
        {
            if (candidates == null) return null;
            var list = new List<DialogueLine>();
            foreach (var c in candidates) if (c != null && c.dialogue != null && c.dialogue.HasContent) list.Add(c.dialogue);
            return list.Count > 0 ? list.ToArray() : null;
        }
    }
}
