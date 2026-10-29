using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    public enum UICorruptionRole { Choice, Paper, Decoration, DialogueFrame }

    [CreateAssetMenu(menuName = "First Jobber/UI/Corruption Settings")]
    public class UICorruptionSettings : ScriptableObject
    {
        public bool enableCorruption = true;
        [Range(0, 2)] public float masterIntensity = 1;
        public bool enableTransientGlitches = true;
        [Tooltip("Keep static print echoes/ink; disable every transient corruption movement.")]
        public bool reduceMotion;
        [Tooltip("Shared upper limit across all surfaces, not per button. No fullscreen flashing.")]
        [Range(.05f, 1)] public float maximumGlitchFrequency = .65f;
        public Color ghostRed = new Color(.32f, .055f, .04f, 1);
        public Color ghostBlue = new Color(.04f, .075f, .18f, 1);
        public Color inkColor = new Color(.055f, .035f, .03f, 1);
        [Header("Choice presentation states")]
        [Range(0, 2)] public float availableMultiplier = .5f;
        [Range(0, 2)] public float selectedMultiplier = .7f;
        [Range(0, 2)] public float consumedMultiplier = 1.45f;
        [Range(0, 2)] public float disabledMultiplier = .55f;
        public bool showConsumedRemnants = true;
        [Tooltip("Opacity of the NEW ink/echo layers on consumed entries. Original sprite colors/alpha are never changed.")]
        [Range(0, 1)] public float consumedRemnantOpacity = .78f;
        [Header("Existing atmosphere phase references (no day rules)")]
        public List<PhaseStyle> phases = new List<PhaseStyle>();
        [Serializable] public class PhaseStyle
        {
            public AtmospherePhaseData phase;
            [Range(0, 1)] public float intensity;
            [Tooltip("Random minimum/maximum seconds between attempts on each visible surface (minimum 1 second). Shared global frequency limit still applies.")]
            public Vector2 burstInterval = new Vector2(10, 18);
            [HideInInspector] public float burstDuration = .18f; // Legacy assets: used when the range is unset.
            [Tooltip("Minimum/maximum burst length in seconds. Zero uses the legacy duration.")]
            public Vector2 burstDurationRange;
            [Range(1, 3)] public int burstComplexity = 1;
            [Min(0)] public float horizontalTearStrength = 10;
            [Tooltip("Probability (0-1) of drawing displaced paper bands per burst; intervals above control timing.")]
            [Range(0, 1)] public float horizontalTearFrequency = .6f;
            [Range(0, 3)] public int ghostLayers = 1;
            [Min(0)] public float textOffset = 1.5f;
            [Range(0, 1)] public float ghostOpacity = .12f;
            [Min(0)] public float jitterPixels = 1f;
            [Tooltip("Ink corruption opacity and mark thickness. Broad redactions are limited to consumed choices.")]
            [Range(0, 1)] public float redactionStrength = .32f;
        }
        public PhaseStyle Resolve(AtmospherePhaseData phase)
        {
            if (phase == null || phases == null) return null;
            for (int i = 0; i < phases.Count; i++) if (phases[i] != null && phases[i].phase == phase) return phases[i];
            return null;
        }
        public float Severity(PhaseStyle phase, ChoiceState state, UICorruptionRole role, float localMultiplier)
        {
            if (!enableCorruption || phase == null) return 0;
            float amount = role == UICorruptionRole.Choice
                ? state == ChoiceState.Consumed ? consumedMultiplier : state == ChoiceState.Selected ? selectedMultiplier
                    : state == ChoiceState.Disabled ? disabledMultiplier : availableMultiplier
                : role == UICorruptionRole.DialogueFrame ? .2f : 1f;
            return Mathf.Clamp01(phase.intensity * masterIntensity * amount * Mathf.Max(0, localMultiplier));
        }
        // Perceptual gain avoids multiplying tiny phase/state values into near-invisible offsets.
        public static float VisualGain(float severity) => Mathf.Sqrt(Mathf.Clamp01(severity));
        // FNV-1a, not string.GetHashCode: repeatable through pooling, categories, locales and process restarts.
        public static uint StableSeed(string id)
        {
            uint hash = 2166136261;
            if (id != null) for (int i = 0; i < id.Length; i++) hash = unchecked((hash ^ id[i]) * 16777619);
            return hash;
        }
        public static float Sample(uint seed, int channel)
        {
            uint x = unchecked(seed + (uint)channel * 0x9e3779b9u);
            x ^= x >> 16; x = unchecked(x * 0x7feb352du); x ^= x >> 15;
            return (x & 65535) / 65535f;
        }

        public static event Action<UICorruptionSettings> Changed;
        public void RefreshPresentation() { Changed?.Invoke(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Changed = null; }
#if UNITY_EDITOR
        bool pendingRefresh;
        void OnValidate()
        {
            if (pendingRefresh) return;
            pendingRefresh = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                pendingRefresh = false;
                if (Application.isPlaying) RefreshPresentation();
            };
        }
#endif
    }
}
