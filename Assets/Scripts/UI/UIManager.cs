using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Owns panel visibility (cross-fade) and the full-screen black overlay used for sleep / day transitions.
    /// Panels stay active in the hierarchy; only alpha / raycast / interactable change.
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

        [Header("Transition overlay")]
        [SerializeField] CanvasGroup overlay;
        [SerializeField] TMP_Text overlayText;

        [SerializeField, Min(0.01f)] float fadeDuration = 0.35f;
        [SerializeField, Min(0.01f)] float overlayFadeDuration = 0.7f;

        CanvasGroup[] panels;
        CanvasGroup current;

        void Awake()
        {
            panels = new[] { mainMenu, createApplicant, resumeEditor, company, interview, dailyEvent, finalDecision, ending };
            foreach (var p in panels) SetState(p, 0f, false);
            if (overlay != null) SetState(overlay, 0f, false);
        }

        void Update()
        {
            float step = Time.unscaledDeltaTime / fadeDuration;
            foreach (var p in panels)
            {
                if (p == null) continue;
                float target = p == current ? 1f : 0f;
                if (!Mathf.Approximately(p.alpha, target)) p.alpha = Mathf.MoveTowards(p.alpha, target, step);
            }
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
            current = target;
            foreach (var p in panels)
            {
                if (p == null) continue;
                bool on = p == target;
                p.interactable = on;
                p.blocksRaycasts = on;
                if (instant) p.alpha = on ? 1f : 0f;
            }
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
        }
    }
}
