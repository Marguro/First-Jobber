using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    /// <summary>Puts AtmosphereSpriteBindings on the game's UI so a phase can swap their sprites.</summary>
    public static class UiPhaseTools
    {
        const string ChoicePrefabPath = "Assets/Prefabs/ChoiceButton.prefab";

        // path under the Canvas, target id, sprite only (true = never touch the Image colour; use for buttons / state-coloured images)
        static readonly (string path, string id, bool spriteOnly)[] Common =
        {
            ("MainMenuPanel/MenuPaper", AtmosphereTargets.MenuPaper, false),
            ("MainMenuPanel/MenuPaper/SubmitButton", AtmosphereTargets.SubmitButton, true),
            ("ResumeEditorPanel/EditorGroup/ChoiceList", AtmosphereTargets.ChoiceListBackground, false),
            ("ResumeEditorPanel/EditorGroup/ConfirmButton", AtmosphereTargets.ConfirmButton, true),
        };

        [MenuItem("Tools/First Jobber/10. Bind Common UI To Atmosphere")]
        public static void BindCommonMenu()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("[UiPhase] No Canvas in the open scene."); return; }
            int added = BindCommon((RectTransform)canvas.transform);
            Debug.Log("[UiPhase] Added " + added + " binding(s). In each Phase asset press 'Add Missing UI Slots' and drag sprites in. Save the scene (Ctrl+S).");
        }

        /// <summary>Returns how many bindings were added (existing ones are left alone).</summary>
        public static int BindCommon(RectTransform canvas)
        {
            int added = 0;
            foreach (var (path, id, spriteOnly) in Common)
            {
                var t = canvas.Find(path);
                if (t == null) continue;
                if (AddBinding(t.gameObject, id, spriteOnly)) added++;
            }
            if (PatchChoicePrefab()) added++;
            if (added > 0 && canvas.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            return added;
        }

        [MenuItem("Tools/First Jobber/11. Bind Selected UI To Atmosphere")]
        public static void BindSelected()
        {
            var report = new List<string>();
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<Image>() == null) continue;
                // Buttons keep their own colour transitions, so only the sprite is swapped there.
                bool isButton = go.GetComponent<Button>() != null;
                if (AddBinding(go, go.name, isButton)) report.Add(go.name);
            }
            if (report.Count == 0) { Debug.LogWarning("[UiPhase] Select UI objects that have an Image (and no binding yet)."); return; }
            Debug.Log("[UiPhase] Bound: " + string.Join(", ", report) + ". In a Phase asset, add Sprite Overrides with these exact Target Ids.");
        }

        static bool AddBinding(GameObject go, string id, bool spriteOnly)
        {
            var image = go.GetComponent<Image>();
            if (image == null || go.GetComponent<AtmosphereSpriteBinding>() != null) return false;
            var binding = Undo.AddComponent<AtmosphereSpriteBinding>(go);
            Configure(binding, id, image, spriteOnly);
            return true;
        }

        static void Configure(AtmosphereSpriteBinding binding, string id, Image image, bool spriteOnly)
        {
            var so = new SerializedObject(binding);
            so.FindProperty("targetId").stringValue = id;
            so.FindProperty("image").objectReferenceValue = image;
            so.FindProperty("spriteOnly").boolValue = spriteOnly;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The list buttons are spawned from this prefab, so every choice / company / answer button follows the phase.</summary>
        static bool PatchChoicePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ChoicePrefabPath) == null) return false;
            var root = PrefabUtility.LoadPrefabContents(ChoicePrefabPath);
            try
            {
                var image = root.GetComponent<Image>();
                if (image == null || root.GetComponent<AtmosphereSpriteBinding>() != null) return false;
                var binding = root.AddComponent<AtmosphereSpriteBinding>();
                Configure(binding, AtmosphereTargets.ChoiceButton, image, true); // state colours (selected / consumed) stay in ChoiceButtonUI
                PrefabUtility.SaveAsPrefabAsset(root, ChoicePrefabPath);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
