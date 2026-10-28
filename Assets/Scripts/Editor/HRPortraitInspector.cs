using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    [UnityEditor.CustomEditor(typeof(HRCharacterData))]
    public class HRPortraitInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var hr = (HRCharacterData)target;
            var missing = new List<string>();
            foreach (HREmotion emotion in Enum.GetValues(typeof(HREmotion)))
                if (hr.emotionVisuals == null || !hr.emotionVisuals.Exists(v => v != null && v.emotion == emotion && v.sprite != null))
                    missing.Add(emotion.ToString());
            EditorGUILayout.HelpBox("All HR dialogue uses this character's portraits. Priority: phase + requested emotion > standard emotion > standard Neutral > Default Portrait > empty UI. Shared questions only need Speaker = HR and Emotion.", MessageType.Info);
            if (missing.Count > 0)
                EditorGUILayout.HelpBox("Standard sprites not assigned: " + string.Join(", ", missing) + ". Configure Emotion Visuals and/or Default Portrait; phase-specific art belongs in Atmosphere Variants.",
                    missing.Count == 4 && hr.defaultPortrait == null ? MessageType.Warning : MessageType.Info);
        }
    }
}
