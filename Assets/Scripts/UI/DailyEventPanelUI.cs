using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public class DailyEventPanelUI : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text captionText;
        [Tooltip("Owns only the existing event background/caption, not the choice popup or game panels.")]
        [SerializeField] CanvasGroup presentationGroup;
        [SerializeField] UIManager panels;
        [Header("Development diagnostics (no per-frame logging)")]
        [SerializeField] bool logBackgroundDiagnostics;
        [SerializeField] Image atmosphereOverlay;
        [SerializeField] CanvasGroup transitionOverlay;
        DailyEventData currentEvent;

        void Awake() { Clear(); }
        void OnDisable() { Clear(); }

        public void Show(string caption, Sprite sprite, bool standaloneFallback = false, DailyEventData eventData = null)
        {
            currentEvent = eventData;
            if (captionText != null) captionText.text = caption;
            if (background != null)
            {
                background.gameObject.SetActive(true);
                background.enabled = true;
                background.raycastTarget = false;
                background.sprite = sprite;
                background.color = sprite != null ? Color.white : new Color(0.04f, 0.04f, 0.06f);
            }
            if (presentationGroup != null)
            {
                // Day 6 runs while FinalDecision is selected; other background-bearing events can
                // run over Resume/Interview. UIManager must still own those screens and their input.
                // This existing graphic is a Canvas sibling of the game panels, so a hidden
                // DailyEventPanel cannot multiply its alpha (notably during Day 6's FinalDecision).
                presentationGroup.ignoreParentGroups = false;
                bool dailyScreen = panels != null && panels.CurrentPanel == panels.PanelFor(GamePhase.DailyEvent);
                presentationGroup.alpha = sprite != null || standaloneFallback || dailyScreen ? 1 : 0;
                presentationGroup.interactable = presentationGroup.blocksRaycasts = false;
            }
            LogState(false);
        }

        public void Clear()
        {
            currentEvent = null;
            if (captionText != null) captionText.text = "";
            if (background != null) background.sprite = null;
            if (presentationGroup != null)
            {
                presentationGroup.alpha = 0;
                presentationGroup.ignoreParentGroups = false;
                presentationGroup.interactable = presentationGroup.blocksRaycasts = false;
            }
        }

        [ContextMenu("Debug/Log current Daily Event background")]
        void LogCurrentBackground() { LogState(true); }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        void LogState(bool force)
        {
            if (!force && !logBackgroundDiagnostics) return;
            float alpha = 1;
            if (background != null)
                foreach (var group in background.GetComponentsInParent<CanvasGroup>(true))
                {
                    if (!group.enabled) continue;
                    alpha *= group.alpha;
                    if (group.ignoreParentGroups) break;
                }
            var manager = AtmosphereManager.Instance;
            var phase = manager != null ? manager.AppliedPhase : null;
            Debug.Log("[DailyEventBackground] event=" + (currentEvent != null ? currentEvent.id + " (" + currentEvent.name + ")" : "none")
                + " background=" + (background != null && background.sprite != null ? background.sprite.name : "none/fallback")
                + " target=" + (background != null ? background.name : "MISSING")
                + " active=" + (background != null && background.gameObject.activeInHierarchy)
                + " enabled=" + (background != null && background.enabled)
                + " effectiveGroupAlpha=" + alpha
                + " imageAlpha=" + (background != null ? background.color.a : 0)
                + " phase=" + (phase != null ? phase.name : "none")
                + " atmosphereAlpha=" + (atmosphereOverlay != null && atmosphereOverlay.isActiveAndEnabled ? atmosphereOverlay.color.a : 0)
                + " transitionAlpha=" + (transitionOverlay != null && transitionOverlay.gameObject.activeInHierarchy ? transitionOverlay.alpha : 0)
                + " (other occlusion not measured)", this);
        }
    }
}
