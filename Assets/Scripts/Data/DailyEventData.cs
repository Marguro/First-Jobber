using UnityEngine;

namespace FirstJobber
{
    [System.Serializable]
    public class DailyEventOption
    {
        public string id;
        public string label;
    }

    [System.Serializable]
    public class DailyEventSegment
    {
        public string id;
        public DialogueLine[] lines;
        public DailyEventOption[] options;
    }

    [CreateAssetMenu(menuName = "First Jobber/Daily Event", fileName = "Event_New")]
    public class DailyEventData : ScriptableObject
    {
        public string id;
        public EventTrigger trigger = EventTrigger.OnDayStart;
        [Tooltip("0 = ignore. Otherwise only on exactly this day.")]
        public int exactDay;
        [Tooltip("0 = ignore. Otherwise on this day or later.")]
        public int minDay;
        public bool repeatable;
        [Tooltip("Higher runs first when several events are eligible.")]
        public int priority;

        [Header("Conditions (flags)")]
        public string requiredFlag;
        public string forbiddenFlag;

        [Header("Effect")]
        [Tooltip("Flags set when the event runs. Built-in flags: resume_distorted, hr_strange.")]
        public string[] setFlags;

        [Header("Presentation (caption/background are used for OnDayStart events)")]
        public string caption;
        public Sprite background;
        [Tooltip("Optional temporary screen flicker (seconds). Does not change the atmosphere phase.")]
        public float flickerSeconds;
        public DialogueLine[] lines;
        public DailyEventSegment[] segments;
    }
}
