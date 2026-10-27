using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public class CompanySelectionController : MonoBehaviour
    {
        [SerializeField] ChoiceListUI companyList;
        [SerializeField] TMP_Text detailsText;
        [SerializeField] Button applyButton;
        [SerializeField] Button backButton;
        [SerializeField] float reactionAutoAdvance = 2.5f;

        public event Action<string> CompanyConfirmed;
        public event Action BackRequested;

        GameContext ctx;
        string pendingId;
        bool locked;
        readonly List<ChoiceItem> itemBuffer = new List<ChoiceItem>();

        ScrollRect detailsScroll;
        RectTransform detailsContent;

        void Awake()
        {
            EnsureDetailsScroll();
            if (applyButton != null) applyButton.onClick.AddListener(OnApply);
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (companyList != null) companyList.ItemChosen += OnCompanyChosen;
        }

        void OnDestroy()
        {
            if (applyButton != null) applyButton.onClick.RemoveListener(OnApply);
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            if (companyList != null) companyList.ItemChosen -= OnCompanyChosen;
        }

        public void Open(GameContext context)
        {
            ctx = context;
            pendingId = null;
            locked = false;
            Refresh();
            companyList.SetInteractable(true);
            if (backButton != null) backButton.interactable = true;
        }

        public void SetStoryLocked(bool value)
        {
            locked = value;
            companyList.SetInteractable(!value);
            if (backButton != null) backButton.interactable = !value;
            Refresh();
        }

        void Refresh()
        {
            itemBuffer.Clear();
            var list = ctx.Database.companies;
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                if (c == null) continue;
                bool consumed = ctx.Choices.IsConsumed(c);
                if (consumed && !ctx.Database.showRejectedCompanies) continue;
                itemBuffer.Add(new ChoiceItem
                {
                    id = c.id,
                    title = LocalizationManager.Get(c, "companyName", c.companyName),
                    subtitle = consumed ? UiText.Rejected.Text : Summary(c),
                    icon = c.logo,
                    state = consumed ? ChoiceState.Consumed : c.id == pendingId ? ChoiceState.Selected : ChoiceState.Available
                });
            }
            companyList.SetItems(itemBuffer, UiText.NoCompaniesLeft.Text, pendingId == null);

            var pending = ctx.Database.FindCompany(pendingId);
            if (detailsText != null)
            {
                string description = pending != null ? LocalizationManager.Get(pending, "description", pending.description) : null;
                detailsText.text = pending == null
                    ? UiText.SelectCompany.Text
                    : LocalizationManager.Get(pending, "companyName", pending.companyName) + "\n" + Summary(pending)
                      + (string.IsNullOrEmpty(description) ? "" : "\n\n" + description);
            }
            if (applyButton != null) applyButton.interactable = !locked && pending != null && !ctx.Choices.IsConsumed(pending);
            ResetDetailsScroll();
        }

        /// <summary>
        /// Puts the details text into a masked, vertically scrolling viewport (mouse wheel + drag) inside its card.
        /// The viewport keeps the text's old rect, so the Company Selection layout does not change. Runs once.
        /// </summary>
        void EnsureDetailsScroll()
        {
            if (detailsText == null || detailsScroll != null) return;
            detailsScroll = detailsText.GetComponentInParent<ScrollRect>();
            if (detailsScroll != null) { detailsContent = detailsScroll.content; return; }

            var textRt = detailsText.rectTransform;
            var card = textRt.parent as RectTransform;
            if (card == null) return;

            var viewport = new GameObject("DescriptionViewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(card, false);
            viewport.anchorMin = textRt.anchorMin; viewport.anchorMax = textRt.anchorMax;
            viewport.offsetMin = textRt.offsetMin; viewport.offsetMax = textRt.offsetMax;

            detailsContent = new GameObject("DescriptionContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            detailsContent.SetParent(viewport, false);
            detailsContent.anchorMin = new Vector2(0, 1); detailsContent.anchorMax = new Vector2(1, 1);
            detailsContent.pivot = new Vector2(0.5f, 1); detailsContent.sizeDelta = Vector2.zero;
            var layout = detailsContent.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            detailsContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            textRt.SetParent(detailsContent, false);
            textRt.anchorMin = new Vector2(0, 1); textRt.anchorMax = new Vector2(1, 1); textRt.pivot = new Vector2(0.5f, 1);
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            detailsText.textWrappingMode = TextWrappingModes.Normal;
            detailsText.overflowMode = TextOverflowModes.Overflow;

            detailsScroll = card.gameObject.GetComponent<ScrollRect>() ?? card.gameObject.AddComponent<ScrollRect>();
            detailsScroll.viewport = viewport;
            detailsScroll.content = detailsContent;
            detailsScroll.horizontal = false; detailsScroll.vertical = true;
            detailsScroll.movementType = ScrollRect.MovementType.Clamped;
            detailsScroll.scrollSensitivity = 40f;
        }

        // New company or new language: recalculate the content height, then show the top.
        void ResetDetailsScroll()
        {
            if (detailsScroll == null || detailsContent == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(detailsContent);
            detailsScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Re-renders the list and details in the current language (selection and lock are kept).</summary>
        public void RefreshLanguage()
        {
            if (ctx != null) Refresh();
        }

        /// <summary>"job title \n salary | industry", localized.</summary>
        static string Summary(CompanyData c)
        {
            return LocalizationManager.Get(c, "jobTitle", c.jobTitle) + "\n"
                + LocalizationManager.Get(c, "salary", c.salary) + "  |  "
                + LocalizationManager.Get(c, "industry", c.industry);
        }

        void OnCompanyChosen(string id)
        {
            if (locked) return;
            var company = ctx.Database.FindCompany(id);
            if (company == null || ctx.Choices.IsConsumed(company)) return;
            pendingId = id;
            Refresh();
            ctx.Dialogue.Play(company.playerReaction, null, reactionAutoAdvance);
        }

        void OnApply()
        {
            if (locked || ctx == null) return;
            var company = ctx.Database.FindCompany(pendingId);
            if (company == null || ctx.Choices.IsConsumed(company)) return;
            locked = true;
            companyList.SetInteractable(false);
            if (applyButton != null) applyButton.interactable = false;
            if (backButton != null) backButton.interactable = false;
            CompanyConfirmed?.Invoke(company.id);
        }

        void OnBack()
        {
            if (locked) return;
            locked = true;
            BackRequested?.Invoke();
        }
    }
}
