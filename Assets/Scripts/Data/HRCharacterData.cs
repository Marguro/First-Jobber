using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    public enum InterviewSlotType { Fixed, ResumeLinked }

    /// <summary>One question slot of an interview, asked in list order.</summary>
    [Serializable]
    public class InterviewSlot
    {
        public InterviewSlotType type = InterviewSlotType.Fixed;
        [Tooltip("Fixed only. Leave empty to take the next unused question from the HR's Fixed Questions list (in list order).")]
        public InterviewQuestionData fixedQuestion;
    }

    /// <summary>Sprites of one HR for one atmosphere phase. Missing emotions fall back to the normal sprite.</summary>
    [Serializable]
    public class HRAtmosphereVariant
    {
        public AtmospherePhaseData phase;
        public List<AtmosphereHrSpriteOverride> sprites = new List<AtmosphereHrSpriteOverride>();
        [Tooltip("Multiplied into the HR colour while this phase is active (placeholder art).")]
        public Color tint = Color.white;
    }

    /// <summary>This HR's own reaction to one resume choice. Without an entry, the choice's shared HR Reactions are used.</summary>
    [Serializable]
    public class HRChoiceReaction
    {
        public ResumeChoiceData choice;
        public DialoguePlaybackMode mode;
        public DialogueLine[] lines;
    }

    /// <summary>
    /// One interviewer. Everything the interview says or asks comes from the HR assigned to the selected company.
    /// Each HR must own its own question assets - do not share a question asset between two HR characters.
    /// </summary>
    [CreateAssetMenu(menuName = "First Jobber/HR Character", fileName = "HR_New")]
    public class HRCharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        [Tooltip("Shown as the speaker name for every HR line.")]
        public string hrName;
        [Tooltip("Optional. Only used for validation messages.")]
        public CompanyData company;
        [TextArea] public string description;

        [Header("Visuals (placeholders are fine until art is ready)")]
        [Tooltip("Used when an emotion has no sprite.")]
        public Sprite defaultPortrait;
        public HREmotion defaultEmotion = HREmotion.Neutral;
        public List<HREmotionVisual> emotionVisuals = new List<HREmotionVisual>();
        [Tooltip("Per atmosphere phase sprites. A phase without an entry uses the normal emotion sprite.")]
        public List<HRAtmosphereVariant> atmosphereVariants = new List<HRAtmosphereVariant>();
        [Tooltip("Optional. Assigned to the Animator of the HR portrait while this HR is shown.")]
        public RuntimeAnimatorController animatorController;

        [Header("Dialogue (empty = falls back to the company / shared dialogue)")]
        public DialoguePlaybackMode introMode;
        public DialogueLine[] introduction;
        public DialoguePlaybackMode resumeReviewMode;
        public DialogueLine[] resumeReview;
        [Tooltip("HR-specific reactions per resume choice.")]
        public List<HRChoiceReaction> choiceReactions = new List<HRChoiceReaction>();
        public DialoguePlaybackMode rejectionMode;
        public DialogueLine[] rejection;

        [Header("Questions")]
        [Tooltip("Asked in this order. Not random, not linked to the resume.")]
        public List<InterviewQuestionData> fixedQuestions = new List<InterviewQuestionData>();
        [Tooltip("Matched against the confirmed resume: exact choice first, then category, higher weight first, then list order.")]
        public List<InterviewQuestionData> resumeLinkedQuestions = new List<InterviewQuestionData>();
        [Tooltip("Used when a Resume Linked slot finds no match (or a fixed slot has nothing left). Recommended.")]
        public InterviewQuestionData fallbackQuestion;
        [Tooltip("Order of the interview. Empty = 1 Fixed + 1 Resume Linked.")]
        public List<InterviewSlot> sequence = new List<InterviewSlot>();

        // ------------------------------------------------------------ queries

        public string DisplayName => string.IsNullOrWhiteSpace(hrName) ? name : hrName;

        public IList<InterviewSlot> EffectiveSequence()
        {
            if (sequence != null && sequence.Count > 0) return sequence;
            return new[]
            {
                new InterviewSlot { type = InterviewSlotType.Fixed },
                new InterviewSlot { type = InterviewSlotType.ResumeLinked }
            };
        }

        public HRChoiceReaction FindChoiceReaction(ResumeChoiceData choice)
        {
            if (choice == null || choiceReactions == null) return null;
            for (int i = 0; i < choiceReactions.Count; i++)
            {
                var r = choiceReactions[i];
                if (r != null && r.choice == choice && r.lines != null && r.lines.Length > 0) return r;
            }
            return null;
        }

        /// <summary>
        /// Sprite for an emotion in the given phase. Order: phase variant, normal emotion sprite, default portrait.
        /// Returns null when nothing is configured (the controller then shows a text placeholder). Never throws.
        /// </summary>
        public Sprite ResolveSprite(HREmotion emotion, AtmospherePhaseData phase, out Color tint)
        {
            tint = Color.white;
            Sprite sprite = null;

            var visual = FindVisual(emotion) ?? (emotion != HREmotion.Neutral ? FindVisual(HREmotion.Neutral) : null);
            if (visual != null) { sprite = visual.sprite; tint = visual.tint; }

            if (phase != null && atmosphereVariants != null)
            {
                for (int i = 0; i < atmosphereVariants.Count; i++)
                {
                    var v = atmosphereVariants[i];
                    if (v == null || v.phase != phase) continue;
                    tint *= v.tint;
                    if (v.sprites != null)
                        for (int s = 0; s < v.sprites.Count; s++)
                            if (v.sprites[s] != null && v.sprites[s].emotion == emotion && v.sprites[s].sprite != null)
                            { sprite = v.sprites[s].sprite; break; }
                    break;
                }
            }
            return sprite != null ? sprite : defaultPortrait;
        }

        HREmotionVisual FindVisual(HREmotion emotion)
        {
            if (emotionVisuals == null) return null;
            for (int i = 0; i < emotionVisuals.Count; i++)
                if (emotionVisuals[i] != null && emotionVisuals[i].emotion == emotion) return emotionVisuals[i];
            return null;
        }

        // ------------------------------------------------------------ validation

        public void Validate(List<string> problems)
        {
            string n = "HR '" + name + "'";
            if (string.IsNullOrWhiteSpace(id)) problems.Add(n + " has an empty id.");
            if (string.IsNullOrWhiteSpace(hrName)) problems.Add(n + " has no hrName.");
            if (introduction == null || introduction.Length == 0) problems.Add(n + " has no introduction (company / shared intro will be used).");
            if (rejection == null || rejection.Length == 0) problems.Add(n + " has no rejection dialogue (company / shared rejection will be used).");
            if (fixedQuestions.Count == 0 && resumeLinkedQuestions.Count == 0 && fallbackQuestion == null)
                problems.Add(n + " has no questions at all (the built-in emergency question will be asked).");
            if (resumeLinkedQuestions.Count > 0 && fallbackQuestion == null)
                problems.Add(n + " has no fallbackQuestion: a resume with no match gets the built-in emergency question.");

            foreach (var q in fixedQuestions) CheckQuestion(q, n + " fixed question", problems, false);
            foreach (var q in resumeLinkedQuestions) CheckQuestion(q, n + " resume-linked question", problems, true);
            if (fallbackQuestion != null) CheckQuestion(fallbackQuestion, n + " fallback question", problems, false);

            foreach (var slot in sequence)
                if (slot != null && slot.type == InterviewSlotType.Fixed && slot.fixedQuestion == null && fixedQuestions.Count == 0)
                    problems.Add(n + " has a Fixed slot but no Fixed Questions.");
        }

        static void CheckQuestion(InterviewQuestionData q, string label, List<string> problems, bool needsLink)
        {
            if (q == null) { problems.Add(label + " list has a missing reference."); return; }
            if (q.answers == null || q.answers.Count == 0) problems.Add(label + " '" + q.name + "' has no answers.");
            if (needsLink && q.linkedChoice == null && q.linkedCategory == null)
                problems.Add(label + " '" + q.name + "' has no linked choice or category, so it can never be picked.");
        }
    }
}
