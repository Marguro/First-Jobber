using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    [UnityEditor.CustomEditor(typeof(GameDatabase))]
    public class GameDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var db = (GameDatabase)target;
            if (!db.requireDayBasedConfidence && db.confidenceDialogue == null)
            {
                DrawDefaultInspector();
                return;
            }

            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            DrawPropertiesExcluding(serializedObject, "m_Script", "resumeConfirmedThreshold", "highConfidenceConfirmedLines",
                "lowConfidenceConfirmedLines", "resumeConfirmedMode", "resumeConfirmedLines");
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox(db.confidenceDialogue != null
                ? "Company Apply uses the assigned day-based configuration. Legacy Resume Confirmed content is preserved but ignored."
                : "Required day-based configuration is missing or has a missing script. Repair the reference; legacy confidence dialogue is disabled.",
                db.confidenceDialogue != null ? MessageType.Info : MessageType.Error);
        }
    }

    [UnityEditor.CustomEditor(typeof(ConfidenceDialogueDatabase))]
    public class ConfidenceDialogueDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
                EditorGUILayout.EnumPopup("Playback Mode", ((ConfidenceDialogueDatabase)target).PlaybackMode);
            }
            EditorGUILayout.HelpBox("RandomOne selects one candidate per Apply. Every Dialogue Line/page inside that candidate plays in order. Exactly the threshold is Low.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("threshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("days"), new GUIContent("Day Configurations"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fallbackHigh"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fallbackLow"), true);
            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Validate Production Confidence Dialogue")) ProductionConfidenceValidation.Validate();
        }
    }

    public static class ProductionConfidenceValidation
    {
        const string Root = "Assets/Data/Production/";
        const string RequestPath = "Temp/ProductionConfidenceTests/request-editor-validation";
        const string ReportPath = "Temp/ProductionConfidenceTests/editor-validation.json";

        [System.Serializable]
        class Report
        {
            public bool passed;
            public string scriptGuid;
            public string assetGuid;
            public List<string> problems;
        }

        // Opt-in, read-only check in the actual running Editor after a script reload.
        // No validation is scheduled during ordinary project startup unless a request file exists.
        [InitializeOnLoadMethod]
        static void ValidateRequestedOnReload()
        {
            if (!File.Exists(RequestPath)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                Validate();
                File.Delete(RequestPath);
            };
        }

        // Checks Unity's imported types, rather than trusting text counts in the asset YAML.
        [MenuItem("Tools/First Jobber/24. Validate Production Confidence Dialogue")]
        public static void Validate()
        {
            var problems = new List<string>();
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Data/ConfidenceDialogueDatabase.cs");
            var config = AssetDatabase.LoadAssetAtPath<ConfidenceDialogueDatabase>(Root + "ConfidenceDialogue_Production.asset");
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "GameDatabase_Production.asset");
            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/LocalizationTable_TH.asset");
            if (script == null || script.GetClass() != typeof(ConfidenceDialogueDatabase))
                problems.Add("MonoScript does not resolve to FirstJobber.ConfidenceDialogueDatabase. Check compilation and the script .meta.");
            if (config == null) problems.Add("Production confidence asset cannot be loaded as ConfidenceDialogueDatabase. Check its m_Script GUID.");
            else if (MonoScript.FromScriptableObject(config) != script) problems.Add("Production confidence asset uses a different MonoScript.");
            if (db == null || !db.requireDayBasedConfidence || db.confidenceDialogue != config || config == null)
                problems.Add("Production database must require and reference the existing Production confidence asset.");
            if (table == null) problems.Add("Thai localization table is missing.");

            if (config != null && table != null)
            {
                LocalizationKeys.VisitAsset(config, null);
                if (config.threshold != 50f) problems.Add("Production confidence threshold must be 50.");
                if (config.days == null || config.days.Count != 5) problems.Add("Expected five day configurations.");
                int[] high = { 3, 1, 2, 2, 3 }, low = { 5, 2, 1, 2, 3 };
                var daysSeen = new HashSet<int>();
                if (config.days != null)
                    foreach (var day in config.days)
                    {
                        if (day == null || day.day < 1 || day.day > 5 || !daysSeen.Add(day.day))
                        { problems.Add("Missing/duplicate/invalid day configuration."); continue; }
                        CheckPool(day.high, high[day.day - 1], day.day, "High", table, problems);
                        CheckPool(day.low, low[day.day - 1], day.day, "Low", table, problems);
                    }
            }
            foreach (var problem in problems) Debug.LogError("[Production Confidence] " + problem);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                passed = problems.Count == 0,
                scriptGuid = AssetDatabase.AssetPathToGUID("Assets/Scripts/Data/ConfidenceDialogueDatabase.cs"),
                assetGuid = AssetDatabase.AssetPathToGUID(Root + "ConfidenceDialogue_Production.asset"),
                problems = problems
            }, true));
            if (problems.Count == 0)
                Debug.Log("[Production Confidence] PASS: Unity resolves the MonoScript and existing asset; Production references it; five days, 24 Player candidates, English and Thai present, threshold 50, RandomOne. Play Mode Apply/HR timing still requires testing.", config);
        }

        static void CheckPool(List<ConfidenceCandidate> pool, int count, int day, string branch,
            LocalizationTable table, List<string> problems)
        {
            string label = "Day " + day + " " + branch;
            if (pool == null || pool.Count != count) { problems.Add(label + " has an incorrect candidate count."); return; }
            var ids = new HashSet<string>();
            foreach (var candidate in pool)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.id) || !ids.Add(candidate.id))
                { problems.Add(label + " has a missing/duplicate candidate ID."); continue; }
                var dialogue = candidate.dialogue;
                if (dialogue == null || !dialogue.HasContent || dialogue.speaker != Speakers.Player)
                { problems.Add(label + "/" + candidate.id + " has missing content or an incorrect speaker."); continue; }
                for (int page = 0; page < dialogue.PageCount; page++)
                {
                    string key = dialogue.GetPageKey(page), english = dialogue.GetPage(page);
                    if (string.IsNullOrWhiteSpace(english) || !table.TryGet(key, out var thai) || string.IsNullOrWhiteSpace(thai))
                        problems.Add(label + "/" + candidate.id + " has missing English/Thai for " + key + ".");
                }
            }
        }
    }
}
