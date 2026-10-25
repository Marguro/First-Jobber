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
        [SerializeField] Color availableColor = new Color(0.88f, 0.88f, 0.84f);
        [SerializeField] Color selectedColor = new Color(0.98f, 0.82f, 0.35f);
        [SerializeField] Color consumedColor = new Color(0.35f, 0.35f, 0.35f);

        public string Id { get; private set; }

        Action<string> onClick;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        void HandleClick() { onClick?.Invoke(Id); }

        public void Setup(ChoiceItem item, Action<string> clicked)
        {
            Id = item.id;
            onClick = clicked;

            if (titleText != null)
            {
                titleText.text = item.title;
                titleText.fontStyle = item.state == ChoiceState.Consumed ? FontStyles.Strikethrough : FontStyles.Normal;
            }
            if (subtitleText != null)
            {
                bool has = !string.IsNullOrEmpty(item.subtitle);
                subtitleText.gameObject.SetActive(has);
                if (has) subtitleText.text = item.subtitle;
            }
            if (badgeText != null)
            {
                bool has = !string.IsNullOrEmpty(item.badge);
                badgeText.gameObject.SetActive(has);
                if (has) badgeText.text = item.badge;
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
                    case ChoiceState.Selected: background.color = selectedColor; break;
                    case ChoiceState.Consumed:
                    case ChoiceState.Disabled: background.color = consumedColor; break;
                    default: background.color = availableColor; break;
                }
            }
            if (button != null)
                button.interactable = item.state == ChoiceState.Available || item.state == ChoiceState.Selected;
        }
    }
}
