using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    [UnityEditor.CustomEditor(typeof(EndingData))]
    public class EndingDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            DrawPropertiesExcluding(serializedObject, "m_Script", "presentationMode", "videoClip", "timeline", "cutscenePrefab",
                "allowSkip", "fadeInDuration", "fadeOutDuration", "actionAfterCutscene", "resetCycle", "timelineMode", "timelineSkippable");
            EditorGUILayout.Space(); EditorGUILayout.LabelField("Presentation after Confirm", EditorStyles.boldLabel);
            var mode = serializedObject.FindProperty("presentationMode");
            EditorGUILayout.PropertyField(mode);
            if ((EndingPresentationMode)mode.enumValueIndex == EndingPresentationMode.Video)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("videoClip"));
                if (serializedObject.FindProperty("videoClip").objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Assign an imported VideoClip. Missing/failed media safely continues to the ending action.", MessageType.Warning);
            }
            if ((EndingPresentationMode)mode.enumValueIndex == EndingPresentationMode.Timeline)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timeline"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cutscenePrefab"));
                EditorGUILayout.HelpBox("A bound Cutscene Prefab takes priority over the bare Timeline. Otherwise assign scene track bindings on EndingController. Directors never autoplay.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timelineMode"), new GUIContent("Dialogue / Timeline order"));
            }
            if ((EndingPresentationMode)mode.enumValueIndex != EndingPresentationMode.None)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timelineSkippable"), new GUIContent("Allow Skip"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeInDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeOutDuration"));
            }
            var action = serializedObject.FindProperty("actionAfterCutscene");
            EditorGUI.BeginChangeCheck(); EditorGUILayout.PropertyField(action);
            if (EditorGUI.EndChangeCheck()) serializedObject.FindProperty("resetCycle").boolValue = action.enumValueIndex == (int)EndingAfterCutsceneAction.RestartDay1;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
