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
        [MenuItem("Tools/First Jobber/UI Corruption/Trigger One Visible Burst (Play Mode)")]
        static void Burst()
        {
            if (!Application.isPlaying) return;
            var surfaces = Object.FindObjectsByType<UICorruptionSurface>(FindObjectsSortMode.None);
            foreach (var surface in surfaces)
            {
                if (!surface.CanPreview) continue;
                surface.PreviewBurst();
                Selection.activeGameObject = surface.gameObject;
                Debug.Log("[UI Corruption] Preview target: " + surface.name + ", severity=" + surface.CurrentSeverity + ". Inspect its PrintEcho / HorizontalPrintTear children during playback.", surface);
                return;
            }
            Debug.LogWarning("[UI Corruption] No visible corruption target. Open Resume Editor/Company/Interview, preview Phase 2 or 3, and disable Reduced Motion to preview movement.");
        }
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
