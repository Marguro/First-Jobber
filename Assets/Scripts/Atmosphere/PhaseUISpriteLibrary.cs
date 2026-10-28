using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    public enum PhaseUIFamily { Individual, AnswerBackground, CompanyBackground }
    [CreateAssetMenu(menuName="First Jobber/Atmosphere UI Sprites", fileName="PhaseUISprites")]
    public class PhaseUISpriteLibrary : ScriptableObject
    {
        public AtmospherePhaseData phase2;
        public AtmospherePhaseData phase3;
        public Sprite answerPhase3;
        public Sprite companyPhase3;
        [Serializable] public class Entry
        {
            public Sprite original;
            public Sprite phase2Sprite;
            public Sprite phase3Sprite;
            public PhaseUIFamily family;
        }
        public List<Entry> entries = new List<Entry>();
        Dictionary<Sprite, Entry> lookup;
        void OnEnable() { lookup=null; }
        void OnValidate() { lookup=null; }
        public bool TryResolve(Sprite original, AtmospherePhaseData phase, out Sprite dedicated)
        {
            dedicated=null;
            if (original == null) return false;
            if (lookup == null)
            {
                lookup=new Dictionary<Sprite, Entry>();
                foreach (var e in entries) if (e != null && e.original != null && !lookup.ContainsKey(e.original)) lookup.Add(e.original,e);
            }
            if (!lookup.TryGetValue(original,out var entry)) return false;
            dedicated=phase != null && phase==phase2 ? entry.phase2Sprite : phase != null && phase==phase3
                ? entry.family==PhaseUIFamily.AnswerBackground ? answerPhase3 : entry.family==PhaseUIFamily.CompanyBackground ? companyPhase3 : entry.phase3Sprite : null;
            return true; // A missing current-phase variant intentionally restores THIS original, never the previous phase.
        }
    }
}
