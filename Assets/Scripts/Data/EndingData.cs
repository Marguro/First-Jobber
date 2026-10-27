using UnityEngine;
using UnityEngine.Timeline;

namespace FirstJobber
{
    public enum EndingTimelineMode
    {
        [Tooltip("Ignore the timeline.")] None,
        [Tooltip("Timeline plays first; dialogue starts when it ends.")] BeforeDialogue,
        [Tooltip("Timeline and dialogue run together; the ending finishes when both are done.")] WithDialogue,
        [Tooltip("Only the timeline plays; the ending's dialogue lines are skipped.")] InsteadOfDialogue
    }

    [CreateAssetMenu(menuName = "First Jobber/Ending", fileName = "Ending_New")]
    public class EndingData : ScriptableObject
    {
        public string id;
        public string title;

        [Header("Final decision button")]
        public string optionLabel;
        public DialogueLine[] previewLines;
        public bool resetCycle;
        [TextArea] public string optionDescription;

        [Header("Ending scene")]
        public DialogueLine[] lines;
        public Sprite background;
        public Color backgroundColor = Color.black;
        [TextArea] public string finalText;
        public AudioClip sound;

        [Header("Timeline (optional)")]
        [Tooltip("EASIEST: a prefab that contains its own PlayableDirector, its timeline and the objects it animates (bind the tracks once, inside the prefab). Spawned under the ending panel when this ending plays. When set, 'Timeline' below is not needed.")]
        public GameObject cutscenePrefab;
        [Tooltip("Advanced: a bare Timeline asset. Tracks are bound by NAME using the EndingController's Timeline Bindings list. Ignored when a Cutscene Prefab is set.")]
        public TimelineAsset timeline;
        public EndingTimelineMode timelineMode = EndingTimelineMode.BeforeDialogue;
        [Tooltip("Space / Enter / click skips the timeline (only when it plays alone, i.e. not With Dialogue).")]
        public bool timelineSkippable = true;
    }
}
