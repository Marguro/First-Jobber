using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>
    /// Plays one dialogue sequence at a time. Starting a new sequence (or calling Stop) cancels the
    /// previous one WITHOUT running its completion callback, so stale callbacks never fire.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] DialogueUI ui;
        [SerializeField, Min(1)] float charactersPerSecond = 55f;
        [Tooltip("A click is ignored for this long after the text finishes, to stop accidental skipping.")]
        [SerializeField] float minAdvanceDelay = 0.2f;

        /// <summary>Resolves {token} placeholders in text (name, age, day, company, job...). Set by GameFlowManager.</summary>
        public Func<string, string> TokenResolver;
        /// <summary>Name shown for speaker "Player".</summary>
        public Func<string> PlayerNameProvider;

        public event Action<DialogueLine> LineShown;
        public event Action<string> LineEvent;

        public bool IsPlaying { get; private set; }

        Coroutine routine;
        int runId;
        bool clicked;
        readonly StringBuilder sb = new StringBuilder();

        void OnEnable() { if (ui != null) ui.Clicked += OnClicked; }
        void OnDisable() { if (ui != null) ui.Clicked -= OnClicked; }

        void OnClicked() { clicked = true; }

        public void Play(IList<DialogueLine> lines, Action onComplete = null, float defaultAutoAdvance = 0f)
        {
            Cancel();
            if (lines == null || lines.Count == 0)
            {
                if (ui != null) ui.Hide();
                onComplete?.Invoke();
                return;
            }
            int id = ++runId;
            routine = StartCoroutine(Run(lines, onComplete, defaultAutoAdvance, id));
        }

        /// <summary>Coroutine helper: yield return dialogue.PlayAndWait(lines). Empty lines finish immediately.</summary>
        public IEnumerator PlayAndWait(IList<DialogueLine> lines, float defaultAutoAdvance = 0f)
        {
            if (lines == null || lines.Count == 0) yield break;
            bool done = false;
            Play(lines, () => done = true, defaultAutoAdvance);
            while (!done) yield return null;
        }

        /// <summary>Cancels the current sequence (no callback) and hides the box.</summary>
        public void Stop()
        {
            Cancel();
            if (ui != null) ui.Hide();
        }

        void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            runId++;
            IsPlaying = false;
        }

        IEnumerator Run(IList<DialogueLine> lines, Action onComplete, float defaultAuto, int id)
        {
            IsPlaying = true;
            if (ui != null) ui.Show();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null) continue;

                if (line.delayBefore > 0f) yield return new WaitForSecondsRealtime(line.delayBefore);

                // Block-level: raised once per block, not once per page.
                LineShown?.Invoke(line);
                if (!string.IsNullOrEmpty(line.eventId)) LineEvent?.Invoke(line.eventId);

                for (int p = 0; p < line.PageCount; p++)
                {
                    string text = Format(line.GetPage(p));
                    if (string.IsNullOrEmpty(text)) continue;

                    int total = ui != null ? ui.SetLine(ResolveSpeaker(line.speaker), text, line.portrait) : 0;
                    if (ui != null) ui.SetContinueIndicator(false);

                    // Typewriter. A click while typing only completes the text.
                    clicked = false;
                    float shown = 0f;
                    while (shown < total && !clicked)
                    {
                        shown += charactersPerSecond * Time.unscaledDeltaTime;
                        if (ui != null) ui.SetVisibleCharacters(Mathf.Min(total, Mathf.FloorToInt(shown)));
                        yield return null;
                    }
                    if (ui != null) ui.SetVisibleCharacters(total);

                    float auto = line.autoAdvanceDelay > 0f ? line.autoAdvanceDelay : defaultAuto;
                    if (ui != null) ui.SetContinueIndicator(auto <= 0f);

                    clicked = false;
                    float waited = 0f;
                    while (true)
                    {
                        waited += Time.unscaledDeltaTime;
                        if (auto > 0f && waited >= auto) break;
                        if (clicked)
                        {
                            clicked = false;
                            if (waited >= minAdvanceDelay) break;
                        }
                        yield return null;
                    }
                }
            }

            IsPlaying = false;
            routine = null;
            // The callback may start a new sequence (runId changes) - then do not hide the box.
            onComplete?.Invoke();
            if (id == runId && ui != null) ui.Hide();
        }

        string ResolveSpeaker(string speaker)
        {
            if (speaker == Speakers.Player && PlayerNameProvider != null)
            {
                string n = PlayerNameProvider();
                if (!string.IsNullOrEmpty(n)) return n;
            }
            return Format(speaker);
        }

        public string Format(string text)
        {
            if (string.IsNullOrEmpty(text) || TokenResolver == null || text.IndexOf('{') < 0) return text;
            sb.Clear();
            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf('{', i);
                if (open < 0) break;
                int close = text.IndexOf('}', open + 1);
                if (close < 0) break;
                sb.Append(text, i, open - i);
                string value = TokenResolver(text.Substring(open + 1, close - open - 1));
                if (value == null) sb.Append(text, open, close - open + 1);
                else sb.Append(value);
                i = close + 1;
            }
            sb.Append(text, i, text.Length - i);
            return sb.ToString();
        }
    }
}
