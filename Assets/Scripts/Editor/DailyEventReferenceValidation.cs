using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    /// <summary>Validate native imported objects, not just YAML. Repairs only stale wrappers whose
    /// exact persisted GUID still resolves; never guesses by name/ID or creates replacement content.</summary>
    [InitializeOnLoad]
    public static class DailyEventReferenceValidation
    {
        const string DatabasePath = "Assets/Data/Production/GameDatabase_Production.asset";
        static DailyEventReferenceValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) ValidateProductionReferences();
            };
        }

        [MenuItem("Tools/First Jobber/Localization/Validate Production Daily Event References")]
        public static void ValidateProductionReferences()
        {
            var errors = Validate(true);
            foreach (var error in errors) Debug.LogError("[Daily Event References] " + error);
            if (errors.Count == 0) Debug.Log("[Daily Event References] Production events are persistent, live, uniquely identified, and match their serialized GUIDs.");
        }

        public static List<string> Validate(bool repairStaleWrappers)
        {
            var errors = new List<string>();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (db == null) { errors.Add("Production database cannot be loaded: " + DatabasePath); return errors; }
            if (db.events == null) { errors.Add("GameDatabase_Production.events is null."); return errors; }
            string yaml = File.ReadAllText(DatabasePath);
            var section = Regex.Match(yaml, @"(?ms)^  events:\s*\r?\n(?<entries>.*?)(?=^  [A-Za-z_]|\z)");
            if (!section.Success) { errors.Add("Cannot verify persisted event GUIDs; database event list was not changed."); return errors; }
            string[] entries = Regex.Split(section.Groups["entries"].Value, @"(?m)^  -");
            if (entries.Length - 1 != db.events.Count)
            { errors.Add("In-memory event count differs from the saved database. Save/review pending Inspector edits; no automatic replacement performed."); return errors; }

            var ids = new HashSet<string>();
            for (int i = 0; i < db.events.Count; i++)
            {
                string context = "GameDatabase_Production.events[" + i + "]";
                var guid = Regex.Match(entries[i + 1], @"guid:\s*([a-fA-F0-9]{32})");
                if (!guid.Success) { errors.Add(context + " has no persisted asset GUID."); continue; }
                string path = AssetDatabase.GUIDToAssetPath(guid.Groups[1].Value);
                var canonical = AssetDatabase.LoadAssetAtPath<DailyEventData>(path);
                if (canonical == null)
                { errors.Add(context + " cannot resolve DailyEventData at GUID " + guid.Groups[1].Value + " (" + path + "). Check script/import errors."); continue; }
                var current = db.events[i];
                // A real null may be an intentional, unsaved Inspector edit. Repair only a
                // destroyed managed wrapper, never overwrite a user's newly cleared slot.
                if (current == null && !ReferenceEquals(current, null) && repairStaleWrappers)
                {
                    string state = ReferenceEquals(current, null) ? "missing" : "destroyed managed wrapper";
                    Undo.RecordObject(db, "Restore same-GUID Daily Event reference");
                    db.events[i] = canonical;
                    Debug.LogWarning("[Daily Event References] Restored " + context + " (" + state + ") to its EXISTING serialized GUID: " + path + ", id=" + canonical.id + ". No event data or GUID changed.", db);
                    current = canonical;
                }
                if (current == null) { errors.Add(context + " is missing/destroyed; expected " + path); continue; }
                if (!EditorUtility.IsPersistent(current) || !AssetDatabase.Contains(current))
                { errors.Add(context + " is a transient instance, not an authored asset. Expected " + path + "; investigate its runtime owner."); continue; }
                if (AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(current)) != guid.Groups[1].Value)
                { errors.Add(context + " differs from its persisted GUID; preserve/review pending edits. Expected " + path); continue; }
                if (string.IsNullOrWhiteSpace(current.id) || !ids.Add(current.id))
                    errors.Add(context + " has an empty/duplicate event ID: " + current.id + " (" + path + ")");
            }
            return errors;
        }
    }

    public class DailyEventReferenceBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            var errors = DailyEventReferenceValidation.Validate(true);
            if (errors.Count > 0) throw new BuildFailedException("Invalid Production Daily Event references:\n" + string.Join("\n", errors));
        }
    }
}
