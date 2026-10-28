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
        [SerializeField, Range(0, 2)] float intensityMultiplier = 1;
        [SerializeField] string stableId = "paper";
        [SerializeField] Rect inkRegion = new Rect(.06f, .2f, .64f, .6f);
        [Tooltip("Optional entrance/exit notifications. The source's tween/RectTransform remains untouched.")]
        [SerializeField] UIVisualMotion motionSource;
        ChoiceState state;
        UICorruptionSettings.PhaseStyle profile;
        UIInkCorruptionGraphic ink;
        TextMeshProUGUI ghost;
        CanvasGroup[] visibilityGroups;
        Vector2 restGhost;
        uint seed;
        float severity;
        bool registeredGhost;
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
            if (ghost != null) Destroy(ghost.gameObject);
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
                var go = new GameObject("PrintEcho (visual only)", typeof(RectTransform), typeof(CanvasRenderer));
                var rt = (RectTransform)go.transform; rt.SetParent(textSource.rectTransform, false); Stretch(rt);
                go.layer = textSource.gameObject.layer;
                go.AddComponent<LayoutElement>().ignoreLayout = true;
                ghost = go.AddComponent<TextMeshProUGUI>(); ghost.raycastTarget = false; ghost.maskable = true; registeredGhost = false;
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
            ghost.gameObject.SetActive(textSource.gameObject.activeInHierarchy && profile.ghostOpacity > 0);
            // Assign only differing content/properties. No Update/string corruption/TMP rebuilding loop.
            if (ghost.text != textSource.text) ghost.text = textSource.text;
            if (ghost.font != textSource.font) ghost.font = textSource.font;
            if (ghost.fontSharedMaterial != textSource.fontSharedMaterial) ghost.fontSharedMaterial = textSource.fontSharedMaterial;
            if (ghost.fontSize != textSource.fontSize) ghost.fontSize = textSource.fontSize;
            ghost.fontStyle = textSource.fontStyle;
            ghost.enableAutoSizing = textSource.enableAutoSizing;
            ghost.fontSizeMin = textSource.fontSizeMin; ghost.fontSizeMax = textSource.fontSizeMax;
            ghost.alignment = textSource.alignment; ghost.textWrappingMode = textSource.textWrappingMode;
            ghost.overflowMode = textSource.overflowMode; ghost.margin = textSource.margin;
            ghost.richText = textSource.richText; ghost.isRightToLeftText = textSource.isRightToLeftText;
            ghost.characterSpacing = textSource.characterSpacing; ghost.wordSpacing = textSource.wordSpacing;
            ghost.lineSpacing = textSource.lineSpacing; ghost.paragraphSpacing = textSource.paragraphSpacing;
            ghost.spriteAsset = textSource.spriteAsset;
            ghost.maxVisibleCharacters = textSource.maxVisibleCharacters;
            Color c = settings.inkColor; c.a *= profile.ghostOpacity * severity * LayerOpacity; ghost.color = c;
            float direction = UICorruptionSettings.Sample(seed, 19) > .5f ? 1 : -1;
            restGhost = new Vector2(direction * profile.textOffset * severity, profile.textOffset * severity * (UICorruptionSettings.Sample(seed, 20) - .5f) * .25f);
            ghost.rectTransform.anchoredPosition = restGhost;
            if (!registeredGhost)
            {
                registeredGhost = true;
                // Reuse the same immediate Thai font/glyph/word-break path as dynamic choice labels.
                ThaiTextApplier.Register(ghost);
            }
        }
        bool Visible()
        {
            if (!gameObject.activeInHierarchy) return false;
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
        public void TryBurst()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || severity <= 0 || profile == null || settings == null
                || !settings.enableTransientGlitches || settings.reduceMotion || burst != null || !Visible()) return;
            float now = Time.unscaledTime;
            if (now < nextGlobalBurst || lastBurst == this && now < nextGlobalBurst + 1f) return;
            nextGlobalBurst = now + 1f / Mathf.Clamp(settings.maximumGlitchFrequency, .05f, 1f); lastBurst = this;
            float amount = profile.jitterPixels * severity;
            float shift = ((float)Random.NextDouble() > .5f ? 1 : -1) * amount;
            float seconds = Mathf.Clamp(profile.burstDuration, .08f, .6f);
            burst = DOTween.Sequence().SetUpdate(true)
                .AppendCallback(() => BurstFrame(shift, severity))
                .AppendInterval(seconds * .45f)
                .AppendCallback(() => BurstFrame(-shift * .4f, severity * .45f))
                .AppendInterval(seconds * .55f)
                .OnComplete(() => { burst = null; ResetBurst(); });
        }
        void BurstFrame(float offset, float inkAmount)
        {
            if (ghost != null) ghost.rectTransform.anchoredPosition = restGhost + new Vector2(offset, 0);
            if (ink != null) { ink.rectTransform.anchoredPosition = new Vector2(offset, 0); ink.SetBurst(inkAmount); }
        }
        void ResetBurst()
        {
            if (ghost != null) ghost.rectTransform.anchoredPosition = restGhost;
            if (ink != null) { ink.rectTransform.anchoredPosition = Vector2.zero; ink.SetBurst(0); }
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
            if (ghost != null) ghost.gameObject.SetActive(false);
        }
    }
}
