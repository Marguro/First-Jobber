using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Picks interview questions from the Question Bank:
    /// 1) exact choice match, 2) category match, 3) generic fallback.
    /// Questions not asked in earlier interviews come first; within a tier higher weight wins, ties keep database order (deterministic).
    /// </summary>
    public static class InterviewQuestionSelector
    {
        static InterviewQuestionData emergencyQuestion;

        public static void Pick(GameDatabase db, GameSession session, IList<ResumeChoiceData> selected,
            CompanyData company, int count, List<InterviewQuestionData> result)
        {
            result.Clear();
            var exact = new List<InterviewQuestionData>();
            var category = new List<InterviewQuestionData>();
            var generic = new List<InterviewQuestionData>();

            foreach (var q in db.questions)
            {
                if (q == null || q.answers == null || q.answers.Count == 0) continue;
                if (!ConditionsMet(q, selected, company)) continue;

                if (q.linkedChoice != null)
                {
                    if (selected.Contains(q.linkedChoice)) exact.Add(q);
                }
                else if (q.linkedCategory != null)
                {
                    if (selected.Any(c => c.category == q.linkedCategory)) category.Add(q);
                }
                else generic.Add(q);
            }

            var ordered = new List<InterviewQuestionData>();
            ordered.AddRange(exact.OrderByDescending(q => q.weight));
            ordered.AddRange(category.OrderByDescending(q => q.weight));
            ordered.AddRange(generic.OrderByDescending(q => q.weight));

            // Pass 1: fresh questions only. Pass 2: allow repeats if the bank is too small.
            for (int pass = 0; pass < 2 && result.Count < count; pass++)
            {
                foreach (var q in ordered)
                {
                    if (result.Count >= count) break;
                    if (result.Contains(q)) continue;
                    if (pass == 0 && session.askedQuestionIds.Contains(q.id)) continue;
                    result.Add(q);
                }
            }

            if (result.Count == 0)
            {
                Debug.LogWarning("[Interview] No usable questions in the Question Bank - using the built-in emergency question.");
                result.Add(GetEmergencyQuestion());
            }
        }

        static bool ConditionsMet(InterviewQuestionData q, IList<ResumeChoiceData> selected, CompanyData company)
        {
            if (q.requiredCompany != null && q.requiredCompany != company) return false;
            if (!string.IsNullOrEmpty(q.requiredTag))
            {
                bool has = company != null && company.HasTag(q.requiredTag);
                for (int i = 0; !has && i < selected.Count; i++) has = selected[i].HasTag(q.requiredTag);
                if (!has) return false;
            }
            return true;
        }

        static InterviewQuestionData GetEmergencyQuestion()
        {
            if (emergencyQuestion != null) return emergencyQuestion;
            emergencyQuestion = ScriptableObject.CreateInstance<InterviewQuestionData>();
            emergencyQuestion.name = "EmergencyQuestion";
            emergencyQuestion.id = "__emergency";
            emergencyQuestion.questionText = "Tell me about yourself.";
            emergencyQuestion.answers = new List<InterviewAnswer>
            {
                new InterviewAnswer
                {
                    id = "a1",
                    text = "I'm a recent graduate looking for an opportunity.",
                    playerReaction = new[] { new DialogueLine(Speakers.Player, "That sounded rehearsed.") },
                    hrResponse = new[] { new DialogueLine(Speakers.HR, "I see.", HREmotion.Neutral) }
                }
            };
            return emergencyQuestion;
        }
    }
}
