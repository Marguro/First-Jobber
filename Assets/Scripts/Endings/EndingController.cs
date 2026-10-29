using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FirstJobber
{
    /// <summary>One shared ending presenter. FinalDecision confirmation remains the only Production entry point.</summary>
    public class EndingController : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text finalText;
        // Retained only to keep legacy scene UI disabled; completion never waits for a button.
        [SerializeField] GameObject buttonsRoot;
        [SerializeField] AudioSource audioSource;
        [SerializeField] DayBgmController dayMusic;
        [Header("Timeline scene bindings")]
        [SerializeField] PlayableDirector director;
        [SerializeField] TimelineBinding[] timelineBindings;
        [SerializeField] Transform cutsceneRoot;
        [Header("Fullscreen video and fades")]
        [SerializeField] GameObject videoRoot;
        [SerializeField] RawImage videoImage;
        [SerializeField] AspectRatioFitter videoAspect;
        [SerializeField] CanvasGroup fadeOverlay;
        [SerializeField] VideoPlayer videoPlayer;
        [Tooltip("Existing Production atmosphere manager. Only fullscreen Video suspends its overlay rendering.")]
        [SerializeField] AtmosphereManager atmosphere;
        [Min(1), SerializeField] float prepareTimeout = 20f;
        [Min(1), SerializeField] float playbackGrace = 30f;

        [Serializable]
        public class TimelineBinding
        {
            [Tooltip("Empty = all endings. An ending-specific entry wins.")]
            public string endingId;
            public string trackName;
            public UnityEngine.Object target;
        }
        public event Action<EndingData> PresentationCompleted;
        bool presenting, completed, skipRequested, videoDone, videoFailed, videoPrepared, timelineDone;
        EndingData current;
        Coroutine runner;
        PlayableDirector active;
        GameObject spawned;
        RenderTexture videoTexture;
        IDisposable videoAtmosphereSuppression, videoAmbientSuppression;
        readonly Dictionary<UnityEngine.Object, UnityEngine.Object> originalBindings = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

        void Awake()
        {
            if (director == null) director = GetComponent<PlayableDirector>();
            if (director == null) director = gameObject.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            if (buttonsRoot != null) buttonsRoot.SetActive(false);
            HidePresentation();
        }
        public void Show(GameContext ctx, EndingData ending, bool previewOnly = false)
        {
            if (presenting) { Debug.LogWarning("[Ending] A presentation is already running; duplicate request ignored.", this); return; }
            if (ending == null || ctx == null) { Debug.LogError("[Ending] Missing ending/context.", this); return; }
            CleanupPlayback();
            RestoreVideoAtmosphere();
            current = ending; completed = skipRequested = false; presenting = true;
            if (background != null) { background.sprite = ending.background; background.color = ending.background != null ? Color.white : ending.backgroundColor; }
            if (titleText != null) { titleText.text = LocalizationManager.Get(ending, "title", ending.title); titleText.gameObject.SetActive(false); }
            if (finalText != null) { finalText.text = ""; finalText.gameObject.SetActive(false); }
            if (buttonsRoot != null) buttonsRoot.SetActive(false);
            // Run yields once first, so a Confirm click cannot also skip its presentation.
            runner = StartCoroutine(Run(ctx, ending));
        }
        IEnumerator Run(GameContext ctx, EndingData ending)
        {
            yield return null;
            if (dayMusic != null) yield return dayMusic.SilenceAndWait();
            if (audioSource != null && ending.sound != null) audioSource.PlayOneShot(ending.sound);
            var mode = ending.presentationMode;
            bool withDialogue = mode == EndingPresentationMode.Timeline && ending.timelineMode == EndingTimelineMode.WithDialogue;
            bool instead = mode == EndingPresentationMode.Timeline && ending.timelineMode == EndingTimelineMode.InsteadOfDialogue;
            bool dialoguePlayed = false;
            if (mode != EndingPresentationMode.None)
            {
                SetFade(1f);
                if (mode == EndingPresentationMode.Video)
                    yield return PlayVideo(ending);
                else
                {
                    bool started = StartTimeline(ending);
                    if (started)
                    {
                        yield return Fade(1f, 0f, ending.fadeInDuration);
                        if (withDialogue)
                        {
                            bool done = false;
                            dialoguePlayed = true;
                            ctx.Dialogue.Play(ending.lines, () => done = true);
                            while (!done)
                            {
                                if (WantsSkip() && active != null && !timelineDone) active.Stop();
                                yield return null;
                            }
                        }
                        yield return WaitTimeline(ending, !withDialogue);
                        yield return Fade(0f, 1f, ending.fadeOutDuration);
                    }
                }
                CleanupPlayback();
                HidePresentation();
            }
            if (!dialoguePlayed && !instead)
            {
                bool done = false;
                ctx.Dialogue.Play(ending.lines, () => done = true);
                while (!done) yield return null;
            }
            if (titleText != null) titleText.gameObject.SetActive(true);
            if (finalText != null) { finalText.text = LocalizationManager.Get(ending, "finalText", ending.finalText); finalText.gameObject.SetActive(true); }
            // GameFlow determines the destination via PresentationCompleted for every mode.
            if (buttonsRoot != null) buttonsRoot.SetActive(false);
            presenting = false;
            runner = null;
            if (!completed)
            {
                completed = true;
                // GameFlow resets the phase/destination before the original overlay is revealed.
                try { PresentationCompleted?.Invoke(ending); }
                finally { RestoreVideoAtmosphere(); }
            }
        }
        public void RequestSkip()
        {
            if (presenting && current != null && current.allowSkip) skipRequested = true;
        }
        bool WantsSkip() => current != null && current.allowSkip && (skipRequested || SkipPressed());
        static bool SkipPressed()
        {
            var kb = Keyboard.current; var mouse = Mouse.current;
            return (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
        IEnumerator PlayVideo(EndingData ending)
        {
            if (ending.videoClip == null || videoRoot == null || videoImage == null || videoAspect == null)
            { Debug.LogWarning("[Ending] '" + ending.id + "': Video clip/fullscreen UI missing; continuing to ending action.", this); yield break; }
            if (videoAtmosphereSuppression == null && atmosphere != null)
                videoAtmosphereSuppression = atmosphere.SuppressOverlayRendering();
            if (atmosphere != null)
            {
                videoAmbientSuppression = atmosphere.SuppressAmbientAudio();
                // Resume only after GameFlow selects an actual gameplay destination.
                atmosphere.SetAmbientGameplayActive(false);
                yield return atmosphere.WaitForAmbientSilence();
                if (WantsSkip()) yield break;
            }
            if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null) videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false; videoPlayer.isLooping = false; videoPlayer.waitForFirstFrame = true;
            videoPlayer.source = VideoSource.VideoClip; videoPlayer.clip = ending.videoClip;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.controlledAudioTrackCount = ending.videoClip.audioTrackCount;
            for (ushort track = 0; track < videoPlayer.controlledAudioTrackCount; track++)
            {
                videoPlayer.EnableAudioTrack(track, true);
                videoPlayer.SetDirectAudioMute(track, false);
                videoPlayer.SetDirectAudioVolume(track, 1f);
            }
            videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            // A zero-area UV rect samples one pixel and looks like a solid gray screen.
            videoImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEnd; videoPlayer.errorReceived += OnVideoError;
            videoDone = videoFailed = videoPrepared = false;
            videoPlayer.Prepare();
            float started = Time.realtimeSinceStartup;
            while (!videoPrepared && !videoPlayer.isPrepared && !videoFailed)
            {
                if (WantsSkip()) yield break;
                if (Time.realtimeSinceStartup - started > Mathf.Max(1, prepareTimeout)) { Debug.LogWarning("[Ending] Video preparation timed out: " + ending.id, this); yield break; }
                yield return null;
            }
            if (videoFailed) yield break;
            int width = Mathf.Max(1, (int)videoPlayer.width), height = Mathf.Max(1, (int)videoPlayer.height);
            float ratio = width / (float)height;
            float scale = Mathf.Min(1f, 4096f / Mathf.Max(width, height));
            videoTexture = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(width * scale)), Mathf.Max(1, Mathf.RoundToInt(height * scale)), 0, RenderTextureFormat.ARGB32);
            if (!videoTexture.Create()) { Debug.LogWarning("[Ending] Unable to allocate video render texture.", this); yield break; }
            videoPlayer.targetTexture = videoTexture; videoImage.texture = videoTexture;
            videoAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; videoAspect.aspectRatio = ratio;
            // Both consumers share the same live target; reveal only after preparation succeeds.
            videoRoot.SetActive(true); videoPlayer.Play();
            Debug.Log("[Ending] Video prepared: " + ending.id + " (" + width + "x" + height + "), shared RenderTexture bound.", this);
            yield return Fade(1f, 0f, ending.fadeInDuration);
            double length = videoPlayer.length;
            double budget = length > 0 && !double.IsInfinity(length) && !double.IsNaN(length) ? length + Mathf.Max(1, playbackGrace) : 300;
            started = Time.realtimeSinceStartup;
            while (!videoDone && !videoFailed)
            {
                if (WantsSkip()) break;
                if (Time.realtimeSinceStartup - started > budget) { Debug.LogWarning("[Ending] Video playback stalled: " + ending.id, this); break; }
                yield return null;
            }
            yield return Fade(0f, 1f, ending.fadeOutDuration);
        }
        void OnVideoPrepared(VideoPlayer player) { if (player == videoPlayer) videoPrepared = true; }
        void OnVideoEnd(VideoPlayer player) { if (player == videoPlayer) videoDone = true; }
        void OnVideoError(VideoPlayer player, string error)
        { if (player != videoPlayer) return; videoFailed = true; Debug.LogWarning("[Ending] Video failed: " + error, this); }

        bool StartTimeline(EndingData ending)
        {
            if (ending.cutscenePrefab != null)
            {
                if (cutsceneRoot == null) { Debug.LogWarning("[Ending] Assign Cutscene Root for bound prefabs.", this); return false; }
                cutsceneRoot.gameObject.SetActive(false);
                spawned = Instantiate(ending.cutscenePrefab, cutsceneRoot);
                var directors = spawned.GetComponentsInChildren<PlayableDirector>(true);
                foreach (var d in directors) d.playOnAwake = false;
                active = directors.Length > 0 ? directors[0] : null;
                cutsceneRoot.gameObject.SetActive(true);
            }
            else { active = director; if (active != null) active.playableAsset = ending.timeline; }
            if (active == null || active.playableAsset == null)
            { Debug.LogWarning("[Ending] '" + ending.id + "': Timeline/Director missing; continuing to ending action.", this); return false; }
            active.playOnAwake = false; active.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime; active.extrapolationMode = DirectorWrapMode.None;
            double duration = active.playableAsset.duration;
            if (double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0)
            { Debug.LogWarning("[Ending] Timeline must have a finite positive duration: " + ending.id, this); return false; }
            foreach (var output in active.playableAsset.outputs)
            {
                var key = output.sourceObject;
                if (key == null) continue;
                var original = active.GetGenericBinding(key); originalBindings[key] = original;
                UnityEngine.Object target = original;
                if (timelineBindings != null)
                    foreach (var binding in timelineBindings)
                    {
                        if (binding == null || binding.trackName != key.name) continue;
                        bool specific = !string.IsNullOrEmpty(binding.endingId);
                        if (specific && binding.endingId != ending.id) continue;
                        target = binding.target; if (specific) break;
                    }
                if (target == null && key is UnityEngine.Timeline.AudioTrack) target = audioSource;
                target = Coerce(output.outputTargetType, target);
                if (target != null) active.SetGenericBinding(key, target);
                else
                {
                    active.ClearGenericBinding(key);
                    if (output.outputTargetType != null)
                        Debug.LogWarning("[Ending] Unbound track '" + key.name + "' for " + ending.id + ". Assign Timeline Bindings or bind it inside the cutscene prefab.", this);
                }
            }
            timelineDone = false; active.stopped += OnTimelineStopped; active.time = 0; active.Play();
            return true;
        }
        static UnityEngine.Object Coerce(Type type, UnityEngine.Object target)
        {
            if (target == null || type == null || type.IsInstanceOfType(target)) return target;
            var go = target as GameObject;
            if (go == null && target is Component component) go = component.gameObject;
            if (go == null) return null;
            if (type == typeof(GameObject)) return go;
            return typeof(Component).IsAssignableFrom(type) ? go.GetComponent(type) : null;
        }
        void OnTimelineStopped(PlayableDirector value) { if (value == active) timelineDone = true; }
        IEnumerator WaitTimeline(EndingData ending, bool maySkip)
        {
            yield return null;
            float started = Time.realtimeSinceStartup;
            double budget = active != null ? active.duration + Mathf.Max(1, playbackGrace) : 0;
            while (active != null && !timelineDone)
            {
                if (maySkip && WantsSkip()) break;
                if (Time.realtimeSinceStartup - started > budget) { Debug.LogWarning("[Ending] Timeline stalled: " + ending.id, this); break; }
                yield return null;
            }
        }
        IEnumerator Fade(float from, float to, float duration)
        {
            if (fadeOverlay == null || duration <= 0) { SetFade(to); yield break; }
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            { SetFade(Mathf.Lerp(from, to, t / duration)); yield return null; }
            SetFade(to);
        }
        void SetFade(float value)
        {
            if (fadeOverlay == null) return;
            fadeOverlay.alpha = value;
            fadeOverlay.blocksRaycasts = presenting;
        }
        void HidePresentation()
        {
            if (videoRoot != null) videoRoot.SetActive(false);
            SetFade(0);
            if (fadeOverlay != null) fadeOverlay.blocksRaycasts = false;
        }
        void CleanupPlayback()
        {
            if (videoRoot != null) videoRoot.SetActive(false);
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.loopPointReached -= OnVideoEnd; videoPlayer.errorReceived -= OnVideoError;
                videoPlayer.Stop(); videoPlayer.targetTexture = null; videoPlayer.clip = null;
            }
            if (videoImage != null) videoImage.texture = null;
            if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); videoTexture = null; }
            if (active != null)
            {
                active.stopped -= OnTimelineStopped; active.Stop();
                foreach (var pair in originalBindings)
                    if (pair.Value != null) active.SetGenericBinding(pair.Key, pair.Value); else active.ClearGenericBinding(pair.Key);
            }
            originalBindings.Clear(); active = null;
            if (spawned != null) Destroy(spawned); spawned = null;
            if (cutsceneRoot != null) cutsceneRoot.gameObject.SetActive(false);
        }
        void RestoreVideoAtmosphere()
        {
            videoAmbientSuppression?.Dispose(); videoAmbientSuppression = null;
            videoAtmosphereSuppression?.Dispose(); videoAtmosphereSuppression = null;
        }
        void OnDisable()
        {
            if (runner != null) { StopCoroutine(runner); runner = null; }
            CleanupPlayback(); presenting = false; HidePresentation(); RestoreVideoAtmosphere();
        }
    }
}
