using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    [Serializable]
    public class InterviewAnswer
    {
        public string id;
        public string text;
        [Tooltip("Player's reaction after choosing this answer.")]
        public DialoguePlaybackMode playerReactionMode;
        public DialogueLine[] playerReaction;
        [Header("HR Reaction (optional)")]
        [Tooltip("Apply this answer's HR emotion even when HR Response is empty. Off preserves existing dialogue-block emotions and pacing.")]
        public bool applyHrReactionEmotion;
        [Tooltip("Resolved through the current company's HRCharacterData. Authored HR dialogue blocks can request their own subsequent emotions.")]
        public HREmotion hrReactionEmotion = HREmotion.Neutral;
        [Min(0.05f), Tooltip("Seconds to show a silent HR reaction. Replaces the normal reaction pause only when no applicable response text exists; no invisible Continue click is required.")]
        public float portraitOnlyDuration = 0.8f;
        [Tooltip("HR response. Set speaker = HR and an emotion on each line.")]
        public DialoguePlaybackMode hrResponseMode;
        [Tooltip("Optional response dialogue. Empty entries are not opened as dialogue pages. For a portrait-only reaction, enable Apply HR Reaction Emotion above and leave this array empty. Authored text blocks retain their individual emotions.")]
        public DialogueLine[] hrResponse;
    }

    [CreateAssetMenu(menuName = "First Jobber/Interview Question", fileName = "Question_New")]
    public class InterviewQuestionData : ScriptableObject
    {
        public string id;
        [TextArea] public string questionText;
        [Tooltip("Emotion for Question Text when Question Dialogue is empty. With Question Dialogue, choose Emotion separately on each HR block. Portraits always come from the active company's HRCharacterData.")]
        public HREmotion hrEmotion = HREmotion.Neutral;
        [Tooltip("Optional. When filled, the HR says these blocks instead of Question Text (multi-line and gender variants allowed). Question Text is still shown on screen.")]
        public DialogueLine[] questionDialogue;

        [Header("Links (all optional - leave both empty for a generic fallback question)")]
        [Tooltip("Exact match: asked only if the player picked this choice. Highest priority.")]
        public ResumeChoiceData linkedChoice;
        [Tooltip("Category match: asked if the player picked any choice of this category (used when Linked Choice is empty).")]
        public ResumeCategoryData linkedCategory;

        [Header("Conditions (optional)")]
        public CompanyData requiredCompany;
        [Tooltip("Needs a selected choice OR the company to have this tag.")]
        public string requiredTag;
        [Min(0)] public int weight = 1;

        public List<InterviewAnswer> answers = new List<InterviewAnswer>();
    }
}
