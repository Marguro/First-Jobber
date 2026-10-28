using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>
    /// Applies the atmosphere phase that matches the current day: camera colour, fullscreen overlay, tints,
    /// sprite variants (via AtmosphereSpriteBinding), ambient audio crossfade and flicker.
    /// It never touches gameplay state. Every field except the configuration is optional.
    /// </summary>
    public class AtmosphereManager : MonoBehaviour
    {
        public static AtmosphereManager Instance { get; private set; }
        /// <summary>Raised after a different phase (or null) becomes active.</summary>
        public static event Action<AtmospherePhaseData> PhaseChanged;

        static readonly List<AtmosphereSpriteBinding> Bindings = new List<AtmosphereSpriteBinding>();

        [SerializeField] AtmosphereConfiguration configuration;
        [SerializeField] Camera targetCamera;
        [Tooltip("Fullscreen Image above the panels and below the dialogue box. Raycast Target must be off.")]
        [SerializeField] Image overlay;
        [SerializeField] AudioSource audioSourceA;
        [SerializeField] AudioSource audioSourceB;
        [SerializeField] bool logPhaseChanges = true;

        public AtmospherePhaseData AppliedPhase { get; private set; }
        public bool IsPreviewing { get; private set; }
        public Color HrTint => AppliedPhase != null ? AppliedPhase.hrTint : Color.white;

        bool hasApplied;
        int lastDay = 1;
        Color baseCameraColor;
        Color overlayRgb = Color.black;
        float overlayAlpha;
        float flickerOffset;
        Coroutine transitionRoutine;
        Coroutine flickerRoutine;
        Coroutine pulseRoutine;
        AudioSource activeSource;
        Coroutine audioRoutine;
        AudioClip activeClip;
        Material originalOverlayMaterial, coverageOverlayMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            PhaseChanged = null;
            Bindings.Clear();
        }

        public static void Register(AtmosphereSpriteBinding b) { if (!Bindings.Contains(b)) Bindings.Add(b); }
        public static void Unregister(AtmosphereSpriteBinding b) { Bindings.Remove(b); }

        void Awake()
        {
            Instance = this;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null) baseCameraColor = targetCamera.backgroundColor;
            if (overlay != null)
            {
                overlay.raycastTarget = false;
                originalOverlayMaterial=overlay.material;
                coverageOverlayMaterial=new Material(overlay.material){name="Atmosphere overlay (phase art excluded)"};
                coverageOverlayMaterial.SetInt("_Stencil",AtmosphereOverlayMask.ProtectionBit);
                coverageOverlayMaterial.SetInt("_StencilReadMask",AtmosphereOverlayMask.ProtectionBit);
                coverageOverlayMaterial.SetInt("_StencilWriteMask",0);
                coverageOverlayMaterial.SetInt("_StencilComp",(int)UnityEngine.Rendering.CompareFunction.NotEqual);
                overlay.material=coverageOverlayMaterial;
                overlayRgb = overlay.color;
                SetOverlay(0f);
                if (!overlay.gameObject.activeSelf) overlay.gameObject.SetActive(true);
            }
        }

        void Start()
        {
            if (configuration == null) { Debug.LogWarning("[Atmosphere] No configuration assigned - atmosphere disabled.", this); return; }
            foreach (var problem in configuration.Validate()) Debug.LogWarning("[Atmosphere] " + problem, configuration);
        }

        void OnDestroy()
        {
            if (overlay!=null && overlay.material==coverageOverlayMaterial) overlay.material=originalOverlayMaterial;
            if (coverageOverlayMaterial!=null) Destroy(coverageOverlayMaterial);
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------ public API

        /// <summary>Applies the phase for this day. Does nothing if that phase is already active.</summary>
        public void Apply(int day, bool instant = false)
        {
            lastDay = day;
            IsPreviewing = false;
            var phase = configuration != null ? configuration.Resolve(day) : null;
            SetPhase(phase, instant);
        }

        /// <summary>Debug: show a phase without touching the game day.</summary>
        public void Preview(AtmospherePhaseData phase)
        {
            IsPreviewing = true;
            SetPhase(phase, false);
        }

        public void ClearPreview() { Apply(lastDay); }
        /// <summary>Inspector mapping refresh only. Does not change phase/day, audio, progression or UI lists.</summary>
        public void RefreshUIBindings() { foreach (var binding in Bindings) if (binding!=null) binding.Apply(AppliedPhase,0f); }

        /// <summary>Temporary flicker (e.g. from a daily event). Does not change the active phase.</summary>
        public void PulseFlicker(float seconds, float intensity = 0.25f)
        {
            if (overlay == null || seconds <= 0f) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine(seconds, intensity));
        }

        public Sprite ResolveHrSprite(HREmotion emotion)
        {
            return AppliedPhase != null ? AppliedPhase.GetHrSprite(emotion) : null;
        }

        // ------------------------------------------------------------ internals

        void SetPhase(AtmospherePhaseData phase, bool instant)
        {
            if (hasApplied && phase == AppliedPhase) return;
            hasApplied = true;
            AppliedPhase = phase;
            if (logPhaseChanges) Debug.Log("[Atmosphere] Phase -> " + (phase != null ? phase.displayName + " (start day " + phase.startDay + ")" : "none") + (IsPreviewing ? " [preview]" : ""));

            float fade = instant ? 0f : (phase != null ? phase.fadeDuration : 0.5f);

            foreach (var b in Bindings) if (b != null) b.Apply(phase, fade);

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(TransitionRoutine(phase, fade));

            ApplyAudio(phase, instant);

            if (flickerRoutine != null) StopCoroutine(flickerRoutine);
            flickerRoutine = phase != null && phase.flickerEnabled ? StartCoroutine(FlickerRoutine(phase)) : null;
            if (flickerRoutine == null && pulseRoutine == null) { flickerOffset = 0f; SetOverlay(overlayAlpha); }

            PhaseChanged?.Invoke(phase);
        }

        IEnumerator TransitionRoutine(AtmospherePhaseData phase, float duration)
        {
            Color camFrom = targetCamera != null ? targetCamera.backgroundColor : Color.black;
            Color camTo = phase != null && phase.useCameraColor ? phase.cameraColor : baseCameraColor;
            Color rgbFrom = overlayRgb;
            Color rgbTo = phase != null ? phase.overlayColor : overlayRgb;
            float alphaFrom = overlayAlpha;
            float alphaTo = phase != null ? phase.overlayOpacity : 0f;

            for (float t = 0f; t < 1f; t += duration > 0f ? Time.unscaledDeltaTime / duration : 1f)
            {
                Step(t);
                yield return null;
            }
            Step(1f);
            transitionRoutine = null;

            void Step(float k)
            {
                if (targetCamera != null) targetCamera.backgroundColor = Color.Lerp(camFrom, camTo, k);
                overlayRgb = Color.Lerp(rgbFrom, rgbTo, k);
                overlayAlpha = Mathf.Lerp(alphaFrom, alphaTo, k);
                SetOverlay(overlayAlpha);
            }
        }

        void SetOverlay(float baseAlpha)
        {
            if (overlay == null) return;
            overlay.color = new Color(overlayRgb.r, overlayRgb.g, overlayRgb.b, Mathf.Clamp01(baseAlpha + flickerOffset));
        }

        IEnumerator FlickerRoutine(AtmospherePhaseData phase)
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(phase.flickerInterval * UnityEngine.Random.Range(0.5f, 1.5f));
                if (pulseRoutine == null)
                {
                    flickerOffset = phase.flickerIntensity * UnityEngine.Random.Range(0.4f, 1f);
                    SetOverlay(overlayAlpha);
                    yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.04f, 0.12f));
                    flickerOffset = 0f;
                    SetOverlay(overlayAlpha);
                }
            }
        }

        IEnumerator PulseRoutine(float seconds, float intensity)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                flickerOffset = UnityEngine.Random.value < 0.5f ? intensity * UnityEngine.Random.value : 0f;
                SetOverlay(overlayAlpha);
                yield return null;
            }
            flickerOffset = 0f;
            SetOverlay(overlayAlpha);
            pulseRoutine = null;
        }

        // ------------------------------------------------------------ audio

        void ApplyAudio(AtmospherePhaseData phase, bool instant)
        {
            if (audioSourceA == null && audioSourceB == null) return;
            AudioClip clip = phase != null ? phase.ambientClip : null;
            float volume = phase != null ? phase.ambientVolume : 0f;
            float fade = instant || phase == null ? (instant ? 0f : 1f) : phase.audioCrossfade;

            if (audioRoutine != null) StopCoroutine(audioRoutine);
            audioRoutine = StartCoroutine(AudioRoutine(clip, volume, fade));
        }

        IEnumerator AudioRoutine(AudioClip clip, float volume, float fade)
        {
            var prev = activeSource;
            AudioSource to = null;

            if (clip != null)
            {
                if (clip == activeClip && prev != null) to = prev; // same clip: only the volume changes
                else
                {
                    to = prev == audioSourceA ? audioSourceB : audioSourceA;
                    if (to == null) to = prev == null ? audioSourceA : audioSourceB;
                    if (to == null) to = prev;
                    if (to != prev || clip != activeClip)
                    {
                        to.clip = clip;
                        to.loop = true;
                        to.volume = 0f;
                        to.Play();
                    }
                }
            }
            activeSource = to;
            activeClip = to != null ? clip : null;

            float fromVolume = prev != null ? prev.volume : 0f;
            float toStartVolume = to != null ? to.volume : 0f;
            for (float t = 0f; t < 1f; t += fade > 0f ? Time.unscaledDeltaTime / fade : 1f)
            {
                Mix(t);
                yield return null;
            }
            Mix(1f);
            if (prev != null && prev != to) prev.Stop();
            audioRoutine = null;

            void Mix(float k)
            {
                if (to != null) to.volume = Mathf.Lerp(toStartVolume, volume, k);
                if (prev != null && prev != to) prev.volume = Mathf.Lerp(fromVolume, 0f, k);
            }
        }
    }
}
