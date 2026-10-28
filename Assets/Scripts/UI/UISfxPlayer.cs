using UnityEngine;

namespace FirstJobber
{
    /// <summary>One scene-owned UI sound voice. Never touches music sources or audio preferences.</summary>
    public class UISfxPlayer : MonoBehaviour
    {
        public static UISfxPlayer Instance { get; private set; }
        [SerializeField] UISfxSettings settings;
        [SerializeField] AudioSource source;
        readonly AudioClip[] lastClip = new AudioClip[3];
        readonly int[] lastFrame = { -1, -1, -1 };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void LateUpdate()
        {
            if (source == null || settings == null) return;
            float volume = Mathf.Clamp01(settings.masterVolume);
            if (source.volume != volume) source.volume = volume;
            if (source.outputAudioMixerGroup != settings.outputMixerGroup) source.outputAudioMixerGroup = settings.outputMixerGroup;
        }
        public void Play(UISfxKind kind)
        {
            if (source == null || settings == null || !isActiveAndEnabled) return;
            int index = (int)kind;
            if (lastFrame[index] == Time.frameCount) return;
            var group = settings.Group(kind);
            var clip = settings.Choose(group, lastClip[index]);
            if (clip == null) return;
            lastFrame[index] = Time.frameCount; lastClip[index] = clip;
            source.volume = Mathf.Clamp01(settings.masterVolume);
            source.outputAudioMixerGroup = settings.outputMixerGroup;
            source.PlayOneShot(clip, Mathf.Clamp01(group.volume));
        }
    }
}
