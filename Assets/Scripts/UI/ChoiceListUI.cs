using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Scrollable pooled list of ChoiceButtonUI. Items are rebuilt from ChoiceItem data.</summary>
    public class ChoiceListUI : MonoBehaviour
    {
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform content;
        [SerializeField] ChoiceButtonUI itemPrefab;
        [SerializeField] GameObject emptyState;
        [SerializeField] TMP_Text emptyText;
        [SerializeField] CanvasGroup canvasGroup;

        public event Action<string> ItemChosen;

        readonly List<ChoiceButtonUI> pool = new List<ChoiceButtonUI>();

        public void SetItems(IList<ChoiceItem> items, string emptyMessage = null, bool resetScroll = false)
        {
            if (itemPrefab == null || content == null)
            {
                Debug.LogError("[ChoiceListUI] Missing itemPrefab/content on " + name, this);
                return;
            }

            int count = items != null ? items.Count : 0;
            for (int i = 0; i < count; i++)
            {
                if (i >= pool.Count) pool.Add(Instantiate(itemPrefab, content));
                var entry = pool[i];
                entry.gameObject.SetActive(true);
                entry.transform.SetSiblingIndex(i);
                entry.Setup(items[i], HandleItem);
            }
            for (int i = count; i < pool.Count; i++) pool[i].gameObject.SetActive(false);

            if (emptyState != null)
            {
                emptyState.SetActive(count == 0);
                if (emptyText != null && !string.IsNullOrEmpty(emptyMessage)) emptyText.text = emptyMessage;
            }

            if (resetScroll && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Clear() { SetItems(null); }

        public void SetInteractable(bool value)
        {
            if (canvasGroup != null) canvasGroup.interactable = value;
        }

        void HandleItem(string id) { ItemChosen?.Invoke(id); }
    }
}
