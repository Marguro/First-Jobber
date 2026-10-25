using System;
using UnityEngine;

namespace FirstJobber
{
    public enum HREmotion { Neutral, Interested, Disappointed, Uncomfortable }

    public static class Speakers
    {
        public const string Player = "Player";
        public const string HR = "HR";
    }

    /// <summary>One line of dialogue. Used everywhere (choices, HR, events, endings).</summary>
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Player / HR / Father / Narrator or any name. 'Player' shows the applicant's name.")]
        public string speaker = Speakers.Player;
        [TextArea(2, 5)] public string text;
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

        public DialogueLine(string speaker, string text, HREmotion emotion = HREmotion.Neutral, float autoAdvance = 0f)
        {
            this.speaker = speaker;
            this.text = text;
            this.emotion = emotion;
            autoAdvanceDelay = autoAdvance;
        }
    }
}
