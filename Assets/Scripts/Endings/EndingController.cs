using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>One reusable ending screen: background, dialogue, final text, restart / menu buttons.</summary>
    public class EndingController : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text finalText;
        [SerializeField] GameObject buttonsRoot;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;
        [SerializeField] AudioSource audioSource;

        [Header("Timeline (optional)")]
        [SerializeField] PlayableDirector director;
        [Tooltip("Timeline tracks are matched to these by track name. Audio tracks with no entry use the AudioSource above.")]
        [SerializeField] TimelineBinding[] timelineBindings;

        [Serializable]
        public class TimelineBinding
        {
            [Tooltip("Leave empty to use this entry for every ending. Fill in (e.g. ending_my_own_terms) to use it only for that ending; an ending-specific entry wins over a general one.")]
            public string endingId;
            public string trackName;
            [Tooltip("A GameObject or component. If the track needs a different type (e.g. an Animator), it is looked up on this object automatically.")]
            public UnityEngine.Object target;
        }

        public event Action RestartRequested;
        public event Action MenuRequested;

        bool clicked;

        void Awake()
        {
            if (director == null) director = GetComponent<PlayableDirector>();
            if (director == null) director = gameObject.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            if (restartButton != null) restartButton.onClick.AddListener(() => Press(RestartRequested));
            if (menuButton != null) menuButton.onClick.AddListener(() => Press(MenuRequested));
        }

        void Press(Action action)
        {
            if (clicked) return;
            clicked = true;
            DestroySpawned();
            action?.Invoke();
        }

        public void Show(GameContext ctx, EndingData ending)
        {
            clicked = false;
            if (background != null)
            {
                background.sprite = ending.background;
                background.color = ending.background != null ? Color.white : ending.backgroundColor;
            }
            if (titleText != null) titleText.text = LocalizationManager.Get(ending, "title", ending.title);
            if (finalText != null) finalText.text = "";
            if (buttonsRoot != null) buttonsRoot.SetActive(false);
            if (audioSource != null && ending.sound != null) audioSource.PlayOneShot(ending.sound);

            StopTimeline();
            DestroySpawned();
            if (runner != null) StopCoroutine(runner);
            bool useTimeline = (ending.cutscenePrefab != null || ending.timeline != null) && ending.timelineMode != EndingTimelineMode.None;
            if (useTimeline && ending.cutscenePrefab == null && director == null)
            {
                Debug.LogError("[Ending] '" + ending.id + "' has a timeline but the EndingController has no PlayableDirector. Falling back to dialogue only.");
                useTimeline = false;
            }
            runner = StartCoroutine(Run(ctx, ending, useTimeline));
        }

        Coroutine runner;

        IEnumerator Run(GameContext ctx, EndingData ending, bool useTimeline)
        {
            var mode = useTimeline ? ending.timelineMode : EndingTimelineMode.None;

            if (mode == EndingTimelineMode.BeforeDialogue || mode == EndingTimelineMode.InsteadOfDialogue)
                yield return PlayTimeline(ending, ending.timelineSkippable);

            if (mode != EndingTimelineMode.InsteadOfDialogue)
            {
                bool dialogueDone = false;
                if (mode == EndingTimelineMode.WithDialogue) StartTimeline(ending);
                ctx.Dialogue.Play(ending.lines, () => dialogueDone = true);
                while (!dialogueDone) yield return null;
                while (mode == EndingTimelineMode.WithDialogue && TimelineRunning) yield return null;
            }

            StopTimeline();
            if (finalText != null) finalText.text = LocalizationManager.Get(ending, "finalText", ending.finalText);
            if (buttonsRoot != null) buttonsRoot.SetActive(true);
            runner = null;
        }

        PlayableDirector active; // the director currently playing (own director, or the one inside a spawned prefab)
        GameObject spawned;

        bool TimelineRunning => active != null && active.playableAsset != null && active.state == PlayState.Playing;

        void DestroySpawned()
        {
            active = null;
            if (spawned != null) Destroy(spawned);
            spawned = null;
        }

        void StartTimeline(EndingData ending)
        {
            if (ending.cutscenePrefab != null)
            {
                spawned = Instantiate(ending.cutscenePrefab, transform);
                spawned.name = ending.cutscenePrefab.name;
                int index = background != null ? background.transform.GetSiblingIndex() + 1 : 0;
                spawned.transform.SetSiblingIndex(index); // above the ending background, below title / buttons
                active = spawned.GetComponentInChildren<PlayableDirector>(true);
                if (active == null || active.playableAsset == null)
                {
                    Debug.LogError("[Ending] Cutscene prefab '" + spawned.name + "' needs a PlayableDirector with a timeline assigned.", spawned);
                    active = null;
                    return;
                }
                active.playOnAwake = false;
                active.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                active.extrapolationMode = DirectorWrapMode.None;
                active.time = 0;
                active.Play();
                return;
            }

            active = director;
            director.Stop();
            director.playableAsset = ending.timeline;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.extrapolationMode = DirectorWrapMode.None;
            foreach (var track in ending.timeline.GetOutputTracks())
            {
                UnityEngine.Object target = null;
                if (timelineBindings != null)
                {
                    foreach (var b in timelineBindings)
                    {
                        if (b == null || b.trackName != track.name) continue;
                        bool specific = !string.IsNullOrEmpty(b.endingId);
                        if (specific && b.endingId != ending.id) continue;
                        target = b.target;
                        if (specific) break; // ending-specific entry wins
                    }
                }
                if (target == null && track is UnityEngine.Timeline.AudioTrack) target = audioSource;
                target = Coerce(track, target);
                if (target != null) director.SetGenericBinding(track, target);
            }
            director.time = 0;
            director.Play();
        }

        /// <summary>Converts the dragged object to what the track needs (GameObject for Activation, Animator for Animation...).</summary>
        static UnityEngine.Object Coerce(UnityEngine.Timeline.TrackAsset track, UnityEngine.Object target)
        {
            if (target == null) return null;
            foreach (var output in track.outputs)
            {
                var type = output.outputTargetType;
                if (type == null || type.IsInstanceOfType(target)) return target;
                GameObject go = target as GameObject;
                if (go == null && target is Component c) go = c.gameObject;
                if (go == null) continue;
                if (type == typeof(GameObject)) return go;
                if (typeof(Component).IsAssignableFrom(type))
                {
                    var comp = go.GetComponent(type);
                    if (comp != null) return comp;
                    Debug.LogWarning("[Ending] Track '" + track.name + "' needs a " + type.Name + " but '" + go.name + "' has none.", go);
                }
            }
            return target;
        }

        IEnumerator PlayTimeline(EndingData ending, bool skippable)
        {
            StartTimeline(ending);
            yield return null; // let the director enter Playing
            while (TimelineRunning)
            {
                if (skippable && SkipPressed())
                {
                    active.time = active.duration;
                    active.Evaluate(); // land every track on its final state
                    break;
                }
                yield return null;
            }
            if (active != null) active.Stop();
        }

        static bool SkipPressed()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        void StopTimeline()
        {
            if (active != null && active.playableAsset != null) active.Stop();
        }

        void OnDisable()
        {
            if (runner != null) { StopCoroutine(runner); runner = null; }
            StopTimeline();
            DestroySpawned();
        }
    }
}
