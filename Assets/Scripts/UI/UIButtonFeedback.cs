using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FirstJobber
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        [SerializeField] UIAnimationSettings settings;
        [Tooltip("Use a dedicated child for layout-controlled list buttons. Never animate the list item's root.")]
        [SerializeField] RectTransform visual;
        [Tooltip("Dedicated button CanvasGroup only; do not assign a panel's visibility group.")]
        [SerializeField] CanvasGroup entranceGroup;
        Button button;
        Vector3 restScale;
        Tween tween;
        bool captured, hovered, pressed;
        int releasedFrame = -1, soundedFrame = -1, acceptedFrame = -1;
        void Awake()
        {
            button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnAcceptedClick);
            if (visual != null)
            {
                var layout = visual.parent != null ? visual.parent.GetComponent<LayoutGroup>() : null;
                if (layout == null || !layout.isActiveAndEnabled) { restScale = visual.localScale; captured = true; }
            }
        }
        bool Interactive => button != null && button.isActiveAndEnabled && button.IsInteractable();
        public void OnPointerEnter(PointerEventData data) { hovered = true; if (Interactive) Scale(settings != null ? settings.hoverScale : 1f, false); }
        public void OnPointerExit(PointerEventData data) { hovered = false; if (!pressed) Scale(1, false); }
        public void OnPointerDown(PointerEventData data)
        {
            pressed = data.button == PointerEventData.InputButton.Left && Interactive;
            if (pressed) Scale(settings != null ? settings.pressScale : 1f, true);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (pressed && Interactive && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, data.position, data.pressEventCamera))
                releasedFrame = Time.frameCount;
            pressed = false;
            Scale(hovered && settings != null ? settings.hoverScale : 1f, false);
        }
        // Mouse sound runs only from Button's accepted onClick, even if an earlier callback disables it.
        // Programmatic onClick.Invoke and data refresh have no matching pointer release.
        void OnAcceptedClick()
        {
            acceptedFrame = Time.frameCount;
            if (releasedFrame == Time.frameCount) ClickFeedback();
        }
        // Button.OnSubmit may run first and its callback may disable the button. An accepted
        // onClick in this same Submit dispatch still counts; programmatic Invoke alone is silent.
        public void OnSubmit(BaseEventData data) { if (Interactive || acceptedFrame == Time.frameCount) ClickFeedback(); }
        void ClickFeedback()
        {
            if (soundedFrame == Time.frameCount) return;
            soundedFrame = Time.frameCount;
            if (UISfxPlayer.Instance != null) UISfxPlayer.Instance.Play(UISfxKind.Click);
            SelectedFeedback();
        }
        void Scale(float multiplier, bool press)
        {
            if (!captured) return;
            tween?.Kill(); RestoreEntrance();
            if (settings == null || !settings.animationsEnabled) { visual.localScale = restScale; return; }
            float amount = 1f + (multiplier - 1f) * settings.MotionAmount;
            tween = visual.DOScale(restScale * amount, press ? settings.pressDuration : settings.hoverDuration).SetEase(settings.buttonEase).SetUpdate(true);
        }
        public void SelectedFeedback()
        {
            if (!captured || !isActiveAndEnabled) return;
            tween?.Kill(); RestoreEntrance(); visual.localScale = restScale;
            if (settings == null || !settings.animationsEnabled) return;
            float amount = 1f + (settings.selectedScale - 1f) * settings.MotionAmount;
            tween = DOTween.Sequence().Append(visual.DOScale(restScale * amount, settings.feedbackDuration * .4f).SetEase(settings.buttonEase))
                .Append(visual.DOScale(restScale, settings.feedbackDuration * .6f).SetEase(settings.buttonEase)).SetUpdate(true);
        }
        public void Reveal(int index, float sectionDelay = 0)
        {
            if (!captured) return;
            tween?.Kill(); RestoreEntrance();
            visual.localScale = restScale;
            if (settings == null || !settings.animationsEnabled) return;
            visual.localScale = restScale * (1f + (settings.popupStartScale - 1f) * settings.MotionAmount);
            float delay = Mathf.Max(0, sectionDelay) + Mathf.Min(index * settings.staggerDelay, settings.maximumStagger);
            var sequence = DOTween.Sequence().SetDelay(delay).SetUpdate(true);
            sequence.Append(visual.DOScale(restScale, settings.itemEntranceDuration).SetEase(settings.entranceEase));
            if (entranceGroup != null)
            {
                entranceGroup.alpha = 0f; entranceGroup.interactable = false;
                sequence.Join(entranceGroup.DOFade(1f, settings.itemEntranceDuration));
            }
            tween = sequence.OnComplete(RestoreEntrance);
        }
        void RestoreEntrance() { if (entranceGroup != null) { entranceGroup.alpha = 1f; entranceGroup.interactable = true; } }
        void OnDisable()
        {
            tween?.Kill();
            if (captured) visual.localScale = restScale;
            RestoreEntrance(); hovered = pressed = false; releasedFrame = -1;
        }
        void OnDestroy() { tween?.Kill(); if (button != null) button.onClick.RemoveListener(OnAcceptedClick); }
    }
}
