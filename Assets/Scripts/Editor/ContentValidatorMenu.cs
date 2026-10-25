using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    public static class ContentValidatorMenu
    {
        [MenuItem("Tools/First Jobber/3. Validate Content")]
        public static void Validate()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
            if (db == null)
            {
                var guids = AssetDatabase.FindAssets("t:GameDatabase");
                if (guids.Length > 0) db = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (db == null) { Debug.LogError("[Content] No GameDatabase found."); return; }

            var problems = db.Validate();
            var atmosphere = AssetDatabase.LoadAssetAtPath<AtmosphereConfiguration>(AtmosphereTools.ConfigPath);
            if (atmosphere != null) problems.AddRange(atmosphere.Validate());
            if (problems.Count == 0)
            {
                Debug.Log("[Content] OK - no problems found.", db);
                return;
            }
            foreach (var p in problems) Debug.LogWarning("[Content] " + p, db);
            Debug.LogWarning("[Content] " + problems.Count + " problem(s) found.", db);
        }
    }
}
