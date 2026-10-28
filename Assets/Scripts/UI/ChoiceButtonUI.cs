using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public enum ChoiceState { Available, Selected, Consumed, Disabled }

    /// <summary>View model for one entry of a ChoiceListUI (resume choice, company, answer, ending...).</summary>
    public struct ChoiceItem
    {
        public string id;
        public string title;
        public string subtitle;
        public Sprite icon;
        public ChoiceState state;
        [Tooltip("Optional small label on the right (e.g. confidence '85%'). Empty = hidden.")]
        public string badge;
        // Only Resume Choice items supply these. Other users of the shared button keep the star hidden.
        public bool showConfidenceStar;
        public float confidence;
    }

    /// <summary>Reusable list entry. The click listener is added once in Awake, so refreshing never duplicates it.</summary>
    public class ChoiceButtonUI : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text badgeText;
        [Tooltip("Show badge data when supplied. Off removes the badge from the layout, including its spacing.")]
        [SerializeField] bool showBadge = true;
        [Tooltip("On uses the project's shared localization font. Off preserves each text's authored font; use a font containing both English and Thai glyphs.")]
        [SerializeField] bool useProjectLocalizationFont = true;
        [Header("Confidence Star")]
        [SerializeField] Image confidenceStar;
        [SerializeField] ConfidenceStarSettings confidenceStarSettings;
        [Header("Background state colors")]
        [Tooltip("Use Background Image.color as the normal color instead of Available Color.")]
        [SerializeField] bool useAuthoredBackgroundColor;
        [Tooltip("Apply Selected Color to gameplay selection. Off preserves the authored/normal paper color; Button transitions still work.")]
        [SerializeField] bool tintSelectedBackground = true;
        [Tooltip("Authoritative normal background color. Button Colors controls hover/pressed tint on top of this.")]
        [SerializeField] Color availableColor = new Color(0.88f, 0.88f, 0.84f);
        [SerializeField] Color selectedColor = new Color(0.98f, 0.82f, 0.35f);
        [SerializeField] Color consumedColor = new Color(0.35f, 0.35f, 0.35f);

        public string Id { get; private set; }
        public bool UsesProjectLocalizationFont => useProjectLocalizationFont;

        Action<string> onClick;
        Color authoredBackgroundColor;
        bool capturedBackgroundColor;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);
            // Pooled buttons are created at runtime: give them the current language's font right away.
            ThaiTextApplier.Register(titleText, useProjectLocalizationFont);
            ThaiTextApplier.Register(subtitleText, useProjectLocalizationFont);
            ThaiTextApplier.Register(badgeText, useProjectLocalizationFont);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        void HandleClick() { onClick?.Invoke(Id); }

        public void Setup(ChoiceItem item, Action<string> clicked)
        {
            if (!capturedBackgroundColor && background != null)
            {
                authoredBackgroundColor = background.color;
                capturedBackgroundColor = true;
            }
            Id = item.id;
            onClick = clicked;

            if (titleText != null)
            {
                titleText.text = item.title;
                titleText.fontStyle = item.state == ChoiceState.Consumed
                    ? titleText.fontStyle | FontStyles.Strikethrough
                    : titleText.fontStyle & ~FontStyles.Strikethrough;
            }
            if (subtitleText != null)
            {
                bool has = !string.IsNullOrEmpty(item.subtitle);
                subtitleText.gameObject.SetActive(has);
                if (has) subtitleText.text = item.subtitle;
            }
            if (badgeText != null)
            {
                bool has = showBadge && !string.IsNullOrEmpty(item.badge);
                badgeText.gameObject.SetActive(has);
                if (has) badgeText.text = item.badge;
            }
            if (confidenceStar != null)
            {
                confidenceStar.sprite = item.showConfidenceStar && confidenceStarSettings != null
                    ? confidenceStarSettings.GetSprite(item.confidence) : null;
                confidenceStar.color = Color.white;
                confidenceStar.preserveAspect = true;
                confidenceStar.raycastTarget = false;
                confidenceStar.gameObject.SetActive(confidenceStar.sprite != null);
            }
            if (iconImage != null)
            {
                bool has = item.icon != null;
                iconImage.gameObject.SetActive(has);
                if (has) iconImage.sprite = item.icon;
            }
            if (background != null)
            {
                switch (item.state)
                {
                    case ChoiceState.Selected:
                        background.color = tintSelectedBackground ? selectedColor : (useAuthoredBackgroundColor ? authoredBackgroundColor : availableColor); break;
                    case ChoiceState.Consumed:
                    case ChoiceState.Disabled: background.color = consumedColor; break;
                    default: background.color = useAuthoredBackgroundColor ? authoredBackgroundColor : availableColor; break;
                }
            }
            if (button != null)
                button.interactable = item.state == ChoiceState.Available || item.state == ChoiceState.Selected;
        }
    }
}
