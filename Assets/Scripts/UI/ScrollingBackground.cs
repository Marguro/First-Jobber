using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    /// <summary>
    /// Endlessly scrolling, looping background. Put it on the full-screen "Background" Image.
    /// It reads that Image's sprite and colour every frame, so Atmosphere sprite swaps and tints keep working.
    /// The Image itself is hidden; tiles are drawn as children and clipped to this rect.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectMask2D))]
    public class ScrollingBackground : MonoBehaviour
    {
        public enum ScrollAxis { Horizontal, Vertical, Diagonal }
        public enum ScrollMode { Loop, EdgeToEdge, CornerToCorner }
        public enum Corner { TopRight, TopLeft, BottomRight, BottomLeft }

        [Tooltip("CornerToCorner only: the corner the camera starts on. It pans to the opposite corner and back (e.g. TopRight -> BottomLeft).")]
        [SerializeField] Corner startCorner = Corner.TopRight;
        [Tooltip("CornerToCorner only: extra zoom so there is room to travel. 1.3 = picture is 30% larger than the screen.")]
        [SerializeField, Min(1.01f)] float cornerZoom = 1.3f;

        [Tooltip("Loop: endless tiling. EdgeToEdge: one oversized picture pans to one edge, pauses, then pans back (never shows a gap).")]
        [SerializeField] ScrollMode mode = ScrollMode.Loop;
        [Tooltip("EdgeToEdge / CornerToCorner: seconds to wait at each end.")]
        [SerializeField, Min(0f)] float edgePause = 1f;

        [Tooltip("Horizontal: the sprite is fitted to the screen height and scrolls sideways. Vertical: fitted to width, scrolls up/down.")]
        [SerializeField] ScrollAxis axis = ScrollAxis.Horizontal;
        [Tooltip("Diagonal axis only: which way the picture moves. (-1,-1) = toward bottom-left (looks like travelling from top-right). (1,1) = toward top-right. Any vector works; it is normalised.")]
        [SerializeField] Vector2 diagonalDirection = new Vector2(-1f, -1f);
        [Tooltip("Canvas units per second. Positive = left (Horizontal) / up (Vertical). Negative reverses. 0 = still.")]
        [SerializeField] float speed = 40f;
        [Tooltip("Zoom the tiles. 1 = exactly fills the screen on the cross axis.")]
        [SerializeField, Min(0.1f)] float scale = 1f;
        [Tooltip("Flip every second tile. Turn ON if the art is not seamless - the join becomes a mirror, so no visible seam.")]
        [SerializeField] bool mirrorAlternateTiles = false;
        [Tooltip("Keep scrolling while Time.timeScale is 0.")]
        [SerializeField] bool useUnscaledTime = true;

        Image source;
        readonly List<Image> tiles = new List<Image>();
        Sprite lastSprite;
        Vector2 lastSize;
        float offset;
        float tileLength = 1f;
        bool dirty = true;

        public float Speed { get => speed; set => speed = value; }

        void Awake() { source = GetComponent<Image>(); }

        void OnEnable() { dirty = true; }

        void OnValidate() { dirty = true; }

        void LateUpdate()
        {
            if (source == null) source = GetComponent<Image>();
            if (source.enabled) source.enabled = false; // children draw instead; the Image only stores sprite + colour

            var rt = (RectTransform)transform;
            Vector2 size = rt.rect.size;
            Sprite sprite = source.sprite;
            if (sprite == null) { SetTilesVisible(false); return; }

            if (dirty || sprite != lastSprite || size != lastSize) Rebuild(sprite, size);
            if (tiles.Count == 0) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (mode == ScrollMode.EdgeToEdge) { UpdateEdgeToEdge(size, dt); return; }
            if (mode == ScrollMode.CornerToCorner) { UpdateCorner(size, dt); return; }
            if (axis == ScrollAxis.Diagonal) { UpdateDiagonal(size, dt); return; }
            offset += speed * dt;
            float period = mirrorAlternateTiles ? tileLength * 2f : tileLength;
            offset = Mathf.Repeat(offset, period);

            bool horizontal = axis == ScrollAxis.Horizontal;
            float length = horizontal ? size.x : size.y;
            float start = -length * 0.5f - tileLength * 0.5f; // centre of the first tile, one tile off-screen
            // Positive speed moves content left (Horizontal) or up (Vertical).
            float shift = horizontal ? -offset : offset - (mirrorAlternateTiles ? tileLength * 2f : tileLength);
            Color color = source.color;

            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                tile.color = color;
                float pos = start + i * tileLength + shift;
                var trt = (RectTransform)tile.transform;
                trt.anchoredPosition = horizontal ? new Vector2(pos + tileLength, 0f) : new Vector2(0f, pos + tileLength);
                float flip = mirrorAlternateTiles && (i % 2 == 1) ? -1f : 1f;
                trt.localScale = horizontal ? new Vector3(flip, 1f, 1f) : new Vector3(1f, flip, 1f);
            }
        }

        float travelTime; // seconds into the current back-and-forth cycle

        void UpdateEdgeToEdge(Vector2 size, float dt)
        {
            bool horizontal = axis == ScrollAxis.Horizontal;
            float travel = tileLength - (horizontal ? size.x : size.y);
            var trt = (RectTransform)tiles[0].transform;
            tiles[0].color = source.color;
            trt.localScale = Vector3.one;
            if (travel <= 0.01f || Mathf.Abs(speed) < 0.01f) { trt.anchoredPosition = Vector2.zero; return; }

            travelTime += dt;
            float leg = travel / Mathf.Abs(speed);
            float cycle = (leg + edgePause) * 2f;
            float t = Mathf.Repeat(travelTime, cycle);
            float k;
            if (t < leg) k = t / leg;                       // going to the far edge
            else if (t < leg + edgePause) k = 1f;           // pause
            else if (t < leg * 2f + edgePause) k = 1f - (t - leg - edgePause) / leg; // coming back
            else k = 0f;                                    // pause
            float pos = Mathf.Lerp(-travel * 0.5f, travel * 0.5f, speed > 0f ? 1f - k : k);
            trt.anchoredPosition = horizontal ? new Vector2(pos, 0f) : new Vector2(0f, pos);
        }

        Vector2 bigSize;
        Vector2 diagTile;
        int diagCols;
        Vector2 diagOffset;

        void UpdateDiagonal(Vector2 size, float dt)
        {
            Vector2 dir = diagonalDirection.sqrMagnitude < 0.0001f ? Vector2.zero : diagonalDirection.normalized;
            diagOffset += dir * speed * dt;
            float px = diagTile.x * (mirrorAlternateTiles ? 2f : 1f);
            float py = diagTile.y * (mirrorAlternateTiles ? 2f : 1f);
            diagOffset.x = Mathf.Repeat(diagOffset.x, px);
            diagOffset.y = Mathf.Repeat(diagOffset.y, py);

            Color color = source.color;
            for (int n = 0; n < tiles.Count; n++)
            {
                int i = n % diagCols, j = n / diagCols;
                var trt = (RectTransform)tiles[n].transform;
                tiles[n].color = color;
                float x = -size.x * 0.5f + (i - 2) * diagTile.x + diagTile.x * 0.5f + diagOffset.x;
                float y = -size.y * 0.5f + (j - 2) * diagTile.y + diagTile.y * 0.5f + diagOffset.y;
                trt.anchoredPosition = new Vector2(x, y);
                trt.localScale = new Vector3(mirrorAlternateTiles && i % 2 == 1 ? -1f : 1f, mirrorAlternateTiles && j % 2 == 1 ? -1f : 1f, 1f);
            }
        }

        void RebuildDiagonal(Sprite sprite, Vector2 size)
        {
            float fill = Mathf.Max(size.x / sprite.rect.width, size.y / sprite.rect.height) * scale;
            diagTile = new Vector2(sprite.rect.width, sprite.rect.height) * fill;
            diagTile = new Vector2(Mathf.Max(1f, diagTile.x), Mathf.Max(1f, diagTile.y));
            diagCols = Mathf.CeilToInt(size.x / diagTile.x) + 4;
            int rows = Mathf.CeilToInt(size.y / diagTile.y) + 4;
            int needed = diagCols * rows;
            while (tiles.Count < needed) tiles.Add(CreateTile());
            for (int n = 0; n < tiles.Count; n++)
            {
                tiles[n].gameObject.SetActive(n < needed);
                tiles[n].sprite = sprite;
                ((RectTransform)tiles[n].transform).sizeDelta = diagTile;
            }
        }

        void UpdateCorner(Vector2 size, float dt)
        {
            var trt = (RectTransform)tiles[0].transform;
            tiles[0].color = source.color;
            trt.localScale = Vector3.one;
            Vector2 travel = bigSize - size;
            float dist = travel.magnitude;
            if (dist <= 0.01f || Mathf.Abs(speed) < 0.01f) { trt.anchoredPosition = Vector2.zero; return; }

            // Viewing the top-right of the picture means the picture sits shifted left+down.
            float sx = (startCorner == Corner.TopRight || startCorner == Corner.BottomRight) ? -1f : 1f;
            float sy = (startCorner == Corner.TopRight || startCorner == Corner.TopLeft) ? -1f : 1f;
            Vector2 from = new Vector2(sx * travel.x * 0.5f, sy * travel.y * 0.5f);

            travelTime += dt;
            float leg = dist / Mathf.Abs(speed);
            float t = Mathf.Repeat(travelTime, (leg + edgePause) * 2f);
            float k;
            if (t < leg) k = t / leg;
            else if (t < leg + edgePause) k = 1f;
            else if (t < leg * 2f + edgePause) k = 1f - (t - leg - edgePause) / leg;
            else k = 0f;
            k = Mathf.SmoothStep(0f, 1f, k);
            trt.anchoredPosition = Vector2.Lerp(from, -from, k);
        }

        void Rebuild(Sprite sprite, Vector2 size)
        {
            dirty = false;
            lastSprite = sprite;
            lastSize = size;
            if (size.x <= 0f || size.y <= 0f) return;

            bool horizontal = axis == ScrollAxis.Horizontal;
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            if (mode == ScrollMode.CornerToCorner)
            {
                float fill = Mathf.Max(size.x / sprite.rect.width, size.y / sprite.rect.height) * cornerZoom * Mathf.Max(1f, scale);
                bigSize = new Vector2(sprite.rect.width, sprite.rect.height) * fill;
                while (tiles.Count < 1) tiles.Add(CreateTile());
                for (int i = 0; i < tiles.Count; i++)
                {
                    tiles[i].gameObject.SetActive(i == 0);
                    tiles[i].sprite = sprite;
                    ((RectTransform)tiles[i].transform).sizeDelta = bigSize;
                }
                return;
            }
            if (mode == ScrollMode.EdgeToEdge)
            {
                // Cover-fit so the picture always overflows the screen along the scroll axis.
                float cover = Mathf.Max(size.x / sprite.rect.width, size.y / sprite.rect.height);
                Vector2 big = new Vector2(sprite.rect.width, sprite.rect.height) * cover * Mathf.Max(1f, scale);
                if (horizontal && big.x < size.x * 1.15f) big *= size.x * 1.15f / big.x; // guarantee some travel
                if (!horizontal && big.y < size.y * 1.15f) big *= size.y * 1.15f / big.y;
                tileLength = horizontal ? big.x : big.y;
                while (tiles.Count < 1) tiles.Add(CreateTile());
                for (int i = 0; i < tiles.Count; i++)
                {
                    tiles[i].gameObject.SetActive(i == 0);
                    tiles[i].sprite = sprite;
                    ((RectTransform)tiles[i].transform).sizeDelta = big;
                }
                return;
            }
            if (axis == ScrollAxis.Diagonal) { RebuildDiagonal(sprite, size); return; }
            Vector2 tileSize = horizontal
                ? new Vector2(size.y * scale * aspect, size.y * scale)
                : new Vector2(size.x * scale, size.x * scale / aspect);
            tileLength = Mathf.Max(1f, horizontal ? tileSize.x : tileSize.y);

            float length = horizontal ? size.x : size.y;
            int needed = Mathf.CeilToInt(length / tileLength) + 3;
            if (mirrorAlternateTiles && needed % 2 == 1) needed++;

            while (tiles.Count < needed) tiles.Add(CreateTile());
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                tile.gameObject.SetActive(i < needed);
                tile.sprite = sprite;
                var trt = (RectTransform)tile.transform;
                trt.sizeDelta = tileSize;
            }
        }

        Image CreateTile()
        {
            var go = new GameObject("Tile", typeof(RectTransform), typeof(Image));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;
            return img;
        }

        void SetTilesVisible(bool visible)
        {
            foreach (var t in tiles) if (t != null) t.gameObject.SetActive(visible);
            if (visible) dirty = true;
        }

        void OnDestroy()
        {
            foreach (var t in tiles) if (t != null) Destroy(t.gameObject);
            tiles.Clear();
        }
    }
}
