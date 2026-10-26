using System;
using System.Collections.Generic;
using System.Linq;
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

        [Header("HR characters (one per company - see CompanyData.hr)")]
        public List<HRCharacterData> hrCharacters = new List<HRCharacterData>();
        [Tooltip("Optional. Used ONLY for a company that has no HR assigned. Leave empty to treat that as a content error.")]
        public HRCharacterData fallbackHr;

        [Header("Applicant gender (dropdown options - edit freely)")]
        public List<GenderOption> genders = new List<GenderOption>
        {
            new GenderOption("male", "Male"),
            new GenderOption("female", "Female"),
            new GenderOption("non_binary", "Non-binary"),
            new GenderOption(Genders.Unspecified, "Prefer Not to Say")
        };
        [Tooltip("First dropdown entry. Choosing it is rejected, so the player must pick a real option.")]
        public string genderPrompt = "Select gender...";

        [Header("Settings")]
        [Min(1)] public int questionsPerInterview = 2;
        [Tooltip("Show choices consumed by earlier rejections as greyed-out entries (otherwise they vanish).")]
        public bool showConsumedChoices = false;
        [Tooltip("Show rejected companies as greyed-out entries (otherwise they vanish).")]
        public bool showRejectedCompanies = true;
        [HideInInspector] public int minAge = 18; // legacy, unused
        [HideInInspector] public int maxAge = 99; // legacy, unused
        public int maxNameLength = 24;

        [Header("Shared dialogue")]
        [Tooltip("Played when the resume first slides into the editor.")]
        public DialoguePlaybackMode resumeIntroMode;
        public DialogueLine[] resumeIntroLines;
        [Header("Resume confirmed (depends on resume confidence)")]
        [Tooltip("Overall confidence ABOVE this value plays the High pool. Equal to it or below plays the Low pool.")]
        [Range(0f, 100f)] public float resumeConfirmedThreshold = 50f;
        [Tooltip("Played when the confirmed confidence is above the threshold. ONE random block is played.")]
        public DialogueLine[] highConfidenceConfirmedLines =
        {
            new DialogueLine
            {
                speaker = Speakers.Player,
                dialogueLines = new List<string> { "I think this looks good.", "I've prepared everything.", "This time will be different." }
            },
            new DialogueLine
            {
                speaker = Speakers.Player,
                dialogueLines = new List<string> { "That's the best I can make it.", "Now, who will take me?" }
            }
        };
        [Tooltip("Played when the confirmed confidence is equal to or below the threshold. ONE random block is played.")]
        public DialogueLine[] lowConfidenceConfirmedLines =
        {
            new DialogueLine
            {
                speaker = Speakers.Player,
                dialogueLines = new List<string> { "I don't think this is enough.", "Maybe I should try anyway." }
            },
            new DialogueLine
            {
                speaker = Speakers.Player,
                dialogueLines = new List<string> { "There's barely anything left.", "Why does this keep happening?", "I guess I'll submit it." }
            }
        };
        [Header("Resume confirmed - legacy / last resort")]
        [Tooltip("Old single collection. Played only when BOTH confidence pools above have no usable block.")]
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

        /// <summary>
        /// Chooses the dialogue collection for Confirm Resume. Confidence is NOT random: above the threshold = High,
        /// otherwise Low. An empty chosen pool falls back to the other pool, then to the legacy collection.
        /// The caller plays the result with the returned mode (pools are always RandomOne: one block, all its lines).
        /// </summary>
        public DialogueLine[] ResolveResumeConfirmedLines(float confidence, out DialoguePlaybackMode mode)
        {
            bool high = confidence > resumeConfirmedThreshold;
            var primary = high ? highConfidenceConfirmedLines : lowConfidenceConfirmedLines;
            var other = high ? lowConfidenceConfirmedLines : highConfidenceConfirmedLines;
            mode = DialoguePlaybackMode.RandomOne;
            if (HasUsableBlock(primary)) return primary;
            if (HasUsableBlock(other)) return other;
            mode = resumeConfirmedMode;
            return resumeConfirmedLines; // may be empty: the dialogue then completes immediately
        }

        static bool HasUsableBlock(DialogueLine[] lines)
        {
            if (lines == null) return false;
            for (int i = 0; i < lines.Length; i++) if (lines[i] != null && lines[i].HasContent) return true;
            return false;
        }

        public GenderOption FindGender(string id)
        {
            if (string.IsNullOrEmpty(id) || genders == null) return null;
            for (int i = 0; i < genders.Count; i++) if (genders[i] != null && genders[i].id == id) return genders[i];
            return null;
        }

        /// <summary>Display name for a gender id; unknown ids fall back to the id itself.</summary>
        public string GenderDisplay(string id)
        {
            var g = FindGender(id);
            return g != null && !string.IsNullOrEmpty(g.displayName) ? LocalizationManager.Get(GenderKey(g.id), g.displayName) : (id ?? "");
        }

        /// <summary>Localization key of a gender option's display name.</summary>
        public static string GenderKey(string id) { return "database/genders/" + id; }

        /// <summary>Localization key of the gender dropdown prompt.</summary>
        public const string GenderPromptKey = "database/genderPrompt";

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
            if (!HasUsableBlock(highConfidenceConfirmedLines) && !HasUsableBlock(lowConfidenceConfirmedLines) && !HasUsableBlock(resumeConfirmedLines))
                problems.Add("No Resume Confirmed dialogue (high, low and legacy are all empty): confirming a resume will be silent.");
            else if (!HasUsableBlock(highConfidenceConfirmedLines) || !HasUsableBlock(lowConfidenceConfirmedLines))
                problems.Add("Only one Resume Confirmed confidence pool has dialogue; the other confidence level reuses it.");
            if (fatherOfferLines == null || fatherOfferLines.Length == 0) problems.Add("fatherOfferLines is empty.");
            if (rejectionHR == null || rejectionHR.Length == 0) problems.Add("rejectionHR is empty (companies without their own rejection will be silent).");

            // gender options
            if (genders == null || genders.Count == 0) problems.Add("No gender options: the applicant dropdown would be empty.");
            else
            {
                var seenGender = new HashSet<string>();
                foreach (var g in genders)
                {
                    if (g == null || string.IsNullOrWhiteSpace(g.id)) { problems.Add("A gender option has an empty id."); continue; }
                    if (!seenGender.Add(g.id)) problems.Add("Gender id '" + g.id + "' is duplicated.");
                    if (string.IsNullOrWhiteSpace(g.displayName)) problems.Add("Gender '" + g.id + "' has no display name.");
                }
                if (!seenGender.Contains(Genders.Unspecified)) problems.Add("No '" + Genders.Unspecified + "' gender option (used for old saves).");
            }

            // HR characters
            CheckIds(hrCharacters, h => h.id, "HR", problems);
            var questionOwner = new Dictionary<InterviewQuestionData, HRCharacterData>();
            foreach (var hr in hrCharacters)
            {
                if (hr == null) continue;
                hr.Validate(problems);
                foreach (var q in hr.fixedQuestions.Concat(hr.resumeLinkedQuestions).Append(hr.fallbackQuestion))
                {
                    if (q == null) continue;
                    if (questionOwner.TryGetValue(q, out var other) && other != hr)
                        problems.Add("Question '" + q.name + "' is used by two HR characters ('" + other.name + "' and '" + hr.name + "'). Give each HR its own question asset.");
                    else questionOwner[q] = hr;
                }
            }
            foreach (var co in companies)
            {
                if (co == null) continue;
                if (co.hr == null && fallbackHr == null)
                    problems.Add("Company '" + co.name + "' has no HR assigned (and no fallbackHr): it will run the old shared interview.");
                else if (co.hr != null && !hrCharacters.Contains(co.hr))
                    problems.Add("Company '" + co.name + "' uses HR '" + co.hr.name + "' which is not in the database HR list.");
            }

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
