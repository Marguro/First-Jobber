using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Foreground-only motion relative to the authored pose. Never sizes a RectTransform.</summary>
    public class UIVisualMotion : MonoBehaviour
    {
        [SerializeField] UIAnimationSettings settings;
        [SerializeField] RectTransform visual;
        [SerializeField] bool move = true;
        [SerializeField] Vector2 offsetMultiplier = Vector2.one;
        [Tooltip("Optional Pro Scale animation supplies duration/ease. Keep Auto Generate/Auto Play off. Scale endpoints are derived from the authored pose and factor below.")]
        [SerializeField] DOTweenAnimation proEntrance;
        [SerializeField, Range(.8f, 1f)] float proEntranceScale = .985f;
        Vector2 restPosition;
        Vector3 restScale;
        Tween movement, feedback;
        bool captured;
        void Awake() { Capture(); }
        void Capture()
        {
            if (captured) return;
            if (visual == null) visual = transform as RectTransform;
            if (visual == null) return;
            // A layout-controlled element must instead supply a dedicated child VisualRoot.
            var layout = visual.parent != null ? visual.parent.GetComponent<LayoutGroup>() : null;
            if (layout != null && layout.isActiveAndEnabled)
            { Debug.LogWarning("[UI Polish] Layout owns " + visual.name + "; assign a child visual instead.", this); return; }
            restPosition = visual.anchoredPosition; restScale = visual.localScale; captured = true;
        }
        public void Enter(float delay = 0)
        {
            Capture(); ResetVisual();
            if (!captured || settings == null || !settings.animationsEnabled || !isActiveAndEnabled) return;
            if (move)
            {
                visual.anchoredPosition = restPosition + Vector2.Scale(settings.entranceOffset, offsetMultiplier) * settings.MotionAmount;
                movement = visual.DOAnchorPos(restPosition, settings.entranceDuration).SetDelay(delay)
                    .SetEase(settings.entranceEase).SetUpdate(true).OnComplete(() => visual.anchoredPosition = restPosition);
            }
            if (proEntrance != null && proEntrance.enabled)
            {
                // Uniform multiplier preserves even an existing artist-authored non-unit pose.
                // An absolute (1,1,1) endpoint would distort the current MenuPaper artwork.
                proEntrance.animationType = DOTweenAnimation.AnimationType.Scale;
                proEntrance.isFrom = true; proEntrance.isRelative = false; proEntrance.optionalBool0 = false;
                proEntrance.endValueV3 = restScale * (1f + (proEntranceScale - 1f) * settings.MotionAmount);
                proEntrance.delay = delay;
                proEntrance.RewindThenRecreateTweenAndPlay();
            }
        }
        public void Exit()
        {
            Capture(); StopOwned();
            if (!captured) return;
            if (settings == null || !settings.animationsEnabled || !move) { ResetVisual(); return; }
            movement = visual.DOAnchorPos(restPosition - Vector2.Scale(settings.entranceOffset, offsetMultiplier) * settings.MotionAmount, settings.exitDuration)
                .SetEase(settings.exitEase).SetUpdate(true).OnComplete(() => { visual.anchoredPosition = restPosition; visual.localScale = restScale; });
        }
        public void Pulse()
        {
            Capture();
            if (!captured || settings == null || !settings.animationsEnabled || proEntrance != null) return;
            feedback?.Kill(); visual.localScale = restScale;
            float size = 1f + (settings.selectedScale - 1f) * settings.MotionAmount;
            feedback = DOTween.Sequence().Append(visual.DOScale(restScale * size, settings.feedbackDuration * .4f).SetEase(settings.buttonEase))
                .Append(visual.DOScale(restScale, settings.feedbackDuration * .6f).SetEase(settings.buttonEase)).SetUpdate(true);
        }
        void StopOwned()
        {
            movement?.Kill(); feedback?.Kill();
            if (proEntrance != null && proEntrance.tween != null) proEntrance.tween.Kill();
        }
        public void ResetVisual()
        {
            StopOwned();
            if (!captured) return;
            visual.anchoredPosition = restPosition; visual.localScale = restScale;
        }
        void OnDisable() { ResetVisual(); }
        void OnDestroy() { StopOwned(); }
    }
}
