using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.UI;
using DemonIdol.VFX;
using DemonIdol.Audio;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Hit and guard presentation. Pooled VFX only. No per-hit allocation.
    /// Portrait, video, and quote hooks stay empty until Izno fills them.
    /// </summary>
    public static class BBContactPresentation
    {
        public static int ComboCount { get; private set; }
        public static int LastHitEvents { get; private set; }
        public static int LastGuardEvents { get; private set; }
        public static string LastSuperCharacterId { get; private set; }

        public static event System.Action<string, string> OnPortraitHook;
        public static event System.Action<string, string> OnVideoHook;
        public static event System.Action<string> OnQuoteHook;

        public static void RaisePortraitHook(string id, string token) => OnPortraitHook?.Invoke(id, token);
        public static void RaiseVideoHook(string id, string token) => OnVideoHook?.Invoke(id, token);
        public static void RaiseQuoteHook(string text) => OnQuoteHook?.Invoke(text);

        static readonly Vector3[] _hitPoints = new Vector3[8];
        static int _hitCursor;

        public static void ResetCounters()
        {
            ComboCount = 0;
            LastHitEvents = 0;
            LastGuardEvents = 0;
            LastSuperCharacterId = null;
        }

        public static void OnCleanHit(Vector3 point, BBFighterBrain attacker, bool counterHit, bool isSuper)
        {
            LastHitEvents++;
            ComboCount++;
            _hitPoints[_hitCursor] = point;
            _hitCursor = (_hitCursor + 1) % _hitPoints.Length;

            float scale = counterHit ? 1.6f : 1f;
            if (attacker != null && attacker.currentAttack != null)
            {
                if (attacker.currentAttack.button == BBButton.C) scale += 0.35f;
                if (isSuper) scale += 0.8f;
            }
            if (SettingsPanel.ReducedGore) scale *= 0.35f;

            if (Application.isPlaying && !SettingsPanel.ReducedGore)
            {
                var ink = InkBloodSplatterManager.Instance;
                Color color = counterHit ? ink.toriiVermilionColor : ink.sumieInkColor;
                ink.SpawnAirborneBurst(point, color, scale);
                ink.SpawnGroundDecal(new Vector3(point.x, 0.02f, 0f), color, scale);
            }

            if (FighterCamera2D.Instance != null)
            {
                if (isSuper) FighterCamera2D.Instance.AddExplosionTrauma(0.9f);
                else if (counterHit) FighterCamera2D.Instance.AddCounterHitTrauma(0.7f);
                else FighterCamera2D.Instance.AddHitTrauma(0.35f * scale);
            }

            if (isSuper && attacker != null)
            {
                string id = attacker.fighter != null && attacker.fighter.appliedDefinition != null
                    ? attacker.fighter.appliedDefinition.id
                    : attacker.profile != null ? attacker.profile.characterId : "";
                LastSuperCharacterId = id;
                OnPortraitHook?.Invoke(id, "super");
                OnVideoHook?.Invoke(id, "super");
                // Super flash raised on startup in BBFighterBrain.StartAttack per spec 1.7
            }
        }

        public static void OnGuard(Vector3 point, bool chip, bool guardCrush)
        {
            LastGuardEvents++;
            ComboCount = 0;
            if (!Application.isPlaying) return;
            float radius = guardCrush ? 3.2f : chip ? 2.2f : 1.6f;
            ShockwaveDistortionEmitter.EmitPooled(point, radius, guardCrush ? 0.36f : 0.24f);
        }

        public static void OnWhiff()
        {
            ComboCount = 0;
        }

        public static void RaiseWinner(string characterId)
        {
            OnPortraitHook?.Invoke(characterId, "winner");
            OnVideoHook?.Invoke(characterId, "victory");
            OnQuoteHook?.Invoke(characterId);
        }
    }

    public static class BBSuperFlash
    {
        public static string ActiveCharacterId { get; private set; }
        public static int FreezeTicks { get; private set; }
        public static Transform ActiveAttacker { get; private set; }

        private static GameObject s_OverlayGo;
        private static UnityEngine.UI.Image s_OverlayImage;

        public static void Raise(string characterId, Transform attacker = null)
        {
            if (SettingsPanel.ReducedFlashing) return;
            ActiveCharacterId = characterId;
            ActiveAttacker = attacker;
            FreezeTicks = 20;

            // Audio: Super Flash SFX & Domain activation
            if (BBAudioBridge.Instance != null)
            {
                BBAudioBridge.Instance.PlayCombatSound(BBSoundEffect.DistortionSuperFlash, attacker != null ? attacker.position : Vector3.zero);
            }
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayDomainActivation();
                CombatAudioDirector.Instance.PlayVoiceDomainSuper();
            }

            // Camera: Zoom-flash attacker
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.TriggerHitImpactZoom(0.5f, 0.35f);
            }

            // HUD Layer: Dim the background
            ShowOverlay();
        }

        public static void Tick()
        {
            if (FreezeTicks > 0)
            {
                FreezeTicks--;
                if (FreezeTicks <= 0)
                {
                    HideOverlay();
                }
            }
        }

        public static void ForceReset()
        {
            FreezeTicks = 0;
            HideOverlay();
        }

        private static void ShowOverlay()
        {
            if (!Application.isPlaying) return;
            EnsureOverlay();
            if (s_OverlayGo != null)
            {
                s_OverlayGo.SetActive(true);
            }
        }

        private static void HideOverlay()
        {
            if (s_OverlayGo != null)
            {
                s_OverlayGo.SetActive(false);
            }
        }

        private static void EnsureOverlay()
        {
            if (s_OverlayGo != null) return;

            Canvas targetCanvas = BBMatchBootstrap.FindBattleCanvas();
            if (targetCanvas == null)
            {
                var canvasGo = new GameObject("BBSuperFlashCanvas");
                UnityEngine.Object.DontDestroyOnLoad(canvasGo);
                targetCanvas = canvasGo.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 998;
                canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            }

            s_OverlayGo = new GameObject("BBSuperFlashDimOverlay");
            s_OverlayGo.transform.SetParent(targetCanvas.transform, false);
            s_OverlayGo.transform.SetAsFirstSibling();

            var rect = s_OverlayGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            s_OverlayImage = s_OverlayGo.AddComponent<UnityEngine.UI.Image>();
            s_OverlayImage.color = new Color(0f, 0f, 0f, 0.70f);
            s_OverlayImage.raycastTarget = false;
        }
    }
}
