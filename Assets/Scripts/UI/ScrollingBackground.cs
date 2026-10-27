using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    public enum BackgroundMotif { Heart, Flower, Star, Cloud }

    [System.Serializable]
    public class BackgroundMotifSlot
    {
        public BackgroundMotif motif;
        [HideInInspector] public RawImage image; // one fixed full-screen layer for this motif type
        public Sprite normalSprite;
        public Sprite blurredSprite;
        public bool isBlurred;
        [HideInInspector] public Vector2 direction = new Vector2(-1f, -0.7f); // legacy serialized settings
        [HideInInspector] public float speed = 20f;
        public Vector2 elementSize = new Vector2(300f, 300f);
        [Tooltip("Tilt of this motif in degrees. Positive = counterclockwise. Applies to both normal and faded artwork.")]
        [Range(-180f, 180f)] public float tiltAngle;
        [Tooltip("Position inside each repeating tile, relative to its centre (-0.5 to 0.5).")]
        public Vector2 startOffset;
        [System.NonSerialized] public Vector2 position;
    }

    /// <summary>
    /// Full-screen UI background. FourMotifs keeps the base Image stationary and repeats four motif types in a GPU shader,
    /// reacting to the existing atmosphere event. Legacy modes retain their scrolling tile behavior.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectMask2D))]
    public class ScrollingBackground : MonoBehaviour
    {
        public enum ScrollAxis { Horizontal, Vertical, Diagonal }
        public enum ScrollMode { Loop, EdgeToEdge, CornerToCorner, FourMotifs }
        public enum Corner { TopRight, TopLeft, BottomRight, BottomLeft }

        [Tooltip("CornerToCorner only: the corner the camera starts on. It pans to the opposite corner and back (e.g. TopRight -> BottomLeft).")]
        [SerializeField] Corner startCorner = Corner.TopRight;
        [Tooltip("CornerToCorner only: extra zoom so there is room to travel. 1.3 = picture is 30% larger than the screen.")]
        [SerializeField, Min(1.01f)] float cornerZoom = 1.3f;

        [Tooltip("FourMotifs: infinite repeating motif wallpaper. Other modes retain the legacy single-sprite scrolling behavior.")]
        [SerializeField] ScrollMode mode = ScrollMode.Loop;
        [Tooltip("EdgeToEdge / CornerToCorner: seconds to wait at each end.")]
        [SerializeField, Min(0f)] float edgePause = 1f;

        [Tooltip("Horizontal: the sprite is fitted to the screen height and scrolls sideways. Vertical: fitted to width, scrolls up/down.")]
        [SerializeField] ScrollAxis axis = ScrollAxis.Horizontal;
        [Tooltip("Normalized direction for motif wallpaper and the legacy Diagonal axis. (1,-1) = down-right. Zero pauses motion.")]
        [SerializeField] Vector2 diagonalDirection = new Vector2(1f, -1f);
        [Tooltip("Scroll speed in canvas units per second. Wallpaper direction is normalized. 0 = paused.")]
        [SerializeField] float speed = 30f;
        [Tooltip("Zoom the tiles. 1 = exactly fills the screen on the cross axis.")]
        [SerializeField, Min(0.1f)] float scale = 1f;
        [Tooltip("Flip every second tile. Turn ON if the art is not seamless - the join becomes a mirror, so no visible seam.")]
        [SerializeField] bool mirrorAlternateTiles = false;
        [Tooltip("Keep scrolling while Time.timeScale is 0.")]
        [SerializeField] bool useUnscaledTime = true;

        [Header("Compact GPU motif wallpaper")]
        [SerializeField] Shader wallpaperShader;
        [SerializeField] bool enableScrolling = true;
        [Tooltip("Repeating pattern size in canvas units, multiplied by Pattern Spacing. Effective repeat size is at least 320x240.")]
        [SerializeField] Vector2 patternTileSize = new Vector2(960f, 640f);
        [SerializeField, Min(0.1f)] float patternSpacing = 1f;
        [SerializeField, Min(0.05f)] float elementScale = 1f;
        [SerializeField] BackgroundMotifSlot[] motifSlots = new BackgroundMotifSlot[4];
        [SerializeField] bool enableProgressionIntegration = true;
        [Tooltip("Delay between individual random replacements when an atmosphere phase increases the target count.")]
        [SerializeField, Min(0.1f)] float motifDeteriorationInterval = 6f;
        [Tooltip("0 = choose once at runtime; another value gives a repeatable random slot order.")]
        [SerializeField] int motifRandomSeed;
        [SerializeField, Range(0, 4)] int previewBlurredCount;

        readonly int[] deteriorationOrder = { 0, 1, 2, 3 };
        bool orderReady;
        RectMask2D motifMask;
        Vector4 originalMaskPadding;
        bool motifMaskCaptured;
        Vector2 wallpaperOffset;
        readonly Material[] layerMaterials = new Material[4];
        readonly Material[] originalMaterials = new Material[4];
        readonly Texture[] originalTextures = new Texture[4];
        readonly Sprite[] renderedSprites = new Sprite[4];
        readonly float[] spriteAspects = { 1f, 1f, 1f, 1f };
        struct LayerShaderState
        {
            public bool initialized;
            public Vector2 area, cell, element, motif, scroll;
            public float angle;
            public Vector2 rotation;
        }
        readonly LayerShaderState[] shaderStates = new LayerShaderState[4];
        static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
        static readonly int CellSizeId = Shader.PropertyToID("_CellSize");
        static readonly int ElementSizeId = Shader.PropertyToID("_ElementSize");
        static readonly int MotifOffsetId = Shader.PropertyToID("_MotifOffset");
        static readonly int ScrollOffsetId = Shader.PropertyToID("_ScrollOffset");
        static readonly int SpriteRectId = Shader.PropertyToID("_SpriteUVRect");
        static readonly int RotationId = Shader.PropertyToID("_MotifRotation");
        int targetBlurredCount;
        float deteriorationTimer;

        public int BlurredCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 4; i++) if (Slot(i) != null && Slot(i).isBlurred) count++;
                return count;
            }
        }
        public int BlurredMask
        {
            get
            {
                int mask = 0;
                for (int i = 0; i < 4; i++) if (Slot(i) != null && Slot(i).isBlurred) mask |= 1 << i;
                return mask;
            }
        }
        public int TargetBlurredCount => targetBlurredCount;

        Image source;
        readonly List<Image> tiles = new List<Image>();
        Sprite lastSprite;
        Vector2 lastSize;
        float offset;
        float tileLength = 1f;
        bool dirty = true;

        public float Speed { get => speed; set => speed = value; }
        public Vector2 Direction { get => diagonalDirection; set => diagonalDirection = value; }
        public Vector2 WallpaperOffset => wallpaperOffset;
        public RawImage GetMotifLayer(int index) => Slot(index)?.image;

        void Awake()
        {
            source = GetComponent<Image>();
            motifMask = GetComponent<RectMask2D>();
            if (motifMask != null) { originalMaskPadding = motifMask.padding; motifMaskCaptured = true; }
            if (mode == ScrollMode.FourMotifs) ResetBackgroundState(); // Inspector previews never leak into a new game.
        }

        void OnEnable()
        {
            dirty = true;
            AtmosphereManager.PhaseChanged += ApplyStateFromProgression;
            if (mode == ScrollMode.FourMotifs && AtmosphereManager.Instance != null)
                ApplyStateFromProgression(AtmosphereManager.Instance.AppliedPhase);
        }

        void OnDisable()
        {
            AtmosphereManager.PhaseChanged -= ApplyStateFromProgression;
            ReleaseLayerMaterials();
        }

        void OnValidate() { dirty = true; }

        void LateUpdate()
        {
            if (source == null) source = GetComponent<Image>();
            if (mode == ScrollMode.FourMotifs)
            {
                source.enabled = true; // the original Image draws the plain base, not a field of repeated tiles
                source.raycastTarget = false;
                SetTilesVisible(false);
                UpdateMotifs(useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                if (Slot(i)?.image != null) Slot(i).image.gameObject.SetActive(false);

            }
            if (motifMaskCaptured && motifMask != null) motifMask.padding = originalMaskPadding;
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

        BackgroundMotifSlot Slot(int i) => motifSlots != null && i < motifSlots.Length ? motifSlots[i] : null;

        void PrepareOrder()
        {
            if (orderReady) return;
            orderReady = true;
            var rng = new System.Random(motifRandomSeed == 0 ? Random.Range(1, int.MaxValue) : motifRandomSeed);
            for (int i = 3; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int old = deteriorationOrder[i]; deteriorationOrder[i] = deteriorationOrder[j]; deteriorationOrder[j] = old;
            }
        }

        [ContextMenu("Background/Reset All To Normal")]
        public void ResetBackgroundState() { ApplyBlurredMask(0); }

        /// <summary>Applies an exact saved per-slot state without rerolling motif identities or motion.</summary>
        public void ApplyBlurredMask(int mask)
        {
            for (int i = 0; i < 4; i++) if (Slot(i) != null) Slot(i).isBlurred = (mask & (1 << i)) != 0;
            targetBlurredCount = BlurredCount;
            deteriorationTimer = 0f;
            RenderMotifs();
        }

        /// <summary>Adjusts only the necessary slots; applying the same count again never rerolls them.</summary>
        public void SetBlurredCount(int count)
        {
            count = Mathf.Clamp(count, 0, 4);
            PrepareOrder();
            while (BlurredCount < count) if (!BlurNext()) break;
            for (int i = 3; i >= 0 && BlurredCount > count; i--)
                if (Slot(deteriorationOrder[i]) != null) Slot(deteriorationOrder[i]).isBlurred = false;
            targetBlurredCount = BlurredCount;
            deteriorationTimer = 0f;
            RenderMotifs();
        }

        bool BlurNext()
        {
            PrepareOrder();
            foreach (int i in deteriorationOrder)
            {
                var slot = Slot(i);
                if (slot == null || slot.isBlurred) continue;
                slot.isBlurred = true;
                return true;
            }
            return false;
        }

        public bool AdvanceRandomDeteriorationStep()
        {
            bool changed = BlurNext();
            targetBlurredCount = BlurredCount;
            deteriorationTimer = 0f;
            RenderMotifs();
            return changed;
        }

        [ContextMenu("Background/Advance One Random Slot")]
        void AdvanceFromInspector() { AdvanceRandomDeteriorationStep(); }

        [ContextMenu("Background/Apply Preview Blurred Count")]
        void ApplyPreview() { SetBlurredCount(previewBlurredCount); }

        [ContextMenu("Background/Restart Motif Positions")]
        public void ResetMotifPositions() { wallpaperOffset = Vector2.zero; }

        public void RefreshMotifPreview()
        {
            if (mode != ScrollMode.FourMotifs) return;
            if (Application.isPlaying) UpdateMotifs(0f);
            // Runtime-only material instances never dirty shared material assets during Inspector previews.
        }

        /// <summary>Called by the existing atmosphere event. Increasing targets are reached one slot at a time.</summary>
        public void ApplyStateFromProgression(AtmospherePhaseData phase)
        {
            if (mode != ScrollMode.FourMotifs || !enableProgressionIntegration) return;
            if (phase == null) { ResetBackgroundState(); return; }
            if (phase.blurredBackgroundElements < 0) return; // this phase does not author a background change
            int count = Mathf.Clamp(phase.blurredBackgroundElements, 0, 4);
            if (count <= BlurredCount) { SetBlurredCount(count); return; }
            if (targetBlurredCount == count) return;
            targetBlurredCount = count;
            deteriorationTimer = motifDeteriorationInterval; // first replacement on the next frame
        }

        Vector2 RepeatSize => new Vector2(Mathf.Max(320f, patternTileSize.x * Mathf.Max(0.1f, patternSpacing)),
            Mathf.Max(240f, patternTileSize.y * Mathf.Max(0.1f, patternSpacing)));

        void EnsureLayerMaterials()
        {
            if (!Application.isPlaying || wallpaperShader == null) return;
            for (int i = 0; i < 4; i++)
            {
                var layer = Slot(i)?.image;
                if (layer == null || layerMaterials[i] != null) continue;
                originalMaterials[i] = layer.material;
                originalTextures[i] = layer.texture;
                var material = new Material(wallpaperShader);
                material.hideFlags = HideFlags.DontSave;
                layerMaterials[i] = material;
                layer.material = material;
                renderedSprites[i] = null;
                shaderStates[i] = default;
            }
        }

        void RenderMotifs()
        {
            if (!Application.isPlaying || mode != ScrollMode.FourMotifs) return;
            EnsureLayerMaterials();
            Vector2 area = ((RectTransform)transform).rect.size;
            Vector2 cell = RepeatSize;
            for (int i = 0; i < 4; i++)
            {
                var slot = Slot(i);
                var material = layerMaterials[i];
                if (slot?.image == null || material == null) continue;
                var layer = slot.image;
                var sprite = slot.isBlurred ? slot.blurredSprite : slot.normalSprite;
                layer.enabled = sprite != null;
                if (sprite == null) continue;
                if (renderedSprites[i] != sprite)
                {
                    var texture = sprite.texture;
                    var rect = sprite.textureRect;
                    layer.texture = texture; // geometry/material dirtiness only on a sprite change, not each frame
                    material.SetVector(SpriteRectId, new Vector4(rect.x / texture.width, rect.y / texture.height,
                        rect.width / texture.width, rect.height / texture.height));
                    spriteAspects[i] = rect.width / Mathf.Max(1f, rect.height);
                    renderedSprites[i] = sprite;
                }
                float width = Mathf.Min(slot.elementSize.x * Mathf.Max(0.05f, elementScale), cell.x - 2f);
                float height = Mathf.Min(slot.elementSize.y * Mathf.Max(0.05f, elementScale), cell.y - 2f);
                float fitWidth = Mathf.Min(width, height * spriteAspects[i]);
                float fitHeight = fitWidth / spriteAspects[i];
                layer.color = Color.white;
                layer.raycastTarget = false;
                ref var state = ref shaderStates[i];
                bool force = !state.initialized;
                if (force || state.angle != slot.tiltAngle)
                {
                    state.angle = slot.tiltAngle;
                    double radians = slot.tiltAngle * (System.Math.PI / 180.0);
                    state.rotation = new Vector2((float)System.Math.Cos(radians), (float)System.Math.Sin(radians));
                    material.SetVector(RotationId, new Vector4(state.rotation.x, state.rotation.y, 0, 0));
                }
                // Keep the rotated artwork inside its repeating cell without changing its proportions.
                float rotatedWidth = Mathf.Abs(state.rotation.x) * fitWidth + Mathf.Abs(state.rotation.y) * fitHeight;
                float rotatedHeight = Mathf.Abs(state.rotation.y) * fitWidth + Mathf.Abs(state.rotation.x) * fitHeight;
                float rotationFit = Mathf.Min(1f, Mathf.Min((cell.x - 2f) / Mathf.Max(1f, rotatedWidth),
                    (cell.y - 2f) / Mathf.Max(1f, rotatedHeight)));
                fitWidth *= rotationFit;
                fitHeight *= rotationFit;
                SetVectorIfChanged(material, RectSizeId, area, ref state.area, force);
                SetVectorIfChanged(material, CellSizeId, cell, ref state.cell, force);
                SetVectorIfChanged(material, ElementSizeId, new Vector2(Mathf.Max(1f, fitWidth), Mathf.Max(1f, fitHeight)), ref state.element, force);
                SetVectorIfChanged(material, MotifOffsetId, new Vector2((slot.startOffset.x + 0.5f) * cell.x, (slot.startOffset.y + 0.5f) * cell.y), ref state.motif, force);
                SetVectorIfChanged(material, ScrollOffsetId, wallpaperOffset, ref state.scroll, force);
                state.initialized = true;
            }
        }

        static void SetVectorIfChanged(Material material, int id, Vector2 value, ref Vector2 previous, bool force)
        {
            if (!force && value.x == previous.x && value.y == previous.y) return;
            previous = value;
            material.SetVector(id, new Vector4(value.x, value.y, 0, 0));
        }

        void UpdateMotifs(float dt)
        {
            if (motifMask == null) motifMask = GetComponent<RectMask2D>();
            if (motifMask != null) motifMask.padding = Vector4.zero;
            if (enableProgressionIntegration && BlurredCount < targetBlurredCount)
            {
                deteriorationTimer += dt;
                if (deteriorationTimer >= Mathf.Max(0.1f, motifDeteriorationInterval))
                {
                    BlurNext();
                    deteriorationTimer = 0f;
                }
            }
            Vector2 cell = RepeatSize;
            if (enableScrolling) wallpaperOffset += diagonalDirection.normalized * speed * dt;
            wallpaperOffset = new Vector2(Mathf.Repeat(wallpaperOffset.x, cell.x), Mathf.Repeat(wallpaperOffset.y, cell.y));
            RenderMotifs();
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
            ReleaseLayerMaterials();
            foreach (var t in tiles) if (t != null) Destroy(t.gameObject);
            tiles.Clear();
        }

        void ReleaseLayerMaterials()
        {
            for (int i = 0; i < 4; i++)
            {
                var material = layerMaterials[i];
                if (material == null) continue;
                var layer = Slot(i)?.image;
                if (layer != null)
                {
                    layer.material = originalMaterials[i];
                    layer.texture = originalTextures[i];
                }
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
                layerMaterials[i] = null;
                renderedSprites[i] = null;
                shaderStates[i] = default;
            }
        }
    }
}
