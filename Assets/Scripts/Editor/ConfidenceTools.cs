using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    /// <summary>Builds the vertical confidence bar, patches existing scenes / prefabs, and applies the example percentages.</summary>
    public static class ConfidenceTools
    {
        static readonly Color PanelColor = new Color(0.13f, 0.13f, 0.17f, 0.92f);
        static readonly Color Accent = new Color(0.98f, 0.82f, 0.35f, 1f);
        static readonly Color EdgeColor = new Color(0.8f, 0.8f, 0.82f, 1f);

        /// <summary>Example percentages for the generated sample choices (applied only to choices still at the default 50).</summary>
        public static readonly Dictionary<string, float> SampleValues = new Dictionary<string, float>
        {
            { "edu_big_city", 85f }, { "edu_small_town", 60f }, { "edu_art_college", 45f }, { "edu_online", 35f },
            { "dream_game_dev", 90f }, { "dream_artist", 75f }, { "dream_programmer", 55f }, { "dream_writer", 40f },
            { "passion_game_design", 80f }, { "passion_technology", 65f }, { "passion_music", 50f }, { "passion_storytelling", 30f }
        };

        // ------------------------------------------------------------ hierarchy

        /// <summary>
        /// Creates ConfidencePanel (far right) under the Resume Editor panel and returns its ResumeConfidenceUI with every reference wired.
        /// Anchors are fractions of the Resume Editor panel; change them in the Inspector to move or resize the bar.
        /// </summary>
        public static ResumeConfidenceUI BuildConfidencePanel(RectTransform resumePanel)
        {
            var panel = GameSceneBuilder.NewRect("ConfidencePanel", resumePanel);
            GameSceneBuilder.Anchor(panel, 0.88f, 0.28f, 0.97f, 0.97f);
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = PanelColor;
            panelImg.raycastTarget = false;
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var title = GameSceneBuilder.NewText(panel, "ConfidenceTitle", "OVERALL CONFIDENCE", 22, Accent,
                TextAlignmentOptions.Center, FontStyles.Bold, 0.04f, 0.89f, 0.96f, 0.99f);
            var percent = GameSceneBuilder.NewText(panel, "ConfidencePercentageText", "0%", 44, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold, 0.04f, 0.8f, 0.96f, 0.89f);

            var bar = GameSceneBuilder.NewRect("VerticalConfidenceBar", panel);
            GameSceneBuilder.Anchor(bar, 0.3f, 0.12f, 0.7f, 0.79f);

            var background = Layer(bar, "BarBackground", new Color(0.05f, 0.05f, 0.07f, 1f));

            var maskRt = GameSceneBuilder.NewRect("BarFillMask", bar);
            GameSceneBuilder.Stretch(maskRt);
            var mask = maskRt.gameObject.AddComponent<RectMask2D>();
            var fill = Layer(maskRt, "BarFill", new Color(0.85f, 0.25f, 0.25f, 1f));

            var frame = Layer(bar, "BarFrame", Color.white);
            frame.enabled = false; // shown automatically once a frame sprite is assigned

            var edges = GameSceneBuilder.NewRect("PlaceholderFrameEdges", bar);
            GameSceneBuilder.Stretch(edges);
            Edge(edges, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -4), new Vector2(0, 0));
            Edge(edges, "Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 4));
            Edge(edges, "Left", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(4, 0));
            Edge(edges, "Right", new Vector2(1, 0), new Vector2(1, 1), new Vector2(-4, 0), new Vector2(0, 0));

            var decoration = Layer(bar, "OptionalDecoration", Color.white);
            decoration.gameObject.SetActive(false);

            var categories = GameSceneBuilder.NewText(panel, "SelectedCategoriesText", "", 20, new Color(0.7f, 0.7f, 0.72f),
                TextAlignmentOptions.Center, FontStyles.Normal, 0.04f, 0.01f, 0.96f, 0.11f);

            var ui = panel.gameObject.AddComponent<ResumeConfidenceUI>();
            GameSceneBuilder.Set(ui, "barBackground", background);
            GameSceneBuilder.Set(ui, "barFill", fill);
            GameSceneBuilder.Set(ui, "barFrame", frame);
            GameSceneBuilder.Set(ui, "decoration", decoration);
            GameSceneBuilder.Set(ui, "placeholderFrameEdges", edges.gameObject);
            GameSceneBuilder.Set(ui, "fillMask", mask);
            GameSceneBuilder.Set(ui, "titleText", title);
            GameSceneBuilder.Set(ui, "percentText", percent);
            GameSceneBuilder.Set(ui, "categoriesText", categories);
            return ui;
        }

        static Image Layer(RectTransform parent, string name, Color color)
        {
            var rt = GameSceneBuilder.NewRect(name, parent);
            GameSceneBuilder.Stretch(rt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void Edge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = GameSceneBuilder.NewRect(name, parent);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = EdgeColor;
            img.raycastTarget = false;
        }

        /// <summary>Adds the optional right-aligned percentage label to a ChoiceButton root and returns it.</summary>
        public static TMP_Text AddBadge(RectTransform buttonRoot)
        {
            var text = GameSceneBuilder.NewTextFlow(buttonRoot, "Badge", 30, new Color(0.25f, 0.25f, 0.3f), FontStyles.Bold);
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var le = text.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 90; le.preferredWidth = 90;
            text.gameObject.SetActive(false);
            return text;
        }

        // ------------------------------------------------------------ menu: existing scene

        [MenuItem("Tools/First Jobber/7. Add Confidence Bar To Open Scene")]
        public static void AddToOpenScene()
        {
            var controller = Object.FindFirstObjectByType<ResumeEditorController>();
            if (controller == null) { Debug.LogError("[Confidence] No ResumeEditorController in the open scene."); return; }

            var so = new SerializedObject(controller);
            var prop = so.FindProperty("confidenceUI");
            if (prop.objectReferenceValue != null)
            {
                Debug.LogWarning("[Confidence] This scene already has a confidence bar.");
                return;
            }

            var panelRt = (RectTransform)controller.transform;
            var ui = BuildConfidencePanel(panelRt);
            Undo.RegisterCreatedObjectUndo(ui.gameObject, "Add Confidence Bar");

            // Make room: the editor column must end before the bar column starts.
            var editor = panelRt.Find("EditorGroup") as RectTransform;
            if (editor != null && editor.anchorMax.x > 0.86f)
                editor.anchorMax = new Vector2(0.86f, editor.anchorMax.y);

            prop.objectReferenceValue = ui;
            so.ApplyModifiedPropertiesWithoutUndo();

            PatchChoicePrefab();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = ui.gameObject;
            Debug.Log("[Confidence] Added ConfidencePanel (far right of the Resume Editor). Save the scene (Ctrl+S).");
        }

        /// <summary>Adds the Badge label to the ChoiceButton prefab if it has none (for scenes built before this feature).</summary>
        public static void PatchChoicePrefab()
        {
            const string path = "Assets/Prefabs/ChoiceButton.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var button = root.GetComponent<ChoiceButtonUI>();
                if (button == null) return;
                var so = new SerializedObject(button);
                var prop = so.FindProperty("badgeText");
                if (prop == null || prop.objectReferenceValue != null) return;
                prop.objectReferenceValue = AddBadge((RectTransform)root.transform);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[Confidence] Added the Badge label to the ChoiceButton prefab.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ------------------------------------------------------------ menu: sample values

        [MenuItem("Tools/First Jobber/8. Apply Sample Confidence Values")]
        public static void ApplySampleValues()
        {
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ResumeChoiceData"))
            {
                var choice = AssetDatabase.LoadAssetAtPath<ResumeChoiceData>(AssetDatabase.GUIDToAssetPath(guid));
                if (choice == null || !SampleValues.TryGetValue(choice.id, out float value)) continue;
                if (!Mathf.Approximately(choice.confidencePercentage, 50f)) continue; // never overwrite a value you edited
                choice.confidencePercentage = value;
                EditorUtility.SetDirty(choice);
                changed++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Confidence] Sample percentages applied to " + changed + " choice(s). Choices you already edited were left alone.");
        }
    }
}
