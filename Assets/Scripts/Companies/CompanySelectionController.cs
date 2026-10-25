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

        void Awake()
        {
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
                    title = c.companyName,
                    subtitle = consumed ? "REJECTED" : c.jobTitle + "\n" + c.salary + "  |  " + c.industry,
                    icon = c.logo,
                    state = consumed ? ChoiceState.Consumed : c.id == pendingId ? ChoiceState.Selected : ChoiceState.Available
                });
            }
            companyList.SetItems(itemBuffer, "No companies left.", pendingId == null);

            var pending = ctx.Database.FindCompany(pendingId);
            if (detailsText != null)
            {
                detailsText.text = pending == null
                    ? "Select a company to see its details."
                    : pending.companyName + "\n" + pending.jobTitle + "\n" + pending.salary + "  |  " + pending.industry
                      + (string.IsNullOrEmpty(pending.description) ? "" : "\n\n" + pending.description);
            }
            if (applyButton != null) applyButton.interactable = !locked && pending != null && !ctx.Choices.IsConsumed(pending);
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
