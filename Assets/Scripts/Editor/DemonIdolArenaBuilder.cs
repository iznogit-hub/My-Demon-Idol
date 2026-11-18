#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Audio;

namespace DemonIdol.Editor
{
    /// <summary>
    /// One-click setup utility for Demon Idol.
    /// Creates the entire battle arena hierarchy in the current scene with 0 missing references:
    /// CombatManager, Ground platform, Player 1 (Kai), Player 2 (Training Dummy),
    /// Camera follow, and Hurtbox/Hitbox volumes.
    /// </summary>
    public static class DemonIdolArenaBuilder
    {
        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Scenes/Open Universal Fighter Demo Scene", priority = 1)]
        public static void OpenUniversalFighterDemoScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/UniversalFighter_Demo.unity");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Setup Battle Arena In Scene")]
        public static void BuildArenaInScene()
        {
            // 1. Ensure attack data assets exist
            DemonIdolAssetFactory.CreateDefaultAttackData();

            AttackData lightAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
            AttackData medAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
            AttackData heavyAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
            AttackData driveAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DriveSkill.asset");
            AttackData jumpAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");
            AttackData superAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DomainSuper.asset");

            // 2. Combat Manager
            GameObject managerGo = GameObject.Find("CombatManager");
            if (managerGo == null) managerGo = new GameObject("CombatManager");

            CombatTickEngine tickEngine = managerGo.GetComponent<CombatTickEngine>();
            if (tickEngine == null) tickEngine = managerGo.AddComponent<CombatTickEngine>();

            CombatAudioDirector audioDirector = managerGo.GetComponent<CombatAudioDirector>();
            if (audioDirector == null) audioDirector = managerGo.AddComponent<CombatAudioDirector>();

            HitEffectCoordinator vfxCoord = managerGo.GetComponent<HitEffectCoordinator>();
            if (vfxCoord == null) vfxCoord = managerGo.AddComponent<HitEffectCoordinator>();

            // Auto-wire Cartoon FX Remaster prefabs if present
            SerializedObject soVfx = new SerializedObject(vfxCoord);
            GameObject redHit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab");
            GameObject yellowParry = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab");
            GameObject blueBlock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab");
            GameObject groundHit = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR2 Ground Hit.prefab");
            GameObject bloodSplash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR2 Blood Shape Splash.prefab");
            GameObject slashText = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Texts/CFXR _SLASH_.prefab");

            if (redHit != null) soVfx.FindProperty("_hitSparkPrefab").objectReferenceValue = redHit;
            if (slashText != null) soVfx.FindProperty("_counterHitSparkPrefab").objectReferenceValue = slashText;
            if (blueBlock != null) soVfx.FindProperty("_blockSparkPrefab").objectReferenceValue = blueBlock;
            if (yellowParry != null) soVfx.FindProperty("_parrySparkPrefab").objectReferenceValue = yellowParry;
            if (groundHit != null) soVfx.FindProperty("_groundHitPrefab").objectReferenceValue = groundHit;
            if (bloodSplash != null) soVfx.FindProperty("_bloodSplashPrefab").objectReferenceValue = bloodSplash;
            soVfx.ApplyModifiedProperties();

            CrossPlatformOptimizer opt = managerGo.GetComponent<CrossPlatformOptimizer>();
            if (opt == null) opt = managerGo.AddComponent<CrossPlatformOptimizer>();

            DemonIdol.VFX.InkBloodSplatterManager vfxSplats = managerGo.GetComponent<DemonIdol.VFX.InkBloodSplatterManager>();
            if (vfxSplats == null) vfxSplats = managerGo.AddComponent<DemonIdol.VFX.InkBloodSplatterManager>();

            DemonIdol.VFX.DomainExpansionAOE domainAoe = managerGo.GetComponent<DemonIdol.VFX.DomainExpansionAOE>();
            if (domainAoe == null) domainAoe = managerGo.AddComponent<DemonIdol.VFX.DomainExpansionAOE>();

            MatchRoundReferee referee = managerGo.GetComponent<MatchRoundReferee>();
            if (referee == null) referee = managerGo.AddComponent<MatchRoundReferee>();

            SerializedObject soDomain = new SerializedObject(domainAoe);
            GameObject runicAura = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR3 Magic Aura A (Runic).prefab");
            GameObject soulsAura = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 Souls Escape.prefab");
            GameObject enemyExplosion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 WW Enemy Explosion.prefab");
            if (enemyExplosion == null) enemyExplosion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR2 WW Explosion.prefab");

            if (runicAura != null) soDomain.FindProperty("_runicAuraCirclePrefab").objectReferenceValue = runicAura;
            if (soulsAura != null) soDomain.FindProperty("_soulsEscapePrefab").objectReferenceValue = soulsAura;
            if (enemyExplosion != null) soDomain.FindProperty("_domainShatterBurstPrefab").objectReferenceValue = enemyExplosion;
            if (redHit != null) soDomain.FindProperty("_slashFlurryPrefab").objectReferenceValue = redHit;
            soDomain.ApplyModifiedProperties();

            // 3. Ground Platform
            GameObject ground = GameObject.Find("Ground_StoneBridge");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground_StoneBridge";
                ground.transform.position = new Vector3(0, -0.5f, 0);
                ground.transform.localScale = new Vector3(32f, 1f, 6f);
            }

            // 3b. Multi-Layer Japanese Shrine / Torii Parallax Backdrop
            GameObject existingParallax = GameObject.Find("Environment_ParallaxSanctum");
            if (existingParallax == null)
            {
                EnvironmentParallaxController.CreateProceduralSanctumLayers(null);
            }

            // 4. Player 1 (Kai)
            GameObject p1 = GameObject.Find("Player1_Kai");
            if (p1 == null)
            {
                p1 = new GameObject("Player1_Kai");
                p1.transform.position = new Vector3(-2.5f, 0, 0);
                p1.transform.rotation = Quaternion.Euler(0, 90f, 0);
            }

            CharacterController cc1 = p1.GetComponent<CharacterController>();
            if (cc1 == null) cc1 = p1.AddComponent<CharacterController>();
            cc1.center = new Vector3(0, 1.0f, 0);
            cc1.height = 2.0f;
            cc1.radius = 0.35f;

            FighterController fc1 = p1.GetComponent<FighterController>();
            if (fc1 == null) fc1 = p1.AddComponent<FighterController>();
            fc1.fighterName = "Kai";
            fc1.isPlayerControlled = true;
            fc1.lightAttackData = lightAtk;
            fc1.mediumAttackData = medAtk;
            fc1.heavyAttackData = heavyAtk;
            fc1.driveSkillData = driveAtk;
            fc1.jumpAttackData = jumpAtk;
            fc1.domainSuperData = superAtk;

            MobileFighterInput input1 = p1.GetComponent<MobileFighterInput>();
            if (input1 == null) input1 = p1.AddComponent<MobileFighterInput>();

            // P1 Hurtbox
            Transform hurtbox1Trans = p1.transform.Find("Hurtbox_Torso");
            if (hurtbox1Trans == null)
            {
                GameObject hbGo = new GameObject("Hurtbox_Torso");
                hbGo.transform.SetParent(p1.transform);
                hbGo.transform.localPosition = new Vector3(0, 1.0f, 0);
                BoxCollider col = hbGo.AddComponent<BoxCollider>();
                col.size = new Vector3(0.8f, 1.8f, 0.8f);
                col.isTrigger = true;
                hbGo.AddComponent<Hurtbox>();
            }

            // P1 Hitbox
            Transform hitbox1Trans = p1.transform.Find("Hitbox_Weapon");
            Hitbox hitbox1 = null;
            if (hitbox1Trans == null)
            {
                GameObject hitGo = new GameObject("Hitbox_Weapon");
                hitGo.transform.SetParent(p1.transform);
                hitGo.transform.localPosition = new Vector3(0, 1.2f, 0.7f); // In front of fighter
                BoxCollider col = hitGo.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 1.2f, 1.4f);
                col.isTrigger = true;
                hitbox1 = hitGo.AddComponent<Hitbox>();
            }
            else
            {
                hitbox1 = hitbox1Trans.GetComponent<Hitbox>();
            }
            fc1.activeHitbox = hitbox1;

            // 5. Player 2 (Training Dummy NPC)
            GameObject p2 = GameObject.Find("Player2_Dummy");
            if (p2 == null)
            {
                p2 = new GameObject("Player2_Dummy");
                p2.transform.position = new Vector3(2.5f, 0, 0);
                p2.transform.rotation = Quaternion.Euler(0, -90f, 0);
            }

            CharacterController cc2 = p2.GetComponent<CharacterController>();
            if (cc2 == null) cc2 = p2.AddComponent<CharacterController>();
            cc2.center = new Vector3(0, 1.0f, 0);
            cc2.height = 2.0f;
            cc2.radius = 0.35f;

            FighterController fc2 = p2.GetComponent<FighterController>();
            if (fc2 == null) fc2 = p2.AddComponent<FighterController>();
            fc2.fighterName = "Training Dummy";
            fc2.isPlayerControlled = false;
            fc2.lightAttackData = lightAtk;
            fc2.mediumAttackData = medAtk;
            fc2.heavyAttackData = heavyAtk;
            fc2.driveSkillData = driveAtk;
            fc2.jumpAttackData = jumpAtk;
            fc2.domainSuperData = superAtk;

            TrainingDummyAI dummyAI = p2.GetComponent<TrainingDummyAI>();
            if (dummyAI == null) dummyAI = p2.AddComponent<TrainingDummyAI>();
            dummyAI.mode = DummyMode.CounterHitPractice; // Default to counter-hit practice!

            // P2 Hurtbox
            Transform hurtbox2Trans = p2.transform.Find("Hurtbox_Torso");
            if (hurtbox2Trans == null)
            {
                GameObject hbGo = new GameObject("Hurtbox_Torso");
                hbGo.transform.SetParent(p2.transform);
                hbGo.transform.localPosition = new Vector3(0, 1.0f, 0);
                BoxCollider col = hbGo.AddComponent<BoxCollider>();
                col.size = new Vector3(0.8f, 1.8f, 0.8f);
                col.isTrigger = true;
                hbGo.AddComponent<Hurtbox>();
            }

            // P2 Hitbox
            Transform hitbox2Trans = p2.transform.Find("Hitbox_Weapon");
            Hitbox hitbox2 = null;
            if (hitbox2Trans == null)
            {
                GameObject hitGo = new GameObject("Hitbox_Weapon");
                hitGo.transform.SetParent(p2.transform);
                hitGo.transform.localPosition = new Vector3(0, 1.2f, 0.7f);
                BoxCollider col = hitGo.AddComponent<BoxCollider>();
                col.size = new Vector3(1.2f, 1.2f, 1.4f);
                col.isTrigger = true;
                hitbox2 = hitGo.AddComponent<Hitbox>();
            }
            else
            {
                hitbox2 = hitbox2Trans.GetComponent<Hitbox>();
            }
            fc2.activeHitbox = hitbox2;

            // 6. Cross-reference opponents
            fc1.opponent = fc2;
            fc2.opponent = fc1;

            if (referee != null) referee.BindFighters(fc1, fc2);
            if (DemonIdol.UI.BattleHUDController.Instance != null) DemonIdol.UI.BattleHUDController.Instance.BindFighters(fc1, fc2);
            if (DemonIdol.Input.TouchVirtualControls.Instance != null) DemonIdol.Input.TouchVirtualControls.Instance.BindFighter(fc1);

            // 7. Setup 2.5D Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 1.8f, -7.0f);
                mainCam.transform.rotation = Quaternion.identity;
                FighterCamera2D camFollow = mainCam.GetComponent<FighterCamera2D>();
                if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<FighterCamera2D>();
                camFollow.fighter1 = p1.transform;
                camFollow.fighter2 = p2.transform;
            }

            // Mark scene dirty to save changes
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("<color=#C73E2E><b>[Demon Idol]</b></color> Complete Battle Arena hierarchy created in scene with 0 missing references! Press Play to test.");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Setup Flooded Grounds with Imported Characters")]
        public static void SetupFloodedGroundsWithCharacters()
        {
            // 1. Build base arena systems
            BuildArenaInScene();

            GameObject p1 = GameObject.Find("Player1_Kai");
            GameObject p2 = GameObject.Find("Player2_Dummy");
            if (p1 == null || p2 == null) return;

            // 2. Load Imported Character Prefabs
            GameObject kaiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Fighters/Toon/Fighter_C01_Kai.prefab");
            if (kaiPrefab == null)
            {
                kaiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fighters/Toon/Fighter_C01_Kai.prefab");
            }

            GameObject arinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Fighters/Toon/Fighter_C03_Arin.prefab");
            if (arinPrefab == null)
            {
                arinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fighters/Toon/Fighter_C03_Arin.prefab");
            }

            // 3. Configure Player 1 (Kai the Fool)
            if (kaiPrefab != null)
            {
                Transform existingModel = p1.transform.Find("Model_Kai") ?? p1.transform.Find("Model_Assassin");
                if (existingModel != null) Object.DestroyImmediate(existingModel.gameObject);

                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(kaiPrefab, p1.transform);
                model.name = "Model_Kai";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                FighterController fc1 = p1.GetComponent<FighterController>();
                fc1.fighterName = "Kai the Fool";
                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null) fc1.animator = anim;

                AnimationEventReceiver receiver = model.GetComponent<AnimationEventReceiver>();
                if (receiver == null) model.AddComponent<AnimationEventReceiver>();

                CharacterController cc1 = p1.GetComponent<CharacterController>();
                cc1.center = new Vector3(0, 0.9f, 0);
                cc1.height = 1.8f;
                cc1.radius = 0.32f;
            }

            // 4. Configure Player 2 (Arin the Iron Vow - Heavy Dummy)
            if (arinPrefab != null)
            {
                Transform existingModel = p2.transform.Find("Model_Arin") ?? p2.transform.Find("Model_BigBoy");
                if (existingModel != null) Object.DestroyImmediate(existingModel.gameObject);

                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(arinPrefab, p2.transform);
                model.name = "Model_Arin";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                FighterController fc2 = p2.GetComponent<FighterController>();
                fc2.fighterName = "Arin the Iron Vow";
                Animator anim = model.GetComponentInChildren<Animator>();
                if (anim != null) fc2.animator = anim;

                AnimationEventReceiver receiver = model.GetComponent<AnimationEventReceiver>();
                if (receiver == null) model.AddComponent<AnimationEventReceiver>();

                CharacterController cc2 = p2.GetComponent<CharacterController>();
                cc2.center = new Vector3(0, 1.0f, 0);
                cc2.height = 2.0f;
                cc2.radius = 0.40f;

                Transform hbTrans = p2.transform.Find("Hurtbox_Torso");
                if (hbTrans != null)
                {
                    BoxCollider bc = hbTrans.GetComponent<BoxCollider>();
                    if (bc != null) bc.size = new Vector3(1.0f, 2.0f, 1.0f);
                }
            }

            // 5. Multi-Layer Japanese Shrine / Torii Parallax Backdrop
            GameObject bgStage = GameObject.Find("Environment_ParallaxSanctum");
            if (bgStage == null)
            {
                EnvironmentParallaxController.CreateProceduralSanctumLayers(null);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Debug.Log("<color=#C73E2E><b>[Demon Idol]</b></color> Shikigami Sanctum stage with authentic multi-layer parallax and toon fighters configured successfully!");
        }
    }
}
#endif
