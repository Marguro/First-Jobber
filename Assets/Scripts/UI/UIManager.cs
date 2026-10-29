using System;
using System.Collections;
using TMPro;
using DG.Tweening;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Owns panel visibility (cross-fade) and the full-screen black overlay used for sleep / day transitions.
    /// Activates managed roots at startup, even when hidden for scene authoring.
    /// Panels then stay active; only alpha / raycast / interactable change.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] CanvasGroup mainMenu;
        [SerializeField] CanvasGroup createApplicant;
        [SerializeField] CanvasGroup resumeEditor;
        [SerializeField] CanvasGroup company;
        [SerializeField] CanvasGroup interview;
        [SerializeField] CanvasGroup dailyEvent;
        [SerializeField] CanvasGroup finalDecision;
        [SerializeField] CanvasGroup ending;
        [Tooltip("Legacy reference retained for restoring an already-applied preset only. All sections, including interviews, use the shared DialogueBox's authored layout and typography.")]
        [SerializeField] InterviewDialogueLayout interviewDialogueLayout;

        [Header("Transition overlay")]
        [SerializeField] CanvasGroup overlay;
        [SerializeField] TMP_Text overlayText;

        [SerializeField, Min(0.01f)] float fadeDuration = 0.35f;
        [SerializeField, Min(0.01f)] float overlayFadeDuration = 0.7f;

        [Header("Foreground presentation (never affects overlays)")]
        [SerializeField] UIAnimationSettings animationSettings;
        [SerializeField] PanelPresentation[] presentations;
        [Serializable] public class PanelPresentation
        {
            public CanvasGroup panel;
            [Tooltip("Overrides this panel's fade durations/eases. Each motion target has its own editable Settings reference.")]
            public UIAnimationSettings settingsOverride;
            public UIVisualMotion[] visuals;
        }
        public event Action SectionChanged;
        public event Action<GamePhase> PhasePresented;
        readonly System.Collections.Generic.Dictionary<CanvasGroup, Tween> panelTweens = new System.Collections.Generic.Dictionary<CanvasGroup, Tween>();
        readonly System.Collections.Generic.Dictionary<CanvasGroup, ChoiceListUI[]> sectionLists = new System.Collections.Generic.Dictionary<CanvasGroup, ChoiceListUI[]>();
        GamePhase? lastPresentedPhase;

        CanvasGroup[] panels;
        CanvasGroup current;
        public CanvasGroup CurrentPanel => current; // Read-only presentation query; navigation remains owned here.

        void Awake()
        {
            panels = new[] { mainMenu, createApplicant, resumeEditor, company, interview, dailyEvent, finalDecision, ending };
            foreach (var p in panels) SetState(p, 0f, false);
            foreach (var p in panels) if (p != null) sectionLists[p] = p.GetComponentsInChildren<ChoiceListUI>(true);
            if (overlay != null) SetState(overlay, 0f, false);
        }

        UIAnimationSettings SettingsFor(CanvasGroup panel)
        {
            if (presentations != null) foreach (var entry in presentations)
                if (entry.panel == panel && entry.settingsOverride != null) return entry.settingsOverride;
            return animationSettings;
        }
        void AnimateVisuals(CanvasGroup panel, bool enter, float delay = 0)
        {
            if (presentations == null) return;
            foreach (var entry in presentations)
                if (entry.panel == panel && entry.visuals != null)
                    foreach (var visual in entry.visuals)
                        if (visual != null) { if (enter) visual.Enter(delay); else visual.Exit(); }
        }
        void OnDisable()
        {
            foreach (var tween in panelTweens.Values) tween?.Kill();
            panelTweens.Clear();
            if (panels != null) foreach (var panel in panels)
                if (panel != null) SetState(panel, panel == current ? 1f : 0f, panel == current);
        }

        public CanvasGroup PanelFor(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MainMenu: return mainMenu;
                case GamePhase.CreateApplicant: return createApplicant;
                case GamePhase.ResumeEditor: return resumeEditor;
                case GamePhase.CompanySelection: return company;
                case GamePhase.InterviewIntroduction:
                case GamePhase.InterviewResumeReview:
                case GamePhase.InterviewQuestion:
                case GamePhase.InterviewReaction:
                case GamePhase.Rejection: return interview;
                case GamePhase.DailyEvent: return dailyEvent;
                case GamePhase.FinalDecision: return finalDecision;
                case GamePhase.Ending: return ending;
                default: return current; // Sleep / DayTransition keep the current panel under the overlay
            }
        }

        public void ShowPanelFor(GamePhase phase, bool instant = false)
        {
            var target = PanelFor(phase);
            if (target == null) return;
            if (lastPresentedPhase != phase) { lastPresentedPhase = phase; PhasePresented?.Invoke(phase); }
            // One authored DialogueBox for every section: never apply interview-only style overrides.
            if (interviewDialogueLayout != null) interviewDialogueLayout.SetInterviewActive(false);
            if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
            if (current == target) return; // Interview sub-phases never restart entrance/SFX.
            var previous = current;
            current = target;
            float exit = 0f;
            if (previous != null && !instant)
            {
                var oldSettings = SettingsFor(previous);
                exit = oldSettings != null && oldSettings.animationsEnabled ? oldSettings.exitDuration : oldSettings == null ? fadeDuration : 0f;
                AnimateVisuals(previous, false);
            }
            foreach (var p in panels)
            {
                if (p == null) continue;
                bool on = p == target;
                if (panelTweens.TryGetValue(p, out var oldTween)) oldTween?.Kill();
                var settings = SettingsFor(p);
                bool animate = !instant && (settings == null || settings.animationsEnabled);
                p.interactable = false; p.blocksRaycasts = false;
                if (!animate)
                {
                    p.alpha = on ? 1f : 0f; p.interactable = on; p.blocksRaycasts = on;
                    continue;
                }
                float duration = settings != null ? (on ? settings.entranceDuration : settings.exitDuration) : fadeDuration;
                if (settings != null && !settings.fadePanels) duration = 0;
                // Kill is per panel; never kill other systems' tweens on the Canvas/overlay.
                panelTweens[p] = p.DOFade(on ? 1f : 0f, duration).SetDelay(on ? exit : 0)
                    .SetEase(settings != null ? (on ? settings.entranceEase : settings.exitEase) : Ease.OutQuad).SetUpdate(true)
                    .OnComplete(() => { if (p == current) { p.interactable = true; p.blocksRaycasts = true; } });
            }
            if (!instant || previous == null) AnimateVisuals(target, true, exit);
            if (sectionLists.TryGetValue(target, out var lists))
                foreach (var list in lists) list.PreparePresentation(instant ? 0 : exit);
            if (previous != null) SectionChanged?.Invoke();
        }

        /// <summary>Fade to black, show caption, run onBlack (swap state), hold, fade back in.</summary>
        public IEnumerator Blackout(string caption, float hold, Action onBlack)
        {
            if (overlay == null) { onBlack?.Invoke(); yield break; }
            if (overlayText != null) overlayText.text = caption;
            overlay.blocksRaycasts = true;
            yield return FadeOverlay(1f);
            onBlack?.Invoke();
            yield return new WaitForSecondsRealtime(hold);
            yield return FadeOverlay(0f);
            overlay.blocksRaycasts = false;
        }

        IEnumerator FadeOverlay(float target)
        {
            while (!Mathf.Approximately(overlay.alpha, target))
            {
                overlay.alpha = Mathf.MoveTowards(overlay.alpha, target, Time.unscaledDeltaTime / overlayFadeDuration);
                yield return null;
            }
        }

        static void SetState(CanvasGroup g, float alpha, bool on)
        {
            if (g == null) return;
            g.alpha = alpha;
            g.interactable = on;
            g.blocksRaycasts = on;
            // CanvasGroup cannot reveal a disabled GameObject. Initialize controllers before flow starts.
            if (!g.gameObject.activeSelf) g.gameObject.SetActive(true);
        }
    }
}
