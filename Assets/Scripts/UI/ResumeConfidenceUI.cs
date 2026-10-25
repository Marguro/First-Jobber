using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    [Serializable]
    public class ConfidenceBand
    {
        public string label = "Band";
        [Tooltip("The band applies from this percentage upward, until a band with a higher 'From Percent' takes over.")]
        [Range(0f, 100f)] public float fromPercent;
        public Color color = Color.white;
    }

    /// <summary>
    /// Vertical resume-confidence bar (fills bottom to top). Pure presentation: it only shows the number it is given.
    /// Event driven - the ResumeEditorController calls SetConfidence; there is no Update() polling.
    /// Layers (all independent Images, all swappable in the Inspector): Background, Fill, Frame, Decoration.
    /// </summary>
    public class ResumeConfidenceUI : MonoBehaviour
    {
        public enum FillMode
        {
            [Tooltip("Image.Type = Filled / Vertical / Bottom. Best for plain or simple sprites.")] ImageFilled,
            [Tooltip("Clips a normal image with a RectMask2D. Use for sliced or complex fill art that Filled renders badly.")] RectMask
        }

        // Target ids for optional per-phase sprite overrides (AtmospherePhaseData > Sprite Overrides).
        public const string AtmosphereBackgroundId = "ConfidenceBackground";
        public const string AtmosphereFillId = "ConfidenceFill";
        public const string AtmosphereFrameId = "ConfidenceFrame";
        public const string AtmosphereDecorationId = "ConfidenceDecoration";

        [Header("Layers (assign your sprites on these Images)")]
        [SerializeField] Image barBackground;
        [SerializeField] Image barFill;
        [SerializeField] Image barFrame;
        [Tooltip("Optional. Hidden while it has no sprite.")]
        [SerializeField] Image decoration;
        [Tooltip("Shown only while the Frame has no sprite (simple placeholder outline).")]
        [SerializeField] GameObject placeholderFrameEdges;

        [Header("Fill")]
        [SerializeField] FillMode fillMode = FillMode.ImageFilled;
        [Tooltip("RectMask Mode only: the RectMask2D that wraps the Fill image.")]
        [SerializeField] RectMask2D fillMask;

        [Header("Texts")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text percentText;
        [SerializeField] TMP_Text categoriesText;
        [SerializeField] string titleLabel = "OVERALL CONFIDENCE";
        [SerializeField] string emptyMessage = "No choices selected";
        [Tooltip("{0} = categories selected, {1} = categories to fill.")]
        [SerializeField] string categoriesFormat = "{0} / {1} categories";
        [SerializeField, Range(0, 3)] int decimals = 1;

        [Header("Colours (lowest to highest band is picked automatically)")]
        [SerializeField] ConfidenceBand[] bands =
        {
            new ConfidenceBand { label = "Low", fromPercent = 0f, color = new Color(0.85f, 0.25f, 0.25f) },
            new ConfidenceBand { label = "Moderate", fromPercent = 40f, color = new Color(0.95f, 0.8f, 0.25f) },
            new ConfidenceBand { label = "High", fromPercent = 70f, color = new Color(0.3f, 0.75f, 0.4f) }
        };

        [Header("Animation")]
        [SerializeField, Min(0f)] float animationDuration = 0.35f;

        public float TargetPercent { get; private set; }
        public float DisplayedPercent { get; private set; }

        CanvasGroup group;
        Sprite originalBackground, originalFill, originalFrame, originalDecoration;
        Coroutine animation;
        Color displayedColor = Color.white;
        bool initialised;
        static Sprite fallbackSprite;

        void Awake() { Init(); }

        void OnEnable()
        {
            Init();
            ApplyAtmosphereSprites();
            ApplyValue(DisplayedPercent, displayedColor);
        }

        void Init()
        {
            if (initialised) return;
            initialised = true;
            group = GetComponent<CanvasGroup>();
            if (barBackground != null) { originalBackground = barBackground.sprite; barBackground.raycastTarget = false; }
            if (barFill != null) { originalFill = barFill.sprite; barFill.raycastTarget = false; }
            if (barFrame != null) { originalFrame = barFrame.sprite; barFrame.raycastTarget = false; }
            if (decoration != null) { originalDecoration = decoration.sprite; decoration.raycastTarget = false; }
            if (titleText != null) { titleText.text = titleLabel; titleText.raycastTarget = false; }
            if (percentText != null) percentText.raycastTarget = false;
            if (categoriesText != null) categoriesText.raycastTarget = false;
            displayedColor = ColorFor(0f);
        }

        /// <summary>Fades the whole bar (used by the Resume Editor's intro animation). Optional CanvasGroup on this object.</summary>
        public void SetAlpha(float alpha)
        {
            Init();
            if (group != null) group.alpha = alpha;
        }

        /// <summary>
        /// Shows a confidence value. 'selected' / 'required' feed the optional category counter.
        /// Safe to call at any time, with any values (clamped).
        /// </summary>
        public void SetConfidence(float percent, int selected, int required, bool animate)
        {
            Init();
            TargetPercent = ResumeConfidenceCalculator.Clamp(percent);
            ApplyAtmosphereSprites();

            // Texts always show the real target value, even while the bar is still animating.
            if (percentText != null) percentText.text = ResumeConfidenceCalculator.FormatPercent(TargetPercent, decimals);
            if (categoriesText != null)
                categoriesText.text = selected <= 0
                    ? LocalizationManager.Get(UiText.ConfidenceEmpty.key, emptyMessage)
                    : UiText.Format(LocalizationManager.Get(UiText.ConfidenceCategories.key, categoriesFormat), categoriesFormat, selected, Mathf.Max(required, selected));

            Color targetColor = selected <= 0 ? ColorFor(0f) : ColorFor(TargetPercent);
            if (animation != null) { StopCoroutine(animation); animation = null; }

            if (!animate || animationDuration <= 0f || !isActiveAndEnabled)
            {
                ApplyValue(TargetPercent, targetColor);
                return;
            }
            animation = StartCoroutine(Animate(TargetPercent, targetColor));
        }

        IEnumerator Animate(float toPercent, Color toColor)
        {
            float fromPercent = DisplayedPercent;
            Color fromColor = displayedColor;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / animationDuration)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                ApplyValue(Mathf.Lerp(fromPercent, toPercent, e), Color.Lerp(fromColor, toColor, e));
                yield return null;
            }
            ApplyValue(toPercent, toColor);
            animation = null;
        }

        void ApplyValue(float percent, Color color)
        {
            DisplayedPercent = percent;
            displayedColor = color;
            if (barFill == null) return;

            float fraction = Mathf.Clamp01(percent / 100f);
            if (barFill.sprite == null) barFill.sprite = FallbackSprite(); // Filled images draw nothing without a sprite
            barFill.color = color;

            if (fillMode == FillMode.ImageFilled)
            {
                barFill.type = Image.Type.Filled;
                barFill.fillMethod = Image.FillMethod.Vertical;
                barFill.fillOrigin = (int)Image.OriginVertical.Bottom;
                barFill.fillAmount = fraction;
                if (fillMask != null) fillMask.padding = Vector4.zero;
            }
            else if (fillMask != null)
            {
                // Clip from the top: padding is (left, bottom, right, top); positive values shrink the visible area.
                float height = ((RectTransform)fillMask.transform).rect.height;
                fillMask.padding = new Vector4(0f, 0f, 0f, height * (1f - fraction));
            }
        }

        Color ColorFor(float percent)
        {
            Color result = Color.white;
            float best = -1f;
            if (bands == null) return result;
            for (int i = 0; i < bands.Length; i++)
            {
                var b = bands[i];
                if (b == null || b.fromPercent > percent || b.fromPercent < best) continue;
                best = b.fromPercent;
                result = b.color;
            }
            return result;
        }

        /// <summary>Applies the active Atmosphere phase's optional sprites, or restores the Inspector sprites. Never touches the numbers.</summary>
        void ApplyAtmosphereSprites()
        {
            var phase = AtmosphereManager.Instance != null ? AtmosphereManager.Instance.AppliedPhase : null;
            SetSprite(barBackground, phase != null ? phase.GetSprite(AtmosphereBackgroundId) : null, originalBackground);
            SetSprite(barFill, phase != null ? phase.GetSprite(AtmosphereFillId) : null, originalFill);
            SetSprite(barFrame, phase != null ? phase.GetSprite(AtmosphereFrameId) : null, originalFrame);
            SetSprite(decoration, phase != null ? phase.GetSprite(AtmosphereDecorationId) : null, originalDecoration);

            // The frame only draws when it has art; otherwise a plain outline stands in. The decoration is optional.
            bool hasFrame = barFrame != null && barFrame.sprite != null;
            if (barFrame != null) barFrame.enabled = hasFrame;
            if (placeholderFrameEdges != null) placeholderFrameEdges.SetActive(!hasFrame);
            if (decoration != null) decoration.gameObject.SetActive(decoration.sprite != null);
        }

        static void SetSprite(Image image, Sprite overrideSprite, Sprite original)
        {
            if (image == null) return;
            var target = overrideSprite != null ? overrideSprite : original;
            if (image.sprite != target && !(target == null && image.sprite == fallbackSprite)) image.sprite = target;
        }

        static Sprite FallbackSprite()
        {
            if (fallbackSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                fallbackSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                fallbackSprite.name = "ConfidenceFallbackWhite";
            }
            return fallbackSprite;
        }
    }
}
