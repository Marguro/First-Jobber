using UnityEditor;
using UnityEngine;

namespace FirstJobber
{
    public static class UICorruptionPreview
    {
        [MenuItem("Tools/First Jobber/UI Corruption/Preview Phase 2 (Play Mode)")]
        static void Phase2() { Preview("Assets/Data/Atmosphere/Phase_02_Deterioration.asset"); }
        [MenuItem("Tools/First Jobber/UI Corruption/Preview Phase 3 (Play Mode)")]
        static void Phase3() { Preview("Assets/Data/Atmosphere/Phase_03_Despair.asset"); }
        static void Preview(string path)
        {
            if (!Application.isPlaying) { Debug.LogWarning("[UI Corruption] Enter Play Mode, then preview the existing atmosphere. No day/save changes."); return; }
            var phase = AssetDatabase.LoadAssetAtPath<AtmospherePhaseData>(path);
            var manager = AtmosphereManager.Instance;
            if (manager == null || phase == null) { Debug.LogError("[UI Corruption] Existing atmosphere manager/phase is missing."); return; }
            manager.Preview(phase);
        }
        [MenuItem("Tools/First Jobber/UI Corruption/Restore Gameplay Phase (Play Mode)")]
        static void Restore() { if (Application.isPlaying && AtmosphereManager.Instance != null) AtmosphereManager.Instance.ClearPreview(); }
    }
}
