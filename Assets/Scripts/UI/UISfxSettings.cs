using System;
using UnityEngine;
using UnityEngine.Audio;

namespace FirstJobber
{
    public enum UISfxKind { Click, Transition, FinishTask }
    [CreateAssetMenu(menuName = "First Jobber/UI/SFX Settings")]
    public class UISfxSettings : ScriptableObject
    {
        // Cosmetic randomness must not consume Unity's gameplay RNG (questions/choices/events).
        readonly System.Random random = new System.Random();
        [Range(0, 1)] public float masterVolume = .8f;
        public AudioMixerGroup outputMixerGroup;
        public bool randomVariation = true;
        public bool avoidImmediateRepetition = true;
        public SoundGroup click = new SoundGroup();
        public SoundGroup transition = new SoundGroup();
        public SoundGroup finishTask = new SoundGroup();
        [Serializable] public class SoundGroup
        {
            public AudioClip[] clips = new AudioClip[2];
            [Range(0, 1)] public float volume = .65f;
        }
        public SoundGroup Group(UISfxKind kind) => kind == UISfxKind.Click ? click : kind == UISfxKind.Transition ? transition : finishTask;
        // One shared selection policy; skips missing clips without allocating a temporary list.
        public AudioClip Choose(SoundGroup group, AudioClip previous)
        {
            if (group == null || group.clips == null) return null;
            int valid = 0, alternatives = 0;
            for (int i = 0; i < group.clips.Length; i++)
                if (group.clips[i] != null) { valid++; if (group.clips[i] != previous) alternatives++; }
            if (valid == 0) return null;
            bool excludePrevious = randomVariation && avoidImmediateRepetition && alternatives > 0;
            int index = randomVariation ? random.Next(excludePrevious ? alternatives : valid) : 0;
            for (int i = 0; i < group.clips.Length; i++)
                if (group.clips[i] != null && (!excludePrevious || group.clips[i] != previous) && index-- == 0) return group.clips[i];
            return null;
        }
    }
}
