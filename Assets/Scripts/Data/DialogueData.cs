using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
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

        public DialogueLine() { }

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

        /// <summary>Number of pages in this block (legacy text counts as one).</summary>
        public int PageCount => HasLines ? dialogueLines.Count : 1;

        public string GetPage(int index) => HasLines ? dialogueLines[index] : text;

        public DialogueLine(string speaker, string text, HREmotion emotion = HREmotion.Neutral, float autoAdvance = 0f)
        {
            this.speaker = speaker;
            this.text = text;
            this.emotion = emotion;
            autoAdvanceDelay = autoAdvance;
        }
    }
}
