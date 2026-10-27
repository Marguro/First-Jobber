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
        [Header("Confidence")]
        [Tooltip("The vertical overall-confidence bar. Optional - the editor works without it.")]
        [SerializeField] ResumeConfidenceUI confidenceUI;
        [Tooltip("Show each choice's own confidence percentage on its button.")]
        [SerializeField] bool showChoiceConfidence = true;
        [SerializeField, Range(0, 3)] int choiceConfidenceDecimals = 0;
        [Header("Choice order")]
        [Tooltip("Show each category's choices in a random order instead of database order. The order is re-rolled once per day and stays fixed while you edit, so buttons never jump around.")]
        [SerializeField] bool shuffleChoices = true;
        [Header("Intro animation")]
        [Tooltip("Optional. The first-day intro starts exactly on this rectangle (the main-menu resume paper) and zooms out to the editor. Empty = start centred and enlarged.")]
        [SerializeField] RectTransform introStartFrom;
        [Tooltip("Hold time before zooming out when starting from the menu paper.")]
        [SerializeField] float introHoldFromMenu = 0.4f;
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
        class ShuffledOrder { public int day; public List<string> ids = new List<string>(); }
        readonly Dictionary<string, ShuffledOrder> orders = new Dictionary<string, ShuffledOrder>();
        readonly List<ResumeCategoryData> requiredBuffer = new List<ResumeCategoryData>();

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
            preview.SetApplicant(ctx.Session.applicantName, ctx.Database.GenderDisplay(ctx.Session.applicantGender));
            preview.SetDistorted(ctx.Session.HasFlag("resume_distorted"));
            if (dayText != null) dayText.text = UiText.Day.Format(ctx.Session.day);

            if (current == null || !ctx.Database.categories.Contains(current) || ctx.Choices.CountAvailable(current) == 0)
                current = FirstCategoryWithChoices();

            RefreshCategories();
            RefreshChoices(true);
            RefreshConfidence(false);
            SetLocked(true);
        }

        /// <summary>Re-renders all texts in the current language without touching state (selection, lock, scroll).</summary>
        public void RefreshLanguage()
        {
            if (ctx == null) return;
            preview.SetApplicant(ctx.Session.applicantName, ctx.Database.GenderDisplay(ctx.Session.applicantGender));
            if (dayText != null) dayText.text = UiText.Day.Format(ctx.Session.day);
            RefreshCategories();
            RefreshChoices(false);
            RefreshConfidence(false);
        }

        /// <summary>Recalculates from the current selections (never from a stored value) and updates the bar.</summary>
        void RefreshConfidence(bool animate)
        {
            if (confidenceUI == null || ctx == null) return;
            float percent = ctx.Choices.GetOverallConfidence(out int selected);
            ctx.Choices.GetRequiredCategories(requiredBuffer);
            confidenceUI.SetConfidence(percent, selected, requiredBuffer.Count, animate);
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

        // The confidence bar sits outside EditorGroup (far right), so it follows the same fade-in.
        void SetEditorAlpha(float alpha)
        {
            if (editorGroup != null) editorGroup.alpha = alpha;
            if (confidenceUI != null) confidenceUI.SetAlpha(alpha);
        }

        void ResetLayout()
        {
            if (previewRect != null)
            {
                previewRect.anchoredPosition = restPosition;
                previewRect.localScale = restScale;
            }
            SetEditorAlpha(1f);
        }

        IEnumerator IntroRoutine(Action onReady)
        {
            // Start centred and enlarged; the editor side is hidden.
            var parent = previewRect.parent as RectTransform;
            float hold = introHold;
            Vector2 startPos;
            Vector3 startScale;
            if (introStartFrom != null && introStartFrom.rect.width > 1f && previewRect.rect.width > 1f)
            {
                ResetLayout(); // measure from the resting position
                Vector3 target = parent.InverseTransformPoint(introStartFrom.TransformPoint(introStartFrom.rect.center));
                Vector3 now = parent.InverseTransformPoint(previewRect.TransformPoint(previewRect.rect.center));
                startPos = restPosition + new Vector2(target.x - now.x, target.y - now.y);
                startScale = restScale * (introStartFrom.rect.width / previewRect.rect.width);
                hold = introHoldFromMenu;
            }
            else
            {
                float centerX = (previewRect.anchorMin.x + previewRect.anchorMax.x) * 0.5f * parent.rect.width;
                startPos = restPosition + new Vector2(parent.rect.width * 0.5f - centerX, 0f);
                startScale = restScale * introStartScale;
            }
            previewRect.anchoredPosition = startPos;
            previewRect.localScale = startScale;
            SetEditorAlpha(0f);

            yield return new WaitForSecondsRealtime(hold);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / introDuration)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                previewRect.anchoredPosition = Vector2.Lerp(startPos, restPosition, e);
                previewRect.localScale = Vector3.Lerp(startScale, restScale, e);
                SetEditorAlpha(e);
                yield return null;
            }
            ResetLayout();

            bool done = false;
            ctx.Dialogue.Play(ctx.Database.resumeIntroLines, () => done = true, 0f, ctx.Database.resumeIntroMode);
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
                    title = LocalizationManager.Get(cat, "displayName", cat.displayName),
                    subtitle = selected != null ? LocalizationManager.Get(selected, "displayText", selected.displayText) : (exhausted ? UiText.NothingLeft.Text : "-"),
                    state = cat == current ? ChoiceState.Selected : ChoiceState.Available
                });
            }
            preview.CategoryList.SetItems(itemBuffer);
        }

        void RefreshChoices(bool resetScroll)
        {
            itemBuffer.Clear();
            string message = UiText.NothingToChoose.Text;
            if (current != null)
            {
                if (categoryTitleText != null) categoryTitleText.text = LocalizationManager.Get(current, "displayName", current.displayName);
                message = LocalizationManager.Get(current, "emptyMessage", current.emptyMessage);
                string selectedId = ctx.Session.GetSelectedChoiceId(current.id);
                ctx.Choices.GetVisibleChoices(current, choiceBuffer);
                if (shuffleChoices) ApplyShuffledOrder(current, choiceBuffer);
                for (int i = 0; i < choiceBuffer.Count; i++)
                {
                    var c = choiceBuffer[i];
                    itemBuffer.Add(new ChoiceItem
                    {
                        id = c.id,
                        title = LocalizationManager.Get(c, "displayText", c.displayText),
                        subtitle = LocalizationManager.Get(c, "description", c.description),
                        badge = showChoiceConfidence ? ResumeConfidenceCalculator.FormatPercent(c.Confidence, choiceConfidenceDecimals) : null,
                        state = ctx.Choices.IsConsumed(c) ? ChoiceState.Consumed
                              : c.id == selectedId ? ChoiceState.Selected : ChoiceState.Available
                    });
                }
            }
            else if (categoryTitleText != null) categoryTitleText.text = "";
            choiceList.SetItems(itemBuffer, message, resetScroll);
        }

        /// <summary>Reorders the buffer by a per-category random order that is re-rolled only when the day or the set of choices changes.</summary>
        void ApplyShuffledOrder(ResumeCategoryData category, List<ResumeChoiceData> list)
        {
            if (list.Count < 2) return;
            if (!orders.TryGetValue(category.id, out var order)) { order = new ShuffledOrder(); orders[category.id] = order; }

            bool same = order.day == ctx.Session.day && order.ids.Count == list.Count;
            for (int i = 0; same && i < list.Count; i++) if (!order.ids.Contains(list[i].id)) same = false;
            if (!same)
            {
                order.day = ctx.Session.day;
                order.ids.Clear();
                for (int i = 0; i < list.Count; i++) order.ids.Add(list[i].id);
                for (int i = order.ids.Count - 1; i > 0; i--) // Fisher-Yates
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (order.ids[i], order.ids[j]) = (order.ids[j], order.ids[i]);
                }
            }
            list.Sort((a, b) => order.ids.IndexOf(a.id).CompareTo(order.ids.IndexOf(b.id)));
        }

        void OnCategoryChosen(string categoryId)
        {
            if (locked) return;
            var cat = ctx.Database.FindCategory(categoryId);
            if (cat == null) return;
            current = cat;
            RefreshCategories();
            RefreshChoices(true);
            RefreshConfidence(false);
        }

        void OnChoiceChosen(string choiceId)
        {
            if (locked || current == null) return;
            var choice = ctx.Database.FindChoice(choiceId);
            if (choice == null || !ctx.Choices.SelectChoice(choice)) return;

            RefreshCategories();
            RefreshChoices(false);
            RefreshConfidence(true);
            UpdateConfirm();
            // Each choice reacts only once per playthrough (keyed by the stable choice id, saved in the session).
            // Marked before playing, so rapid clicks cannot queue it twice. Nothing is marked when there is nothing to say.
            if (ctx.Session.HasPlayedChoiceReaction(choice.id)) return;
            if (!HasReaction(choice.playerReaction)) return;
            ctx.Session.MarkChoiceReactionPlayed(choice.id);
            // Replaces any reaction still playing, so rapid clicks never stack.
            ctx.Dialogue.Play(choice.playerReaction, null, reactionAutoAdvance, choice.playerReactionMode);
        }

        static bool HasReaction(DialogueLine[] lines)
        {
            if (lines == null) return false;
            for (int i = 0; i < lines.Length; i++) if (lines[i] != null && lines[i].HasContent) return true;
            return false;
        }

        void OnConfirmClicked()
        {
            if (locked || confirmed || ctx == null || !ctx.Choices.CanConfirm()) return;
            confirmed = true;
            ctx.Session.confirmedConfidence = ctx.Choices.GetOverallConfidence(out _); // kept for this application only
            SetLocked(true);
            Confirmed?.Invoke();
        }
    }
}
