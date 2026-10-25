using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FirstJobber
{
    [Serializable]
    public class ChoiceSelection
    {
        public string categoryId;
        public string choiceId;
    }

    [Serializable]
    public class HistoryEntry
    {
        public int day;
        public string companyId;
        public List<string> choiceIds = new List<string>();
        public string result;
        [Tooltip("Overall confidence that was confirmed for this application.")]
        public float confidence;
    }

    /// <summary>
    /// The only owner of runtime progress. Plain serializable data (JSON-friendly).
    /// ScriptableObject content is never written to.
    /// </summary>
    [Serializable]
    public class GameSession
    {
        public string applicantName;
        public int applicantAge;
        public int day = 1;
        public GamePhase phase;

        // Today's temporary state
        public List<ChoiceSelection> selections = new List<ChoiceSelection>();
        public string selectedCompanyId;
        public bool applicationOpen;
        public List<string> interviewQuestionIds = new List<string>();
        public int interviewIndex;
        /// <summary>Overall resume confidence (0-100) locked in when today's resume was confirmed. Cleared when the application is consumed.</summary>
        public float confirmedConfidence;

        // Permanent state for this playthrough
        public List<string> consumedChoiceIds = new List<string>();
        public List<string> consumedCompanyIds = new List<string>();
        public List<HistoryEntry> history = new List<HistoryEntry>();
        public List<string> triggeredEvents = new List<string>();
        public List<string> flags = new List<string>();
        public List<string> askedQuestionIds = new List<string>();
        public string endingId;

        // ---- selections ----
        public string GetSelectedChoiceId(string categoryId)
        {
            for (int i = 0; i < selections.Count; i++)
                if (selections[i].categoryId == categoryId) return selections[i].choiceId;
            return null;
        }

        public void SetSelection(string categoryId, string choiceId)
        {
            for (int i = 0; i < selections.Count; i++)
            {
                if (selections[i].categoryId != categoryId) continue;
                selections[i].choiceId = choiceId;
                return;
            }
            selections.Add(new ChoiceSelection { categoryId = categoryId, choiceId = choiceId });
        }

        public void RemoveSelection(string categoryId)
        {
            for (int i = selections.Count - 1; i >= 0; i--)
                if (selections[i].categoryId == categoryId) selections.RemoveAt(i);
        }

        // ---- flags ----
        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag)) flags.Add(flag);
        }

        // ---- persistence ----
        static string SavePath => Path.Combine(Application.persistentDataPath, "first_jobber_save.json");

        public static bool HasSave() => File.Exists(SavePath);

        public void Save()
        {
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(this)); }
            catch (Exception e) { Debug.LogWarning("[GameSession] Save failed: " + e.Message); }
        }

        public static bool TryLoad(out GameSession session)
        {
            session = null;
            try
            {
                if (!File.Exists(SavePath)) return false;
                session = JsonUtility.FromJson<GameSession>(File.ReadAllText(SavePath));
                return session != null && !string.IsNullOrEmpty(session.applicantName);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameSession] Load failed: " + e.Message);
                return false;
            }
        }

        public static void DeleteSave()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception e) { Debug.LogWarning("[GameSession] Delete save failed: " + e.Message); }
        }
    }
}
