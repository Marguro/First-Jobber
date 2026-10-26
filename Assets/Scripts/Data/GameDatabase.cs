using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Single entry point to all authored content. List order = display order.
    /// Never modified at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "First Jobber/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Content (order = display order)")]
        public List<ResumeCategoryData> categories = new List<ResumeCategoryData>();
        public List<ResumeChoiceData> choices = new List<ResumeChoiceData>();
        public List<CompanyData> companies = new List<CompanyData>();
        public List<InterviewQuestionData> questions = new List<InterviewQuestionData>();
        public List<DailyEventData> events = new List<DailyEventData>();
        public List<EndingData> endings = new List<EndingData>();

        [Header("Settings")]
        [Min(1)] public int questionsPerInterview = 2;
        [Tooltip("Show choices consumed by earlier rejections as greyed-out entries (otherwise they vanish).")]
        public bool showConsumedChoices = false;
        [Tooltip("Show rejected companies as greyed-out entries (otherwise they vanish).")]
        public bool showRejectedCompanies = true;
        public int minAge = 18;
        public int maxAge = 99;
        public int maxNameLength = 24;

        [Header("Shared dialogue")]
        [Tooltip("Played when the resume first slides into the editor.")]
        public DialoguePlaybackMode resumeIntroMode;
        public DialogueLine[] resumeIntroLines;
        [Tooltip("Played when the player confirms the resume, before the company list opens.")]
        public DialoguePlaybackMode resumeConfirmedMode;
        public DialogueLine[] resumeConfirmedLines =
        {
            new DialogueLine
            {
                speaker = Speakers.Player,
                dialogueLines = new List<string>
                {
                    "That's the best I can make it.",
                    "I've sent so many applications already.",
                    "Now, who will take me?"
                }
            }
        };
        [Tooltip("Played after a company is chosen, before the interview starts. Supports {company} and {job}.")]
        public DialoguePlaybackMode companyConfirmedMode;
        public DialogueLine[] companyConfirmedLines = { new DialogueLine(Speakers.Player, "{company}... {job}. Please let this be the one.") };
        public DialoguePlaybackMode defaultHrIntroMode;
        public DialogueLine[] defaultHrIntro;
        [Tooltip("HR says this before reacting to each resume choice.")]
        public DialoguePlaybackMode hrReviewMode;
        public DialogueLine[] hrReviewLines;
        public DialoguePlaybackMode rejectionHRMode;
        public DialogueLine[] rejectionHR;
        public DialoguePlaybackMode rejectionPlayerMode;
        public DialogueLine[] rejectionPlayer;
        [Tooltip("The father's phone call before the final decision.")]
        public DialoguePlaybackMode fatherOfferMode;
        public DialogueLine[] fatherOfferLines;

        [NonSerialized] Dictionary<string, ResumeCategoryData> categoryLookup;
        [NonSerialized] Dictionary<string, ResumeChoiceData> choiceLookup;
        [NonSerialized] Dictionary<string, CompanyData> companyLookup;
        [NonSerialized] Dictionary<string, EndingData> endingLookup;

        void OnEnable() { InvalidateLookups(); }
        void OnValidate() { InvalidateLookups(); }

        public void InvalidateLookups()
        {
            categoryLookup = null;
            choiceLookup = null;
            companyLookup = null;
            endingLookup = null;
        }

        static Dictionary<string, T> Build<T>(List<T> list, Func<T, string> idOf) where T : UnityEngine.Object
        {
            var map = new Dictionary<string, T>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item == null) continue;
                string id = idOf(item);
                if (string.IsNullOrEmpty(id)) continue;
                if (!map.ContainsKey(id)) map.Add(id, item);
            }
            return map;
        }

        public ResumeCategoryData FindCategory(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            categoryLookup ??= Build(categories, c => c.id);
            categoryLookup.TryGetValue(id, out var result);
            return result;
        }

        public ResumeChoiceData FindChoice(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            choiceLookup ??= Build(choices, c => c.id);
            choiceLookup.TryGetValue(id, out var result);
            return result;
        }

        public CompanyData FindCompany(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            companyLookup ??= Build(companies, c => c.id);
            companyLookup.TryGetValue(id, out var result);
            return result;
        }

        public EndingData FindEnding(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            endingLookup ??= Build(endings, c => c.id);
            endingLookup.TryGetValue(id, out var result);
            return result;
        }

        /// <summary>Checks content and returns human readable problems (empty list = OK).</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            CheckIds(categories, c => c.id, "Category", problems);
            CheckIds(choices, c => c.id, "Choice", problems);
            CheckIds(companies, c => c.id, "Company", problems);
            CheckIds(questions, c => c.id, "Question", problems);
            CheckIds(events, c => c.id, "Event", problems);
            CheckIds(endings, c => c.id, "Ending", problems);

            if (categories.Count == 0) problems.Add("No resume categories.");
            if (companies.Count == 0) problems.Add("No companies.");
            if (endings.Count == 0) problems.Add("No endings. The final decision needs at least one.");
            else if (endings.Count != 3) problems.Add("Endings count is " + endings.Count + " (design expects 3).");
            if (fatherOfferLines == null || fatherOfferLines.Length == 0) problems.Add("fatherOfferLines is empty.");
            if (rejectionHR == null || rejectionHR.Length == 0) problems.Add("rejectionHR is empty (companies without their own rejection will be silent).");

            foreach (var cat in categories)
            {
                if (cat == null) continue;
                int count = 0;
                foreach (var ch in choices) if (ch != null && ch.category == cat) count++;
                if (count == 0) problems.Add("Category '" + cat.name + "' has no choices.");
            }

            foreach (var ch in choices)
            {
                if (ch == null) continue;
                if (ch.category == null) problems.Add("Choice '" + ch.name + "' has no category.");
                else if (!categories.Contains(ch.category)) problems.Add("Choice '" + ch.name + "' uses a category that is not in the database list.");
                if (string.IsNullOrWhiteSpace(ch.displayText)) problems.Add("Choice '" + ch.name + "' has empty displayText.");
                if (ch.playerReaction == null || ch.playerReaction.Length == 0) problems.Add("Choice '" + ch.name + "' has no playerReaction.");
                if (ch.hrReactions == null || ch.hrReactions.Length == 0) problems.Add("Choice '" + ch.name + "' has no hrReactions (HR will skip it).");
            }

            foreach (var co in companies)
            {
                if (co == null) continue;
                if (string.IsNullOrWhiteSpace(co.companyName)) problems.Add("Company '" + co.name + "' has empty companyName.");
                if (co.playerReaction == null || co.playerReaction.Length == 0) problems.Add("Company '" + co.name + "' has no playerReaction.");
            }

            int genericCount = 0;
            foreach (var q in questions)
            {
                if (q == null) continue;
                if (string.IsNullOrWhiteSpace(q.questionText)) problems.Add("Question '" + q.name + "' has empty text.");
                if (q.answers == null || q.answers.Count == 0) problems.Add("Question '" + q.name + "' has no answers.");
                else
                {
                    var seen = new HashSet<string>();
                    foreach (var a in q.answers)
                    {
                        if (a == null || string.IsNullOrWhiteSpace(a.text)) { problems.Add("Question '" + q.name + "' has an empty answer."); continue; }
                        if (string.IsNullOrEmpty(a.id) || !seen.Add(a.id)) problems.Add("Question '" + q.name + "' has missing/duplicate answer id '" + a.id + "'.");
                        if (a.playerReaction == null || a.playerReaction.Length == 0) problems.Add("Answer '" + a.id + "' in '" + q.name + "' has no playerReaction.");
                        if (a.hrResponse == null || a.hrResponse.Length == 0) problems.Add("Answer '" + a.id + "' in '" + q.name + "' has no hrResponse.");
                    }
                }
                if (q.linkedChoice != null && q.linkedCategory != null && q.linkedChoice.category != q.linkedCategory)
                    problems.Add("Question '" + q.name + "' links a choice and a category that do not match.");
                if (q.linkedChoice != null && !choices.Contains(q.linkedChoice)) problems.Add("Question '" + q.name + "' links a choice that is not in the database.");
                if (q.linkedChoice == null && q.linkedCategory == null) genericCount++;
            }
            if (genericCount < questionsPerInterview)
                problems.Add("Only " + genericCount + " generic questions; at least " + questionsPerInterview + " recommended as fallback.");

            foreach (var ev in events)
            {
                if (ev == null) continue;
                if (ev.lines == null || ev.lines.Length == 0) problems.Add("Event '" + ev.name + "' has no lines.");
            }

            foreach (var en in endings)
            {
                if (en == null) continue;
                if (string.IsNullOrWhiteSpace(en.optionLabel)) problems.Add("Ending '" + en.name + "' has empty optionLabel.");
                if (en.lines == null || en.lines.Length == 0) problems.Add("Ending '" + en.name + "' has no lines.");
            }

            return problems;
        }

        static void CheckIds<T>(List<T> list, Func<T, string> idOf, string label, List<string> problems) where T : UnityEngine.Object
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) { problems.Add(label + " list has a missing reference at index " + i + "."); continue; }
                string id = idOf(list[i]);
                if (string.IsNullOrWhiteSpace(id)) problems.Add(label + " '" + list[i].name + "' has an empty id.");
                else if (!seen.Add(id)) problems.Add(label + " id '" + id + "' is duplicated.");
            }
        }
    }
}
