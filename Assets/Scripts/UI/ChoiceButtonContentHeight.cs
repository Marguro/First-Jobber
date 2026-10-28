using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Reports wrapped text space to the parent list, without writing any child's position or size.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public class ChoiceButtonContentHeight : MonoBehaviour, ILayoutElement
    {
        [Tooltip("Optional responsive growth. Root LayoutElement remains the authored minimum/preferred height. No internal RectTransform is moved.")]
        [SerializeField] bool growForWrappedText = true;
        [Tooltip("Manually positioned Title/Subtitle. Use vertically stretched anchors for regions that should grow with the button. Fixed-height regions remain fixed.")]
        [SerializeField] TMP_Text[] textRegions;
        float measuredHeight = -1f;
        void OnEnable() { LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform); }
        void OnDisable() { LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform); }
#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null) LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            };
        }
#endif
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical()
        {
            measuredHeight = -1f;
            if (!growForWrappedText || textRegions == null) return;
            foreach (var text in textRegions)
            {
                if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text)) continue;
                var rect = text.rectTransform;
                float needed = text.GetPreferredValues(text.text, Mathf.Max(1f, rect.rect.width), float.PositiveInfinity).y;
                // childHeight = fraction * rootHeight + offset, including nested manual containers.
                float fraction = 1f, offset = 0f;
                while (rect != null && rect != transform)
                {
                    offset += fraction * rect.sizeDelta.y;
                    fraction *= rect.anchorMax.y - rect.anchorMin.y;
                    rect = rect.parent as RectTransform;
                }
                if (rect == transform && fraction > 0.001f)
                    measuredHeight = Mathf.Max(measuredHeight, (needed - offset) / fraction);
            }
        }
        public float minWidth => -1f;
        public float maxWidth => LayoutUtility.DefaultMaxSize;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => -1f;
        public float maxHeight => LayoutUtility.DefaultMaxSize;
        public float preferredHeight => measuredHeight;
        public float flexibleHeight => -1f;
        // Same priority as the authored LayoutElement: Unity takes the larger height.
        public int layoutPriority => 0;
    }
}
