using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.UI;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Visual
{
    /// <summary>
    /// Phase 4: In-Match AMV Fight-Feel Accents (JJK Kinetic AMV Direction).
    /// Features:
    /// - Counter Hit / Guard Crush: 2-3 frame stark Monochrome + KineticRed silhouette flash + micro-chromatic pulse
    /// - Dash / Overdrive / Rapid Cancel: Pooled Cyan / Vermilion silhouette afterimage ghosts
    /// - Distortion Drive / Burst: Glass-shatter screen-fracture overlay + character cut-in
    /// - Match-Winning KO: 0.35s dramatic slow-mo impact flash with monochrome/x-ray shock frame
    /// Strictly presentation-only: preserves 100% deterministic BBMatchSim physics.
    /// Strictly reduced-motion safe: respects SettingsPanel.ReducedMotion and ReducedFlashing.
    /// </summary>
    public class AMVImpactFx : MonoBehaviour
    {
        public static AMVImpactFx Instance { get; private set; }

        private static GameObject s_CanvasGo;
        private static Canvas s_Canvas;
        private static Image s_ImpactFlashImage;
        private static Image s_GlassFractureImage;
        private static GameObject s_CutInBannerRoot;
        private static Text s_CutInNameText;
        private static Text s_CutInSubText;

        // Pooled Afterimage Ghost Objects
        private static readonly List<GameObject> s_GhostPool = new List<GameObject>(16);

        private Coroutine _flashRoutine;
        private Coroutine _superCutInRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStaticState()
        {
            s_GhostPool.Clear();
            s_CanvasGo = null;
            s_Canvas = null;
            s_ImpactFlashImage = null;
            s_GlassFractureImage = null;
            s_CutInBannerRoot = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureCanvas();
        }

        public static void EnsureCanvas()
        {
            if (s_CanvasGo != null) return;

            s_CanvasGo = new GameObject("AMV_Impact_Canvas");
            if (Application.isPlaying) DontDestroyOnLoad(s_CanvasGo);

            s_Canvas = s_CanvasGo.AddComponent<Canvas>();
            s_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            s_Canvas.sortingOrder = 999;

            var scaler = s_CanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (Instance == null)
            {
                Instance = s_CanvasGo.AddComponent<AMVImpactFx>();
            }

            // Impact Flash Overlay
            var flashObj = new GameObject("AMV_FlashOverlay");
            flashObj.transform.SetParent(s_CanvasGo.transform, false);
            var flashRect = flashObj.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.sizeDelta = Vector2.zero;
            s_ImpactFlashImage = flashObj.AddComponent<Image>();
            s_ImpactFlashImage.color = Color.clear;
            s_ImpactFlashImage.raycastTarget = false;

            // Glass Fracture Overlay
            var glassObj = new GameObject("AMV_GlassFracture");
            glassObj.transform.SetParent(s_CanvasGo.transform, false);
            var glassRect = glassObj.AddComponent<RectTransform>();
            glassRect.anchorMin = Vector2.zero;
            glassRect.anchorMax = Vector2.one;
            glassRect.sizeDelta = Vector2.zero;
            s_GlassFractureImage = glassObj.AddComponent<Image>();
            s_GlassFractureImage.color = Color.clear;
            s_GlassFractureImage.raycastTarget = false;
            glassObj.SetActive(false);

            // Distortion Cut-in Banner
            s_CutInBannerRoot = new GameObject("AMV_CutInBanner");
            s_CutInBannerRoot.transform.SetParent(s_CanvasGo.transform, false);
            var bannerRect = s_CutInBannerRoot.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0f, 0.40f);
            bannerRect.anchorMax = new Vector2(1f, 0.60f);
            bannerRect.sizeDelta = Vector2.zero;

            var bannerBg = s_CutInBannerRoot.AddComponent<Image>();
            bannerBg.color = SumiStyleTokens.CarbonInkWash;
            bannerBg.raycastTarget = false;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var nameObj = new GameObject("Txt_FighterCutIn");
            nameObj.transform.SetParent(s_CutInBannerRoot.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 0.5f);
            nameRect.anchoredPosition = new Vector2(0f, 25f);
            nameRect.sizeDelta = new Vector2(1000f, 60f);
            s_CutInNameText = nameObj.AddComponent<Text>();
            s_CutInNameText.font = font;
            s_CutInNameText.fontSize = 52;
            s_CutInNameText.fontStyle = FontStyle.Bold;
            s_CutInNameText.alignment = TextAnchor.MiddleCenter;
            s_CutInNameText.color = SumiStyleTokens.KineticYellow;

            var subObj = new GameObject("Txt_DomainSub");
            subObj.transform.SetParent(s_CutInBannerRoot.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.5f);
            subRect.anchorMax = new Vector2(1f, 0.5f);
            subRect.anchoredPosition = new Vector2(0f, -25f);
            subRect.sizeDelta = new Vector2(1000f, 40f);
            s_CutInSubText = subObj.AddComponent<Text>();
            s_CutInSubText.font = font;
            s_CutInSubText.fontSize = 26;
            s_CutInSubText.fontStyle = FontStyle.Bold;
            s_CutInSubText.alignment = TextAnchor.MiddleCenter;
            s_CutInSubText.color = SumiStyleTokens.KineticRed;

            s_CutInBannerRoot.SetActive(false);
        }

        // =========================================================================
        // 1. COUNTER HIT / GUARD CRUSH (2-3f Monochrome + KineticRed Flash)
        // =========================================================================
        public static void TriggerCounterOrCrushFlash(Vector3 point, bool isCounterHit, bool isGuardCrush)
        {
            EnsureCanvas();
            if (Instance == null) return;

            Color flashColor = isCounterHit ? SumiStyleTokens.KineticRed :
                               isGuardCrush ? SumiStyleTokens.KineticYellow :
                               Color.white;

            float duration = SettingsPanel.ReducedMotion ? 0.04f : 0.08f; // ~2-3 frames at 60Hz
            if (Instance._flashRoutine != null) Instance.StopCoroutine(Instance._flashRoutine);
            Instance._flashRoutine = Instance.StartCoroutine(Instance.DoMonochromeFlash(flashColor, duration));
        }

        public static void TriggerDemonParryFlash(Vector3 point)
        {
            EnsureCanvas();
            if (Instance == null) return;

            Color flashColor = new Color(1f, 0.92f, 0.35f, 1f); // Gold-white
            float duration = SettingsPanel.ReducedMotion ? 0.08f : 0.18f; // ~0.18s Demon Idol silhouette flash
            if (Instance._flashRoutine != null) Instance.StopCoroutine(Instance._flashRoutine);
            Instance._flashRoutine = Instance.StartCoroutine(Instance.DoMonochromeFlash(flashColor, duration));
        }

        private IEnumerator DoMonochromeFlash(Color targetColor, float duration)
        {
            if (s_ImpactFlashImage == null) yield break;

            // Frame 1: Stark inverted white / monochrome shock
            s_ImpactFlashImage.color = new Color(1f, 1f, 1f, SettingsPanel.ReducedFlashing ? 0.25f : 0.85f);
            yield return new WaitForSecondsRealtime(duration * 0.4f);

            // Frame 2-3: Kinetic primary color pulse
            s_ImpactFlashImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, SettingsPanel.ReducedFlashing ? 0.20f : 0.70f);
            yield return new WaitForSecondsRealtime(duration * 0.6f);

            s_ImpactFlashImage.color = Color.clear;
            _flashRoutine = null;
        }

        // =========================================================================
        // 2. DASH / OVERDRIVE / RAPID CANCEL (Pooled Silhouette Afterimage Ghosts)
        // =========================================================================
        public static void SpawnAfterimageGhost(Transform sourceTransform, Color ghostColor, float lifetime = 0.22f)
        {
            if (sourceTransform == null || SettingsPanel.ReducedMotion) return;

            GameObject ghost = GetPooledGhost();
            ghost.transform.position = sourceTransform.position;
            ghost.transform.rotation = sourceTransform.rotation;
            ghost.transform.localScale = sourceTransform.localScale;
            ghost.SetActive(true);

            if (Instance != null)
            {
                Instance.StartCoroutine(Instance.FadeGhostRoutine(ghost, ghostColor, lifetime));
            }
        }

        private static GameObject GetPooledGhost()
        {
            for (int i = 0; i < s_GhostPool.Count; i++)
            {
                if (s_GhostPool[i] != null && !s_GhostPool[i].activeInHierarchy)
                {
                    return s_GhostPool[i];
                }
            }

            GameObject newGhost = GameObject.CreatePrimitive(PrimitiveType.Quad);
            newGhost.name = "AMV_GhostInstance";
            var col = newGhost.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            if (Application.isPlaying) DontDestroyOnLoad(newGhost);
            s_GhostPool.Add(newGhost);
            return newGhost;
        }

        private IEnumerator FadeGhostRoutine(GameObject ghost, Color ghostColor, float lifetime)
        {
            MeshRenderer mr = ghost.GetComponent<MeshRenderer>();
            Material mat = mr != null ? mr.material : null;

            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0.65f, 0f, elapsed / lifetime);
                if (mat != null)
                {
                    mat.color = new Color(ghostColor.r, ghostColor.g, ghostColor.b, alpha);
                }
                yield return null;
            }

            ghost.SetActive(false);
        }

        // =========================================================================
        // 3. DISTORTION DRIVE (236236C) / BURST (Glass Fracture + Character Cut-In)
        // =========================================================================
        public static void TriggerDistortionCutIn(string characterId)
        {
            EnsureCanvas();
            if (Instance == null) return;

            if (Instance._superCutInRoutine != null) Instance.StopCoroutine(Instance._superCutInRoutine);
            Instance._superCutInRoutine = Instance.StartCoroutine(Instance.DoSuperCutInSequence(characterId));
        }

        private IEnumerator DoSuperCutInSequence(string characterId)
        {
            var def = CharacterRegistry.GetCharacterById(characterId);
            string charName = def != null ? def.displayName.ToUpperInvariant() : characterId.ToUpperInvariant();
            string domainName = def != null && !string.IsNullOrEmpty(def.domainName) ? $"DOMAIN: {def.domainName.ToUpperInvariant()}" : "DOMAIN EXPANSION";

            if (s_CutInNameText != null) s_CutInNameText.text = charName;
            if (s_CutInSubText != null) s_CutInSubText.text = domainName;

            if (s_GlassFractureImage != null)
            {
                s_GlassFractureImage.gameObject.SetActive(true);
                s_GlassFractureImage.color = new Color(0.9f, 0.95f, 1f, 0.45f);
            }

            if (s_CutInBannerRoot != null)
            {
                s_CutInBannerRoot.SetActive(true);
            }

            float duration = SettingsPanel.ReducedMotion ? 0.08f : 0.35f;
            yield return new WaitForSecondsRealtime(duration);

            if (s_GlassFractureImage != null) s_GlassFractureImage.gameObject.SetActive(false);
            if (s_CutInBannerRoot != null) s_CutInBannerRoot.SetActive(false);
            _superCutInRoutine = null;
        }

        // =========================================================================
        // 4. MATCH-WINNING KO (0.35s Slow-mo Impact Flash)
        // =========================================================================
        public static void TriggerKODramaticFlash(bool isHeavyOrSuper)
        {
            EnsureCanvas();
            if (Instance == null) return;

            float duration = SettingsPanel.ReducedMotion ? 0.05f : 0.35f;
            Color koFlash = isHeavyOrSuper ? SumiStyleTokens.KineticRed : Color.white;
            if (Instance._flashRoutine != null) Instance.StopCoroutine(Instance._flashRoutine);
            Instance._flashRoutine = Instance.StartCoroutine(Instance.DoMonochromeFlash(koFlash, duration));
        }
    }
}
