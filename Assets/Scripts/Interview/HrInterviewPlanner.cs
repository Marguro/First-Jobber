using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Builds the question list of one interview from the assigned HR's own question lists and sequence.
    /// Deterministic unless the HR's pick modes are Random; the plan is built once per interview (never on UI refresh)
    /// and never repeats a question inside one interview. Never returns an empty list.
    /// </summary>
    public static class HrInterviewPlanner
    {
        public static void Plan(HRCharacterData hr, GameSession session, IList<ResumeChoiceData> selected,
            CompanyData company, List<InterviewQuestionData> result)
        {
            result.Clear();
            int fixedCursor = 0;
            var slots = hr.EffectiveSequence();

            for (int s = 0; s < slots.Count; s++)
            {
                var slot = slots[s];
                if (slot == null) continue;

                InterviewQuestionData q = null;
                if (slot.type == InterviewSlotType.Fixed)
                {
                    if (Usable(slot.fixedQuestion)) q = slot.fixedQuestion;
                    else if (hr.fixedPickMode == QuestionPickMode.Random)
                    {
                        var pool = new List<InterviewQuestionData>();
                        foreach (var candidate in hr.fixedQuestions)
                            if (Usable(candidate) && !result.Contains(candidate) && !pool.Contains(candidate)) pool.Add(candidate);
                        if (pool.Count > 0) q = pool[Random.Range(0, pool.Count)];
                    }
                    else
                    {
                        // Next unused question of the Fixed list, in the order the designer wrote it.
                        while (q == null && fixedCursor < hr.fixedQuestions.Count)
                        {
                            var candidate = hr.fixedQuestions[fixedCursor++];
                            if (Usable(candidate) && !result.Contains(candidate)) q = candidate;
                        }
                    }
                }
                else q = PickResumeLinked(hr, session, selected, company, result);

                if (q != null && result.Contains(q)) q = null;
                if (q == null && Usable(hr.fallbackQuestion) && !result.Contains(hr.fallbackQuestion)) q = hr.fallbackQuestion;
                if (q == null)
                {
                    Debug.LogWarning("[Interview] " + hr.name + " slot " + s + " (" + slot.type + ") has no question; slot skipped.", hr);
                    continue;
                }
                result.Add(q);
            }

            if (result.Count == 0)
            {
                Debug.LogWarning("[Interview] " + hr.name + " produced no questions - using the built-in emergency question.", hr);
                result.Add(InterviewQuestionSelector.GetEmergencyQuestion());
            }
        }

        // Exact choice match first, then category match. Inside a tier: not-yet-asked first, higher weight first, then list order.
        static InterviewQuestionData PickResumeLinked(HRCharacterData hr, GameSession session, IList<ResumeChoiceData> selected,
            CompanyData company, List<InterviewQuestionData> alreadyUsed)
        {
            var exact = new List<InterviewQuestionData>();
            var category = new List<InterviewQuestionData>();

            foreach (var q in hr.resumeLinkedQuestions)
            {
                if (!Usable(q) || alreadyUsed.Contains(q) || !ConditionsMet(q, selected, company)) continue;
                if (q.linkedChoice != null) { if (selected.Contains(q.linkedChoice)) exact.Add(q); }
                else if (q.linkedCategory != null) { if (selected.Any(c => c != null && c.category == q.linkedCategory)) category.Add(q); }
            }

            if (hr.resumeLinkedPickMode == QuestionPickMode.Random)
            {
                exact.AddRange(category); // every match is equally likely
                return exact.Count > 0 ? exact[Random.Range(0, exact.Count)] : null;
            }
            return PickTier(exact, session) ?? PickTier(category, session);
        }

        static InterviewQuestionData PickTier(List<InterviewQuestionData> tier, GameSession session)
        {
            if (tier.Count == 0) return null;
            var ordered = tier.OrderByDescending(q => q.weight).ToList(); // stable: ties keep list order
            foreach (var q in ordered) if (!session.askedQuestionIds.Contains(q.id)) return q;
            return ordered[0];
        }

        static bool Usable(InterviewQuestionData q) => q != null && q.answers != null && q.answers.Count > 0;

        static bool ConditionsMet(InterviewQuestionData q, IList<ResumeChoiceData> selected, CompanyData company)
        {
            if (q.requiredCompany != null && q.requiredCompany != company) return false;
            if (!string.IsNullOrEmpty(q.requiredTag))
            {
                bool has = company != null && company.HasTag(q.requiredTag);
                for (int i = 0; !has && i < selected.Count; i++) has = selected[i] != null && selected[i].HasTag(q.requiredTag);
                if (!has) return false;
            }
            return true;
        }
    }
}
