using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Optional interview-only layout for the ONE shared DialogueUI. Rect markers are artist-authored.</summary>
    public class InterviewDialogueLayout : MonoBehaviour
    {
        [Header("Existing shared dialogue objects")]
        [SerializeField] RectTransform dialogueRoot;
        [SerializeField] TMP_Text speakerText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] RectTransform continueIndicator;
        [SerializeField] Image background;
        [SerializeField] AtmosphereSpriteBinding backgroundBinding;
        [Header("Edit these RectTransform markers with the Rect Tool")]
        [SerializeField] RectTransform rootLayout;
        [SerializeField] RectTransform speakerLayout;
        [SerializeField] RectTransform bodyLayout;
        [SerializeField] RectTransform continueLayout;
        [Header("Interview paper style")]
        [SerializeField] Sprite paperSprite;
        [SerializeField] Color paperColor = Color.white;
        [SerializeField] Color speakerColor = Color.white;
        [SerializeField] Color bodyColor = new Color(.28f, .19f, .06f);
        [SerializeField, Min(1)] float speakerFontSize = 32;
        [Header("Speaker name (interview only)")]
        [Tooltip("Padding inside SpeakerNameArea. Does not inherit the shared box's old body-width margin.")]
        [SerializeField] Vector4 speakerMargins = Vector4.zero;
        [SerializeField, Min(1)] float speakerMinimumFontSize = 26;
        [SerializeField, Min(1)] float bodyFontSize = 28;
        bool applied, captured, bindingEnabled;
        Pose rootOriginal, speakerOriginal, bodyOriginal, continueOriginal;
        Sprite originalSprite;
        Color originalBackground, originalSpeaker, originalBody;
        float originalSpeakerSize, originalBodySize;
        Image.Type originalType;
        bool originalAspect;
        Vector4 originalSpeakerMargins;
        TextWrappingModes originalSpeakerWrapping;
        TextOverflowModes originalSpeakerOverflow;
        HorizontalAlignmentOptions originalSpeakerHorizontal;
        VerticalAlignmentOptions originalSpeakerVertical;
        bool originalSpeakerAutoSize;
        float originalSpeakerMinSize, originalSpeakerMaxSize;

        struct Pose
        {
            Vector2 min, max, position, size, pivot;
            Vector3 scale;
            public Pose(RectTransform rect)
            { min=rect.anchorMin; max=rect.anchorMax; position=rect.anchoredPosition; size=rect.sizeDelta; pivot=rect.pivot; scale=rect.localScale; }
            public void Apply(RectTransform rect)
            { rect.anchorMin=min; rect.anchorMax=max; rect.anchoredPosition=position; rect.sizeDelta=size; rect.pivot=pivot; rect.localScale=scale; }
        }
        public void SetInterviewActive(bool value)
        {
            if (value == applied) return;
            if (dialogueRoot == null || rootLayout == null) return;
            if (value)
            {
                rootOriginal=new Pose(dialogueRoot);
                if (speakerText != null) { speakerOriginal=new Pose(speakerText.rectTransform); originalSpeaker=speakerText.color; originalSpeakerSize=speakerText.fontSize; }
                if (speakerText != null)
                {
                    originalSpeakerMargins=speakerText.margin; originalSpeakerWrapping=speakerText.textWrappingMode;
                    originalSpeakerOverflow=speakerText.overflowMode; originalSpeakerAutoSize=speakerText.enableAutoSizing;
                    originalSpeakerMinSize=speakerText.fontSizeMin; originalSpeakerMaxSize=speakerText.fontSizeMax;
                    originalSpeakerHorizontal=speakerText.horizontalAlignment; originalSpeakerVertical=speakerText.verticalAlignment;
                }
                if (bodyText != null) { bodyOriginal=new Pose(bodyText.rectTransform); originalBody=bodyText.color; originalBodySize=bodyText.fontSize; }
                if (continueIndicator != null) continueOriginal=new Pose(continueIndicator);
                if (background != null) { originalSprite=backgroundBinding != null ? backgroundBinding.BaseSprite : background.sprite; originalBackground=backgroundBinding != null ? backgroundBinding.BaseColor : background.color; originalType=background.type; originalAspect=background.preserveAspect; }
                bindingEnabled=backgroundBinding != null && backgroundBinding.enabled;
                captured=true; applied=true;
                new Pose(rootLayout).Apply(dialogueRoot);
                if (speakerText != null && speakerLayout != null) { new Pose(speakerLayout).Apply(speakerText.rectTransform); speakerText.color=speakerColor; speakerText.fontSize=speakerFontSize; }
                if (speakerText != null && speakerLayout != null)
                {
                    speakerText.margin=speakerMargins;
                    speakerText.textWrappingMode=TextWrappingModes.NoWrap;
                    speakerText.horizontalAlignment=HorizontalAlignmentOptions.Left;
                    speakerText.verticalAlignment=VerticalAlignmentOptions.Middle;
                    speakerText.fontSizeMin=Mathf.Min(speakerMinimumFontSize, speakerFontSize);
                    speakerText.fontSizeMax=speakerFontSize;
                    speakerText.enableAutoSizing=true;
                    speakerText.overflowMode=TextOverflowModes.Ellipsis;
                }
                if (bodyText != null && bodyLayout != null) { new Pose(bodyLayout).Apply(bodyText.rectTransform); bodyText.color=bodyColor; bodyText.fontSize=bodyFontSize; }
                if (continueIndicator != null && continueLayout != null) new Pose(continueLayout).Apply(continueIndicator);
                if (background != null) { background.sprite=paperSprite; background.color=paperColor; background.type=Image.Type.Simple; background.preserveAspect=true; }
                if (backgroundBinding != null) backgroundBinding.SetAuthoredVisual(paperSprite,paperColor);
            }
            else if (captured)
            {
                rootOriginal.Apply(dialogueRoot);
                if (speakerText != null) { speakerOriginal.Apply(speakerText.rectTransform); speakerText.color=originalSpeaker; speakerText.fontSize=originalSpeakerSize; }
                if (speakerText != null)
                {
                    speakerText.margin=originalSpeakerMargins; speakerText.textWrappingMode=originalSpeakerWrapping;
                    speakerText.overflowMode=originalSpeakerOverflow; speakerText.fontSizeMin=originalSpeakerMinSize;
                    speakerText.fontSizeMax=originalSpeakerMaxSize; speakerText.enableAutoSizing=originalSpeakerAutoSize;
                    speakerText.horizontalAlignment=originalSpeakerHorizontal; speakerText.verticalAlignment=originalSpeakerVertical;
                }
                if (bodyText != null) { bodyOriginal.Apply(bodyText.rectTransform); bodyText.color=originalBody; bodyText.fontSize=originalBodySize; }
                if (continueIndicator != null) continueOriginal.Apply(continueIndicator);
                if (background != null) { background.sprite=originalSprite; background.color=originalBackground; background.type=originalType; background.preserveAspect=originalAspect; }
                if (backgroundBinding != null) { backgroundBinding.SetAuthoredVisual(originalSprite,originalBackground); backgroundBinding.enabled=bindingEnabled; }
                captured=applied=false;
            }
        }
    }
}
