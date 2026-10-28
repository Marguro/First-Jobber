using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>A few irregular ink quads on a NEW masked UI layer. No source textures/materials are edited.</summary>
    public class UIInkCorruptionGraphic : MaskableGraphic
    {
        uint seed;
        float strength, burst;
        bool consumed, paper;
        Rect region = new Rect(.06f, .2f, .64f, .6f);
        public void Configure(uint stableSeed, float value, bool isConsumed, Rect normalizedRegion, Color ink, bool damagePaper = false)
        {
            seed = stableSeed; strength = value; consumed = isConsumed; paper = damagePaper; region = normalizedRegion;
            color = ink; raycastTarget = false; maskable = true; SetVerticesDirty();
        }
        public void SetBurst(float value) { burst = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (strength <= 0) return;
            Rect r = rectTransform.rect;
            int count = (consumed || paper) && strength > .4f ? 4 : 2;
            for (int i = 0; i < count; i++)
            {
                float u = UICorruptionSettings.Sample(seed, i * 5 + 1);
                float v = UICorruptionSettings.Sample(seed, i * 5 + 2);
                float width = consumed ? .32f + u * .53f : paper ? .18f + u * .2f : .12f + u * .23f;
                float x = region.x + (1 - width) * region.width * v;
                // Functional/decorative ink stays at the edge. Broad redactions are consumed-only.
                float y = consumed ? region.y + region.height * (.15f + .7f * u)
                    : paper ? region.y + region.height * (i % 2 == 0 ? .06f : .92f) : region.y + .025f * u;
                float height = consumed ? .012f + strength * .065f * v : paper ? .01f + strength * .022f * v : .006f + .006f * u;
                float alpha = Mathf.Clamp01(strength * (consumed ? .85f : paper ? .3f + strength * .15f : .18f) + burst * .08f);
                Color c = color; c.a *= alpha;
                float left = r.xMin + x * r.width;
                float bottom = r.yMin + y * r.height;
                float w = width * region.width * r.width;
                float h = Mathf.Max(1, height * r.height);
                float shear = (UICorruptionSettings.Sample(seed, i * 5 + 3) - .5f) * h;
                int n = vh.currentVertCount;
                vh.AddVert(new Vector3(left, bottom), c, Vector2.zero);
                vh.AddVert(new Vector3(left + w, bottom + shear), c, Vector2.zero);
                vh.AddVert(new Vector3(left + w * .97f, bottom + h + shear), c, Vector2.zero);
                vh.AddVert(new Vector3(left + w * .02f, bottom + h), c, Vector2.zero);
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n + 2, n + 3, n);
            }
        }
    }
}
