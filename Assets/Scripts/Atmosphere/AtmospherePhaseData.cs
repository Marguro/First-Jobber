using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstJobber
{
    [Serializable]
    public class AtmosphereSpriteOverride
    {
        [Tooltip("Must match the Target Id of an AtmosphereSpriteBinding, e.g. BedroomBackground, ResumeBackground, UIDecoration.")]
        public string targetId;
        [Tooltip("Empty = keep the binding's original sprite.")]
        public Sprite sprite;
    }

    [Serializable]
    public class AtmosphereHrSpriteOverride
    {
        public HREmotion emotion;
        [Tooltip("Empty = fall back to the sprite configured in HRReactionController.")]
        public Sprite sprite;
    }

    /// <summary>One visual/audio state of the game. Activated by day through AtmosphereConfiguration.</summary>
    [CreateAssetMenu(menuName = "First Jobber/Atmosphere Phase", fileName = "Phase_New")]
    public class AtmospherePhaseData : ScriptableObject
    {
        [Header("General")]
        public string id;
        public string displayName;
        [Tooltip("The phase is active from this day until a phase with a higher start day takes over.")]
        [Min(1)] public int startDay = 1;
        public bool enabled = true;

        [Header("Moving background motifs")]
        [Tooltip("-1 = leave the current background state alone. 0-4 = target blurred slots, replaced individually over time.")]
        [Range(-1, 4)] public int blurredBackgroundElements = -1;

        [Header("Visuals (all optional)")]
        public bool useCameraColor;
        public Color cameraColor = Color.black;
        [Tooltip("Fullscreen overlay colour (RGB). Opacity is set separately.")]
        public Color overlayColor = Color.black;
        [Range(0f, 1f)] public float overlayOpacity;
        [Tooltip("Multiplied into every binding that has 'Apply Ui Tint' enabled. White = no change.")]
        public Color uiTint = Color.white;
        [Tooltip("Multiplied into the HR portrait colour (useful for placeholders). White = no change.")]
        public Color hrTint = Color.white;
        [Min(0f)] public float fadeDuration = 1f;

        [Header("Flicker (optional)")]
        public bool flickerEnabled;
        [Min(0.1f)] public float flickerInterval = 3f;
        [Range(0f, 1f)] public float flickerIntensity = 0.15f;

        [Header("Sprite variants")]
        [Tooltip("Environment, UI decoration and prop sprites, matched by Target Id.")]
        public List<AtmosphereSpriteOverride> spriteOverrides = new List<AtmosphereSpriteOverride>();
        [Tooltip("HR portrait per emotion for this phase.")]
        public List<AtmosphereHrSpriteOverride> hrSprites = new List<AtmosphereHrSpriteOverride>();

        [Header("Audio (optional)")]
        public AudioClip ambientClip;
        [Range(0f, 1f)] public float ambientVolume = 0.5f;
        [Min(0f)] public float audioCrossfade = 2f;

        public Sprite GetSprite(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return null;
            for (int i = 0; i < spriteOverrides.Count; i++)
            {
                var o = spriteOverrides[i];
                if (o != null && o.targetId == targetId && o.sprite != null) return o.sprite;
            }
            return null;
        }

        public Sprite GetHrSprite(HREmotion emotion)
        {
            for (int i = 0; i < hrSprites.Count; i++)
            {
                var o = hrSprites[i];
                if (o != null && o.emotion == emotion && o.sprite != null) return o.sprite;
            }
            return null;
        }
    }
}
