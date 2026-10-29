using UnityEditor;
using UnityEngine;

namespace FirstJobber
{
    [UnityEditor.CustomEditor(typeof(UICorruptionSurface))]
    public class UICorruptionSurfaceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            var surface = (UICorruptionSurface)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Current Severity", surface.CurrentSeverity);
                EditorGUILayout.Toggle("Visible Preview Target", surface.CanPreview);
            }
            using (new EditorGUI.DisabledScope(!surface.CanPreview))
                if (GUILayout.Button("Trigger Glitch Now")) surface.PreviewBurst();
            EditorGUILayout.HelpBox("Preview uses the current phase without changing day/save data. Reduced Motion suppresses movement. Inspect the generated PrintEcho and HorizontalPrintTear children for the visual layers.", MessageType.Info);
        }
    }
}
