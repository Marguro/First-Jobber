using UnityEditor;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Inspector for a dialogue block: shared settings plus a "Dialogue Lines" foldout where every page is a
    /// multiline box with up / down / delete buttons. The serialized layout is unchanged, so old assets still load.
    /// </summary>
    [CustomPropertyDrawer(typeof(DialogueLine))]
    public class DialogueLineDrawer : PropertyDrawer
    {
        const float Gap = 2f;
        const float ButtonW = 22f;
        static float Row => EditorGUIUtility.singleLineHeight;
        static float TextH => Row * 3f;

        static readonly string[] Shared = { "speaker", "emotion", "portrait", "autoAdvanceDelay", "delayBefore", "eventId", "genderIds", "variantGroup" };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = Row;
            if (!property.isExpanded) return h;
            h += Gap;

            foreach (var n in Shared) h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(n), true) + Gap;

            var text = property.FindPropertyRelative("text");
            var lines = property.FindPropertyRelative("dialogueLines");
            if (!string.IsNullOrEmpty(text.stringValue))
                h += (lines.arraySize > 0 ? Row * 2f : Row + TextH + Row) + Gap;

            h += Row + Gap; // "Dialogue Lines" foldout
            if (lines.isExpanded) h += lines.arraySize * (TextH + Gap) + Row;
            return h;
        }

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            var text = property.FindPropertyRelative("text");
            var lines = property.FindPropertyRelative("dialogueLines");

            property.isExpanded = EditorGUI.Foldout(new Rect(pos.x, pos.y, pos.width, Row), property.isExpanded,
                label.text + "  -  " + Summary(property, text, lines), true);
            if (!property.isExpanded) return;

            EditorGUI.indentLevel++;
            float indent = EditorGUI.indentLevel * 15f;
            float y = pos.y + Row + Gap;

            foreach (var n in Shared)
            {
                var p = property.FindPropertyRelative(n);
                float h = EditorGUI.GetPropertyHeight(p, true);
                bool automaticPortrait = n == "portrait" && property.FindPropertyRelative("speaker").stringValue == Speakers.HR;
                using (new EditorGUI.DisabledScope(automaticPortrait))
                    EditorGUI.PropertyField(new Rect(pos.x, y, pos.width, h), p,
                        automaticPortrait ? new GUIContent("Portrait (automatic from active HR)", "Legacy value is preserved but ignored. Configure HRCharacterData Emotion Visuals / Atmosphere Variants.") : new GUIContent(p.displayName), true);
                y += h + Gap;
            }

            // Text from before multi-line blocks existed. Used only while Dialogue Lines is empty.
            if (!string.IsNullOrEmpty(text.stringValue))
            {
                bool shadowed = lines.arraySize > 0;
                EditorGUI.LabelField(new Rect(pos.x, y, pos.width, Row),
                    shadowed ? "Legacy Text (ignored - Dialogue Lines is used)" : "Legacy Text (used until Dialogue Lines has content)",
                    EditorStyles.miniLabel);
                y += Row;
                var btn = new Rect(pos.x + indent, 0f, 0f, Row);
                if (!shadowed)
                {
                    text.stringValue = EditorGUI.TextArea(new Rect(pos.x + indent, y, pos.width - indent, TextH), text.stringValue);
                    y += TextH;
                    btn.y = y; btn.width = 220f;
                    if (GUI.Button(btn, "Move into Dialogue Lines"))
                    {
                        lines.arraySize = 1;
                        lines.GetArrayElementAtIndex(0).stringValue = text.stringValue;
                        text.stringValue = "";
                        lines.isExpanded = true;
                        Commit(property);
                    }
                }
                else
                {
                    btn.y = y; btn.width = 160f;
                    if (GUI.Button(btn, "Clear legacy text"))
                    {
                        text.stringValue = "";
                        Commit(property);
                    }
                }
                y += Row + Gap;
            }

            lines.isExpanded = EditorGUI.Foldout(new Rect(pos.x, y, pos.width, Row), lines.isExpanded, "Dialogue Lines [" + lines.arraySize + "]", true);
            y += Row + Gap;

            if (lines.isExpanded)
            {
                int action = 0, target = -1;
                for (int i = 0; i < lines.arraySize; i++)
                {
                    const float labelW = 44f;
                    EditorGUI.LabelField(new Rect(pos.x + indent, y, labelW, Row), "Line " + i, EditorStyles.miniLabel);
                    var box = new Rect(pos.x + indent + labelW, y, pos.width - indent - labelW - ButtonW - 2f, TextH);
                    var el = lines.GetArrayElementAtIndex(i);
                    el.stringValue = EditorGUI.TextArea(box, el.stringValue);

                    float bx = box.xMax + 2f;
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUI.Button(new Rect(bx, y, ButtonW, Row), "↑")) { action = 1; target = i; }
                    using (new EditorGUI.DisabledScope(i == lines.arraySize - 1))
                        if (GUI.Button(new Rect(bx, y + Row, ButtonW, Row), "↓")) { action = 2; target = i; }
                    if (GUI.Button(new Rect(bx, y + Row * 2f, ButtonW, Row), "X")) { action = 3; target = i; }
                    y += TextH + Gap;
                }

                if (GUI.Button(new Rect(pos.x + indent, y, 120f, Row), "+ Add Line")) action = 4;

                if (action != 0)
                {
                    switch (action)
                    {
                        case 1: lines.MoveArrayElement(target, target - 1); break;
                        case 2: lines.MoveArrayElement(target, target + 1); break;
                        case 3: lines.DeleteArrayElementAtIndex(target); break;
                        case 4:
                            lines.arraySize++;
                            lines.GetArrayElementAtIndex(lines.arraySize - 1).stringValue = "";
                            break;
                    }
                    Commit(property);
                }
            }
            EditorGUI.indentLevel--;
        }

        static void Commit(SerializedProperty property)
        {
            property.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI(); // layout changed mid-draw; redraw on the next event
        }

        static string Summary(SerializedProperty property, SerializedProperty text, SerializedProperty lines)
        {
            string speaker = property.FindPropertyRelative("speaker").stringValue;
            string first = lines.arraySize > 0 ? lines.GetArrayElementAtIndex(0).stringValue : text.stringValue;
            first = (first ?? "").Replace("\n", " ");
            if (first.Length > 36) first = first.Substring(0, 36) + "...";
            string count = lines.arraySize > 1 ? " (" + lines.arraySize + " lines)" : "";
            return speaker + ": " + first + count;
        }
    }
}
