using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.UI;
using DemonIdol.Audio;
using DemonIdol.VFX;
using DemonIdol.Input;
using DemonIdol.Launch;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Master match director for the clean homegrown Demon Idol fighting engine.
    /// Orchestrates match initialization, character customization, camera binding,
    /// HUD wiring, and referee execution without any UFE dependencies.
    /// </summary>
    public class HomegrownCombatDirector : MonoBehaviour
    {
        private static HomegrownCombatDirector _instance;
        public static HomegrownCombatDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HomegrownCombatDirector>();
                    if (_instance == null)
                    {
                        var go = new GameObject("HomegrownCombatDirector");
                        _instance = go.AddComponent<HomegrownCombatDirector>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Fighter Instances")]
        [SerializeField] private FighterController _fighter1;
        [SerializeField] private FighterController _fighter2;

        [Header("Match Context")]
        private string _activeP1Id = "C12";
        private string _activeP2Id = "C07";
        private string _activeStageId = "ST01";
        private bool _p2IsCpu = true;
        private bool _isTraining = false;

        public FighterController Fighter1 => _fighter1;
        public FighterController Fighter2 => _fighter2;
        public string ActiveP1Id => _activeP1Id;
        public string ActiveP2Id => _activeP2Id;
        public string ActiveStageId => _activeStageId;

        public struct MatchLaunchRoster
        {
            public string P1Id;
            public string P2Id;
            public string StageId;
            public bool P2IsCpu;
            public bool IsTraining;
        }

        /// <summary>
        /// A match begun from the menu (MatchId set) uses that session.
        /// A fresh session still has the C01/C02 defaults and no MatchId, so PlayerPrefs win.
        /// </summary>
        public static MatchLaunchRoster ResolveLaunchRoster()
        {
            var roster = new MatchLaunchRoster
            {
                P1Id = "C01",
                P2Id = "C02",
                StageId = "ST07",
                P2IsCpu = true,
                IsTraining = false
            };

            OfflineMatchSession session = LaunchServices.Session;
            bool sessionBegun = session != null && !string.IsNullOrEmpty(session.MatchId);
            if (sessionBegun)
            {
                if (!string.IsNullOrEmpty(session.Player1Id)) roster.P1Id = session.Player1Id;
                if (!string.IsNullOrEmpty(session.Player2Id)) roster.P2Id = session.Player2Id;
                if (!string.IsNullOrEmpty(session.StageId)) roster.StageId = session.StageId;
                roster.P2IsCpu = session.P2IsCpu;
                roster.IsTraining = session.Mode == PlayModeKind.Training;
            }
            else
            {
                string p1Pref = PlayerPrefs.GetString("MDI_P1_CharacterId", "");
                string p2Pref = PlayerPrefs.GetString("MDI_P2_CharacterId", "");
                string stagePref = PlayerPrefs.GetString("MDI_StageId", "");
                string modePref = PlayerPrefs.GetString("MDI_GameMode", "");
                if (!string.IsNullOrEmpty(p1Pref)) roster.P1Id = p1Pref;
                if (!string.IsNullOrEmpty(p2Pref)) roster.P2Id = p2Pref;
                if (!string.IsNullOrEmpty(stagePref)) roster.StageId = stagePref;
                if (!string.IsNullOrEmpty(modePref))
                {
                    PlayModeKind mode = OfflineMatchSession.ParseMode(modePref);
                    roster.P2IsCpu = mode != PlayModeKind.LocalVersus;
                    roster.IsTraining = mode == PlayModeKind.Training;
                }
            }

            if (CharacterRegistry.GetCharacterById(roster.P1Id) == null)
                roster.P1Id = "C01";
            if (CharacterRegistry.GetCharacterById(roster.P2Id) == null)
                roster.P2Id = "C02";
            return roster;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DemonIdol.Audio.CombatAudioDirector.DestroyDuplicateComponent(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            CombatTickEngine.EnsureRunning();
            EnsureFighterReferences();
            InitializeHomegrownMatch();
            if (MatchRoundReferee.Instance != null && MatchRoundReferee.Instance.State == MatchState.Inactive)
            {
                MatchRoundReferee.Instance.StartMatch();
            }
        }

        public void SetMatchContext(string p1Id, string p2Id, string stageId, bool p2IsCpu = true, bool isTraining = false)
        {
            _activeP1Id = string.IsNullOrEmpty(p1Id) ? "C01" : p1Id;
            _activeP2Id = string.IsNullOrEmpty(p2Id) ? "C02" : p2Id;
            _activeStageId = string.IsNullOrEmpty(stageId) ? "ST01" : stageId;
            _p2IsCpu = p2IsCpu;
            _isTraining = isTraining;
        }

        public void EnsureFighterReferences()
        {
            if (_fighter1 == null || _fighter2 == null)
            {
                var fighters = FindObjectsByType<FighterController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var f in fighters)
                {
                    // Ignore child fighters nested inside other fighters
                    if (f.transform.parent != null && f.transform.parent.GetComponent<FighterController>() != null) continue;

                    if (f.isPlayerControlled && _fighter1 == null) _fighter1 = f;
                    else if (!f.isPlayerControlled && _fighter2 == null) _fighter2 = f;
                }

                if (_fighter1 == null && fighters.Length > 0)
                {
                    foreach (var f in fighters)
                    {
                        if (f.transform.parent != null && f.transform.parent.GetComponent<FighterController>() != null) continue;
                        _fighter1 = f;
                        break;
                    }
                }
                if (_fighter2 == null && fighters.Length > 1)
                {
                    foreach (var f in fighters)
                    {
                        if (f.transform.parent != null && f.transform.parent.GetComponent<FighterController>() != null) continue;
                        if (f != _fighter1) { _fighter2 = f; break; }
                    }
                }
            }

            if (_fighter1 == null)
            {
                _fighter1 = SpawnRuntimeFighter("Player1", _activeP1Id, new Vector3(-2.5f, 0, 0), Quaternion.Euler(0, 90f, 0), true);
            }

            if (_fighter2 == null)
            {
                _fighter2 = SpawnRuntimeFighter("Player2", _activeP2Id, new Vector3(2.5f, 0, 0), Quaternion.Euler(0, -90f, 0), false);
            }

            if (_fighter1 != null) _fighter1.gameObject.SetActive(true);
            if (_fighter2 != null) _fighter2.gameObject.SetActive(true);
        }

        public void InitializeHomegrownMatch()
        {
            EnsureFighterReferences();

            MatchLaunchRoster roster = ResolveLaunchRoster();
            _activeP1Id = roster.P1Id;
            _activeP2Id = roster.P2Id;
            _activeStageId = roster.StageId;
            _p2IsCpu = roster.P2IsCpu;
            _isTraining = roster.IsTraining;

            // 2. Configure P1
            if (_fighter1 != null)
            {
                ConfigureFighter(_fighter1, _activeP1Id, true);
            }

            // 3. Configure P2
            if (_fighter2 != null)
            {
                ConfigureFighter(_fighter2, _activeP2Id, !_p2IsCpu);
                if (_p2IsCpu)
                {
                    if (_isTraining)
                    {
                        var dummy = _fighter2.GetComponent<TrainingDummyAI>();
                        if (dummy == null) dummy = _fighter2.gameObject.AddComponent<TrainingDummyAI>();
                        dummy.enabled = true;

                        var ai = _fighter2.GetComponent<CombatAIController>();
                        if (ai != null) ai.enabled = false;

                        var dojo = GetComponent<PracticeDojoController>();
                        if (dojo == null) dojo = gameObject.AddComponent<PracticeDojoController>();
                        dojo.enabled = true;
                    }
                    else
                    {
                        var dummy = _fighter2.GetComponent<TrainingDummyAI>();
                        if (dummy != null) dummy.enabled = false;

                        var ai = _fighter2.GetComponent<CombatAIController>();
                        if (ai == null) ai = _fighter2.gameObject.AddComponent<CombatAIController>();
                        ai.ConfigurePersonalityFromFighter();
                        ai.enabled = true;
                    }
                }
            }

            // 4. Link opponents for facing and collision
            if (_fighter1 != null && _fighter2 != null)
            {
                _fighter1.opponent = _fighter2;
                _fighter2.opponent = _fighter1;
            }

            // 5. Bind Camera (Direct 2D Framing, zero vendor interference)
            if (FighterCamera2D.Instance != null && _fighter1 != null && _fighter2 != null)
            {
                FighterCamera2D.Instance.SetTargets(_fighter1.transform, _fighter2.transform);
                FighterCamera2D.Instance.ResetTrauma();
                FighterCamera2D.Instance.ReturnToBaseCombatFrame();
            }

            // 6. Bind Touch Virtual Controls
            if (TouchVirtualControls.Instance != null && _fighter1 != null)
            {
                TouchVirtualControls.Instance.BindFighter(_fighter1);
            }

            // 7. Bind HUD
            if (BattleHUDController.Instance != null && _fighter1 != null && _fighter2 != null)
            {
                BattleHUDController.Instance.BindFighters(_fighter1, _fighter2);
                string p1Name = CharacterRegistry.GetCharacterById(_activeP1Id)?.displayName ?? _activeP1Id;
                string p2Name = CharacterRegistry.GetCharacterById(_activeP2Id)?.displayName ?? _activeP2Id;
                BattleHUDController.Instance.SetFighterNames(p1Name, p2Name);

                if (MatchRoundReferee.Instance != null)
                {
                    BattleHUDController.Instance.SubscribeReferee(MatchRoundReferee.Instance);
                }
            }

            // 8. Bind Referee
            if (MatchRoundReferee.Instance != null && _fighter1 != null && _fighter2 != null)
            {
                MatchRoundReferee.Instance.BindFighters(_fighter1, _fighter2);
                MatchRoundReferee.Instance.isTrainingMode = _isTraining;
            }

            // BlazBlue is the only match path. Classic input stays in the project and is turned off here.
            DemonIdol.BlazBlue.BBMatchBootstrap.EnsureMatch(_isTraining);
            var screens = DemonIdol.BlazBlue.BBMatchScreenDirector.Instance;
            if (screens == null)
            {
                var screenHost = new GameObject("BBMatchScreenDirector");
                screens = screenHost.AddComponent<DemonIdol.BlazBlue.BBMatchScreenDirector>();
            }
            if (!_isTraining)
                screens.PlayVersus(_activeP1Id, _activeP2Id, _activeStageId, _p2IsCpu ? "Arcade" : "Versus");

            // 9. Bind Hit Events to VFX & Audio
            BindHitEvents();

            // 10. Configure Stage Bounds & Visuals
            var stage = StageRegistry.GetStageById(_activeStageId);
            if (stage != null)
            {
                PushbackSolver.SetArenaBounds(stage.ArenaLimitX, stage.wallThreshold);
                Transform t1 = _fighter1 != null ? _fighter1.transform : null;
                Transform t2 = _fighter2 != null ? _fighter2.transform : null;

                if (DemonIdol.CameraControl.StageCameraManager.Instance != null)
                {
                    DemonIdol.CameraControl.StageCameraManager.Instance.SwitchToStageCamera(_activeStageId, t1, t2);
                }
                else if (FighterCamera2D.Instance != null)
                {
                    FighterCamera2D.Instance.ApplyStageCameraLimits(stage);
                }

                if (stage.stagePrefab != null || string.Equals(_activeStageId, "ST07", StringComparison.OrdinalIgnoreCase))
                {
                    var arenaRoot = GameObject.Find("Arcade_Stage_Root");
                    if (arenaRoot != null)
                    {
                        var oldDiorama = arenaRoot.transform.Find("Backdrop_Diorama");
                        GameObject pfab = stage.stagePrefab;
#if UNITY_EDITOR
                        if (pfab == null)
                        {
                            pfab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stages/ST07_CollegeRaimon.prefab");
                        }
#endif
                        if (pfab != null && (oldDiorama == null || oldDiorama.gameObject.name != pfab.name))
                        {
                            if (oldDiorama != null)
                            {
                                SafeDestroy(oldDiorama.gameObject);
                            }
                            var newDiorama = Instantiate(pfab, arenaRoot.transform);
                            newDiorama.name = "Backdrop_Diorama";
                            newDiorama.transform.localPosition = Vector3.zero;
                            newDiorama.transform.localRotation = Quaternion.identity;
                        }

                        var zoneAnchor = arenaRoot.GetComponentInChildren<CampusZoneAnchor>();
                        if (zoneAnchor != null)
                        {
                            zoneAnchor.ApplyZone(_activeStageId);
                        }
                    }
                }
            }
        }

        public static void ConfigureFighter(FighterController fc, string charId, bool isPlayer)
        {
            if (fc == null) return;
            fc.isPlayerControlled = isPlayer;

            string canonical = CharacterRegistry.ResolveCanonicalId(charId);
            if (!string.IsNullOrEmpty(canonical))
                charId = canonical;
            if (CharacterRegistry.GetCharacterById(charId) == null)
                charId = isPlayer ? "C01" : "C02";

            var charDef = CharacterRegistry.GetCharacterById(charId);
            if (charDef != null)
            {
                fc.ApplyCharacterDefinition(charDef);
            }

            string expectedModelName = "Model_" + (charDef != null ? charDef.id : charId);
            Transform existingModel = fc.transform.Find(expectedModelName);

            // If existing model is already present and has renderers, keep it and update animator
            if (existingModel != null && existingModel.GetComponentInChildren<Renderer>() != null)
            {
                Animator anim = existingModel.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    if (charDef != null && charDef.animatorController != null)
                    {
                        anim.runtimeAnimatorController = charDef.animatorController;
                    }
                    if (charDef != null && charDef.avatar != null)
                    {
                        anim.avatar = charDef.avatar;
                    }
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }
                fc.transform.localScale = Vector3.one;
                Vector3 p = fc.transform.position;
                if (p.y < 0f) { p.y = 0f; fc.transform.position = p; }
                return;
            }

            // Need replacement: find prefab
            GameObject prefab = charDef != null ? charDef.modelPrefab : null;
            if (prefab == null && charDef != null)
            {
                var look = FighterVisualCatalog.Find(charDef.id);
                if (look != null)
                {
#if UNITY_EDITOR
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(look.PrefabAssetPath);
#endif
                    if (prefab == null) prefab = Resources.Load<GameObject>(look.PrefabAssetPath);
                }
            }

            if (prefab == null && !string.IsNullOrEmpty(charId))
            {
#if UNITY_EDITOR
                string canonId = CharacterRegistry.ResolveCanonicalId(charId);
                string[] found = System.IO.Directory.GetFiles("Assets/Prefabs/Characters", canonId + "*.prefab", System.IO.SearchOption.AllDirectories);
                if (found != null && found.Length > 0)
                {
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(found[0]);
                }
#endif
            }

            // ONLY remove old models if we have a valid replacement prefab
            if (prefab != null)
            {
                for (int i = fc.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = fc.transform.GetChild(i);
                    if (child.name.StartsWith("Model_") || child.name.StartsWith("Model"))
                    {
                        SafeDestroy(child.gameObject);
                    }
                }

                GameObject model = Instantiate(prefab, fc.transform);
                model.name = expectedModelName;
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                // Strip duplicate logic/physics components from child model to prevent collision clash
                // Destroy FighterController FIRST so RequireComponent constraint on CharacterController is released
                foreach (var subFc in model.GetComponentsInChildren<FighterController>(true)) SafeDestroy(subFc);
                foreach (var cc in model.GetComponentsInChildren<CharacterController>(true)) SafeDestroy(cc);
                foreach (var mi in model.GetComponentsInChildren<MobileFighterInput>(true)) SafeDestroy(mi);
                foreach (var hb in model.GetComponentsInChildren<Hitbox>(true)) SafeDestroy(hb.gameObject);
                foreach (var hb in model.GetComponentsInChildren<Hurtbox>(true)) SafeDestroy(hb.gameObject);
                foreach (var pb in model.GetComponentsInChildren<FighterPropBinder>(true)) SafeDestroy(pb);

                // Strip any weapons, wands, staves, props
                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                {
                    if (t != null && t != model.transform && (
                        t.name.StartsWith("Prop_") || 
                        t.name.Contains("Staff") || 
                        t.name.Contains("Blaster") || 
                        t.name.Contains("Katana") || 
                        t.name.Contains("Sword") ||
                        t.name.Contains("MagicWand") ||
                        t.name.Contains("Wand") ||
                        t.name.Contains("Orb") ||
                        t.name.Contains("Gun")))
                    {
                        SafeDestroy(t.gameObject);
                    }
                }

                // Ensure all renderers are enabled and on the fighter layer
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.gameObject.layer = fc.gameObject.layer;
                    r.enabled = true;
                }

                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    if (charDef != null && charDef.animatorController != null)
                    {
                        anim.runtimeAnimatorController = charDef.animatorController;
                    }
                    if (charDef != null && charDef.avatar != null)
                    {
                        anim.avatar = charDef.avatar;
                    }
                    else
                    {
                        Animator pAnim = prefab.GetComponentInChildren<Animator>();
                        if (pAnim != null && pAnim.avatar != null)
                        {
                            anim.avatar = pAnim.avatar;
                        }
                    }
#if UNITY_EDITOR
                    if (anim.runtimeAnimatorController == null)
                    {
                        anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller");
                    }
#endif
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }

                // Ensure CharacterController on fighter root is properly calibrated
                var fcCc = fc.GetComponent<CharacterController>();
                if (fcCc != null)
                {
                    fcCc.center = new Vector3(0, 0.95f, 0);
                    fcCc.height = 1.9f;
                    fcCc.radius = 0.35f;
                }

                // Ensure fighter root position is at ground level
                Vector3 curPos = fc.transform.position;
                if (curPos.y < 0f || float.IsNaN(curPos.y))
                {
                    curPos.y = 0f;
                    fc.transform.position = curPos;
                }

                if (model.GetComponent<AnimationEventReceiver>() == null)
                {
                    model.AddComponent<AnimationEventReceiver>();
                }
            }

            if (fc.animator != null)
            {
                fc.animator.applyRootMotion = false;
            }

            fc.transform.localScale = Vector3.one;
        }

        private void BindHitEvents()
        {
            if (_fighter1 != null)
            {
                _fighter1.OnHitProcessed -= HandleFighterHit;
                _fighter1.OnHitProcessed += HandleFighterHit;
            }
            if (_fighter2 != null)
            {
                _fighter2.OnHitProcessed -= HandleFighterHit;
                _fighter2.OnHitProcessed += HandleFighterHit;
            }
        }

        private void HandleFighterHit(HitResult result, AttackData attack, Vector3 hitPoint)
        {
            if (result == HitResult.Whiff || result == HitResult.Crushed) return;

            bool isHeavy = attack != null && (attack.attackType == AttackType.Heavy || attack.damage >= 80f);
            bool isCounterHit = result == HitResult.CounterHit;

            // Route to HitEffectCoordinator
            if (HitEffectCoordinator.Instance != null)
            {
                float damage = attack != null ? attack.damage : 50f;
                HitEffectCoordinator.Instance.TriggerHitEffect(hitPoint, damage, isCounterHit, isHeavy);
            }

            // Route to CombatAudioDirector
            if (CombatAudioDirector.Instance != null)
            {
                if (result == HitResult.Parried) CombatAudioDirector.Instance.PlayParry();
                else if (result == HitResult.Blocked || result == HitResult.BarrierBlocked) CombatAudioDirector.Instance.PlayBlock();
                else if (isHeavy) CombatAudioDirector.Instance.PlayHitHeavy();
                else CombatAudioDirector.Instance.PlayHitLight();
            }
        }

        public void Rematch()
        {
            InitializeHomegrownMatch();
            if (MatchRoundReferee.Instance != null)
            {
                MatchRoundReferee.Instance.StartMatch();
            }
        }

        private FighterController SpawnRuntimeFighter(string fighterGoName, string charId, Vector3 pos, Quaternion rot, bool isPlayer)
        {
            GameObject existing = GameObject.Find(fighterGoName);
            if (existing != null)
            {
                var existingFc = existing.GetComponent<FighterController>();
                if (existingFc != null) return existingFc;
            }

            GameObject go = new GameObject(fighterGoName);
            go.transform.position = pos;
            go.transform.rotation = rot;

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0);
            cc.height = 1.9f;
            cc.radius = 0.35f;

            FighterController fc = go.AddComponent<FighterController>();
            fc.fighterName = fighterGoName;
            fc.isPlayerControlled = isPlayer;

            // Attack data
            fc.lightAttackData = LoadAttackDataAsset("Kai_LightAttack");
            fc.mediumAttackData = LoadAttackDataAsset("Kai_MediumAttack");
            fc.heavyAttackData = LoadAttackDataAsset("Kai_HeavyAttack");
            fc.driveSkillData = LoadAttackDataAsset("Kai_DriveSkill");
            fc.jumpAttackData = LoadAttackDataAsset("Kai_JumpAttack");
            fc.domainSuperData = LoadAttackDataAsset("Kai_DomainSuper");
            fc.throwAttackData = LoadAttackDataAsset("Kai_Throw");
            fc.lowAttackData = LoadAttackDataAsset("Kai_LowAttack");

            // Hurtbox
            GameObject hbGo = new GameObject("Hurtbox_Torso");
            hbGo.transform.SetParent(go.transform, false);
            hbGo.transform.localPosition = new Vector3(0, 1.0f, 0);
            BoxCollider hbCol = hbGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.8f, 1.8f, 0.8f);
            hbCol.isTrigger = true;
            hbGo.AddComponent<Hurtbox>();

            // Hitbox
            GameObject hitGo = new GameObject("Hitbox_Weapon");
            hitGo.transform.SetParent(go.transform, false);
            hitGo.transform.localPosition = new Vector3(0, 1.2f, 0.7f);
            BoxCollider hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.2f, 1.2f, 1.4f);
            hitCol.isTrigger = true;
            Hitbox hitbox = hitGo.AddComponent<Hitbox>();
            fc.activeHitbox = hitbox;

            if (isPlayer)
            {
                go.AddComponent<MobileFighterInput>();
            }

            // Visual model
            var charDef = CharacterRegistry.GetCharacterById(charId);
            GameObject prefab = charDef != null ? charDef.modelPrefab : null;
            if (prefab == null && charDef != null)
            {
                var look = FighterVisualCatalog.Find(charDef.id);
                if (look != null)
                {
#if UNITY_EDITOR
                    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(look.PrefabAssetPath);
#endif
                    if (prefab == null) prefab = Resources.Load<GameObject>(look.PrefabAssetPath);
                }
            }

            if (prefab != null)
            {
                string expectedModelName = "Model_" + (charDef != null ? charDef.id : charId);
                GameObject model = Instantiate(prefab, go.transform);
                model.name = expectedModelName;
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    if (charDef != null && charDef.animatorController != null)
                    {
                        anim.runtimeAnimatorController = charDef.animatorController;
                    }
#if UNITY_EDITOR
                    if (anim.runtimeAnimatorController == null)
                    {
                        anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller");
                    }
#endif
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }
                if (model.GetComponent<AnimationEventReceiver>() == null)
                {
                    model.AddComponent<AnimationEventReceiver>();
                }
            }

            if (charDef != null)
            {
                fc.ApplyCharacterDefinition(charDef);
            }

            return fc;
        }

        private AttackData LoadAttackDataAsset(string assetName)
        {
            AttackData data = Resources.Load<AttackData>($"CombatData/{assetName}");
#if UNITY_EDITOR
            if (data == null)
            {
                data = UnityEditor.AssetDatabase.LoadAssetAtPath<AttackData>($"Assets/CombatData/{assetName}.asset");
            }
#endif
            return data;
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
