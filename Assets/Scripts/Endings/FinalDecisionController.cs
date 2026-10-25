using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>The father's offer, then a list of all endings (all always selectable).</summary>
    public class FinalDecisionController : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] ChoiceListUI endingList;

        public event Action<string> EndingChosen;

        GameContext ctx;
        bool chosen;
        readonly List<ChoiceItem> items = new List<ChoiceItem>();

        void Awake()
        {
            if (endingList != null) endingList.ItemChosen += OnChosen;
        }

        void OnDestroy()
        {
            if (endingList != null) endingList.ItemChosen -= OnChosen;
        }

        public void Open(GameContext context)
        {
            ctx = context;
            chosen = false;
            endingList.Clear();
            endingList.gameObject.SetActive(false);
            if (titleText != null) titleText.text = "";

            ctx.Dialogue.Play(ctx.Database.fatherOfferLines, ShowOptions);
        }

        void ShowOptions()
        {
            items.Clear();
            foreach (var e in ctx.Database.endings)
            {
                if (e == null) continue;
                items.Add(new ChoiceItem
                {
                    id = e.id,
                    title = LocalizationManager.Get(e, "optionLabel", e.optionLabel),
                    subtitle = LocalizationManager.Get(e, "optionDescription", e.optionDescription),
                    state = ChoiceState.Available
                });
            }
            if (titleText != null) titleText.text = UiText.WhatWillYouDo.Text;
            endingList.gameObject.SetActive(true);
            endingList.SetItems(items, UiText.NoEndings.Text, true);
            endingList.SetInteractable(true);
        }

        void OnChosen(string id)
        {
            if (chosen) return;
            chosen = true;
            endingList.SetInteractable(false);
            EndingChosen?.Invoke(id);
        }
    }
}
