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
        /// <summary>Gender id of the applicant. Blocks with Gender IDs are filtered against it.</summary>
        public Func<string> GenderIdProvider;
        /// <summary>Name shown for speaker "HR" (the HR of the selected company). Null/empty keeps "HR".</summary>
        public Func<string> HrNameProvider;
        /// <summary>CompanyData.hr for the active application. HR lines never use their legacy portrait override.</summary>
        public Func<HRCharacterData> HrCharacterProvider;
        readonly HashSet<HRCharacterData> warnedMissingHrPortraits = new HashSet<HRCharacterData>();
        bool warnedMissingHrContext;

        public Sprite ResolvePortrait(DialogueLine line)
        {
            if (line == null) return null;
            if (line.speaker != Speakers.HR) return line.portrait;
            var character = HrCharacterProvider?.Invoke();
            if (character == null)
            {
                if (!warnedMissingHrContext)
                {
                    warnedMissingHrContext = true;
                    Debug.LogWarning("[Dialogue] HR line has no active CompanyData.hr. Showing no portrait; legacy line portraits are ignored.", this);
                }
                return null;
            }
            var phase = AtmosphereManager.Instance != null ? AtmosphereManager.Instance.AppliedPhase : null;
            var sprite = character.ResolveSprite(line.emotion, phase, out _);
            if (sprite == null && warnedMissingHrPortraits.Add(character))
                Debug.LogWarning("[Dialogue] HR '" + character.DisplayName + "' has no usable portrait. Assign Emotion Visuals / Default Portrait in HRCharacterData.", character);
            return sprite;
        }

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

        /// <summary>
        /// Plays a collection. Sequential plays every block; RandomOne picks one valid block (once, here, before
        /// playback starts - so advancing lines or clicking never rerolls) and plays only that block.
        /// </summary>
        public void Play(IList<DialogueLine> lines, Action onComplete = null, float defaultAutoAdvance = 0f,
            DialoguePlaybackMode mode = DialoguePlaybackMode.Sequential)
        {
            Cancel();
            lines = ResolveGender(lines);                       // 1. gender filter / variant groups
            if (mode == DialoguePlaybackMode.RandomOne) lines = PickOne(lines); // 2. then pick one of what is left
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
        public IEnumerator PlayAndWait(IList<DialogueLine> lines, float defaultAutoAdvance = 0f,
            DialoguePlaybackMode mode = DialoguePlaybackMode.Sequential)
        {
            if (lines == null || lines.Count == 0) yield break;
            bool done = false;
            Play(lines, () => done = true, defaultAutoAdvance, mode);
            while (!done) yield return null;
        }

        /// <summary>
        /// Applies gender conditions. Blocks without Gender IDs always stay. Blocks sharing a variantGroup are
        /// alternatives: matching specific blocks win, otherwise the group's no-gender blocks are the fallback.
        /// Returns the original list untouched when nothing uses gender data.
        /// </summary>
        public IList<DialogueLine> ResolveGender(IList<DialogueLine> lines)
        {
            if (lines == null) return null;
            bool needed = false;
            for (int i = 0; i < lines.Count && !needed; i++)
                needed = lines[i] != null && (lines[i].IsGenderSpecific || !string.IsNullOrEmpty(lines[i].variantGroup));
            if (!needed) return lines;

            string gender = GenderIdProvider != null ? GenderIdProvider() : null;
            var result = new List<DialogueLine>(lines.Count);
            var doneGroups = new HashSet<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                if (l == null) continue;
                if (string.IsNullOrEmpty(l.variantGroup))
                {
                    if (l.MatchesGender(gender)) result.Add(l);
                    continue;
                }
                if (!doneGroups.Add(l.variantGroup)) continue;

                int before = result.Count;
                for (int j = i; j < lines.Count; j++)
                    if (lines[j] != null && lines[j].variantGroup == l.variantGroup && lines[j].IsGenderSpecific && lines[j].MatchesGender(gender))
                        result.Add(lines[j]);
                if (result.Count != before) continue;
                for (int j = i; j < lines.Count; j++)
                    if (lines[j] != null && lines[j].variantGroup == l.variantGroup && !lines[j].IsGenderSpecific)
                        result.Add(lines[j]);
            }
            return result;
        }

        /// <summary>First page of the first block that applies to the applicant, with tokens resolved (for on-screen question text).</summary>
        public string FirstText(IList<DialogueLine> lines)
        {
            var resolved = ResolveGender(lines);
            if (resolved == null) return null;
            for (int i = 0; i < resolved.Count; i++)
            {
                if (resolved[i] == null) continue;
                for (int p = 0; p < resolved[i].PageCount; p++)
                {
                    string t = resolved[i].GetPage(p);
                    if (!string.IsNullOrWhiteSpace(t)) return Format(LocalizationManager.Get(resolved[i].GetPageKey(p), t));
                }
            }
            return null;
        }

        // Equal chance for every non-null block that has something to show. Returns null when there is none.
        static readonly List<DialogueLine> validBuffer = new List<DialogueLine>();
        static IList<DialogueLine> PickOne(IList<DialogueLine> lines)
        {
            if (lines == null) return null;
            validBuffer.Clear();
            for (int i = 0; i < lines.Count; i++)
                if (lines[i] != null && lines[i].HasContent) validBuffer.Add(lines[i]);
            if (validBuffer.Count == 0) return null;
            var picked = validBuffer[UnityEngine.Random.Range(0, validBuffer.Count)];
            validBuffer.Clear();
            return new[] { picked };
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
                    string text = Format(LocalizationManager.Get(line.GetPageKey(p), line.GetPage(p)));
                    if (string.IsNullOrEmpty(text)) continue;

                    int total = ui != null ? ui.SetLine(ResolveSpeaker(line.speaker), text, ResolvePortrait(line), line.GetPageKey(p)) : 0;
                    if (ui != null) ui.SetContinueIndicator(false);

                    // Typewriter. A click while typing only completes the text.
                    clicked = false;
                    float shown = 0f;
                    while (!clicked)
                    {
                        // TMP may reparse after a late font/preprocessor refresh. Read the current
                        // parsed count without forcing a second mesh rebuild on every reveal frame.
                        if (ui != null) total = ui.RevealCharacterCount;
                        if (shown >= total) break;
                        shown += charactersPerSecond * Time.unscaledDeltaTime;
                        if (ui != null) ui.SetVisibleCharacters(Mathf.Min(total, Mathf.FloorToInt(shown)));
                        yield return null;
                    }
                    // Completed/skip state has no character cap: later Thai preprocessing must
                    // never hide the tail of a fully revealed page. The next SetLine resets to zero.
                    if (ui != null) ui.SetVisibleCharacters(int.MaxValue);

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
            if (speaker == Speakers.HR && HrNameProvider != null)
            {
                string n = HrNameProvider();
                if (!string.IsNullOrEmpty(n)) return n;
            }
            return Format(LocalizationManager.Get(LocalizationKeys.SpeakerKey(speaker), speaker));
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
