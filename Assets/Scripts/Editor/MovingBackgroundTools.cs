using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    [UnityEditor.CustomEditor(typeof(ScrollingBackground))]
    public class ScrollingBackgroundEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool motifs = serializedObject.FindProperty("mode").enumValueIndex == (int)ScrollingBackground.ScrollMode.FourMotifs;
            if (motifs)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
                EditorGUILayout.LabelField("Compact GPU Wallpaper", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));
                var source = ((ScrollingBackground)target).GetComponent<Image>();
                EditorGUI.BeginChangeCheck();
                var sprite = (Sprite)EditorGUILayout.ObjectField("Base Sprite", source.sprite, typeof(Sprite), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(source, "Change background base sprite");
                    source.sprite = sprite;
                    EditorUtility.SetDirty(source);
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wallpaperShader"));
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Infinite Scrolling", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableScrolling"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"), new GUIContent("Scroll Speed (canvas px/s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("diagonalDirection"), new GUIContent("Diagonal Direction"));
                foreach (var field in new[] { "useUnscaledTime", "patternTileSize", "patternSpacing", "elementScale" })
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Decorative Motif Types", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("motifSlots"), true);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Progression", EditorStyles.boldLabel);
                foreach (var field in new[] { "enableProgressionIntegration", "motifDeteriorationInterval", "motifRandomSeed", "previewBlurredCount" })
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            }
            else DrawDefaultInspector();
            bool changed = serializedObject.ApplyModifiedProperties();
            var background = (ScrollingBackground)target;
            if (!motifs) return;
            if (changed && !Application.isPlaying) background.RefreshMotifPreview();
            EditorGUILayout.HelpBox("Four fixed RawImages repeat the original sprite textures in a shader on a separate Canvas. Speed and normalized Direction update UV offsets; no RectTransforms move. Element Scale fits inside each repeat cell. Enter Play Mode for animated/deterioration preview; shared material assets are never edited by runtime preview.", MessageType.Info);
            EditorGUILayout.LabelField("Blurred / target", background.BlurredCount + " / " + background.TargetBlurredCount);
            var objects = new List<Object> { background };
            var slots = serializedObject.FindProperty("motifSlots");
            for (int i = 0; i < slots.arraySize; i++)
            {
                var image = slots.GetArrayElementAtIndex(i).FindPropertyRelative("image").objectReferenceValue as RawImage;
                if (image != null) { objects.Add(image); objects.Add(image.rectTransform); }
            }
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Reset Background State (all normal)")) Preview(background.ResetBackgroundState);
                if (GUILayout.Button("Advance One Random Deterioration Step")) Preview(() => background.AdvanceRandomDeteriorationStep());
                if (GUILayout.Button("Apply Preview Blurred Count")) Preview(() => background.SetBlurredCount(serializedObject.FindProperty("previewBlurredCount").intValue));
                if (GUILayout.Button("Reset Wallpaper Offset")) Preview(background.ResetMotifPositions);
            }

            void Preview(System.Action action)
            {
                Undo.RecordObjects(objects.ToArray(), "Preview moving background");
                action();
                background.RefreshMotifPreview();
                foreach (var obj in objects) EditorUtility.SetDirty(obj);
            }
        }
    }

    public static class MovingBackgroundTools
    {
        const string Folder = "Assets/Sprites/UI/Background/";

        /// <summary>Used only when authoring/rebuilding a scene. The runtime never loads assets by path.</summary>
        public static void Configure(GameObject root)
        {
            var source = root.GetComponent<Image>() ?? root.AddComponent<Image>();
            source.sprite = SpriteFor("BG_Base.png");
            source.color = Color.white;
            source.enabled = true;
            source.raycastTarget = false;
            var controller = root.GetComponent<ScrollingBackground>() ?? root.AddComponent<ScrollingBackground>();
            var so = new SerializedObject(controller);
            so.FindProperty("mode").enumValueIndex = (int)ScrollingBackground.ScrollMode.FourMotifs;
            so.FindProperty("wallpaperShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/UI/MotifWallpaper.shader");
            so.FindProperty("speed").floatValue = 30f;
            so.FindProperty("diagonalDirection").vector2Value = new Vector2(1f, -1f);
            var slots = so.FindProperty("motifSlots");
            slots.arraySize = 4;
            var canvasChild = root.transform.Find("WallpaperCanvas") as RectTransform;
            if (canvasChild == null)
            {
                canvasChild = new GameObject("WallpaperCanvas", typeof(RectTransform), typeof(Canvas)).GetComponent<RectTransform>();
                canvasChild.SetParent(root.transform, false);
            }
            canvasChild.anchorMin = Vector2.zero; canvasChild.anchorMax = Vector2.one;
            canvasChild.offsetMin = canvasChild.offsetMax = Vector2.zero;
            canvasChild.GetComponent<Canvas>().overrideSorting = false;
            int[] spriteNumbers = { 4, 1, 2, 3 }; // heart, flower, star, cloud
            string[] names = { "Heart", "Flower", "Star", "Cloud" };
            Vector2[] offsets = { new Vector2(-0.33f, 0.27f), new Vector2(0.31f, -0.26f), new Vector2(-0.30f, -0.28f), new Vector2(0.32f, 0.28f) };
            for (int i = 0; i < 4; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                var image = slot.FindPropertyRelative("image").objectReferenceValue as RawImage;
                if (image == null)
                {
                    var child = canvasChild.Find(names[i] + "Layer");
                    if (child == null)
                    {
                        child = new GameObject(names[i] + "Layer", typeof(RectTransform), typeof(RawImage)).transform;
                        child.SetParent(canvasChild, false);
                    }
                    image = child.GetComponent<RawImage>() ?? child.gameObject.AddComponent<RawImage>();
                    var rt = image.rectTransform;
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    slot.FindPropertyRelative("elementSize").vector2Value = i == 3 ? new Vector2(340, 240) : new Vector2(300, 300);
                    slot.FindPropertyRelative("image").objectReferenceValue = image;
                    slot.FindPropertyRelative("direction").vector2Value = new Vector2(-1f, -0.65f - i * 0.1f);
                    slot.FindPropertyRelative("speed").floatValue = 16f + i * 2f;
                    slot.FindPropertyRelative("startOffset").vector2Value = offsets[i];
                }
                slot.FindPropertyRelative("motif").enumValueIndex = i;
                if (slot.FindPropertyRelative("normalSprite").objectReferenceValue == null)
                    slot.FindPropertyRelative("normalSprite").objectReferenceValue = SpriteFor("ICON_" + spriteNumbers[i] + ".png");
                if (slot.FindPropertyRelative("blurredSprite").objectReferenceValue == null)
                    slot.FindPropertyRelative("blurredSprite").objectReferenceValue = SpriteFor("ICON_F_" + spriteNumbers[i] + ".png");
                var normal = slot.FindPropertyRelative("normalSprite").objectReferenceValue as Sprite;
                image.texture = normal != null ? normal.texture : null;
                image.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Background/" + names[i] + "Wallpaper.mat");
                image.color = Color.white;
                image.raycastTarget = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            controller.ResetBackgroundState();
            controller.RefreshMotifPreview();
        }

        static Sprite SpriteFor(string name)
        {
            Sprite result = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Folder + name))
                if (asset is Sprite sprite && (result == null || sprite.rect.width * sprite.rect.height > result.rect.width * result.rect.height)) result = sprite;
            if (result == null) Debug.LogError("[Background] Missing imported sprite: " + Folder + name);
            return result;
        }
    }

    public static class CompactWallpaperValidation
    {
        const string Request = "Temp/BackgroundTests/request-compact-editor-validation";
        const string ReportPath = "Temp/BackgroundTests/compact-editor-validation.json";

        [System.Serializable]
        class Report
        {
            public bool passed, shaderSupported;
            public int backgroundObjects, rawImages, canvases, layoutGroups;
            public List<string> errors = new List<string>();
        }

        [InitializeOnLoadMethod]
        static void RequestedValidation()
        {
            if (!File.Exists(Request)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                Validate();
                File.Delete(Request);
            };
        }

        [MenuItem("Tools/First Jobber/26. Validate Compact Wallpaper")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Wallpaper] Leave Play Mode before preview-scene validation."); return; }
            var report = new Report();
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/UI/MotifWallpaper.shader");
            report.shaderSupported = shader != null && shader.isSupported;
            if (!report.shaderSupported) report.errors.Add("Wallpaper shader is missing or unsupported.");
            foreach (var name in new[] { "Heart", "Flower", "Star", "Cloud" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Background/" + name + "Wallpaper.mat");
                if (material == null || material.shader != shader) report.errors.Add(name + " material has an incorrect shader reference.");
                else ShaderUtil.CompilePass(material, 0, true);
            }
            if (shader != null)
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity.ToString() == "Error") report.errors.Add(message.message);
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
            try
            {
                ScrollingBackground background = null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<ScrollingBackground>(true))
                    {
                        if (background != null) report.errors.Add("More than one background controller exists.");
                        background = candidate;
                    }
                if (background == null) report.errors.Add("Production background is missing.");
                else
                {
                    report.backgroundObjects = background.GetComponentsInChildren<RectTransform>(true).Length;
                    report.rawImages = background.GetComponentsInChildren<RawImage>(true).Length;
                    report.canvases = background.GetComponentsInChildren<Canvas>(true).Length;
                    report.layoutGroups = background.GetComponentsInChildren<LayoutGroup>(true).Length;
                    if (report.backgroundObjects != 6 || report.rawImages != 4 || report.canvases != 1 || report.layoutGroups != 0)
                        report.errors.Add("Expected six background objects, four RawImages, one isolated Canvas and no LayoutGroups.");
                    for (int i = 0; i < 4; i++)
                    {
                        var layer = background.GetMotifLayer(i);
                        if (layer == null || layer.raycastTarget || layer.material.shader != shader || layer.texture == null)
                            report.errors.Add("Invalid material/texture/raycast setup on motif layer " + i + ".");
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            report.passed = report.errors.Count == 0;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            if (report.passed) Debug.Log("[Wallpaper] PASS native shader compilation and saved-scene structure: 6 objects, 4 RawImages, isolated Canvas. Play Mode timing and Profiler measurements are separate.");
            else foreach (var error in report.errors) Debug.LogError("[Wallpaper] " + error);
        }
    }
}
