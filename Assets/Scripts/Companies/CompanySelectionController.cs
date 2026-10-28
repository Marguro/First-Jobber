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
        [SerializeField] TMP_Text companyNameText;
        [SerializeField] TMP_Text headingText;
        [Header("Session-stable company paper variants")]
        [Tooltip("Runtime paper sprite source. Shuffled without repeats and cached per company ID/session. The prefab Background sprite is only a fallback/preview; positions and dimensions always come from the prefab.")]
        [SerializeField] Sprite[] backgroundVariants;
        readonly Dictionary<string, Sprite> assignedBackgrounds = new Dictionary<string, Sprite>();
        readonly List<Sprite> variantDeck = new List<Sprite>();
        readonly System.Random visualRandom = new System.Random();
        GameSession visualSession;
        int nextVariant;
        [SerializeField] Button applyButton;
        [SerializeField] Button backButton;
        [SerializeField] float reactionAutoAdvance = 2.5f;

        public event Action<string> CompanyConfirmed;
        public event Action BackRequested;

        GameContext ctx;
        string pendingId;
        bool locked;
        readonly List<ChoiceItem> itemBuffer = new List<ChoiceItem>();

        [Header("Authored description scroll area")]
        [SerializeField] ScrollRect detailsScroll;
        [SerializeField] RectTransform detailsContent;

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
            if (!ReferenceEquals(visualSession, context.Session))
            {
                visualSession = context.Session;
                assignedBackgrounds.Clear();
                variantDeck.Clear();
                nextVariant = 0;
            }
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
            if (headingText != null) headingText.text = UiText.ChooseCompany.Text;
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
                    badge = Salary(c),
                    backgroundSprite = BackgroundFor(c.id),
                    state = consumed ? ChoiceState.Consumed : c.id == pendingId ? ChoiceState.Selected : ChoiceState.Available
                });
            }
            companyList.SetItems(itemBuffer, UiText.NoCompaniesLeft.Text, pendingId == null);

            var pending = ctx.Database.FindCompany(pendingId);
            if (companyNameText != null) companyNameText.text = pending == null ? "" : LocalizationManager.Get(pending, "companyName", pending.companyName);
            if (detailsText != null)
            {
                string description = pending != null ? LocalizationManager.Get(pending, "description", pending.description) : null;
                detailsText.text = pending == null
                    ? UiText.SelectCompany.Text
                    : description;
            }
            if (applyButton != null) applyButton.interactable = !locked && pending != null && !ctx.Choices.IsConsumed(pending);
            ResetDetailsScroll();
        }

        /// <summary>
        /// Uses the Inspector-authored scroll area. Runtime updates content/scroll position, never geometry.
        /// </summary>
        void EnsureDetailsScroll()
        {
            if (detailsText == null) return;
            if (detailsScroll == null) detailsScroll = detailsText.GetComponentInParent<ScrollRect>();
            if (detailsScroll == null)
            {
                Debug.LogError("[Company UI] Assign an authored Details Scroll/Content. Runtime no longer creates or repositions description UI.", this);
                return;
            }
            if (detailsContent == null) detailsContent = detailsScroll.content;
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

        static string Salary(CompanyData c)
        {
            return string.IsNullOrWhiteSpace(c.salary) ? UiText.SalaryNotSpecified.Text
                : LocalizationManager.Get(c, "salary", c.salary);
        }

        Sprite BackgroundFor(string companyId)
        {
            if (assignedBackgrounds.TryGetValue(companyId, out var sprite)) return sprite;
            if (nextVariant >= variantDeck.Count)
            {
                variantDeck.Clear();
                if (backgroundVariants != null)
                    foreach (var variant in backgroundVariants)
                        if (variant != null && !variantDeck.Contains(variant)) variantDeck.Add(variant);
                for (int i = variantDeck.Count - 1; i > 0; i--)
                {
                    int j = visualRandom.Next(i + 1);
                    var swap = variantDeck[i]; variantDeck[i] = variantDeck[j]; variantDeck[j] = swap;
                }
                nextVariant = 0;
            }
            sprite = variantDeck.Count == 0 ? null : variantDeck[nextVariant++];
            assignedBackgrounds[companyId] = sprite;
            return sprite;
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
