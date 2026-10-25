using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Finds and plays data-driven DailyEventData for a trigger point. Adding an event = adding an asset
    /// to GameDatabase.events; no flow code changes.
    /// </summary>
    public class DailyEventManager : MonoBehaviour
    {
        [SerializeField] GameDatabase database;
        [SerializeField] DialogueManager dialogue;
        [SerializeField] DailyEventPanelUI eventPanel;

        readonly List<DailyEventData> scratch = new List<DailyEventData>();

        public bool HasEligible(EventTrigger trigger, GameSession session)
        {
            Collect(trigger, session, scratch);
            return scratch.Count > 0;
        }

        public void ClearPanel() { if (eventPanel != null) eventPanel.Clear(); }

        /// <summary>Plays every eligible event (highest priority first), then calls onDone.</summary>
        public void Run(EventTrigger trigger, GameSession session, Action onDone)
        {
            var queue = new List<DailyEventData>();
            Collect(trigger, session, queue);
            RunFrom(queue, 0, session, onDone);
        }

        void RunFrom(List<DailyEventData> queue, int index, GameSession session, Action onDone)
        {
            if (index >= queue.Count) { onDone?.Invoke(); return; }

            var ev = queue[index];
            if (!session.triggeredEvents.Contains(ev.id)) session.triggeredEvents.Add(ev.id);
            if (ev.setFlags != null)
                for (int i = 0; i < ev.setFlags.Length; i++) session.SetFlag(ev.setFlags[i]);

            if (ev.trigger == EventTrigger.OnDayStart && eventPanel != null) eventPanel.Show(LocalizationManager.Get(ev, "caption", ev.caption), ev.background);
            if (ev.flickerSeconds > 0f && AtmosphereManager.Instance != null) AtmosphereManager.Instance.PulseFlicker(ev.flickerSeconds);
            dialogue.Play(ev.lines, () => RunFrom(queue, index + 1, session, onDone));
        }

        void Collect(EventTrigger trigger, GameSession session, List<DailyEventData> result)
        {
            result.Clear();
            var list = database.events;
            for (int i = 0; i < list.Count; i++)
            {
                var ev = list[i];
                if (ev == null || ev.trigger != trigger) continue;
                if (ev.exactDay > 0 && session.day != ev.exactDay) continue;
                if (ev.minDay > 0 && session.day < ev.minDay) continue;
                if (!ev.repeatable && session.triggeredEvents.Contains(ev.id)) continue;
                if (!string.IsNullOrEmpty(ev.requiredFlag) && !session.HasFlag(ev.requiredFlag)) continue;
                if (!string.IsNullOrEmpty(ev.forbiddenFlag) && session.HasFlag(ev.forbiddenFlag)) continue;
                result.Add(ev);
            }
            // Stable insertion sort by priority (descending) keeps database order for equal priority.
            for (int i = 1; i < result.Count; i++)
            {
                var key = result[i];
                int j = i - 1;
                while (j >= 0 && result[j].priority < key.priority) { result[j + 1] = result[j]; j--; }
                result[j + 1] = key;
            }
        }
    }
}
