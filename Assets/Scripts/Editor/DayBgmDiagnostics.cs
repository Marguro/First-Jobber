using UnityEditor;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>Inspect the loaded scene, rather than assuming the saved scene matches it.</summary>
    [InitializeOnLoad]
    public static class DayBgmDiagnostics
    {
        static DayBgmDiagnostics()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Diagnose;
            };
        }

        [MenuItem("Tools/First Jobber/Audio/Diagnose Day BGM")]
        public static void Diagnose()
        {
            // This is an Editor diagnostic only. Runtime uses explicit serialized references.
            var flows = Object.FindObjectsByType<GameFlowManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var flow in flows)
            {
                var music = new SerializedObject(flow).FindProperty("dayMusic").objectReferenceValue as DayBgmController;
                if (music == null)
                    Debug.LogError("[Day BGM] The LOADED GameFlowManager has no Day Music reference. Stop Play Mode and reload Assets/Scenes/Game.unity after saving your scene edits. The saved Production scene already contains the connection.", flow);
                else music.Diagnose();
                if (flow.Phase == GamePhase.MainMenu)
                    Debug.Log("[Day BGM] Main Menu uses Day 1 music. Submit keeps playback; Continue switches to the saved day.", flow);
            }
            if (EditorUtility.audioMasterMute)
                Debug.LogWarning("[Day BGM] Unity Editor audio is MUTED. Use Tools > First Jobber > Audio > Unmute Editor Audio, or turn off Mute Audio in Game View.");
        }

        [MenuItem("Tools/First Jobber/Audio/Unmute Editor Audio")]
        static void Unmute()
        {
            EditorUtility.audioMasterMute = false;
            Debug.Log("[Day BGM] Unity Editor audio unmuted. No gameplay volume preferences changed.");
        }
    }
}

