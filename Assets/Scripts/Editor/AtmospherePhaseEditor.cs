using UnityEditor;
using UnityEngine;

namespace FirstJobber.EditorTools
{
    /// <summary>Phase inspector: one click lists every known UI sprite slot so artists can just drag sprites in.</summary>
    [UnityEditor.CustomEditor(typeof(AtmospherePhaseData))]
    public class AtmospherePhaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var phase = (AtmospherePhaseData)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI sprite slots", EditorStyles.boldLabel);
            int missing = 0;
            foreach (var id in AtmosphereTargets.Known) if (!Has(phase, id)) missing++;

            if (missing == 0) EditorGUILayout.HelpBox("All known UI slots are listed under Sprite Overrides. Drag a sprite into the ones this phase should change; empty slots keep the original look.", MessageType.Info);
            else if (GUILayout.Button("Add Missing UI Slots (" + missing + ")"))
            {
                Undo.RecordObject(phase, "Add UI slots");
                foreach (var id in AtmosphereTargets.Known)
                    if (!Has(phase, id)) phase.spriteOverrides.Add(new AtmosphereSpriteOverride { targetId = id });
                EditorUtility.SetDirty(phase);
            }
        }

        static bool Has(AtmospherePhaseData phase, string id)
        {
            foreach (var o in phase.spriteOverrides) if (o != null && o.targetId == id) return true;
            return false;
        }
    }
}
