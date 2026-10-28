using System.Collections;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>Day music only. AtmosphereManager continues to own separate ambient sound.</summary>
    public class DayBgmController : MonoBehaviour
    {
        [SerializeField] DayBgmConfiguration configuration;
        [SerializeField] AudioSource sourceA;
        [SerializeField] AudioSource sourceB;
        [SerializeField, Min(1)] int previewDay = 1;
        AudioSource incoming, outgoing;
        DayBgmConfiguration.DayMusic playingEntry;
        float incomingGain, outgoingGain, fromIncoming, fromOutgoing, elapsed, duration;
        bool transitioning, silenced = true, paused;
        int currentDay = -1;

        public void PlayDay(int day)
        {
            if (configuration == null || sourceA == null || sourceB == null)
            { Debug.LogError("[Day BGM] Configuration or audio sources are missing.", this); return; }
            if (currentDay == day && !silenced) return;
            currentDay = day;
            var entry = configuration.Resolve(day);
            if (entry == null || (entry.enabled && entry.audioClip == null))
            {
                Debug.LogWarning("[Day BGM] Day " + day + " has no assigned clip. Behavior: " + configuration.missingClipBehavior, this);
                if (configuration.missingClipBehavior == MissingBgmBehavior.KeepPrevious && !silenced) return;
                Silence(); return;
            }
            if (!entry.enabled) { Silence(); return; }
            bool wasSilenced = silenced;
            silenced = false;
            playingEntry = entry;
            // Same track across days keeps its playback position, including a finished non-looping clip.
            if (!wasSilenced && incoming != null && incoming.clip == entry.audioClip)
            { incoming.loop = entry.loop; return; }
            // Interrupted crossfade: keep the louder voice and reuse the quieter one.
            if (outgoing != null && outgoingGain > incomingGain)
            {
                var swap = incoming; incoming = outgoing; outgoing = swap;
                float gain = incomingGain; incomingGain = outgoingGain; outgoingGain = gain;
            }
            if (outgoing != null) outgoing.Stop();
            outgoing = incoming; outgoingGain = incomingGain;
            incoming = outgoing == sourceA ? sourceB : sourceA;
            incoming.Stop(); incoming.clip = entry.audioClip; incoming.loop = entry.loop;
            incoming.volume = 0f; incomingGain = 0f;
            incoming.ignoreListenerPause = configuration.playDuringPause;
            incoming.Play();
            Debug.Log("[Day BGM] Playing day " + day + ": " + entry.audioClip.name + "; fade=" + configuration.defaultFadeDuration + "s; master=" + configuration.masterBgmVolume + "; day volume=" + entry.volumeMultiplier, this);
            if (paused) incoming.Pause();
            BeginTransition();
        }

        public void Silence()
        {
            silenced = true;
            BeginTransition();
        }

        public IEnumerator SilenceAndWait()
        {
            Silence();
            while (transitioning) yield return null;
        }

        void BeginTransition()
        {
            fromIncoming = incomingGain; fromOutgoing = outgoingGain;
            elapsed = 0f;
            duration = configuration != null ? Mathf.Max(0f, configuration.defaultFadeDuration) : 0f;
            transitioning = true;
        }

        void Update()
        {
            if (configuration == null) return;
            bool shouldPause = !configuration.playDuringPause && (Time.timeScale == 0f || AudioListener.pause);
            if (shouldPause != paused)
            {
                paused = shouldPause;
                SetPaused(sourceA, paused); SetPaused(sourceB, paused);
            }
            // Unscaled fades complete even while gameplay/music is paused.
            if (transitioning)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                bool sequential = !configuration.enableCrossfade && outgoing != null && !silenced;
                incomingGain = Mathf.Lerp(fromIncoming, silenced ? 0f : 1f, sequential ? Mathf.Clamp01(t * 2f - 1f) : t);
                outgoingGain = Mathf.Lerp(fromOutgoing, 0f, sequential ? Mathf.Clamp01(t * 2f) : t);
                if (t >= 1f)
                {
                    transitioning = false;
                    if (outgoing != null) outgoing.Stop();
                    outgoing = null;
                    if (silenced && incoming != null) incoming.Stop();
                }
            }
            ApplyVolume(incoming, incomingGain);
            ApplyVolume(outgoing, outgoingGain);
        }

        void ApplyVolume(AudioSource source, float gain)
        {
            if (source == null) return;
            source.ignoreListenerPause = configuration.playDuringPause;
            source.outputAudioMixerGroup = configuration.outputMixerGroup;
            // Outgoing voice retains its own authored multiplier while fading.
            float multiplier = 1f;
            if (source == incoming && playingEntry != null) multiplier = playingEntry.volumeMultiplier;
            else multiplier = source == sourceA ? multiplierA : multiplierB;
            if (source == sourceA) multiplierA = multiplier; else multiplierB = multiplier;
            source.volume = Mathf.Clamp01(configuration.masterBgmVolume) * Mathf.Clamp01(multiplier) * gain;
        }
        float multiplierA = 1f, multiplierB = 1f;
        [ContextMenu("Diagnose current music (Play Mode)")]
        public void Diagnose()
        {
            Debug.Log("[Day BGM] day=" + currentDay + "; enabled=" + isActiveAndEnabled
                + "; config=" + (configuration != null ? configuration.name : "MISSING")
                + "; listener volume=" + AudioListener.volume + "; listener paused=" + AudioListener.pause
                + "; timeScale=" + Time.timeScale + "; silenced=" + silenced, this);
            DiagnoseSource(sourceA, "A"); DiagnoseSource(sourceB, "B");
        }
        void DiagnoseSource(AudioSource source, string label)
        {
            if (source == null) { Debug.LogError("[Day BGM] Source " + label + " is MISSING.", this); return; }
            Debug.Log("[Day BGM] Source " + label + ": enabled=" + source.isActiveAndEnabled
                + "; clip=" + (source.clip != null ? source.clip.name : "none")
                + "; playing=" + source.isPlaying + "; mute=" + source.mute
                + "; volume=" + source.volume + "; spatialBlend=" + source.spatialBlend, this);
        }
        void SetPaused(AudioSource source, bool value)
        { if (source == null) return; if (value) source.Pause(); else source.UnPause(); }
        void OnDisable()
        {
            if (sourceA != null) sourceA.Stop();
            if (sourceB != null) sourceB.Stop();
            incoming = outgoing = null; incomingGain = outgoingGain = 0f;
            transitioning = paused = false; silenced = true; currentDay = -1;
        }
        [ContextMenu("Preview assigned day (Play Mode only)")]
        void Preview()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) PlayDay(previewDay);
#endif
        }
    }
}
