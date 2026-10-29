using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>Transient, raycast-free print fragments. Copies the current Image's generated
    /// Simple/Sliced/Filled geometry, then clips triangles into displaced horizontal bands.
    /// The original Image, material, sprite binding, layout and hitbox are never changed.</summary>
    public class UIHorizontalTearGraphic : Image
    {
        readonly List<UIVertex> triangles = new List<UIVertex>(192);
        readonly UIVertex[] clipA = new UIVertex[8], clipB = new UIVertex[8];
        float displacement;
        uint pattern;

        public void Configure(Image source, float offset, uint seed, float opacity)
        {
            sprite = source.overrideSprite;
            type = source.type; preserveAspect = source.preserveAspect;
            fillCenter = source.fillCenter; fillMethod = source.fillMethod;
            fillAmount = source.fillAmount; fillClockwise = source.fillClockwise;
            fillOrigin = source.fillOrigin; pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            useSpriteMesh = source.useSpriteMesh;
            // Reuse only the authored base material. UI masking builds this copy's own stencil state.
            material = source.material;
            color = source.color * new Color(1, 1, 1, opacity);
            raycastTarget = false; maskable = source.maskable;
            displacement = offset; pattern = seed;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (sprite == null || Mathf.Abs(displacement) < .01f) { vh.Clear(); return; }
            base.OnPopulateMesh(vh);
            triangles.Clear(); vh.GetUIVertexStream(triangles); vh.Clear();
            Rect bounds = rectTransform.rect;
            const int bands = 5;
            for (int band = 0; band < bands; band++)
            {
                // Leave alternating strips untouched: no full-screen flicker or opaque overlay.
                if ((band + (pattern & 1)) % 2 == 0) continue;
                float low = bounds.yMin + bounds.height * band / bands;
                float high = bounds.yMin + bounds.height * (band + 1) / bands;
                float offset = displacement * (band % 3 == 0 ? -.65f : 1f);
                for (int i = 0; i + 2 < triangles.Count; i += 3)
                {
                    clipA[0] = triangles[i]; clipA[1] = triangles[i + 1]; clipA[2] = triangles[i + 2];
                    int count = Clip(clipA, 3, clipB, low, true);
                    count = Clip(clipB, count, clipA, high, false);
                    if (count < 3) continue;
                    int first = vh.currentVertCount;
                    for (int v = 0; v < count; v++)
                    {
                        var vertex = clipA[v]; vertex.position.x += offset; vh.AddVert(vertex);
                    }
                    for (int v = 1; v < count - 1; v++) vh.AddTriangle(first, first + v, first + v + 1);
                }
            }
        }

        static int Clip(UIVertex[] input, int count, UIVertex[] output, float edge, bool above)
        {
            if (count == 0) return 0;
            int result = 0;
            UIVertex previous = input[count - 1];
            bool previousInside = above ? previous.position.y >= edge : previous.position.y <= edge;
            for (int i = 0; i < count; i++)
            {
                UIVertex current = input[i];
                bool inside = above ? current.position.y >= edge : current.position.y <= edge;
                if (inside != previousInside)
                {
                    float t = (edge - previous.position.y) / (current.position.y - previous.position.y);
                    UIVertex cut = previous;
                    cut.position = Vector3.LerpUnclamped(previous.position, current.position, t);
                    cut.uv0 = Vector4.LerpUnclamped(previous.uv0, current.uv0, t);
                    cut.uv1 = Vector4.LerpUnclamped(previous.uv1, current.uv1, t);
                    cut.color = Color32.Lerp(previous.color, current.color, t);
                    output[result++] = cut;
                }
                if (inside) output[result++] = current;
                previous = current; previousInside = inside;
            }
            return result;
        }
    }
}
