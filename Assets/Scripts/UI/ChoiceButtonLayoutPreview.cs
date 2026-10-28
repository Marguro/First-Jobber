#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FirstJobber.EditorTools
{
    /// <summary>Editor-only sample data. Uses linked instances of the original prefab, never production data.</summary>
    [ExecuteAlways]
    public class ChoiceButtonLayoutPreview : MonoBehaviour
    {
        public const string ScenePath = "Assets/Editor/UI/ChoiceButtonPreview.unity";
        [Serializable]
        public class Sample
        {
            public ChoiceButtonUI view;
            public string title;
            [TextArea] public string subtitle;
            public string badge;
            public bool star = true;
            [Range(0, 100)] public float confidence = 80;
            public ChoiceState state;
        }
        public Sample[] samples;

        [MenuItem("Tools/First Jobber/UI/Open ChoiceButton Layout Preview")]
        static void OpenPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.delayCall += RefreshLoaded;
        }

        [MenuItem("Tools/First Jobber/UI/Edit Original ChoiceButton Prefab")]
        static void EditPrefab()
        {
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ChoiceButton.prefab"));
        }

        [MenuItem("Tools/First Jobber/UI/Validate ChoiceButton Layout")]
        static void ValidateLayout()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                ChoiceButtonLayoutPreview preview = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    preview = root.GetComponentInChildren<ChoiceButtonLayoutPreview>(true);
                    if (preview != null) break;
                }
                if (preview == null || preview.samples == null || preview.samples.Length < 6)
                    throw new InvalidOperationException("Preview samples were not loaded.");
                preview.Refresh();
                var first = preview.samples[0].view;
                var content = first.transform.parent as RectTransform;
                var viewport = content.parent as RectTransform;
                var list = viewport.parent as RectTransform;
                foreach (float width in new[] { 340f, 823.41f, 1200f })
                {
                    list.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(list);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                    float previousBottom = float.PositiveInfinity;
                    foreach (var sample in preview.samples)
                    {
                        var rt = (RectTransform)sample.view.transform;
                        if (rt.localScale != Vector3.one || rt.rect.height < LayoutUtility.GetMinHeight(rt) - .1f)
                            throw new InvalidOperationException("Scale/minimum height failed at width " + width);
                        float top = rt.anchoredPosition.y + rt.rect.height * rt.pivot.y;
                        if (top > previousBottom + .1f) throw new InvalidOperationException("Rows overlap.");
                        previousBottom = top - rt.rect.height;
                        var star = rt.Find("ConfidenceStar") as RectTransform;
                        var texts = rt.Find("Texts") as RectTransform;
                        var background = rt.Find("Background").GetComponent<Image>();
                        if (background.sprite != null && background.type == Image.Type.Sliced &&
                            (background.rectTransform.rect.height < background.minHeight - .1f || background.rectTransform.rect.width < background.minWidth - .1f))
                            throw new InvalidOperationException("Button is smaller than its sprite borders. Increase Minimum Height/available width.");
                        if (star.gameObject.activeSelf)
                        {
                            var a = new Vector3[4]; var b = new Vector3[4];
                            texts.GetWorldCorners(a); star.GetWorldCorners(b);
                            if (a[2].x > b[0].x + .1f) throw new InvalidOperationException("Text and star columns overlap.");
                        }
                    }
                }
                var layout = first.GetComponent<LayoutElement>();
                float targetHeight = ((RectTransform)first.transform).rect.height + 40f;
                layout.preferredHeight = targetHeight;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                if (((RectTransform)first.transform).rect.height < targetHeight - .1f)
                    throw new InvalidOperationException("Parent list did not honor authored Preferred Height.");
                Debug.Log("[UI Preview] PASS native layout: 340/823/1200 widths, six rows, text/star separation, unit scales, Preferred Height. Mouse/hover and visual artwork still need Game View review.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.projectChanged -= ScheduleRefresh;
            EditorApplication.projectChanged += ScheduleRefresh;
        }

        static void ScheduleRefresh() { EditorApplication.delayCall += RefreshLoaded; }
        static void RefreshLoaded()
        {
            foreach (var preview in Resources.FindObjectsOfTypeAll<ChoiceButtonLayoutPreview>())
                if (preview != null && preview.gameObject.scene.path == ScenePath) preview.Refresh();
        }

        void OnEnable()
        {
            if (Application.isPlaying && EventSystem.current == null)
            {
                var input = new GameObject("Preview Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Refresh();
        }

        void OnValidate() { EditorApplication.delayCall += RefreshLoaded; }

        [ContextMenu("Refresh Preview Samples")]
        public void Refresh()
        {
            if (samples == null) return;
            for (int i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                if (sample == null || sample.view == null) continue;
                sample.view.Setup(new ChoiceItem {
                    id = "preview_" + i,
                    title = sample.title,
                    subtitle = sample.subtitle,
                    badge = sample.badge,
                    showConfidenceStar = sample.star,
                    confidence = sample.confidence,
                    state = sample.state
                }, id => Debug.Log("[UI Preview] Clicked " + id + " (no gameplay action)."));
            }
            Canvas.ForceUpdateCanvases();
        }
    }
}

#endif
