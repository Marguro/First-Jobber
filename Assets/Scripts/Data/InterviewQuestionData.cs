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
        public DialogueLine[] playerReaction;
        [Tooltip("HR response. Set speaker = HR and an emotion on each line.")]
        public DialogueLine[] hrResponse;
    }

    [CreateAssetMenu(menuName = "First Jobber/Interview Question", fileName = "Question_New")]
    public class InterviewQuestionData : ScriptableObject
    {
        public string id;
        [TextArea] public string questionText;
        public HREmotion hrEmotion = HREmotion.Neutral;

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
