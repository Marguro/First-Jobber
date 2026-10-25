using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Runtime view over static choices + session state: what is available, selected or consumed.
    /// </summary>
    public class ChoiceManager
    {
        readonly GameDatabase db;
        readonly GameSession session;

        public ChoiceManager(GameDatabase db, GameSession session)
        {
            this.db = db;
            this.session = session;
        }

        public bool IsConsumed(ResumeChoiceData choice) => choice != null && session.consumedChoiceIds.Contains(choice.id);
        public bool IsConsumed(CompanyData company) => company != null && session.consumedCompanyIds.Contains(company.id);

        /// <summary>Choices of a category that can still be picked (database order).</summary>
        public void GetAvailableChoices(ResumeCategoryData category, List<ResumeChoiceData> result)
        {
            result.Clear();
            for (int i = 0; i < db.choices.Count; i++)
            {
                var c = db.choices[i];
                if (c != null && c.category == category && !IsConsumed(c)) result.Add(c);
            }
        }

        /// <summary>Choices to display: available ones, plus consumed ones if the database says so.</summary>
        public void GetVisibleChoices(ResumeCategoryData category, List<ResumeChoiceData> result)
        {
            result.Clear();
            for (int i = 0; i < db.choices.Count; i++)
            {
                var c = db.choices[i];
                if (c == null || c.category != category) continue;
                if (!IsConsumed(c) || db.showConsumedChoices) result.Add(c);
            }
        }

        public int CountAvailable(ResumeCategoryData category)
        {
            int n = 0;
            for (int i = 0; i < db.choices.Count; i++)
            {
                var c = db.choices[i];
                if (c != null && c.category == category && !IsConsumed(c)) n++;
            }
            return n;
        }

        public ResumeChoiceData GetSelectedChoice(ResumeCategoryData category)
        {
            var c = db.FindChoice(session.GetSelectedChoiceId(category.id));
            return c != null && !IsConsumed(c) ? c : null;
        }

        public bool SelectChoice(ResumeChoiceData choice)
        {
            if (choice == null || choice.category == null || IsConsumed(choice)) return false;
            session.SetSelection(choice.category.id, choice.id);
            return true;
        }

        /// <summary>Drops selections whose choice no longer exists or was consumed.</summary>
        public void PruneInvalidSelections()
        {
            for (int i = session.selections.Count - 1; i >= 0; i--)
            {
                var c = db.FindChoice(session.selections[i].choiceId);
                if (c == null || IsConsumed(c)) session.selections.RemoveAt(i);
            }
        }

        /// <summary>Categories that still have at least one available choice.</summary>
        public void GetRequiredCategories(List<ResumeCategoryData> result)
        {
            result.Clear();
            for (int i = 0; i < db.categories.Count; i++)
            {
                var cat = db.categories[i];
                if (cat != null && CountAvailable(cat) > 0) result.Add(cat);
            }
        }

        static readonly List<ResumeCategoryData> requiredBuffer = new List<ResumeCategoryData>();

        public bool CanConfirm()
        {
            GetRequiredCategories(requiredBuffer);
            if (requiredBuffer.Count == 0) return false;
            for (int i = 0; i < requiredBuffer.Count; i++)
                if (GetSelectedChoice(requiredBuffer[i]) == null) return false;
            return true;
        }

        readonly List<ResumeChoiceData> confidenceBuffer = new List<ResumeChoiceData>();

        /// <summary>Live overall confidence (0-100) of the currently selected, still-available choices. Does not modify anything.</summary>
        public float GetOverallConfidence(out int selectedCount)
        {
            GetSelectedChoices(confidenceBuffer);
            return ResumeConfidenceCalculator.Calculate(confidenceBuffer, out selectedCount);
        }

        /// <summary>Selected choices in category order (only categories with a valid selection).</summary>
        public void GetSelectedChoices(List<ResumeChoiceData> result)
        {
            result.Clear();
            for (int i = 0; i < db.categories.Count; i++)
            {
                if (db.categories[i] == null) continue;
                var c = GetSelectedChoice(db.categories[i]);
                if (c != null) result.Add(c);
            }
        }

        public void GetAvailableCompanies(List<CompanyData> result)
        {
            result.Clear();
            for (int i = 0; i < db.companies.Count; i++)
            {
                var c = db.companies[i];
                if (c != null && !IsConsumed(c)) result.Add(c);
            }
        }

        static readonly List<CompanyData> companyBuffer = new List<CompanyData>();

        /// <summary>True if the player can still build a full application.</summary>
        public bool HasValidApplication()
        {
            GetAvailableCompanies(companyBuffer);
            if (companyBuffer.Count == 0) return false;
            GetRequiredCategories(requiredBuffer);
            return requiredBuffer.Count > 0;
        }

        /// <summary>Marks today's choices + company consumed, writes history, clears temporary state.</summary>
        public void ConsumeApplication(string result)
        {
            var entry = new HistoryEntry { day = session.day, companyId = session.selectedCompanyId, result = result, confidence = session.confirmedConfidence };
            for (int i = 0; i < session.selections.Count; i++)
            {
                string id = session.selections[i].choiceId;
                if (string.IsNullOrEmpty(id)) continue;
                entry.choiceIds.Add(id);
                if (!session.consumedChoiceIds.Contains(id)) session.consumedChoiceIds.Add(id);
            }
            if (!string.IsNullOrEmpty(session.selectedCompanyId) && !session.consumedCompanyIds.Contains(session.selectedCompanyId))
                session.consumedCompanyIds.Add(session.selectedCompanyId);

            session.history.Add(entry);
            session.selections.Clear();
            session.confirmedConfidence = 0f;
            session.selectedCompanyId = null;
            session.interviewQuestionIds.Clear();
            session.interviewIndex = 0;
        }
    }
}
