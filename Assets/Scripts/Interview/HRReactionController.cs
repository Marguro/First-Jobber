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
        [Tooltip("Owned by HRCharacterData. Missing requested sprites fall back to this HR's Neutral, then Default Portrait.")]
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
        [SerializeField, HideInInspector] List<HREmotionVisual> visuals = new List<HREmotionVisual>(); // legacy serialized data, never used as a character fallback
        [Tooltip("Optional. Receives the Animator Controller of the current HR character.")]
        [SerializeField] Animator animator;
        [Header("Ambient")]
        [SerializeField] float fanSpeed = 240f;
        [SerializeField] float swayAmount = 4f;
        [SerializeField] float flickerInterval = 4f;

        /// <summary>Set by the interview when flag hr_strange is active: faster fan, stronger jitter and flicker.</summary>
        public bool StrangeMode { get; set; }
        public HREmotion Current { get; private set; } = HREmotion.Neutral;
        /// <summary>The HR currently shown. Null clears the portrait instead of borrowing legacy art.</summary>
        public HRCharacterData Character { get; private set; }

        /// <summary>Switches to another HR; null clears the portrait. Keeps no visual/animator from the previous HR.</summary>
        public void SetCharacter(HRCharacterData character)
        {
            Character = character;
            if (animator != null)
                animator.runtimeAnimatorController = character != null ? character.animatorController : null;
            SetEmotion(character != null ? character.defaultEmotion : HREmotion.Neutral);
        }

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
            if (Character != null) { ShowCharacter(emotion); return; }
            if (portrait != null)
            {
                portrait.sprite = null;
                portrait.enabled = false;
                portrait.color = Color.white;
            }
            if (placeholderLabel != null) placeholderLabel.text = "HR\n" + emotion;
        }

        // An HR owns its sprites: phase variant -> standard emotion -> Neutral -> default portrait -> text placeholder.
        // The shared AtmospherePhaseData.hrSprites are NOT used here, so one HR never shows another HR's art.
        void ShowCharacter(HREmotion emotion)
        {
            var atmosphere = AtmosphereManager.Instance;
            var phase = atmosphere != null ? atmosphere.AppliedPhase : null;
            Sprite sprite = Character.ResolveSprite(emotion, phase, out Color tint);
            if (atmosphere != null) tint *= atmosphere.HrTint;
            if (portrait != null) { portrait.sprite = sprite; portrait.enabled = sprite != null; portrait.color = tint; }
            if (placeholderLabel != null) placeholderLabel.text = sprite != null ? "" : Character.DisplayName + "\n" + emotion;
        }

        void OnAtmosphereChanged(AtmospherePhaseData phase) { SetEmotion(Current); }

        public void ResetToNeutral() { SetEmotion(Character != null ? Character.defaultEmotion : HREmotion.Neutral); }

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
