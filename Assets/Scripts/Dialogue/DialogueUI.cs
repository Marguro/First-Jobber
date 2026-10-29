using System;
using TMPro;
using DG.Tweening;
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
        [SerializeField] UIAnimationSettings animationSettings;
        Tween visibilityTween;
        [SerializeField] bool logTextRevealDiagnostics;
        string diagnosticKey, assignedText;
        public int RevealCharacterCount => bodyText != null ? bodyText.textInfo.characterCount : 0;

        public event Action Clicked;

        bool visible;

        void Awake()
        {
            if (clickArea != null) clickArea.onClick.AddListener(RaiseClicked);
            Hide();
        }

        void OnDestroy()
        {
            visibilityTween?.Kill();
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
            bool alreadyVisible = visible;
            visible = true;
            if (group == null) return;
            if (!alreadyVisible)
            {
                visibilityTween?.Kill();
                if (animationSettings != null && animationSettings.animationsEnabled)
                    visibilityTween = group.DOFade(1f, animationSettings.dialogueFadeDuration).SetEase(animationSettings.entranceEase).SetUpdate(true);
                else group.alpha = 1f;
            }
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        public void Hide()
        {
            bool wasVisible = visible;
            visible = false;
            if (group == null) return;
            group.blocksRaycasts = false;
            group.interactable = false;
            // Empty blocks/cancel callbacks can request Hide again in the same chain.
            // Do not turn an already-running exit into an abrupt alpha cut.
            if (!wasVisible && visibilityTween != null) return;
            visibilityTween?.Kill();
            if (wasVisible && animationSettings != null && animationSettings.animationsEnabled)
                visibilityTween = group.DOFade(0f, animationSettings.dialogueFadeDuration).SetEase(animationSettings.exitEase).SetUpdate(true);
            else group.alpha = 0f;
        }

        void OnDisable() { visibilityTween?.Kill(); visible = false; if (group != null) group.alpha = 0f; }

        /// <summary>Sets the line and returns how many characters it contains (for the typewriter).</summary>
        public int SetLine(string speaker, string text, Sprite portrait, string localizationKey = null)
        {
            if (speakerText != null)
            {
                ThaiTextApplier.Register(speakerText);
                speakerText.maxVisibleCharacters = speakerText.maxVisibleWords = speakerText.maxVisibleLines = int.MaxValue;
                speakerText.firstVisibleCharacter = 0;
                speakerText.text = speaker;
                speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            }
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.gameObject.SetActive(portrait != null);
            }
            if (bodyText == null) return 0;
            diagnosticKey = localizationKey; assignedText = text;
            // Install the existing locale/font/preprocessor BEFORE counting. Its normal safety scan
            // may otherwise add Thai word-break characters after the typewriter cached its length.
            ThaiTextApplier.Register(bodyText);
            bodyText.maxVisibleCharacters = bodyText.maxVisibleWords = bodyText.maxVisibleLines = int.MaxValue;
            bodyText.firstVisibleCharacter = 0;
            bodyText.text = text;
            bodyText.ForceMeshUpdate(true, true);
            int count = bodyText.textInfo.characterCount; // Parsed TMP elements, not UTF-16 or markup length.
            bodyText.maxVisibleCharacters = 0; // No frame is yielded with the complete mesh visible.
            LogReveal(false, false);
            return count;
        }

        public void SetVisibleCharacters(int count)
        {
            if (bodyText != null)
            {
                bodyText.maxVisibleCharacters = count;
                if (count == int.MaxValue) LogReveal(true, false);
            }
        }

        [ContextMenu("Debug/Log current dialogue reveal")]
        void LogCurrentReveal() { LogReveal(bodyText != null && bodyText.maxVisibleCharacters == int.MaxValue, true); }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        void LogReveal(bool completed, bool force)
        {
            if ((!force && !logTextRevealDiagnostics) || bodyText == null) return;
            string processed = bodyText.textPreprocessor != null ? bodyText.textPreprocessor.PreprocessText(bodyText.text ?? "") : bodyText.text;
            Debug.Log("[DialogueReveal] key=" + diagnosticKey + " (event/block/page encoded in key)"
                + " localized=" + assignedText + " localizedLength=" + (assignedText != null ? assignedText.Length : 0)
                + " TMP.textLength=" + (bodyText.text != null ? bodyText.text.Length : 0)
                + " processedLength=" + (processed != null ? processed.Length : 0)
                + " processedDiffers=" + (processed != bodyText.text)
                + " assignedStringChanged=" + (assignedText != bodyText.text)
                + " TMP.characters=" + bodyText.textInfo.characterCount
                + " maxCharacters=" + bodyText.maxVisibleCharacters + " maxWords=" + bodyText.maxVisibleWords
                + " maxLines=" + bodyText.maxVisibleLines + " completed=" + completed, this);
        }

        public void SetContinueIndicator(bool on)
        {
            if (continueIndicator != null) continueIndicator.SetActive(on);
        }
    }
}
