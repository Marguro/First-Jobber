using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    /// <summary>Inspector preview buttons for the AtmosphereManager (Play Mode only, never changes the game day).</summary>
    [UnityEditor.CustomEditor(typeof(AtmosphereManager))]
    public class AtmosphereManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var manager = (AtmosphereManager)target;
            var config = serializedObject.FindProperty("configuration").objectReferenceValue as AtmosphereConfiguration;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to preview phases.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Active", manager.AppliedPhase != null ? manager.AppliedPhase.displayName : "(none)");
            if (manager.IsPreviewing) EditorGUILayout.HelpBox("Previewing - game progress is unchanged.", MessageType.Warning);
            if (config == null) return;

            foreach (var phase in config.phases.Where(p => p != null).OrderBy(p => p.startDay))
            {
                if (GUILayout.Button("Preview: " + phase.displayName + " (day " + phase.startDay + ")")) manager.Preview(phase);
            }
            if (GUILayout.Button("Clear preview (back to current day)")) manager.ClearPreview();
        }
    }

    public static class AtmosphereTools
    {
        const string Folder = "Assets/Data/Atmosphere";
        public const string ConfigPath = Folder + "/AtmosphereConfiguration.asset";

        /// <summary>Creates the 3 example phases + configuration (never overwrites existing assets).</summary>
        public static AtmosphereConfiguration GenerateSampleAssets()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(Folder);

            var normal = Phase("Phase_01_Normal", "normal", "Normal", 1, p =>
            {
                p.fadeDuration = 1f;
            });
            var deterioration = Phase("Phase_02_Deterioration", "deterioration", "Deterioration", 3, p =>
            {
                p.useCameraColor = true; p.cameraColor = new Color(0.04f, 0.05f, 0.08f);
                p.overlayColor = new Color(0.05f, 0.1f, 0.2f); p.overlayOpacity = 0.25f;
                p.uiTint = new Color(0.8f, 0.85f, 0.95f);
                p.hrTint = new Color(0.75f, 0.8f, 0.9f);
                p.fadeDuration = 1.5f;
            });
            var despair = Phase("Phase_03_Despair", "despair", "Despair", 4, p =>
            {
                p.useCameraColor = true; p.cameraColor = new Color(0.01f, 0.01f, 0.02f);
                p.overlayColor = Color.black; p.overlayOpacity = 0.5f;
                p.uiTint = new Color(0.55f, 0.55f, 0.6f);
                p.hrTint = new Color(0.55f, 0.5f, 0.5f);
                p.flickerEnabled = true; p.flickerInterval = 3f; p.flickerIntensity = 0.15f;
                p.fadeDuration = 2f;
            });

            var config = AssetDatabase.LoadAssetAtPath<AtmosphereConfiguration>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AtmosphereConfiguration>();
                config.phases.AddRange(new[] { normal, deterioration, despair });
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            AssetDatabase.SaveAssets();
            return config;
        }

        static AtmospherePhaseData Phase(string file, string id, string display, int startDay, System.Action<AtmospherePhaseData> setup)
        {
            string path = Folder + "/" + file + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<AtmospherePhaseData>(path);
            if (existing != null) return existing;

            var p = ScriptableObject.CreateInstance<AtmospherePhaseData>();
            p.id = id; p.displayName = display; p.startDay = startDay;
            setup(p);
            // Empty slots so artists can see which targets exist; null sprites fall back to the original look.
            foreach (var t in AtmosphereTargets.Known)
                p.spriteOverrides.Add(new AtmosphereSpriteOverride { targetId = t });
            foreach (HREmotion e in System.Enum.GetValues(typeof(HREmotion)))
                p.hrSprites.Add(new AtmosphereHrSpriteOverride { emotion = e });
            AssetDatabase.CreateAsset(p, path);
            return p;
        }

        [MenuItem("Tools/First Jobber/4. Add Atmosphere To Open Scene")]
        public static void AddToOpenScene()
        {
            var flow = Object.FindFirstObjectByType<GameFlowManager>();
            if (flow == null) { Debug.LogError("[Atmosphere] No GameFlowManager in the open scene."); return; }
            if (Object.FindFirstObjectByType<AtmosphereManager>() != null)
            {
                Debug.LogWarning("[Atmosphere] This scene already has an AtmosphereManager.");
                return;
            }
            var config = AssetDatabase.LoadAssetAtPath<AtmosphereConfiguration>(ConfigPath) ?? GenerateSampleAssets();

            var canvas = Object.FindFirstObjectByType<Canvas>();
            var canvasRt = canvas != null ? (RectTransform)canvas.transform : null;

            // Overlay: above the panels, below the dialogue box so text stays readable; never blocks clicks.
            Image overlay = null;
            if (canvasRt != null)
            {
                var go = new GameObject("AtmosphereOverlay", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvasRt, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                overlay = go.GetComponent<Image>();
                overlay.color = new Color(0, 0, 0, 0);
                overlay.raycastTarget = false;
                var dialogueBox = canvasRt.Find("DialogueBox");
                go.transform.SetSiblingIndex(dialogueBox != null ? dialogueBox.GetSiblingIndex() : canvasRt.childCount - 1);

                Bind(canvasRt, "Background", "MainBackground");
                Bind(canvasRt, "ResumeEditorPanel/ResumePreview", "ResumeBackground");
                Bind(canvasRt, "CompanyPanel/DetailsCard", "CompanyPanelBackground");
                Bind(canvasRt, "InterviewPanel/HRArea", "InterviewBackground");
                Bind(canvasRt, "DialogueBox", "UIDecoration");
            }

            var systems = flow.gameObject;
            var manager = systems.AddComponent<AtmosphereManager>();
            var audioHost = new GameObject("AtmosphereAudio");
            audioHost.transform.SetParent(systems.transform, false);
            var a = audioHost.AddComponent<AudioSource>();
            var b = audioHost.AddComponent<AudioSource>();
            foreach (var s in new[] { a, b }) { s.playOnAwake = false; s.loop = true; s.volume = 0f; }

            SetRef(manager, "configuration", config);
            SetRef(manager, "targetCamera", Camera.main);
            SetRef(manager, "overlay", overlay);
            SetRef(manager, "audioSourceA", a);
            SetRef(manager, "audioSourceB", b);
            SetRef(flow, "atmosphere", manager);

            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            Debug.Log("[Atmosphere] Added to the open scene. Save the scene (Ctrl+S).");
        }

        [MenuItem("Tools/First Jobber/5. Add Scrolling Background To Open Scene")]
        public static void AddScrollingBackground()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var bg = canvas != null ? canvas.transform.Find("Background") : null;
            if (bg == null) { Debug.LogError("[Scroll] No 'Background' object under the Canvas."); return; }
            if (bg.GetComponent<ScrollingBackground>() != null) { Debug.LogWarning("[Scroll] Background already scrolls."); return; }
            Undo.AddComponent<ScrollingBackground>(bg.gameObject);
            Selection.activeGameObject = bg.gameObject;
            EditorSceneManager.MarkSceneDirty(bg.gameObject.scene);
            Debug.Log("[Scroll] Added. Assign a sprite to the Background's Image, then tune Speed / Axis on ScrollingBackground.");
        }

        [MenuItem("Tools/First Jobber/6. Add Ending Timeline Support To Open Scene")]
        public static void AddEndingTimeline()
        {
            var ending = Object.FindFirstObjectByType<EndingController>();
            if (ending == null) { Debug.LogError("[Ending] No EndingController in the open scene."); return; }
            var director = ending.GetComponent<UnityEngine.Playables.PlayableDirector>();
            if (director == null) director = Undo.AddComponent<UnityEngine.Playables.PlayableDirector>(ending.gameObject);
            director.playOnAwake = false;
            SetRef(ending, "director", director);
            EditorSceneManager.MarkSceneDirty(ending.gameObject.scene);
            Debug.Log("[Ending] PlayableDirector assigned on EndingPanel. Save the scene (Ctrl+S).");
        }

        static void Bind(RectTransform root, string path, string targetId)
        {
            var t = root.Find(path);
            if (t == null) return;
            var binding = t.gameObject.AddComponent<AtmosphereSpriteBinding>();
            SetRef(binding, "targetId", targetId);
            SetRef(binding, "image", t.GetComponent<Image>());
        }

        static void SetRef(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError("[Atmosphere] Missing field " + field + " on " + target.GetType().Name); return; }
            if (value is string str) prop.stringValue = str;
            else prop.objectReferenceValue = value as Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
