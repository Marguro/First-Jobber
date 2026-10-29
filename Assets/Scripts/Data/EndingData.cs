using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.Video;

namespace FirstJobber
{
    public enum EndingPresentationMode { None, Video, Timeline }
    public enum EndingAfterCutsceneAction { CompleteEnding, RestartDay1 }
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
        [HideInInspector] public bool resetCycle; // Preserved for legacy authored assets/imports.
        [TextArea] public string optionDescription;

        [Header("Ending scene")]
        public DialogueLine[] lines;
        public Sprite background;
        public Color backgroundColor = Color.black;
        [TextArea] public string finalText;
        public AudioClip sound;

        [Header("Presentation after confirmation")]
        public EndingPresentationMode presentationMode = EndingPresentationMode.Timeline;
        public VideoClip videoClip;
        [Tooltip("Exact MP4 filename under Assets/StreamingAssets/EndingVideos (not a path or URL). Web builds use this; Editor/desktop use Video Clip.")]
        public string webVideoFileName;
        public static bool IsValidWebVideoFileName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value == value.Trim()
                && value.IndexOfAny(new[] { '/', '\\', ':', '?', '#', '%' }) < 0
                && value.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase)
                && value != ".mp4";
        }
        // Reuse the existing serialized skip flag so older endings keep their authored setting.
        public bool allowSkip { get => timelineSkippable; set => timelineSkippable = value; }
        [Min(0)] public float fadeInDuration = .25f;
        [Min(0)] public float fadeOutDuration = .25f;
        public EndingAfterCutsceneAction actionAfterCutscene;
        public bool RestartsCycle => resetCycle || actionAfterCutscene == EndingAfterCutsceneAction.RestartDay1;

        [Header("Timeline (optional)")]
        [Tooltip("EASIEST: a prefab that contains its own PlayableDirector, its timeline and the objects it animates (bind the tracks once, inside the prefab). Spawned under the ending panel when this ending plays. When set, 'Timeline' below is not needed.")]
        public GameObject cutscenePrefab;
        [Tooltip("Advanced: a bare Timeline asset. Tracks are bound by NAME using the EndingController's Timeline Bindings list. Ignored when a Cutscene Prefab is set.")]
        public PlayableAsset timeline;
        public EndingTimelineMode timelineMode = EndingTimelineMode.BeforeDialogue;
        [Tooltip("Shared Video/Timeline skip setting, shown as Allow Skip by the Ending inspector.")]
        public bool timelineSkippable = true;
    }
}
