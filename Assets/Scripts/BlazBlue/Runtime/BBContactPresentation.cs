using System.Collections.Generic;
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
        public static Vector3 LastContactPoint { get; set; }
        public static Vector3 LastSparkPosition { get; set; }
        public static bool HasContactThisTick { get; set; }

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
            LastContactPoint = Vector3.zero;
            LastSparkPosition = Vector3.zero;
            HasContactThisTick = false;
        }

        public static void OnCleanHit(Vector3 point, BBFighterBrain attacker, bool counterHit, bool isSuper)
        {
            LastHitEvents++;
            ComboCount++;
            LastContactPoint = point;
            LastSparkPosition = point;
            HasContactThisTick = true;
            _hitPoints[_hitCursor] = point;
            _hitCursor = (_hitCursor + 1) % _hitPoints.Length;

            float scale = counterHit ? 1.6f : 1f;
            bool isHeavy = false;
            if (attacker != null && attacker.currentAttack != null)
            {
                if (attacker.currentAttack.button == BBButton.B) scale += 0.2f;
                if (attacker.currentAttack.button == BBButton.C || attacker.currentAttack.button == BBButton.D)
                {
                    scale += 0.45f;
                    isHeavy = true;
                }
                if (isSuper)
                {
                    scale += 0.9f;
                    isHeavy = true;
                }
            }
            if (SettingsPanel.ReducedGore) scale *= 0.35f;

            // Spawn tier-scaled hit spark
            SpawnHitSpark(point, scale, isSuper, isHeavy);

            // Audio impact SFX
            if (CombatAudioDirector.Instance != null)
            {
                if (isSuper)
                {
                    CombatAudioDirector.Instance.PlayHitHeavy();
                    CombatAudioDirector.Instance.PlayVoiceDomainSuper();
                }
                else if (isHeavy)
                {
                    CombatAudioDirector.Instance.PlayHitHeavy();
                }
                else if (attacker != null && attacker.currentAttack != null && attacker.currentAttack.button == BBButton.B)
                {
                    CombatAudioDirector.Instance.PlayHitMedium();
                }
                else
                {
                    CombatAudioDirector.Instance.PlayHitLight();
                }
            }

            if (BBAudioBridge.Instance != null)
            {
                BBSoundEffect sfx = isSuper ? BBSoundEffect.DistortionSuperFlash :
                                    isHeavy ? BBSoundEffect.HitHeavy :
                                    (attacker != null && attacker.currentAttack != null && attacker.currentAttack.button == BBButton.B) ? BBSoundEffect.HitMedium :
                                    BBSoundEffect.HitLight;
                BBAudioBridge.Instance.PlayCombatSound(sfx, point);
            }

            if (Application.isPlaying && !SettingsPanel.ReducedGore)
            {
                var ink = InkBloodSplatterManager.Instance;
                if (ink != null)
                {
                    Color color = counterHit ? ink.toriiVermilionColor : ink.sumieInkColor;
                    ink.SpawnAirborneBurst(point, color, scale);
                    ink.SpawnGroundDecal(new Vector3(point.x, 0.02f, 0f), color, scale);
                }
            }

            if (FighterCamera2D.Instance != null)
            {
                if (isSuper) FighterCamera2D.Instance.AddTrauma(0.9f);
                else if (isHeavy) FighterCamera2D.Instance.AddTrauma(0.35f);
                else FighterCamera2D.Instance.AddTrauma(0.15f);
            }

            if (isSuper && attacker != null)
            {
                string id = attacker.fighter != null && attacker.fighter.appliedDefinition != null
                    ? attacker.fighter.appliedDefinition.id
                    : attacker.profile != null ? attacker.profile.characterId : "";
                LastSuperCharacterId = id;
                OnPortraitHook?.Invoke(id, "super");
                OnVideoHook?.Invoke(id, "super");
                BBSuperFlash.Raise(id, attacker.transform);
            }
        }

        private static readonly List<GameObject> s_SparkPool = new List<GameObject>();

        private static GameObject GetPooledSpark(GameObject prefab, Vector3 point, Quaternion rot, Vector3 scale)
        {
            if (prefab == null) return null;
            for (int i = 0; i < s_SparkPool.Count; i++)
            {
                var s = s_SparkPool[i];
                if (s != null && !s.activeInHierarchy && s.name.StartsWith(prefab.name))
                {
                    s.transform.position = point;
                    s.transform.rotation = rot;
                    s.transform.localScale = scale;
                    s.SetActive(true);
                    return s;
                }
            }

            var instance = Object.Instantiate(prefab, point, rot);
            instance.name = prefab.name + "_Pooled";
            instance.transform.localScale = scale;
            s_SparkPool.Add(instance);
            return instance;
        }

        public static GameObject SpawnHitSpark(Vector3 point, float scale, bool isSuper, bool isHeavy)
        {
            LastSparkPosition = point;
            GameObject sparkPrefab = null;

            var rdb = DemonIdol.Data.MDIRosterDatabase.Instance;
            if (rdb != null)
            {
                sparkPrefab = isSuper ? rdb.hitSparkSuperPrefab : (isHeavy ? rdb.hitSparkHeavyPrefab : rdb.hitSparkLightPrefab);
            }

#if UNITY_EDITOR
            if (sparkPrefab == null)
            {
                if (isSuper)
                    sparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Fire/CFXR4 Sword Hit FIRE (Cross).prefab")
                        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab");
                else if (isHeavy)
                    sparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab");
                else
                    sparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab");
            }
#endif
            if (sparkPrefab != null)
            {
                Vector3 targetScale = Vector3.one * Mathf.Clamp(scale, 0.8f, 2.5f);
                return GetPooledSpark(sparkPrefab, point, Quaternion.identity, targetScale);
            }
            return null;
        }

        public static GameObject SpawnGuardSpark(Vector3 point, bool guardCrush)
        {
            GameObject sparkPrefab = null;
            var rdb = DemonIdol.Data.MDIRosterDatabase.Instance;
            if (rdb != null)
            {
                sparkPrefab = rdb.guardSparkPrefab;
            }

#if UNITY_EDITOR
            if (sparkPrefab == null)
            {
                sparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab");
            }
#endif
            if (sparkPrefab != null)
            {
                Vector3 targetScale = Vector3.one * (guardCrush ? 1.5f : 1.0f);
                return GetPooledSpark(sparkPrefab, point, Quaternion.identity, targetScale);
            }
            return null;
        }

        public static void OnGuard(Vector3 point, bool chip, bool guardCrush)
        {
            LastGuardEvents++;
            ComboCount = 0;
            SpawnGuardSpark(point, guardCrush);

            if (CombatAudioDirector.Instance != null)
            {
                if (guardCrush)
                    CombatAudioDirector.Instance.PlayParry();
                else
                    CombatAudioDirector.Instance.PlayBlock();
            }

            if (BBAudioBridge.Instance != null)
            {
                BBAudioBridge.Instance.PlayCombatSound(guardCrush ? BBSoundEffect.BarrierShatter : BBSoundEffect.BlockNormal, point);
            }

            if (!Application.isPlaying) return;
            float radius = guardCrush ? 3.2f : chip ? 2.2f : 1.6f;
            ShockwaveDistortionEmitter.EmitPooled(point, radius, guardCrush ? 0.36f : 0.24f);
        }

        public static void OnWhiff()
        {
            ComboCount = 0;
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlaySwingLight();
            }
        }

        public static void OnAttackSwing(BBFighterBrain attacker)
        {
            if (attacker == null || attacker.currentAttack == null) return;

            bool isHeavyOrSpecial = attacker.currentAttack.button == BBButton.C
                || attacker.currentAttack.button == BBButton.D
                || attacker.currentAttack.kind != BBMoveKind.Normal;

            if (CombatAudioDirector.Instance != null)
            {
                if (isHeavyOrSpecial)
                    CombatAudioDirector.Instance.PlaySwingHeavy();
                else
                    CombatAudioDirector.Instance.PlaySwingLight();
            }

            if (BBAudioBridge.Instance != null && attacker.fighter != null)
            {
                var archetype = BBRosterArchetype.Get(attacker.fighter.appliedDefinition != null
                    ? attacker.fighter.appliedDefinition.id
                    : attacker.fighter.fighterName);
                BBAudioBridge.Instance.PlayWeaponSwing(archetype.warrior, attacker.transform.position);
            }
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

            // Camera: Zoom-flash attacker (subtle if reduced flashing)
            if (FighterCamera2D.Instance != null && !SettingsPanel.ReducedFlashing)
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
            EnsureOverlay();
            if (s_OverlayGo != null)
            {
                if (s_OverlayImage != null)
                {
                    s_OverlayImage.color = new Color(0f, 0f, 0f, SettingsPanel.ReducedFlashing ? 0.35f : 0.75f);
                }
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
