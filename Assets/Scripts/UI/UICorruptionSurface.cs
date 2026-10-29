using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstJobber
{
    [DisallowMultipleComponent]
    public class UICorruptionSurface : MonoBehaviour
    {
        [SerializeField] UICorruptionSettings settings;
        [SerializeField] UICorruptionRole role;
        [Tooltip("New ink is parented here. Never moved or recolored by this component.")]
        [SerializeField] RectTransform inkHost;
        [Tooltip("Optional nonessential label to echo. Do not assign dialogue body, speaker name, salary or HR portrait.")]
        [SerializeField] TMP_Text textSource;
        [Tooltip("Decorative Image whose CURRENT sprite is copied for horizontal print tears. Never assign an HR portrait or video.")]
        [SerializeField] Image tearSource;
        [Tooltip("Existing binding controlling Tear Source; may live on the button root rather than its Background child.")]
        [SerializeField] AtmosphereSpriteBinding tearPhaseBinding;
        [SerializeField, Range(0, 2)] float intensityMultiplier = 1;
        [SerializeField] string stableId = "paper";
        [SerializeField] Rect inkRegion = new Rect(.06f, .2f, .64f, .6f);
        [Tooltip("Optional entrance/exit notifications. The source's tween/RectTransform remains untouched.")]
        [SerializeField] UIVisualMotion motionSource;
        ChoiceState state;
        UICorruptionSettings.PhaseStyle profile;
        UIInkCorruptionGraphic ink;
        TextMeshProUGUI ghost; // First echo retained for diagnostics.
        readonly TextMeshProUGUI[] echoes = new TextMeshProUGUI[3];
        readonly Vector2[] echoRest = new Vector2[3];
        readonly Color[] echoColor = new Color[3];
        readonly bool[] echoRegistered = new bool[3];
        UIHorizontalTearGraphic tear;
        bool tearing;
        CanvasGroup[] visibilityGroups;
        Vector2 restGhost;
        uint seed;
        float severity;
        Coroutine schedule;
        Sequence burst;
        Tween delayedEntrance;
        WaitForSecondsRealtime wait;
        static readonly System.Random Random = new System.Random(); // Independent of gameplay RNG.
        static float nextGlobalBurst;
        static UICorruptionSurface lastBurst;
        public float CurrentSeverity => severity;
        public uint CurrentSeed => seed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { nextGlobalBurst = 0; lastBurst = null; }
        void OnEnable()
        {
            visibilityGroups = GetComponentsInParent<CanvasGroup>(true);
            AtmosphereManager.PhaseChanged += OnPhase;
            UICorruptionSettings.Changed += OnSettings;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            if (motionSource != null) motionSource.PresentationStarted += OnEntrance;
            Refresh();
        }
        void OnDisable()
        {
            AtmosphereManager.PhaseChanged -= OnPhase;
            UICorruptionSettings.Changed -= OnSettings;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            if (motionSource != null) motionSource.PresentationStarted -= OnEntrance;
            StopPresentation(); HideLayers();
        }
        void OnDestroy()
        {
            StopPresentation();
            // Only objects created/owned by this component; pooling and phase changes never destroy them.
            if (ink != null) Destroy(ink.gameObject);
            for (int i = 0; i < echoes.Length; i++) if (echoes[i] != null) Destroy(echoes[i].gameObject);
            if (tear != null) Destroy(tear.gameObject);
        }
        public void BindChoice(string id, ChoiceState choiceState)
        {
            bool changed = stableId != id || state != choiceState;
            stableId = id; state = choiceState;
            if (changed) { Refresh(); TryBurst(); }
            else if (severity > 0) SyncGhost();
        }
        void OnPhase(AtmospherePhaseData phase) { Refresh(); }
        void OnSettings(UICorruptionSettings value) { if (value == settings) Refresh(); }
        void OnTextChanged(UnityEngine.Object changed) { if (changed == textSource && severity > 0 && isActiveAndEnabled) SyncGhost(); }
        void OnEntrance(float delay)
        {
            delayedEntrance?.Kill(); delayedEntrance = null;
            if (severity <= 0 || settings == null || settings.reduceMotion || !settings.enableTransientGlitches) return;
            if (delay <= 0) TryBurst();
            else delayedEntrance = DOVirtual.DelayedCall(delay, () => { delayedEntrance = null; TryBurst(); }, true);
        }
        [ContextMenu("Refresh corruption (Play Mode only)")]
        public void Refresh()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            StopPresentation();
            seed = UICorruptionSettings.StableSeed(stableId);
            profile = settings != null ? settings.Resolve(AtmosphereManager.Instance != null ? AtmosphereManager.Instance.AppliedPhase : null) : null;
            severity = settings != null ? settings.Severity(profile, state, role, intensityMultiplier) : 0;
            if (severity <= 0) { HideLayers(); return; }
            EnsureLayers();
            if (ink != null)
            {
                ink.gameObject.SetActive(true);
                ink.Configure(seed, severity * profile.redactionStrength * LayerOpacity, state == ChoiceState.Consumed, inkRegion, settings.inkColor,
                    role == UICorruptionRole.Paper || role == UICorruptionRole.Decoration);
            }
            SyncGhost(); ResetBurst();
            if (settings.enableTransientGlitches && !settings.reduceMotion)
            {
                if (wait == null) wait = new WaitForSecondsRealtime(1);
                schedule = StartCoroutine(Schedule());
            }
        }
        // Functional options keep quieter permanent echoes; bursts reveal their full ink density.
        // Consumed/decorative surfaces retain the severe permanent treatment.
        float StaticEchoWeight => role == UICorruptionRole.Choice && state != ChoiceState.Consumed ? .35f : 1f;
        static Color WithAlpha(Color color, float weight) { color.a *= weight; return color; }
        float LayerOpacity => state == ChoiceState.Consumed ? settings.consumedRemnantOpacity : 1;
        void EnsureLayers()
        {
            if (inkHost != null && ink == null)
            {
                var go = new GameObject("CorruptionInk (visual only)", typeof(RectTransform), typeof(CanvasRenderer));
                var rt = (RectTransform)go.transform; rt.SetParent(inkHost, false); Stretch(rt);
                go.layer = inkHost.gameObject.layer;
                go.AddComponent<LayoutElement>().ignoreLayout = true;
                ink = go.AddComponent<UIInkCorruptionGraphic>();
            }
            if (textSource != null && ghost == null)
            {
                // A bounded pool, allocated once. Phase/locale/category changes reuse these layers.
                for (int i = 0; i < echoes.Length; i++)
                {
                    var go = new GameObject("PrintEcho_" + (i + 1) + " (visual only)", typeof(RectTransform), typeof(CanvasRenderer));
                    var rt = (RectTransform)go.transform; rt.SetParent(textSource.rectTransform, false); Stretch(rt);
                    go.layer = textSource.gameObject.layer;
                    go.AddComponent<LayoutElement>().ignoreLayout = true;
                    echoes[i] = go.AddComponent<TextMeshProUGUI>(); echoes[i].raycastTarget = false; echoes[i].maskable = true;
                }
                ghost = echoes[0];
            }
            if (tearSource != null && tear == null)
            {
                var go = new GameObject("HorizontalPrintTear (visual only)", typeof(RectTransform), typeof(CanvasRenderer));
                var rt = (RectTransform)go.transform; rt.SetParent(tearSource.rectTransform, false);
                rt.pivot = tearSource.rectTransform.pivot; Stretch(rt); // Same local geometry, including non-centered author pivots.
                go.layer = tearSource.gameObject.layer;
                go.AddComponent<LayoutElement>().ignoreLayout = true;
                tear = go.AddComponent<UIHorizontalTearGraphic>(); tear.raycastTarget = false;
                // Use exactly the existing current-phase exemption policy on the new copy, too.
                go.AddComponent<AtmosphereOverlayMask>().Configure(tearPhaseBinding != null ? tearPhaseBinding : tearSource.GetComponent<AtmosphereSpriteBinding>());
                go.SetActive(false);
            }
        }
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        void SyncGhost()
        {
            if (ghost == null || textSource == null || profile == null) return;
            int count = Mathf.Clamp(profile.ghostLayers, 0, echoes.Length);
            float gain = UICorruptionSettings.VisualGain(severity);
            for (int i = 0; i < echoes.Length; i++)
            {
                var echo = echoes[i];
                bool active = i < count && textSource.gameObject.activeInHierarchy && profile.ghostOpacity > 0;
                echo.gameObject.SetActive(active);
                if (!active) continue;
                if (echo.text != textSource.text) echo.text = textSource.text;
                if (echo.font != textSource.font) echo.font = textSource.font;
                if (echo.fontSharedMaterial != textSource.fontSharedMaterial) echo.fontSharedMaterial = textSource.fontSharedMaterial;
                if (echo.fontSize != textSource.fontSize) echo.fontSize = textSource.fontSize;
                echo.fontStyle = textSource.fontStyle;
                echo.enableAutoSizing = textSource.enableAutoSizing;
                echo.fontSizeMin = textSource.fontSizeMin; echo.fontSizeMax = textSource.fontSizeMax;
                echo.alignment = textSource.alignment; echo.textWrappingMode = textSource.textWrappingMode;
                echo.overflowMode = textSource.overflowMode; echo.margin = textSource.margin;
                echo.richText = textSource.richText; echo.isRightToLeftText = textSource.isRightToLeftText;
                echo.characterSpacing = textSource.characterSpacing; echo.wordSpacing = textSource.wordSpacing;
                echo.lineSpacing = textSource.lineSpacing; echo.paragraphSpacing = textSource.paragraphSpacing;
                echo.spriteAsset = textSource.spriteAsset; echo.maxVisibleCharacters = textSource.maxVisibleCharacters;
                Color c = i == 1 ? settings.ghostRed : i == 2 ? settings.ghostBlue : settings.inkColor;
                c.a *= profile.ghostOpacity * gain * LayerOpacity / (1f + i * .25f);
                echoColor[i] = c; echo.color = WithAlpha(c, StaticEchoWeight);
                float direction = (UICorruptionSettings.Sample(seed, 19) > .5f ? 1 : -1) * (i == 1 ? -1 : 1);
                echoRest[i] = new Vector2(direction * profile.textOffset * gain * (1f + i * .35f),
                    profile.textOffset * gain * (UICorruptionSettings.Sample(seed, 20 + i) - .5f) * .25f);
                echoRest[i] *= StaticEchoWeight;
                echo.rectTransform.anchoredPosition = echoRest[i];
                if (!echoRegistered[i]) { echoRegistered[i] = true; ThaiTextApplier.Register(echo); }
            }
            restGhost = echoRest[0];
        }
        bool Visible()
        {
            if (!gameObject.activeInHierarchy || inkHost == null || !inkHost.gameObject.activeInHierarchy) return false;
            // RectMask2D culls scrolled-out labels. Hidden list items must not consume the shared burst budget.
            if (textSource != null && (!textSource.isActiveAndEnabled || textSource.canvasRenderer.cull)) return false;
            if (textSource == null && tearSource != null && (!tearSource.isActiveAndEnabled || tearSource.canvasRenderer.cull)) return false;
            if (visibilityGroups != null) for (int i = 0; i < visibilityGroups.Length; i++)
                if (visibilityGroups[i] != null && visibilityGroups[i].alpha < .05f) return false;
            return true;
        }
        IEnumerator Schedule()
        {
            while (true)
            {
                float low = Mathf.Max(1, profile.burstInterval.x), high = Mathf.Max(low, profile.burstInterval.y);
                wait.Reset(); // A phase change can cancel a previous wait before it expires.
                wait.waitTime = Mathf.Lerp(low, high, (float)Random.NextDouble());
                yield return wait;
                TryBurst();
            }
        }
        [ContextMenu("Preview one burst (Play Mode, current phase only)")]
        public void TryBurst() { StartBurst(false); }
        [ContextMenu("Force visible preview burst (Play Mode, ignores cooldown only)")]
        public void PreviewBurst() { StartBurst(true); }
        public bool CanPreview => Application.isPlaying && isActiveAndEnabled && severity > 0 && settings != null
            && settings.enableTransientGlitches && !settings.reduceMotion && burst == null && Visible();
        [ContextMenu("Log corruption target and current severity")]
        void LogTarget() { Debug.Log("[UI Corruption] " + name + " id=" + stableId + " severity=" + severity + " visible=" + Visible() + " tear=" + (tearSource != null), this); }
        bool StartBurst(bool manual)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || severity <= 0 || profile == null || settings == null
                || !settings.enableTransientGlitches || settings.reduceMotion || burst != null || !Visible()) return false;
            float now = Time.unscaledTime;
            if (!manual && (now < nextGlobalBurst || lastBurst == this && now < nextGlobalBurst + 1f)) return false;
            nextGlobalBurst = now + 1f / Mathf.Clamp(settings.maximumGlitchFrequency, .05f, 1f); lastBurst = this;
            float gain = UICorruptionSettings.VisualGain(severity);
            float amount = profile.jitterPixels * gain;
            float shift = ((float)Random.NextDouble() > .5f ? 1 : -1) * amount;
            float low = profile.burstDurationRange.x > 0 ? profile.burstDurationRange.x : profile.burstDuration;
            float high = Mathf.Max(low, profile.burstDurationRange.y);
            float seconds = Mathf.Clamp(Mathf.Lerp(low, high, (float)Random.NextDouble()), .08f, .6f);
            int stages = Mathf.Clamp(profile.burstComplexity, 1, 3);
            tearing = tear != null && profile.horizontalTearFrequency > 0 && (manual || Random.NextDouble() < profile.horizontalTearFrequency);
            float firstWeight = .7f + .6f * (float)Random.NextDouble();
            float secondWeight = .7f + .6f * (float)Random.NextDouble();
            float thirdWeight = .7f + .6f * (float)Random.NextDouble();
            float totalWeight = firstWeight + (stages > 1 ? secondWeight : 0) + (stages > 2 ? thirdWeight : 0);
            burst = DOTween.Sequence().SetUpdate(true);
            for (int i = 0; i < stages; i++)
            {
                float offset = shift * (i == 0 ? .7f : i == 1 ? -.35f : 1.2f);
                float stageTime = seconds * (i == 0 ? firstWeight : i == 1 ? secondWeight : thirdWeight) / totalWeight;
                float hold = .6f + .2f * (float)Random.NextDouble();
                burst.AppendCallback(() => BurstFrame(offset, severity));
                burst.AppendInterval(stageTime * hold);
                burst.AppendCallback(ResetBurst);
                burst.AppendInterval(stageTime * (1f - hold));
            }
            burst.OnComplete(() => { burst = null; ResetBurst(); });
            return true;
        }
        void BurstFrame(float offset, float inkAmount)
        {
            if (!Visible()) { ResetBurst(); return; }
            for (int i = 0; i < echoes.Length; i++)
                if (echoes[i] != null)
                {
                    echoes[i].color = echoColor[i];
                    echoes[i].rectTransform.anchoredPosition = echoRest[i] + new Vector2(offset * (i == 1 ? -.8f : 1f), 0);
                }
            if (ink != null) { ink.rectTransform.anchoredPosition = new Vector2(offset, 0); ink.SetBurst(inkAmount); }
            if (tearing && tear != null && tearSource != null)
            {
                float direction = offset < 0 ? -1f : 1f;
                tear.gameObject.SetActive(true); tear.enabled = true;
                tear.Configure(tearSource, direction * profile.horizontalTearStrength * UICorruptionSettings.VisualGain(severity), seed, .95f);
            }
        }
        void ResetBurst()
        {
            for (int i = 0; i < echoes.Length; i++) if (echoes[i] != null)
            {
                echoes[i].rectTransform.anchoredPosition = echoRest[i];
                echoes[i].color = WithAlpha(echoColor[i], StaticEchoWeight);
            }
            if (ink != null) { ink.rectTransform.anchoredPosition = Vector2.zero; ink.SetBurst(0); }
            if (tear != null) tear.enabled = false; // Keep the stencil material cached between burst stages.
        }
        void StopPresentation()
        {
            if (schedule != null) StopCoroutine(schedule);
            schedule = null; burst?.Kill(); burst = null; ResetBurst();
            delayedEntrance?.Kill(); delayedEntrance = null;
        }
        void HideLayers()
        {
            if (ink != null) ink.gameObject.SetActive(false);
            for (int i = 0; i < echoes.Length; i++) if (echoes[i] != null) echoes[i].gameObject.SetActive(false);
            if (tear != null) tear.gameObject.SetActive(false);
        }
    }
}
