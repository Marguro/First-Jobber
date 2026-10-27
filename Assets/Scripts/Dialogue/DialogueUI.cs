using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Bottom dialogue box. Pure presentation: DialogueManager drives it.</summary>
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text speakerText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] GameObject continueIndicator;
        [SerializeField] Button clickArea;
        [SerializeField] Image portraitImage;

        public event Action Clicked;

        bool visible;

        void Awake()
        {
            if (clickArea != null) clickArea.onClick.AddListener(RaiseClicked);
            Hide();
        }

        void OnDestroy()
        {
            if (clickArea != null) clickArea.onClick.RemoveListener(RaiseClicked);
        }

        void Update()
        {
            if (!visible) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) RaiseClicked();
        }

        void RaiseClicked() { Clicked?.Invoke(); }

        public void Show()
        {
            // An artist may leave DialogueBox inactive in the scene. Activate BEFORE setting state:
            // its first Awake wires the click area and calls Hide().
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            visible = true;
            if (group == null) return;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        public void Hide()
        {
            visible = false;
            if (group == null) return;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        /// <summary>Sets the line and returns how many characters it contains (for the typewriter).</summary>
        public int SetLine(string speaker, string text, Sprite portrait)
        {
            if (speakerText != null)
            {
                speakerText.text = speaker;
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            }
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.gameObject.SetActive(portrait != null);
            }
            if (bodyText == null) return 0;
            bodyText.text = text;
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            return bodyText.textInfo.characterCount;
        }

        public void SetVisibleCharacters(int count)
        {
            if (bodyText != null) bodyText.maxVisibleCharacters = count;
        }

        public void SetContinueIndicator(bool on)
        {
            if (continueIndicator != null) continueIndicator.SetActive(on);
        }
    }
}
