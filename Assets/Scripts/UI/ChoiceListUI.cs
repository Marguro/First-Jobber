using System;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Scrollable pooled list of ChoiceButtonUI. Items are rebuilt from ChoiceItem data.</summary>
    public class ChoiceListUI : MonoBehaviour
    {
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform content;
        [Tooltip("This section's independent button prefab. Its LayoutElement owns item height; this container's layout owns list padding/spacing/width.")]
        [SerializeField] ChoiceButtonUI itemPrefab;
        [SerializeField] GameObject emptyState;
        [SerializeField] TMP_Text emptyText;
        [SerializeField] CanvasGroup canvasGroup;
        [Header("Presentation")]
        [SerializeField] UIAnimationSettings presentationSettings;
        [SerializeField] UIVisualMotion popupMotion;
        Tween popupTween;
        float revealAfter;

        public event Action<string> ItemChosen;

        readonly Dictionary<ChoiceButtonUI, List<ChoiceButtonUI>> pools = new Dictionary<ChoiceButtonUI, List<ChoiceButtonUI>>();
        List<ChoiceButtonUI> pool;

        public void SetItems(IList<ChoiceItem> items, string emptyMessage = null, bool resetScroll = false)
        {
            SetItemsUsingPrefab(items, itemPrefab, emptyMessage, resetScroll);
        }

        /// <summary>For a distinct role in this section, e.g. Ending Confirm/Back. Pools stay separate and reusable.</summary>
        public void SetItemsUsingPrefab(IList<ChoiceItem> items, ChoiceButtonUI prefab, string emptyMessage = null, bool resetScroll = false)
        {
            if (prefab == null || content == null)
            {
                if (pool != null) for (int i = 0; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
                Debug.LogError("[ChoiceListUI] Missing section-specific button prefab/content on " + name + ". Assign the section prefab; no generic fallback is used.", this);
                return;
            }
            if (!pools.TryGetValue(prefab, out var nextPool))
            {
                nextPool = new List<ChoiceButtonUI>();
                pools.Add(prefab, nextPool);
            }
            if (pool != nextPool && pool != null)
                for (int i = 0; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
            pool = nextPool;

            int count = items != null ? items.Count : 0;
            bool changed = false;
            for (int i = 0; i < count; i++)
                if (i >= pool.Count || !pool[i].gameObject.activeSelf || pool[i].Id != items[i].id) { changed = true; break; }
            for (int i = 0; i < count; i++)
            {
                if (i >= pool.Count) pool.Add(Instantiate(prefab, content));
                var entry = pool[i];
                entry.gameObject.SetActive(true);
                entry.transform.SetSiblingIndex(i);
                entry.Setup(items[i], HandleItem);
                if (changed)
                {
                    var feedback = entry.GetComponent<UIButtonFeedback>();
                    if (feedback != null) feedback.Reveal(i, Mathf.Max(0, revealAfter - Time.unscaledTime));
                }
            }
            for (int i = count; i < pool.Count; i++) pool[i].gameObject.SetActive(false);

            if (emptyState != null)
            {
                emptyState.SetActive(count == 0);
                if (emptyText != null && !string.IsNullOrEmpty(emptyMessage)) emptyText.text = emptyMessage;
            }

            LayoutRebuilder.MarkLayoutForRebuild(content); // Data changed; manual child RectTransforms remain untouched.

            if (resetScroll && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Clear() { SetItems(null); }

        /// <summary>Called by the owning section, not by category/language refresh. Aligns staggering with its entrance.</summary>
        public void PreparePresentation(float delay)
        {
            revealAfter = Time.unscaledTime + Mathf.Max(0, delay);
            if (pool == null) return;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].gameObject.activeSelf)
                {
                    var feedback = pool[i].GetComponent<UIButtonFeedback>();
                    if (feedback != null) feedback.Reveal(i, delay);
                }
        }

        /// <summary>Modal list within its owning panel. Keep it above that panel's background/caption.</summary>
        public void ShowPopup(IList<ChoiceItem> items)
        {
            popupTween?.Kill();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            SetItems(items, resetScroll: true);
            SetInteractable(true);
            if (popupMotion != null) popupMotion.Enter();
            if (canvasGroup != null && presentationSettings != null && presentationSettings.animationsEnabled)
            {
                canvasGroup.alpha = 0f;
                popupTween = canvasGroup.DOFade(1f, presentationSettings.popupDuration).SetEase(presentationSettings.entranceEase).SetUpdate(true);
            }
        }

        public void HidePopup()
        {
            popupTween?.Kill();
            SetInteractable(false);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            if (canvasGroup != null && presentationSettings != null && presentationSettings.animationsEnabled && gameObject.activeInHierarchy)
                popupTween = canvasGroup.DOFade(0f, presentationSettings.popupDuration).SetEase(presentationSettings.exitEase).SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
            else gameObject.SetActive(false);
        }

        void OnDisable() { popupTween?.Kill(); }
        void OnDestroy() { popupTween?.Kill(); }

        public void SetInteractable(bool value)
        {
            if (canvasGroup != null) canvasGroup.interactable = value;
        }

        void HandleItem(string id) { ItemChosen?.Invoke(id); }
    }
}
