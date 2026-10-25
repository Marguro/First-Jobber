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

        public string TargetId => targetId;

        Sprite originalSprite;
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

        void OnDisable() { AtmosphereManager.Unregister(this); }

        void Capture()
        {
            if (captured) return;
            if (image != null) { originalSprite = image.sprite; originalColor = image.color; captured = true; }
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

            Sprite targetSprite = phase != null ? phase.GetSprite(targetId) : null;
            if (targetSprite == null) targetSprite = originalSprite;

            Color targetColor = originalColor;
            if (phase != null && applyUiTint)
            {
                targetColor = new Color(originalColor.r * phase.uiTint.r, originalColor.g * phase.uiTint.g,
                    originalColor.b * phase.uiTint.b, originalColor.a);
            }

            if (routine != null) StopCoroutine(routine);
            routine = null;

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
