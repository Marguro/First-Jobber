using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace FirstJobber
{
    [CreateAssetMenu(menuName = "First Jobber/UI/Animation Settings")]
    public class UIAnimationSettings : ScriptableObject
    {
        public bool animationsEnabled = true;
        [Header("Panels (foreground only)")]
        [Min(0)] public float entranceDuration = .36f;
        [Min(0)] public float exitDuration = .24f;
        public Vector2 entranceOffset = new Vector2(0, -16);
        public Ease entranceEase = Ease.OutCubic;
        public Ease exitEase = Ease.InQuad;
        public bool fadePanels = true;
        [Header("Buttons and lists")]
        [Min(1)] public float hoverScale = 1.018f;
        [Min(0)] public float hoverDuration = .14f;
        [Range(.8f, 1f)] public float pressScale = .982f;
        [Min(0)] public float pressDuration = .09f;
        [Min(1)] public float selectedScale = 1.02f;
        [Min(0)] public float feedbackDuration = .34f;
        public Ease buttonEase = Ease.OutQuad;
        [Min(0)] public float staggerDelay = .05f;
        [Min(0)] public float maximumStagger = .25f;
        [Min(0)] public float itemEntranceDuration = .24f;
        [Header("Popup and dialogue")]
        [Range(.8f, 1f)] public float popupStartScale = .975f;
        [Min(0)] public float popupDuration = .24f;
        [Min(0)] public float dialogueFadeDuration = .2f;
        [Header("Motion restraint (uses existing atmosphere, never changes it)")]
        public List<PhaseMotion> phaseMotion = new List<PhaseMotion>();
        [Serializable] public class PhaseMotion
        {
            public AtmospherePhaseData phase;
            [Range(0, 1)] public float amount = 1f;
        }
        public float MotionAmount
        {
            get
            {
                var phase = AtmosphereManager.Instance != null ? AtmosphereManager.Instance.AppliedPhase : null;
                for (int i = 0; i < phaseMotion.Count; i++)
                    if (phaseMotion[i] != null && phaseMotion[i].phase == phase) return Mathf.Clamp01(phaseMotion[i].amount);
                return 1f;
            }
        }
    }
}
