using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStaticState()
        {
            s_SparkPool.Clear();
            ResetCounters();
            OnPortraitHook = null;
            OnVideoHook = null;
            OnQuoteHook = null;
        }

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

        private static Vector3 SanitizePoint(Vector3 point)
        {
            if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z) ||
                float.IsInfinity(point.x) || float.IsInfinity(point.y) || float.IsInfinity(point.z))
            {
                return Vector3.zero;
            }
            return point;
        }

        public static void OnCleanHit(Vector3 point, BBFighterBrain attacker, bool counterHit, bool isSuper)
        {
            point = SanitizePoint(point);
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
            if (BBAccessibility.ReducedGore) scale *= 0.35f;

            // Directional strike vector alignment
            Vector3 strikeDir = attacker != null ? (point - attacker.transform.position).normalized : Vector3.right;
            if (strikeDir.sqrMagnitude < 0.001f) strikeDir = Vector3.right * (attacker != null ? attacker.FacingDirection : 1);
            Quaternion strikeRot = Quaternion.LookRotation(strikeDir, Vector3.up);

            // Spawn tier-scaled hit spark
            SpawnHitSpark(point, scale, isSuper, isHeavy, strikeRot);

            // AMV Kinetic impact flash on counter hits
            if (counterHit)
            {
                DemonIdol.BlazBlue.Visual.AMVImpactFx.TriggerCounterOrCrushFlash(point, isCounterHit: true, isGuardCrush: false);
            }

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

            if (Application.isPlaying && !BBAccessibility.ReducedGore)
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

        public static void PrewarmSparkPools(int countPerType = 4)
        {
            var rdb = DemonIdol.Data.MDIRosterDatabase.Instance;
            if (rdb == null) return;

            GameObject[] prefabs = new GameObject[]
            {
                rdb.hitSparkLightPrefab,
                rdb.hitSparkHeavyPrefab,
                rdb.hitSparkSuperPrefab,
                rdb.guardSparkPrefab
            };

            for (int p = 0; p < prefabs.Length; p++)
            {
                var prefab = prefabs[p];
                if (prefab == null) continue;

                int existingCount = 0;
                for (int i = 0; i < s_SparkPool.Count; i++)
                {
                    if (s_SparkPool[i] != null && s_SparkPool[i].name.StartsWith(prefab.name))
                        existingCount++;
                }

                for (int i = existingCount; i < countPerType; i++)
                {
                    var instance = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    instance.name = prefab.name + "_Pooled";
                    var trails = instance.GetComponentsInChildren<TrailRenderer>(true);
                    for (int t = 0; t < trails.Length; t++)
                    {
                        trails[t].Clear();
                    }
                    instance.SetActive(false);
                    s_SparkPool.Add(instance);
                }
            }
        }

        private static GameObject GetPooledSpark(GameObject prefab, Vector3 point, Quaternion rot, Vector3 scale)
        {
            if (prefab == null) return null;
            point = SanitizePoint(point);

            // Prune null or destroyed instances from pool
            for (int i = s_SparkPool.Count - 1; i >= 0; i--)
            {
                if (s_SparkPool[i] == null)
                {
                    s_SparkPool.RemoveAt(i);
                }
            }

            for (int i = 0; i < s_SparkPool.Count; i++)
            {
                var s = s_SparkPool[i];
                if (s != null && !s.activeInHierarchy && s.name.StartsWith(prefab.name))
                {
                    var trails = s.GetComponentsInChildren<TrailRenderer>(true);
                    for (int t = 0; t < trails.Length; t++)
                    {
                        trails[t].Clear();
                    }
                    var systems = s.GetComponentsInChildren<ParticleSystem>(true);
                    for (int p = 0; p < systems.Length; p++)
                    {
                        systems[p].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                    s.transform.position = point;
                    s.transform.rotation = rot;
                    s.transform.localScale = scale;
                    s.SetActive(true);
                    for (int p = 0; p < systems.Length; p++)
                    {
                        systems[p].Play(true);
                    }
                    return s;
                }
            }

            var instance = Object.Instantiate(prefab, point, rot);
            instance.name = prefab.name + "_Pooled";
            instance.transform.localScale = scale;
            var newTrails = instance.GetComponentsInChildren<TrailRenderer>(true);
            for (int t = 0; t < newTrails.Length; t++)
            {
                newTrails[t].Clear();
            }
            s_SparkPool.Add(instance);
            return instance;
        }

        public static GameObject SpawnHitSpark(Vector3 point, float scale, bool isSuper, bool isHeavy, Quaternion? rotation = null)
        {
            point = SanitizePoint(point);
            LastSparkPosition = point;
            GameObject sparkPrefab = null;

            var rdb = DemonIdol.Data.MDIRosterDatabase.Instance;
            if (rdb != null)
            {
                sparkPrefab = isSuper ? rdb.hitSparkSuperPrefab : (isHeavy ? rdb.hitSparkHeavyPrefab : rdb.hitSparkLightPrefab);
            }

            if (sparkPrefab != null)
            {
                Vector3 targetScale = Vector3.one * Mathf.Clamp(scale, 0.8f, 2.5f);
                Quaternion rot = rotation.HasValue ? rotation.Value : Quaternion.identity;
                return GetPooledSpark(sparkPrefab, point, rot, targetScale);
            }
            return null;
        }

        public static GameObject SpawnGuardSpark(Vector3 point, bool guardCrush, bool isBarrier = false)
        {
            point = SanitizePoint(point);
            GameObject sparkPrefab = null;
            var rdb = DemonIdol.Data.MDIRosterDatabase.Instance;
            if (rdb != null)
            {
                sparkPrefab = guardCrush ? rdb.guardSparkPrefab : (isBarrier ? rdb.hitSparkLightPrefab : rdb.guardSparkPrefab);
            }

            if (sparkPrefab != null)
            {
                Vector3 targetScale = Vector3.one * (guardCrush ? 1.5f : (isBarrier ? 1.35f : 1.0f));
                return GetPooledSpark(sparkPrefab, point, Quaternion.identity, targetScale);
            }
            return null;
        }

        public static void OnGuard(Vector3 point, bool chip, bool guardCrush, bool isBarrier = false)
        {
            point = SanitizePoint(point);
            LastGuardEvents++;
            ComboCount = 0;
            SpawnGuardSpark(point, guardCrush, isBarrier);

            if (guardCrush)
            {
                DemonIdol.BlazBlue.Visual.AMVImpactFx.TriggerCounterOrCrushFlash(point, isCounterHit: false, isGuardCrush: true);
            }

            if (CombatAudioDirector.Instance != null)
            {
                if (guardCrush)
                    CombatAudioDirector.Instance.PlayParry();
                else
                    CombatAudioDirector.Instance.PlayBlock();
            }

            if (BBAudioBridge.Instance != null)
            {
                BBAudioBridge.Instance.PlayCombatSound(guardCrush ? BBSoundEffect.BarrierShatter : (isBarrier ? BBSoundEffect.BarrierShatter : BBSoundEffect.BlockNormal), point);
            }

            if (!Application.isPlaying) return;
            float radius = guardCrush ? 3.2f : isBarrier ? 2.8f : (chip ? 2.2f : 1.6f);
            ShockwaveDistortionEmitter.EmitPooled(point, radius, guardCrush ? 0.36f : 0.24f);
        }

        public static void OnInstantBlock(Vector3 point, BBFighterBrain defender)
        {
            point = SanitizePoint(point);
            SpawnGuardSpark(point, false, true);
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayParry();
            }
            if (BBAudioBridge.Instance != null)
            {
                BBAudioBridge.Instance.PlayCombatSound(BBSoundEffect.BarrierShatter, point);
            }
            DemonIdol.BlazBlue.Visual.AMVImpactFx.TriggerDemonParryFlash(point);
            defender?.TriggerRimFlash(new Color(0.2f, 1f, 0.9f, 1f), 2);
        }

        public static void OnWallBounce(Vector3 point)
        {
            point = SanitizePoint(point);
            SpawnHitSpark(point, 1.8f, false, true);
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayHitHeavy();
            }
            if (BBAudioBridge.Instance != null)
            {
                BBAudioBridge.Instance.PlayCombatSound(BBSoundEffect.HitHeavy, point);
            }
            DemonIdol.BlazBlue.Visual.AMVImpactFx.TriggerCounterOrCrushFlash(point, false, true);
        }

        public static void OnReversal(Vector3 point)
        {
            point = SanitizePoint(point);
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayParry();
            }
            if (BBAudioBridge.Instance != null)
            {
                BBAudioBridge.Instance.PlayCombatSound(BBSoundEffect.BarrierShatter, point);
            }
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
            if (FighterCamera2D.Instance != null && !BBAccessibility.ReducedFlashing)
            {
                FighterCamera2D.Instance.TriggerHitImpactZoom(0.5f, 0.35f);
            }

            // HUD Layer: Dim the background
            ShowOverlay();

            // AMV Distortion Cut-in & Glass Fracture
            DemonIdol.BlazBlue.Visual.AMVImpactFx.TriggerDistortionCutIn(characterId);
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
                    s_OverlayImage.color = new Color(0f, 0f, 0f, BBAccessibility.ReducedFlashing ? 0.35f : 0.75f);
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
