using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace FirstJobber
{
    public enum MissingBgmBehavior { Silence, KeepPrevious }

    [CreateAssetMenu(menuName = "First Jobber/Day BGM Configuration")]
    public class DayBgmConfiguration : ScriptableObject
    {
        [Range(0, 1)] public float masterBgmVolume = 1f;
        [Min(0)] public float defaultFadeDuration = 1.5f;
        public bool enableCrossfade = true;
        public MissingBgmBehavior missingClipBehavior;
        [Tooltip("Continue during Time.timeScale == 0 and AudioListener.pause.")]
        public bool playDuringPause;
        [Tooltip("Optional existing mixer group; no saved mixer preferences are changed.")]
        public AudioMixerGroup outputMixerGroup;
        public List<DayMusic> days = new List<DayMusic>();

        [Serializable]
        public class DayMusic
        {
            [Min(1)] public int day = 1;
            [Tooltip("Disabled entries intentionally silence music, even with Keep Previous.")]
            public bool enabled = true;
            public AudioClip audioClip;
            [Range(0, 1)] public float volumeMultiplier = 1f;
            public bool loop = true;
        }

        public DayMusic Resolve(int day)
        {
            for (int i = 0; i < days.Count; i++)
                if (days[i] != null && days[i].day == day) return days[i];
            return null;
        }

        void OnValidate()
        {
            var seen = new HashSet<int>();
            foreach (var entry in days)
                if (entry != null && !seen.Add(entry.day))
                    Debug.LogWarning("[Day BGM] Duplicate day " + entry.day + "; first entry wins.", this);
        }
    }
}
