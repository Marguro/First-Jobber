using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>
    /// Resume editor: category rows on the left (inside the resume preview), choices on the right.
    /// Contains no content - everything comes from the GameDatabase through the ChoiceManager.
    /// </summary>
    public class ResumeEditorController : MonoBehaviour
    {
        [SerializeField] ResumePreviewUI preview;
        [SerializeField] RectTransform previewRect;
        [SerializeField] CanvasGroup editorGroup;
        [SerializeField] TMP_Text dayText;
        [SerializeField] TMP_Text categoryTitleText;
        [SerializeField] ChoiceListUI choiceList;
        [SerializeField] Button confirmButton;
        [Header("Intro animation")]
        [SerializeField] float introHold = 1.2f;
        [SerializeField] float introDuration = 0.9f;
        [SerializeField] float introStartScale = 1.35f;
        [Header("Dialogue")]
        [SerializeField] float reactionAutoAdvance = 2f;

        public event Action Confirmed;

        GameContext ctx;
        ResumeCategoryData current;
        bool locked = true;
        bool confirmed;
        Vector2 restPosition;
        Vector3 restScale;

        readonly List<ResumeChoiceData> choiceBuffer = new List<ResumeChoiceData>();
        readonly List<ChoiceItem> itemBuffer = new List<ChoiceItem>();

        void Awake()
        {
            if (previewRect != null)
            {
                restPosition = previewRect.anchoredPosition;
                restScale = previewRect.localScale;
            }
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (choiceList != null) choiceList.ItemChosen += OnChoiceChosen;
            if (preview != null && preview.CategoryList != null) preview.CategoryList.ItemChosen += OnCategoryChosen;
        }

        void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (choiceList != null) choiceList.ItemChosen -= OnChoiceChosen;
            if (preview != null && preview.CategoryList != null) preview.CategoryList.ItemChosen -= OnCategoryChosen;
        }

        /// <summary>Rebuilds all state from the session without animation (safe to call while another panel is visible).</summary>
        public void Refresh(GameContext context)
        {
            ctx = context;
            confirmed = false;
            ctx.Choices.PruneInvalidSelections();
            preview.SetApplicant(ctx.Session.applicantName, ctx.Session.applicantAge);
            preview.SetDistorted(ctx.Session.HasFlag("resume_distorted"));
            if (dayText != null) dayText.text = "Day " + ctx.Session.day;

            if (current == null || !ctx.Database.categories.Contains(current) || ctx.Choices.CountAvailable(current) == 0)
                current = FirstCategoryWithChoices();

            RefreshCategories();
            RefreshChoices(true);
            SetLocked(true);
        }

        /// <summary>Shows the editor. onReady is called when the player may start (after the optional intro animation).</summary>
        public void Open(GameContext context, bool playIntro, Action onReady)
        {
            StopAllCoroutines();
            Refresh(context);
            if (playIntro) StartCoroutine(IntroRoutine(onReady));
            else
            {
                ResetLayout();
                onReady?.Invoke();
            }
        }

        public void Unlock() { SetLocked(false); }

        void ResetLayout()
        {
            if (previewRect != null)
            {
                previewRect.anchoredPosition = restPosition;
                previewRect.localScale = restScale;
            }
            if (editorGroup != null) editorGroup.alpha = 1f;
        }

        IEnumerator IntroRoutine(Action onReady)
        {
            // Start centred and enlarged; the editor side is hidden.
            var parent = previewRect.parent as RectTransform;
            float centerX = (previewRect.anchorMin.x + previewRect.anchorMax.x) * 0.5f * parent.rect.width;
            Vector2 startPos = restPosition + new Vector2(parent.rect.width * 0.5f - centerX, 0f);
            Vector3 startScale = restScale * introStartScale;
            previewRect.anchoredPosition = startPos;
            previewRect.localScale = startScale;
            if (editorGroup != null) editorGroup.alpha = 0f;

            yield return new WaitForSecondsRealtime(introHold);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / introDuration)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                previewRect.anchoredPosition = Vector2.Lerp(startPos, restPosition, e);
                previewRect.localScale = Vector3.Lerp(startScale, restScale, e);
                if (editorGroup != null) editorGroup.alpha = e;
                yield return null;
            }
            ResetLayout();

            bool done = false;
            ctx.Dialogue.Play(ctx.Database.resumeIntroLines, () => done = true);
            while (!done) yield return null;

            onReady?.Invoke();
        }

        ResumeCategoryData FirstCategoryWithChoices()
        {
            var cats = ctx.Database.categories;
            for (int i = 0; i < cats.Count; i++)
                if (cats[i] != null && ctx.Choices.CountAvailable(cats[i]) > 0) return cats[i];
            for (int i = 0; i < cats.Count; i++)
                if (cats[i] != null) return cats[i];
            return null;
        }

        void SetLocked(bool value)
        {
            locked = value;
            if (choiceList != null) choiceList.SetInteractable(!value);
            if (preview != null && preview.CategoryList != null) preview.CategoryList.SetInteractable(!value);
            UpdateConfirm();
        }

        void UpdateConfirm()
        {
            if (confirmButton != null)
                confirmButton.interactable = !locked && !confirmed && ctx != null && ctx.Choices.CanConfirm();
        }

        void RefreshCategories()
        {
            itemBuffer.Clear();
            var cats = ctx.Database.categories;
            for (int i = 0; i < cats.Count; i++)
            {
                var cat = cats[i];
                if (cat == null) continue;
                var selected = ctx.Choices.GetSelectedChoice(cat);
                bool exhausted = ctx.Choices.CountAvailable(cat) == 0;
                itemBuffer.Add(new ChoiceItem
                {
                    id = cat.id,
                    title = cat.displayName,
                    subtitle = selected != null ? selected.displayText : (exhausted ? "(nothing left)" : "-"),
                    state = cat == current ? ChoiceState.Selected : ChoiceState.Available
                });
            }
            preview.CategoryList.SetItems(itemBuffer);
        }

        void RefreshChoices(bool resetScroll)
        {
            itemBuffer.Clear();
            string message = "Nothing to choose.";
            if (current != null)
            {
                if (categoryTitleText != null) categoryTitleText.text = current.displayName;
                message = current.emptyMessage;
                string selectedId = ctx.Session.GetSelectedChoiceId(current.id);
                ctx.Choices.GetVisibleChoices(current, choiceBuffer);
                for (int i = 0; i < choiceBuffer.Count; i++)
                {
                    var c = choiceBuffer[i];
                    itemBuffer.Add(new ChoiceItem
                    {
                        id = c.id,
                        title = c.displayText,
                        subtitle = c.description,
                        state = ctx.Choices.IsConsumed(c) ? ChoiceState.Consumed
                              : c.id == selectedId ? ChoiceState.Selected : ChoiceState.Available
                    });
                }
            }
            else if (categoryTitleText != null) categoryTitleText.text = "";
            choiceList.SetItems(itemBuffer, message, resetScroll);
        }

        void OnCategoryChosen(string categoryId)
        {
            if (locked) return;
            var cat = ctx.Database.FindCategory(categoryId);
            if (cat == null) return;
            current = cat;
            RefreshCategories();
            RefreshChoices(true);
        }

        void OnChoiceChosen(string choiceId)
        {
            if (locked || current == null) return;
            var choice = ctx.Database.FindChoice(choiceId);
            if (choice == null || !ctx.Choices.SelectChoice(choice)) return;

            RefreshCategories();
            RefreshChoices(false);
            UpdateConfirm();
            // Replaces any reaction still playing, so rapid clicks never stack.
            ctx.Dialogue.Play(choice.playerReaction, null, reactionAutoAdvance);
        }

        void OnConfirmClicked()
        {
            if (locked || confirmed || ctx == null || !ctx.Choices.CanConfirm()) return;
            confirmed = true;
            SetLocked(true);
            Confirmed?.Invoke();
        }
    }
}
