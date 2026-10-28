using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    /// <summary>Editor-only assignment and idempotent prefab setup; never runs during UI refresh.</summary>
    public static class ConfidenceStarTools
    {
        public const string SettingsPath = "Assets/Data/UI/ConfidenceStarSettings.asset";
        const string SpriteFolder = "Assets/Sprites/UI/Confident_Bar/";

        [MenuItem("Tools/First Jobber/25. Configure Choice Confidence Stars")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/ChoiceButton.prefab");
            try
            {
                ConfigurePrefabRoot(root);
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/ChoiceButton.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void ConfigurePrefabRoot(GameObject root)
        {
            var settings = AssetDatabase.LoadAssetAtPath<ConfidenceStarSettings>(SettingsPath);
            if (settings == null)
            {
                if (File.Exists(SettingsPath)) throw new System.InvalidOperationException("Confidence star settings exist but cannot be loaded. Repair the script reference before configuring.");
                if (!AssetDatabase.IsValidFolder("Assets/Data/UI")) AssetDatabase.CreateFolder("Assets/Data", "UI");
                settings = ScriptableObject.CreateInstance<ConfidenceStarSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            if (settings.starLevel1 == null) settings.starLevel1 = MainSprite("Star point_1.png");
            if (settings.starLevel2 == null) settings.starLevel2 = MainSprite("Star point_2.png");
            if (settings.starLevel3 == null) settings.starLevel3 = MainSprite("Star point_3.png");
            if (settings.starLevel4 == null) settings.starLevel4 = MainSprite("Star point_4.png");
            if (settings.starLevel5 == null) settings.starLevel5 = MainSprite("Star point_5.png");
            if (settings.defaultStar == null) settings.defaultStar = MainSprite("Confident_Star.png");
            EditorUtility.SetDirty(settings);

            var child = root.transform.Find("ConfidenceStar");
            bool created = child == null;
            if (child == null)
            {
                child = new GameObject("ConfidenceStar", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).transform;
                child.SetParent(root.transform, false);
                child.gameObject.layer = root.layer;
            }
            var image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            var layout = child.GetComponent<LayoutElement>();
            if (layout == null || created)
            {
                if (layout == null) layout = child.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = layout.preferredWidth = 48f;
                layout.minHeight = layout.preferredHeight = 48f;
                layout.flexibleWidth = layout.flexibleHeight = 0f;
            }
            child.SetAsLastSibling();
            if (created) child.gameObject.SetActive(false); // Preserve authored preview state on existing stars.

            var texts = root.transform.Find("Texts");
            if (texts != null)
            {
                if (texts.GetComponent<LayoutElement>() == null)
                    texts.gameObject.AddComponent<LayoutElement>().minWidth = 0f;
            }
            var button = root.GetComponent<ChoiceButtonUI>();
            var so = new SerializedObject(button);
            so.FindProperty("confidenceStar").objectReferenceValue = image;
            var config = so.FindProperty("confidenceStarSettings");
            if (config.objectReferenceValue == null) config.objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite MainSprite(string file)
        {
            Sprite largest = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(SpriteFolder + file))
                if (asset is Sprite sprite && (largest == null || sprite.rect.width * sprite.rect.height > largest.rect.width * largest.rect.height)) largest = sprite;
            if (largest == null) Debug.LogError("[Confidence Stars] Missing imported sprite: " + SpriteFolder + file);
            return largest;
        }
    }
}
