using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    [Serializable]
    public class HREmotionVisual
    {
        public HREmotion emotion;
        [Tooltip("Optional. Without a sprite the placeholder tint + label are used.")]
        public Sprite sprite;
        public Color tint = Color.white;
    }

    /// <summary>
    /// HR portrait: expression changes whenever an HR dialogue line is shown, plus ambient animation
    /// (spinning ceiling fan, slight sway, screen flicker). Works with placeholders when art is missing.
    /// </summary>
    public class HRReactionController : MonoBehaviour
    {
        [SerializeField] DialogueManager dialogue;
        [SerializeField] Image portrait;
        [SerializeField] TMP_Text placeholderLabel;
        [SerializeField] RectTransform swayTarget;
        [SerializeField] RectTransform fan;
        [SerializeField] Image flickerOverlay;
        [SerializeField] List<HREmotionVisual> visuals = new List<HREmotionVisual>();
        [Header("Ambient")]
        [SerializeField] float fanSpeed = 240f;
        [SerializeField] float swayAmount = 4f;
        [SerializeField] float flickerInterval = 4f;

        /// <summary>Set by the interview when flag hr_strange is active: faster fan, stronger jitter and flicker.</summary>
        public bool StrangeMode { get; set; }
        public HREmotion Current { get; private set; } = HREmotion.Neutral;

        Vector2 swayBase;
        float nextFlicker;
        float flickerLeft;

        void Awake()
        {
            if (swayTarget != null) swayBase = swayTarget.anchoredPosition;
            SetEmotion(HREmotion.Neutral);
        }

        void OnEnable()
        {
            if (dialogue != null) dialogue.LineShown += OnLine;
            AtmosphereManager.PhaseChanged += OnAtmosphereChanged;
        }
        void OnDisable()
        {
            if (dialogue != null) dialogue.LineShown -= OnLine;
            AtmosphereManager.PhaseChanged -= OnAtmosphereChanged;
        }

        void OnLine(DialogueLine line)
        {
            if (line.speaker == Speakers.HR) SetEmotion(line.emotion);
        }

        public void SetEmotion(HREmotion emotion)
        {
            Current = emotion;
            HREmotionVisual visual = null;
            for (int i = 0; i < visuals.Count; i++)
            {
                if (visuals[i] != null && visuals[i].emotion == emotion) { visual = visuals[i]; break; }
            }
            if (visual == null) Debug.LogWarning("[HR] No visual configured for emotion " + emotion, this);

            // The emotion decides WHICH expression; the atmosphere phase decides which VARIANT of it is shown.
            Sprite sprite = visual != null ? visual.sprite : null;
            Color tint = visual != null ? visual.tint : Color.white;
            var atmosphere = AtmosphereManager.Instance;
            if (atmosphere != null)
            {
                var variant = atmosphere.ResolveHrSprite(emotion);
                if (variant != null) sprite = variant;
                tint *= atmosphere.HrTint;
            }

            if (portrait != null)
            {
                portrait.sprite = sprite;
                portrait.color = tint;
            }
            if (placeholderLabel != null) placeholderLabel.text = sprite != null ? "" : emotion.ToString();
        }

        void OnAtmosphereChanged(AtmospherePhaseData phase) { SetEmotion(Current); }

        public void ResetToNeutral() { SetEmotion(HREmotion.Neutral); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float speed = StrangeMode ? fanSpeed * 3f : fanSpeed;
            if (fan != null) fan.Rotate(0f, 0f, -speed * dt);

            if (swayTarget != null)
            {
                float t = Time.unscaledTime;
                var offset = new Vector2(Mathf.Sin(t * 0.7f) * swayAmount, Mathf.Sin(t * 1.1f) * swayAmount * 0.5f);
                if (StrangeMode) offset += new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-2f, 2f));
                swayTarget.anchoredPosition = swayBase + offset;
            }

            if (flickerOverlay != null)
            {
                float interval = StrangeMode ? flickerInterval * 0.3f : flickerInterval;
                if (Time.unscaledTime >= nextFlicker)
                {
                    nextFlicker = Time.unscaledTime + interval * UnityEngine.Random.Range(0.6f, 1.4f);
                    flickerLeft = 0.08f;
                }
                flickerLeft -= dt;
                var c = flickerOverlay.color;
                c.a = flickerLeft > 0f ? UnityEngine.Random.Range(0.05f, StrangeMode ? 0.25f : 0.12f) : 0f;
                flickerOverlay.color = c;
            }
        }
    }
}
