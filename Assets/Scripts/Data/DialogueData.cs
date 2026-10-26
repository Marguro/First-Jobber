using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    /// <summary>How a collection of dialogue blocks is played.</summary>
    public enum DialoguePlaybackMode
    {
        /// <summary>Play every block in order.</summary>
        Sequential,
        /// <summary>Pick ONE block at random each time the collection is triggered and play only it.</summary>
        RandomOne
    }

    public enum HREmotion { Neutral, Interested, Disappointed, Uncomfortable }

    public static class Speakers
    {
        public const string Player = "Player";
        public const string HR = "HR";
    }

    /// <summary>
    /// One dialogue BLOCK: a speaker (with emotion/portrait/timing) saying one or more pages in a row.
    /// Used everywhere (choices, HR, events, endings). Old assets only have <see cref="text"/>; it is used as a
    /// single page whenever <see cref="dialogueLines"/> is empty, so nothing authored is lost.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Player / HR / Father / Narrator or any name. 'Player' shows the applicant's name.")]
        public string speaker = Speakers.Player;
        [Tooltip("Legacy single-page text. Ignored as soon as 'Dialogue Lines' has content.")]
        [TextArea(2, 5)] public string text;
        [Tooltip("Pages shown one after another by the same speaker. Each page waits for its own click.")]
        public List<string> dialogueLines = new List<string>();
        [Tooltip("Only used when speaker is HR: changes the HR expression.")]
        public HREmotion emotion = HREmotion.Neutral;
        [Tooltip("> 0 : advance automatically after this many seconds. 0 : wait for a click.")]
        public float autoAdvanceDelay;
        [Tooltip("Pause before the line appears (seconds).")]
        public float delayBefore;
        [Tooltip("Optional id raised through DialogueManager.LineEvent.")]
        public string eventId;
        public Sprite portrait;

        [Header("Gender (optional)")]
        [Tooltip("Empty = this block is for every gender. Otherwise only used for these Gender IDs (e.g. male, female, non_binary).")]
        public string[] genderIds;
        [Tooltip("Blocks that share a group are alternatives for ONE spot: the block whose Gender IDs match is played; a block with no Gender IDs is the fallback. Leave empty for ordinary blocks.")]
        public string variantGroup;

        /// <summary>Runtime only: localization key, set by LocalizationKeys.Visit. Empty = always show the English text.</summary>
        [NonSerialized] public string locKey;

        public DialogueLine() { }

        public bool IsGenderSpecific => genderIds != null && genderIds.Length > 0;

        /// <summary>True when this block may be shown to the given gender (a block with no Gender IDs matches everyone).</summary>
        public bool MatchesGender(string genderId)
        {
            if (!IsGenderSpecific) return true;
            if (string.IsNullOrEmpty(genderId)) return false;
            for (int i = 0; i < genderIds.Length; i++) if (genderIds[i] == genderId) return true;
            return false;
        }

        bool HasLines
        {
            get
            {
                if (dialogueLines == null) return false;
                for (int i = 0; i < dialogueLines.Count; i++)
                    if (!string.IsNullOrWhiteSpace(dialogueLines[i])) return true;
                return false;
            }
        }

        /// <summary>True if this block would show at least one non-empty page.</summary>
        public bool HasContent => HasLines || !string.IsNullOrWhiteSpace(text);

        /// <summary>Number of pages in this block (legacy text counts as one).</summary>
        public int PageCount => HasLines ? dialogueLines.Count : 1;

        public string GetPage(int index) => HasLines ? dialogueLines[index] : text;

        /// <summary>
        /// Localization key of one page. A legacy single-text block keeps its original key (locKey), so existing
        /// translations still apply; a multi-page block gets one key per page (".../lines/&lt;page&gt;").
        /// Null when no key was stamped (then the English text is shown).
        /// </summary>
        public string GetPageKey(int index)
        {
            if (string.IsNullOrEmpty(locKey)) return null;
            if (!HasLines) return locKey;
            const string suffix = "/text";
            string root = locKey.EndsWith(suffix, StringComparison.Ordinal) ? locKey.Substring(0, locKey.Length - suffix.Length) : locKey;
            return root + "/lines/" + index;
        }

        public DialogueLine(string speaker, string text, HREmotion emotion = HREmotion.Neutral, float autoAdvance = 0f)
        {
            this.speaker = speaker;
            this.text = text;
            this.emotion = emotion;
            autoAdvanceDelay = autoAdvance;
        }
    }
}
