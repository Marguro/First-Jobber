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
        DailyEventData choiceEvent;
        DailyEventSegment choiceSegment;
        Action<string> choiceSelected;
        readonly List<ChoiceItem> choiceItems = new List<ChoiceItem>();
        public bool IsWaitingForChoice => choiceSegment != null;

        void OnEnable() { LocalizationManager.LanguageChanged += RefreshChoiceLanguage; }
        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= RefreshChoiceLanguage;
            CloseChoice();
            ClearPanel();
        }

        void CloseChoice()
        {
            choiceGeneration++;
            if (choiceList != null)
            {
                if (choiceSelected != null) choiceList.ItemChosen -= choiceSelected;
                choiceList.HidePopup();
            }
            choiceSelected = null;
            choiceEvent = null;
            choiceSegment = null;
        }

        void RefreshChoiceLanguage()
        {
            if (!IsWaitingForChoice) return;
            choiceItems.Clear();
            foreach (var option in choiceSegment.options) choiceItems.Add(new ChoiceItem {
                id = option.id,
                title = LocalizationManager.Get(choiceEvent, "segments/" + choiceSegment.id + "/options/" + option.id + "/label", option.label),
                state = ChoiceState.Available });
            choiceList.SetItems(choiceItems);
        }

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
            if (IsWaitingForChoice) throw new InvalidOperationException("Resolve the open Daily Event choice before starting another event sequence.");
            var queue = new List<DailyEventData>();
            Collect(trigger, session, queue);
            if (session.day == 1 && trigger == EventTrigger.BeforeResume)
            {
                if (database.finalStoryDay > 0 && queue.Count == 0 && !session.triggeredEvents.Contains("event_day01_opening"))
                    Debug.LogError("[DailyEvent] Production Day 1 opening event is missing or ineligible.", this);
                else Debug.Log("[DailyEvent] Day 1 BeforeResume events found: " + queue.Count, this);
            }
            RunFrom(queue, 0, session, onDone);
        }

        void RunFrom(List<DailyEventData> queue, int index, GameSession session, Action onDone)
        {
            if (index >= queue.Count) { onDone?.Invoke(); return; }

            var ev = queue[index];
            Debug.Log("[DailyEvent] Dialogue started: " + ev.id, this);
            if (!session.triggeredEvents.Contains(ev.id)) session.triggeredEvents.Add(ev.id);
            if (ev.setFlags != null)
                for (int i = 0; i < ev.setFlags.Length; i++) session.SetFlag(ev.setFlags[i]);

            // Background is event data, not restricted to the OnDayStart trigger. Present before dialogue/choices.
            if (eventPanel != null) eventPanel.Show(LocalizationManager.Get(ev, "caption", ev.caption), ev.background,
                ev.trigger == EventTrigger.OnDayStart, ev);
            if (ev.flickerSeconds > 0f && AtmosphereManager.Instance != null) AtmosphereManager.Instance.PulseFlicker(ev.flickerSeconds);
            if (ev.segments != null && ev.segments.Length > 0)
                RunSegment(ev, 0, session, () => CompleteEvent(queue, index, session, onDone));
            else dialogue.Play(ev.lines, () => CompleteEvent(queue, index, session, onDone));
        }

        void CompleteEvent(List<DailyEventData> queue, int index, GameSession session, Action done)
        {
            Debug.Log("[DailyEvent] Dialogue completed: " + queue[index].id, this);
            session.Save();
            ClearPanel(); // Complete before the next event/callback can install its own background.
            RunFrom(queue, index + 1, session, done);
        }

        void RunSegment(DailyEventData ev, int index, GameSession session, Action done)
        {
            if (index >= ev.segments.Length) { done?.Invoke(); return; }
            var segment = ev.segments[index];
            if (segment.options != null && segment.options.Length > 0)
            {
                if (choiceList == null) throw new InvalidOperationException("DailyEventManager.choiceList is not assigned.");
                Debug.Log("[DailyEvent] Choice node reached: " + ev.id + "/" + segment.id + " (" + segment.options.Length + " options).", this);
                // Completion callbacks run before DialogueManager hides its previous box. Hide it now,
                // so neither its click area nor keyboard advance can compete with the choice buttons.
                dialogue.Stop();
                choiceEvent = ev;
                choiceSegment = segment;
                RefreshChoiceLanguage();
                int generation = ++choiceGeneration;
                choiceSelected = id => {
                    if (generation != choiceGeneration || !choiceItems.Exists(item => item.id == id)) return;
                    CloseChoice();
                    session.SetFlag("event_choice/" + ev.id + "/" + segment.id + "/" + id);
                    Debug.Log("[DailyEvent] Selected " + ev.id + "/" + segment.id + "/" + id + "; resuming dialogue.", this);
                    RunSegment(ev, index + 1, session, done);
                };
                choiceList.ItemChosen += choiceSelected;
                choiceList.ShowPopup(choiceItems);
                Debug.Log("[DailyEvent] Choice popup activated: " + choiceList.name + ", activeInHierarchy=" + choiceList.gameObject.activeInHierarchy + ", options=" + choiceItems.Count, this);
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

#if UNITY_EDITOR
        // Preview uses the real segment player and UI, but never calls CompleteEvent / Save.
        public void EditorPreviewEvent(DailyEventData ev, GameSession previewSession, Action done)
        {
            if (!Application.isPlaying || IsWaitingForChoice || ev == null) return;
            LocalizationKeys.VisitAsset(ev, null);
            if (eventPanel != null) eventPanel.Show(LocalizationManager.Get(ev, "caption", ev.caption), ev.background,
                ev.trigger == EventTrigger.OnDayStart, ev);
            RunSegment(ev, 0, previewSession, () => { ClearPanel(); done?.Invoke(); });
        }
#endif
    }
}
