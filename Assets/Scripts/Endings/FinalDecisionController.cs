using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>Final choices with a sequential preview and a separate confirmation for production story.</summary>
    public class FinalDecisionController : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] ChoiceListUI endingList;
        [Tooltip("Independent Confirm/Back button prefab. Ending choices use endingList's own prefab.")]
        [SerializeField] ChoiceButtonUI actionButtonPrefab;

        public event Action<string> EndingChosen;

        GameContext ctx;
        bool chosen;
        string pendingId;
        bool confirming;
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

            if (ctx.Database.finalStoryDay > 0) ShowOptions();
            else ctx.Dialogue.Play(ctx.Database.fatherOfferLines, ShowOptions, 0f, ctx.Database.fatherOfferMode);
        }

        public void PrepareStory()
        {
            chosen = true;
            endingList.Clear();
            endingList.gameObject.SetActive(false);
            if (titleText != null) titleText.text = "";
        }

        void ShowOptions()
        {
            chosen = false;
            confirming = false;
            pendingId = null;
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
            if (confirming)
            {
                if (id == "back") { ShowOptions(); return; }
                if (id != "confirm" || string.IsNullOrEmpty(pendingId)) return;
                chosen = true;
                endingList.SetInteractable(false);
                EndingChosen?.Invoke(pendingId);
                return;
            }
            var ending = ctx.Database.FindEnding(id);
            if (ending == null) return;
            if (ctx.Database.finalStoryDay == 0) { chosen = true; EndingChosen?.Invoke(id); return; }
            pendingId = id;
            chosen = true;
            endingList.SetInteractable(false);
            endingList.gameObject.SetActive(false);
            ctx.Dialogue.Play(ending.previewLines, () => {
                if (actionButtonPrefab == null)
                {
                    Debug.LogError("[FinalDecision] Assign Action Button Prefab for Confirm/Back.", this);
                    ShowOptions();
                    return;
                }
                confirming = true;
                chosen = false;
                items.Clear();
                items.Add(new ChoiceItem { id = "confirm", title = UiText.ConfirmDecision.Text, state = ChoiceState.Available });
                items.Add(new ChoiceItem { id = "back", title = UiText.BackToDecisions.Text, state = ChoiceState.Available });
                endingList.gameObject.SetActive(true);
                endingList.SetItemsUsingPrefab(items, actionButtonPrefab, resetScroll: true);
                endingList.SetInteractable(true);
            });
        }
    }
}
