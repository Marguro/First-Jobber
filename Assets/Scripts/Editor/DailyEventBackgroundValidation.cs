using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    public static class DailyEventBackgroundValidation
    {
        [MenuItem("Tools/First Jobber/Validate Daily Event Backgrounds")]
        public static void Validate()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/Production/GameDatabase_Production.asset");
            if (db == null) { Debug.LogError("[DailyEventBackground] Production database missing."); return; }
            var required = new HashSet<string> { "event_day01_opening", "event_day02_before_resume", "event_day04_before_resume", "event_day06_final_story" };
            var ids = new HashSet<string>();
            for (int i = 0; i < db.events.Count; i++)
            {
                var ev = db.events[i];
                if (ev == null) { Debug.LogError("[DailyEventBackground] Missing/destroyed events[" + i + "].", db); continue; }
                if (string.IsNullOrEmpty(ev.id) || !ids.Add(ev.id)) Debug.LogError("[DailyEventBackground] Missing/duplicate ID at events[" + i + "].", db);
                bool requiredArt = required.Remove(ev.id);
                if (requiredArt && ev.background == null) Debug.LogError("[DailyEventBackground] Required background is missing: " + ev.id, ev);
                Debug.Log("[DailyEventBackground] " + ev.id + " | " + AssetDatabase.GetAssetPath(ev)
                    + " | " + (ev.background != null ? AssetDatabase.GetAssetPath(ev.background) + " / " + ev.background.name : "unassigned: existing fallback policy"), ev);
            }
            foreach (var missing in required) Debug.LogError("[DailyEventBackground] Event absent from Production database: " + missing, db);
            var manager = Object.FindFirstObjectByType<DailyEventManager>(FindObjectsInactive.Include);
            if (manager == null) { Debug.LogWarning("[DailyEventBackground] Open the Production Game scene to validate UI references."); return; }
            var panel = new SerializedObject(manager).FindProperty("eventPanel").objectReferenceValue as DailyEventPanelUI;
            if (panel == null) { Debug.LogError("[DailyEventBackground] DailyEventManager.eventPanel is missing.", manager); return; }
            var serialized = new SerializedObject(panel);
            var image = serialized.FindProperty("background").objectReferenceValue as Image;
            var group = serialized.FindProperty("presentationGroup").objectReferenceValue as CanvasGroup;
            var panels = serialized.FindProperty("panels").objectReferenceValue as UIManager;
            if (image == null || group == null || panels == null) Debug.LogError("[DailyEventBackground] Background/presentation group/UIManager reference missing.", panel);
            else
            {
                if (image.transform.IsChildOf(panel.transform)) Debug.LogError("[DailyEventBackground] Background still inherits the hidden DailyEventPanel. Day 6 cannot display it.", image);
                if (image.raycastTarget || group.blocksRaycasts) Debug.LogError("[DailyEventBackground] Decorative background must not block input.", image);
                Debug.Log("[DailyEventBackground] Existing background target=" + image.name + "; own CanvasGroup=" + group.name + "; aspect=" + image.preserveAspect + ". Validation is read-only.", image);
            }
        }
    }
}
