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
            CapturePortraitLayout();
            Character = character;
            if (animator != null)
                animator.runtimeAnimatorController = character != null ? character.animatorController : null;
            SetEmotion(character != null ? character.defaultEmotion : HREmotion.Neutral);
        }

        Vector2 swayBase;
        float nextFlicker;
        float flickerLeft;
        RectTransform portraitRect;
        Vector2 baseAnchorMin, baseAnchorMax, basePivot, basePosition, baseSize;
        Vector3 baseScale;
        Vector2 portraitRestPosition;
        bool portraitLayoutCaptured, applyingPortraitLayout;

        void CapturePortraitLayout()
        {
            if (portraitLayoutCaptured || portrait == null) return;
            portraitRect=portrait.rectTransform;
            if (portraitRect == null) return;
            baseAnchorMin=portraitRect.anchorMin; baseAnchorMax=portraitRect.anchorMax;
            basePivot=portraitRect.pivot; basePosition=portraitRect.anchoredPosition;
            baseSize=portraitRect.sizeDelta; baseScale=portraitRect.localScale;
            portraitLayoutCaptured=true;
        }

        [ContextMenu("Reapply current HR portrait settings")]
        public void ApplyPortraitPresentation()
        {
            if (applyingPortraitLayout) return;
            CapturePortraitLayout();
            if (!portraitLayoutCaptured) return;
            applyingPortraitLayout=true;
            try
            {
                // Always start from the scene's original pose, never the previous HR's transformed pose.
                portraitRect.anchorMin=baseAnchorMin; portraitRect.anchorMax=baseAnchorMax;
                portraitRect.pivot=basePivot; portraitRect.sizeDelta=baseSize;
                portraitRect.anchoredPosition=basePosition; portraitRect.localScale=baseScale;
                if (Character != null)
                {
                    if (Character.alignPortraitToBottom)
                    {
                        var parent=portraitRect.parent as RectTransform;
                        Vector2 area=parent != null ? parent.rect.size : Vector2.zero;
                        Vector2 size=new Vector2(area.x*(baseAnchorMax.x-baseAnchorMin.x)+baseSize.x,
                            area.y*(baseAnchorMax.y-baseAnchorMin.y)+baseSize.y);
                        float centerX=area.x*(baseAnchorMin.x+(baseAnchorMax.x-baseAnchorMin.x)*basePivot.x-.5f)
                            +basePosition.x+size.x*(.5f-basePivot.x);
                        portraitRect.anchorMin=portraitRect.anchorMax=new Vector2(.5f,0f);
                        portraitRect.pivot=new Vector2(.5f,0f);
                        portraitRect.sizeDelta=size;
                        portraitRect.anchoredPosition=new Vector2(centerX,0f);
                    }
                    portraitRect.anchoredPosition+=new Vector2(Character.portraitOffsetX,Character.portraitOffsetY);
                    float scale=System.Math.Max(.01f,Character.portraitScale);
                    portraitRect.localScale=new Vector3(baseScale.x*scale,baseScale.y*scale,baseScale.z*scale);
                }
                portrait.preserveAspect=true;
                portraitRestPosition=portraitRect.anchoredPosition;
                if (Character != null && Character.alignPortraitToBottom && swayTarget != null && swayTarget!=portraitRect)
                    swayTarget.anchoredPosition=new Vector2(swayTarget.anchoredPosition.x,swayBase.y);
            }
            finally { applyingPortraitLayout=false; }
        }
        void OnRectTransformDimensionsChange() { if (portraitLayoutCaptured) ApplyPortraitPresentation(); }

        void Awake()
        {
            if (swayTarget != null) swayBase = swayTarget.anchoredPosition;
            CapturePortraitLayout();
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
            ApplyPortraitPresentation();
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
                if (Character != null && Character.alignPortraitToBottom) offset.y=0f;
                var origin=portraitLayoutCaptured && swayTarget==portraitRect ? portraitRestPosition : swayBase;
                swayTarget.anchoredPosition = origin + offset;
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
