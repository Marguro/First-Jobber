using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>
    /// Put on any UI Image or SpriteRenderer. Swaps its sprite (by Target Id) and/or tints it
    /// according to the active atmosphere phase. Phases without an override restore the original look.
    /// </summary>
    public class AtmosphereSpriteBinding : MonoBehaviour
    {
        [Tooltip("Stable id used by AtmospherePhaseData.spriteOverrides, e.g. BedroomBackground.")]
        [SerializeField] string targetId;
        [SerializeField] Image image;
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Multiply the original colour by the phase's UI Tint.")]
        [SerializeField] bool applyUiTint = true;
        [Tooltip("Only swap the sprite; never touch the Image colour. Use on buttons and anything whose colour is driven by another script.")]
        [SerializeField] bool spriteOnly;
        [Header("Phase-authored UI artwork")]
        [SerializeField] PhaseUISpriteLibrary uiSprites;
        [Tooltip("Authoritative original/normal artwork. Edit this field when changing a phase-controlled Image's base sprite; the visible Image.sprite is the rendered phase result.")]
        [SerializeField] Sprite baseSprite;
        [SerializeField] AtmosphereOverlayMask overlayMask;
        public bool DedicatedSpriteActive { get; private set; }
        public bool AtmosphereFallbackActive => !DedicatedSpriteActive;
        public Sprite BaseSprite { get { Capture(); return originalSprite; } }
        public Color BaseColor { get { Capture(); return originalColor; } }
        public void ConfigureUI(PhaseUISpriteLibrary library, Sprite original, AtmosphereOverlayMask mask)
        { uiSprites=library; baseSprite=original; overlayMask=mask; }
        public void RefreshAuthoredMapping()
        {
            Capture();
            if (baseSprite!=configuredBaseSnapshot)
            {
                if (baseSprite!=null) originalSprite=baseSprite;
                configuredBaseSnapshot=baseSprite;
            }
            Apply(AtmosphereManager.Instance!=null ? AtmosphereManager.Instance.AppliedPhase : null,0f);
        }
        /// <summary>Data/interaction owner updates its authored variant or state color, not the phase-rendered sprite.</summary>
        public void SetAuthoredVisual(Sprite sprite, Color color)
        {
            Capture(); originalSprite=sprite; originalColor=color; captured=true;
            Apply(AtmosphereManager.Instance != null ? AtmosphereManager.Instance.AppliedPhase : null,0f);
        }

        public string TargetId => targetId;

        Sprite originalSprite;
        Sprite configuredBaseSnapshot;
        Color originalColor = Color.white;
        Coroutine routine;
        bool captured;

        void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            Capture();
        }

        void OnEnable()
        {
            Capture();
            AtmosphereManager.Register(this);
            var manager = AtmosphereManager.Instance;
            if (manager != null && manager.AppliedPhase != null) Apply(manager.AppliedPhase, 0f);
        }

        void OnDisable() { AtmosphereManager.Unregister(this); if (routine!=null) StopCoroutine(routine); routine=null; }

        void Capture()
        {
            if (captured) return;
            if (image != null) { originalSprite = baseSprite != null ? baseSprite : image.sprite; configuredBaseSnapshot=baseSprite; originalColor = image.color; captured = true; }
            else if (spriteRenderer != null) { originalSprite = spriteRenderer.sprite; originalColor = spriteRenderer.color; captured = true; }
        }

        Sprite CurrentSprite => image != null ? image.sprite : spriteRenderer != null ? spriteRenderer.sprite : null;
        Color CurrentColor => image != null ? image.color : spriteRenderer != null ? spriteRenderer.color : Color.white;

        void SetVisual(Sprite sprite, Color color, bool setSprite)
        {
            if (image != null) { if (setSprite) image.sprite = sprite; image.color = color; }
            else if (spriteRenderer != null) { if (setSprite) spriteRenderer.sprite = sprite; spriteRenderer.color = color; }
        }

        /// <summary>phase == null restores the original sprite and colour.</summary>
        public void Apply(AtmospherePhaseData phase, float fade)
        {
            Capture();
            if (!captured) return;

            Sprite dedicated=null;
            bool mapped=uiSprites!=null && uiSprites.TryResolve(originalSprite,phase,out dedicated);
            bool wasDedicated=DedicatedSpriteActive;
            DedicatedSpriteActive=dedicated!=null;
            Sprite targetSprite = DedicatedSpriteActive ? dedicated : mapped ? originalSprite : phase != null ? phase.GetSprite(targetId) : null;
            if (targetSprite == null) targetSprite = originalSprite;

            Color targetColor = originalColor;
            if (!DedicatedSpriteActive && phase != null && applyUiTint && !spriteOnly)
            {
                targetColor = new Color(originalColor.r * phase.uiTint.r, originalColor.g * phase.uiTint.g,
                    originalColor.b * phase.uiTint.b, originalColor.a);
            }

            if (routine != null) StopCoroutine(routine);
            routine = null;

            if (overlayMask!=null) overlayMask.Refresh();
            if (DedicatedSpriteActive || wasDedicated)
            {
                SetVisual(targetSprite,targetColor,true); // No atmosphere alpha dip/tint on authored phase art.
                return;
            }
            if (spriteOnly)
            {
                SetVisual(targetSprite, CurrentColor, true);
                return;
            }

            bool spriteChanges = CurrentSprite != targetSprite;
            if (fade <= 0f || !isActiveAndEnabled)
            {
                SetVisual(targetSprite, targetColor, true);
                return;
            }
            routine = StartCoroutine(Fade(targetSprite, targetColor, fade, spriteChanges));
        }

        IEnumerator Fade(Sprite targetSprite, Color targetColor, float duration, bool spriteChanges)
        {
            Color from = CurrentColor;
            bool swapped = !spriteChanges;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                if (!swapped && t >= 0.5f) { SetVisual(targetSprite, CurrentColor, true); swapped = true; }
                Color c = Color.Lerp(from, targetColor, t);
                if (spriteChanges) c.a *= Mathf.Abs(2f * t - 1f); // dip through transparent while swapping
                SetVisual(null, c, false);
                yield return null;
            }
            SetVisual(targetSprite, targetColor, true);
            routine = null;
        }
    }
}
