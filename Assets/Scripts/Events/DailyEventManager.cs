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
        [SerializeField] ChoiceListUI choiceList;
        int choiceGeneration;

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
            if (ev.segments != null && ev.segments.Length > 0)
                RunSegment(ev, 0, session, () => CompleteEvent(queue, index, session, onDone));
            else dialogue.Play(ev.lines, () => CompleteEvent(queue, index, session, onDone));
        }

        void CompleteEvent(List<DailyEventData> queue, int index, GameSession session, Action done)
        {
            session.Save();
            RunFrom(queue, index + 1, session, done);
        }

        void RunSegment(DailyEventData ev, int index, GameSession session, Action done)
        {
            if (index >= ev.segments.Length) { done?.Invoke(); return; }
            var segment = ev.segments[index];
            if (segment.options != null && segment.options.Length > 0)
            {
                if (choiceList == null) throw new InvalidOperationException("DailyEventManager.choiceList is not assigned.");
                var items = new List<ChoiceItem>();
                foreach (var option in segment.options) items.Add(new ChoiceItem {
                    id = option.id, title = LocalizationManager.Get(ev, "segments/" + segment.id + "/options/" + option.id + "/label", option.label),
                    state = ChoiceState.Available });
                int generation = ++choiceGeneration;
                Action<string> selected = null;
                selected = id => {
                    if (generation != choiceGeneration || !items.Exists(item => item.id == id)) return;
                    choiceGeneration++;
                    choiceList.ItemChosen -= selected;
                    choiceList.SetInteractable(false);
                    choiceList.gameObject.SetActive(false);
                    session.SetFlag("event_choice/" + ev.id + "/" + segment.id + "/" + id);
                    RunSegment(ev, index + 1, session, done);
                };
                choiceList.ItemChosen += selected;
                choiceList.gameObject.SetActive(true);
                choiceList.SetItems(items, resetScroll: true);
                choiceList.SetInteractable(true);
                return;
            }
            dialogue.Play(segment.lines, () => RunSegment(ev, index + 1, session, done));
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
