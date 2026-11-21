#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DemonIdol.Combat;
using DemonIdol.UI;
using DemonIdol.Audio;
using DemonIdol.VFX;
using DemonIdol.Input;
using DemonIdol.Launch;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master Game Setup for Demon Idol: Home Scene Edition.
    /// Wires Assets/Scenes/Home.unity into the complete, authoritative Demon Idol fighting game:
    /// 1. Generates 14 CharacterDefinition & MoveSet assets for the anime sorcerer roster (C01-C14)
    /// 2. Sets up Combat Arena Platform, stage collision boundaries, and lighting
    /// 3. Arranges the 14 styled characters into a cinematic suspense showcase backdrop
    /// 4. Configures CombatManager, HomegrownCombatDirector, MatchRoundReferee, and FighterController rigs
    /// 5. Deploys Master_BloodTech_Canvas with 14-character grid, Battle HUD, and Touch Virtual Controls
    /// 6. Configures FighterCamera2D and Suspense Volume Post-Processing (Chromatic Aberration, SMAA, Dark Occult grading)
    /// </summary>
    public static class DemonIdolHomeGameSetup
    {
        private const string SCENE_PATH = "Assets/Scenes/Home.unity";
        private const string CHAR_DATA_DIR = "Assets/CombatData/Characters";
        private const string MOVESET_DATA_DIR = "Assets/CombatData/MoveSets";
        private const string MDI_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Home/Setup Demon Idol Game In Home Scene", priority = 0)]
        public static void SetupHomeSceneAsGame()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[DemonIdolHomeSetup] Please exit Play Mode before running setup.");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += SetupHomeSceneAsGame;
                return;
            }

            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: SETTING UP HOME SCENE WITH 14 CHARACTERS       </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            // 1. Open Home scene
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            // 2. Generate 14 CharacterDefinition and MoveSet Assets
            GenerateAll14CharacterAssets();

            // 3. Setup Combat Arena Ground Platform
            SetupArenaGroundPlatform();

            // 4. Arrange 14 Characters into Cinematic Suspense Roster Lineup
            OrganizeShowcaseCharacters(scene);

            // 5. Setup Combat Manager Core Systems
            SetupCombatManager();

            // 6. Setup Homegrown Combat Director & Initial Fighters
            SetupCombatDirectorAndFighters();

            // 7. Setup Master UI Canvas (14-Slot Selection, Battle HUD, Touch Controls)
            DemonIdolUIBuilder.BuildMasterUI();

            // 8. Setup Main Camera with FighterCamera2D & Suspense Post-Processing
            SetupCameraAndPostProcessing(scene);

            // 9. Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676><b>[DemonIdolHomeSetup] COMPLETE:</b> Home.unity is now the official Demon Idol game environment with 14 characters!</color>");
            Debug.Log("<color=#00e676>=============================================================</color>");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Home/Generate 14 Character & MoveSet Assets", priority = 2)]
        public static void GenerateAll14CharacterAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/CombatData"))
            {
                AssetDatabase.CreateFolder("Assets", "CombatData");
            }
            if (!AssetDatabase.IsValidFolder(CHAR_DATA_DIR))
            {
                AssetDatabase.CreateFolder("Assets/CombatData", "Characters");
            }
            if (!AssetDatabase.IsValidFolder(MOVESET_DATA_DIR))
            {
                AssetDatabase.CreateFolder("Assets/CombatData", "MoveSets");
            }

            // Ensure base attack data assets exist
            DemonIdolAssetFactory.CreateDefaultAttackData();

            var lightAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
            var medAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
            var heavyAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
            var driveAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DriveSkill.asset");
            var jumpAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");
            var superAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DomainSuper.asset");
            var throwAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_Throw.asset");
            var lowAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");

            RuntimeAnimatorController templateAnim = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);

            for (int i = 0; i < FighterRosterCatalog.Roster.Length; i++)
            {
                var profile = FighterRosterCatalog.Roster[i];
                string charId = profile.id;
                string shortName = CharacterRegistry.GetCharacterNameShort(charId);

                // 1. Create or Load MoveSet Asset
                string moveSetPath = $"{MOVESET_DATA_DIR}/{charId}_{shortName}_MoveSet.asset";
                MoveSet ms = AssetDatabase.LoadAssetAtPath<MoveSet>(moveSetPath);
                if (ms == null)
                {
                    ms = ScriptableObject.CreateInstance<MoveSet>();
                    AssetDatabase.CreateAsset(ms, moveSetPath);
                }

                ms.characterId = charId;
                ms.moveSetName = $"{profile.displayName} Combat MoveSet";
                ms.standingL = lightAtk;
                ms.lightAttack = lightAtk;
                ms.crouchingL = lowAtk;
                ms.lowSweepAttack = lowAtk;
                ms.mediumAttack = medAtk;
                ms.standingH = heavyAtk;
                ms.heavyAttack = heavyAtk;
                ms.forwardH = heavyAtk;
                ms.crouchingH = lowAtk;
                ms.neutralS = driveAtk;
                ms.driveSkill = driveAtk;
                ms.forwardS = driveAtk;
                ms.downS = driveAtk;
                ms.airL = jumpAtk;
                ms.airH = jumpAtk;
                ms.airS = driveAtk;
                ms.tacticalThrow = throwAtk;
                ms.domainSuper = superAtk;
                ms.defensiveBurst = driveAtk;
                EditorUtility.SetDirty(ms);

                // 2. Create or Load CharacterDefinition Asset
                string charDefPath = $"{CHAR_DATA_DIR}/CharacterDefinition_{charId}_{shortName}.asset";
                CharacterDefinition cdef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(charDefPath);
                if (cdef == null)
                {
                    cdef = ScriptableObject.CreateInstance<CharacterDefinition>();
                    AssetDatabase.CreateAsset(cdef, charDefPath);
                }

                cdef.id = charId;
                cdef.displayName = profile.displayName;
                cdef.title = profile.title;
                cdef.archetype = profile.archetype;
                cdef.description = profile.description;
                cdef.speed = profile.speed;
                cdef.power = profile.power;
                cdef.reach = profile.reach;
                cdef.defense = profile.defense;
                cdef.domainName = profile.domainName;
                cdef.domainDescription = profile.domainDescription;
                cdef.signatureStageId = $"ST{((i % 12) + 1):D2}";

                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(profile.prefabPath);
                if (modelPrefab != null)
                {
                    cdef.modelPrefab = modelPrefab;
                }

                cdef.animatorController = templateAnim;
                cdef.moveSet = ms;
                EditorUtility.SetDirty(cdef);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00e676>[DemonIdolHomeSetup] Successfully generated 14 CharacterDefinition and MoveSet assets in {CHAR_DATA_DIR}.</color>");
        }

        private static void SetupArenaGroundPlatform()
        {
            GameObject ground = GameObject.Find("Ground_CombatPlatform");
            if (ground == null)
            {
                ground = GameObject.Find("Ground_StoneBridge");
            }
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground_CombatPlatform";
            }
            else
            {
                ground.name = "Ground_CombatPlatform";
            }

            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.rotation = Quaternion.identity;
            ground.transform.localScale = new Vector3(32f, 0.5f, 8f);

            var col = ground.GetComponent<BoxCollider>();
            if (col == null) col = ground.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = Vector3.one;

            var mr = ground.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Environments/RGPoly_StylizedAsia/Stylized Asia RG/Materials/Cocnrate 1  Mat.mat");
                if (concreteMat != null)
                {
                    mr.sharedMaterial = concreteMat;
                }
            }

            PushbackSolver.SetArenaBounds(14f, 13.2f);
        }

        private static void OrganizeShowcaseCharacters(Scene scene)
        {
            GameObject showcaseRoot = GameObject.Find("Home_ShowcaseRoster");
            if (showcaseRoot == null)
            {
                showcaseRoot = new GameObject("Home_ShowcaseRoster");
                showcaseRoot.transform.position = Vector3.zero;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            var roster = FighterRosterCatalog.Roster;
            int matched = 0;
            float spacingX = 1.8f;
            float startX = -((roster.Length - 1) * spacingX * 0.5f);

            for (int i = 0; i < roster.Length; i++)
            {
                var profile = roster[i];
                string targetName = profile.displayName.ToLowerInvariant();
                Transform existingChild = showcaseRoot.transform.Find($"Showcase_{profile.displayName}");
                GameObject foundGo = existingChild != null ? existingChild.gameObject : null;

                if (foundGo == null)
                {
                    foreach (GameObject r in roots)
                    {
                        if (r == showcaseRoot) continue;
                        if (r.name.ToLowerInvariant().Contains(targetName))
                        {
                            foundGo = r;
                            break;
                        }
                    }
                }

                if (foundGo == null)
                {
                    GameObject pObj = AssetDatabase.LoadAssetAtPath<GameObject>(profile.prefabPath);
                    if (pObj != null)
                    {
                        foundGo = (GameObject)PrefabUtility.InstantiatePrefab(pObj, showcaseRoot.transform);
                    }
                }

                if (foundGo != null)
                {
                    matched++;
                    foundGo.name = $"Showcase_{profile.displayName}";
                    float x = startX + (i * spacingX);
                    float z = 5.0f + (Mathf.Abs(x) * 0.12f); // Gentle curved arc
                    foundGo.transform.SetParent(showcaseRoot.transform, true);
                    foundGo.transform.position = new Vector3(x, 0f, z);
                    foundGo.transform.rotation = Quaternion.Euler(0f, 180f - (x * 2.5f), 0f); // Slight inward turn

                    // Ensure Animator has MDI_FighterTemplate
                    Animator anim = foundGo.GetComponentInChildren<Animator>();
                    if (anim != null && anim.runtimeAnimatorController == null)
                    {
                        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);
                    }
                }
            }

            Debug.Log($"<color=#00e676>[DemonIdolHomeSetup] Organized {matched}/14 characters into cinematic background lineup under '{showcaseRoot.name}'.</color>");
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void SetupCombatManager()
        {
            GameObject managerGo = GameObject.Find("CombatManager");
            if (managerGo == null) managerGo = new GameObject("CombatManager");

            var tickEngine = GetOrAdd<CombatTickEngine>(managerGo);
            var audioDirector = GetOrAdd<CombatAudioDirector>(managerGo);
            var vfxCoord = GetOrAdd<HitEffectCoordinator>(managerGo);
            var referee = GetOrAdd<MatchRoundReferee>(managerGo);
            var domainAoe = GetOrAdd<DomainExpansionAOE>(managerGo);
            var opt = GetOrAdd<CrossPlatformOptimizer>(managerGo);
            var splats = GetOrAdd<InkBloodSplatterManager>(managerGo);

            // Wire CFXR hit sparks
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

            // Wire Domain Expansion AOE prefabs
            SerializedObject soDomain = new SerializedObject(domainAoe);
            GameObject runicAura = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR3 Magic Aura A (Runic).prefab");
            GameObject soulsAura = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 Souls Escape.prefab");
            GameObject enemyExplosion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 WW Enemy Explosion.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR2 WW Explosion.prefab");

            if (runicAura != null) soDomain.FindProperty("_runicAuraCirclePrefab").objectReferenceValue = runicAura;
            if (soulsAura != null) soDomain.FindProperty("_soulsEscapePrefab").objectReferenceValue = soulsAura;
            if (enemyExplosion != null) soDomain.FindProperty("_domainShatterBurstPrefab").objectReferenceValue = enemyExplosion;
            if (redHit != null) soDomain.FindProperty("_slashFlurryPrefab").objectReferenceValue = redHit;
            soDomain.ApplyModifiedProperties();
        }

        private static void SetupCombatDirectorAndFighters()
        {
            GameObject directorGo = GameObject.Find("HomegrownCombatDirector");
            if (directorGo == null) directorGo = new GameObject("HomegrownCombatDirector");

            HomegrownCombatDirector director = directorGo.GetComponent<HomegrownCombatDirector>();
            if (director == null) director = directorGo.AddComponent<HomegrownCombatDirector>();

            // Ensure P1 (Akio) and P2 (Aika) FighterController rigs
            FighterController p1 = SetupCombatFighter("Player1_Akio", "C01", new Vector3(-2.5f, 0f, 0f), 90f, true);
            FighterController p2 = SetupCombatFighter("Player2_Aika", "C02", new Vector3(2.5f, 0f, 0f), -90f, false);

            SerializedObject soDir = new SerializedObject(director);
            soDir.FindProperty("_fighter1").objectReferenceValue = p1;
            soDir.FindProperty("_fighter2").objectReferenceValue = p2;
            soDir.ApplyModifiedProperties();

            director.SetMatchContext("C01", "C02", "ST01", true, false);
        }

        private static FighterController SetupCombatFighter(string rootName, string charId, Vector3 pos, float eulerY, bool isPlayer)
        {
            GameObject go = GameObject.Find(rootName);
            if (go == null)
            {
                go = new GameObject(rootName);
            }
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, eulerY, 0f);

            CharacterController cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0);
            cc.height = 1.9f;
            cc.radius = 0.35f;

            FighterController fc = go.GetComponent<FighterController>();
            if (fc == null) fc = go.AddComponent<FighterController>();
            fc.fighterName = rootName;
            fc.isPlayerControlled = isPlayer;

            // Attack Data
            fc.lightAttackData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
            fc.mediumAttackData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
            fc.heavyAttackData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
            fc.driveSkillData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DriveSkill.asset");
            fc.jumpAttackData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");
            fc.domainSuperData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DomainSuper.asset");
            fc.throwAttackData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_Throw.asset");
            fc.lowAttackData = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");

            // Hurtbox
            Transform hbTrans = go.transform.Find("Hurtbox_Torso");
            GameObject hbGo = hbTrans != null ? hbTrans.gameObject : new GameObject("Hurtbox_Torso");
            hbGo.transform.SetParent(go.transform, false);
            hbGo.transform.localPosition = new Vector3(0, 1.0f, 0);
            BoxCollider hbCol = hbGo.GetComponent<BoxCollider>();
            if (hbCol == null) hbCol = hbGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.8f, 1.8f, 0.8f);
            hbCol.isTrigger = true;
            Hurtbox hurtbox = hbGo.GetComponent<Hurtbox>();
            if (hurtbox == null) hurtbox = hbGo.AddComponent<Hurtbox>();

            // Hitbox
            Transform hitTrans = go.transform.Find("Hitbox_Weapon");
            GameObject hitGo = hitTrans != null ? hitTrans.gameObject : new GameObject("Hitbox_Weapon");
            hitGo.transform.SetParent(go.transform, false);
            hitGo.transform.localPosition = new Vector3(0, 1.2f, 0.7f);
            BoxCollider hitCol = hitGo.GetComponent<BoxCollider>();
            if (hitCol == null) hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.2f, 1.2f, 1.4f);
            hitCol.isTrigger = true;
            Hitbox hitbox = hitGo.GetComponent<Hitbox>();
            if (hitbox == null) hitbox = hitGo.AddComponent<Hitbox>();
            fc.activeHitbox = hitbox;

            if (isPlayer)
            {
                if (go.GetComponent<MobileFighterInput>() == null)
                    go.AddComponent<MobileFighterInput>();
            }

            // Bind Visual Model
            var charDef = CharacterRegistry.GetCharacterById(charId);
            string modelName = "Model_" + charId;
            Transform exModel = go.transform.Find(modelName);
            if (exModel == null)
            {
                for (int i = go.transform.childCount - 1; i >= 0; i--)
                {
                    Transform c = go.transform.GetChild(i);
                    if (c.name.StartsWith("Model_") || c.name.StartsWith("Model"))
                        UnityEngine.Object.DestroyImmediate(c.gameObject);
                }

                GameObject prefab = charDef != null ? charDef.modelPrefab : null;
                if (prefab != null)
                {
                    GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
                    model.name = modelName;
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one;

                    Animator anim = model.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.applyRootMotion = false;
                        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);
                        fc.animator = anim;
                        fc.CacheAnimatorParams();
                    }

                    if (model.GetComponent<AnimationEventReceiver>() == null)
                    {
                        model.AddComponent<AnimationEventReceiver>();
                    }
                }
            }
            else
            {
                Animator anim = exModel.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    fc.animator = anim;
                    fc.CacheAnimatorParams();
                }
            }

            if (charDef != null)
            {
                fc.ApplyCharacterDefinition(charDef);
            }

            return fc;
        }

        private static void SetupCameraAndPostProcessing(Scene scene)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            }
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.043f, 0.051f, 0.078f, 1f); // #0B0D14

                UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;

                FighterCamera2D cam2d = cam.GetComponent<FighterCamera2D>();
                if (cam2d == null) cam2d = cam.gameObject.AddComponent<FighterCamera2D>();

                GameObject p1Go = GameObject.Find("Player1_Akio");
                GameObject p2Go = GameObject.Find("Player2_Aika");
                if (p1Go != null && p2Go != null)
                {
                    cam2d.SetTargets(p1Go.transform, p2Go.transform);
                }
                cam2d.usePhysicalCamera = true;
                cam2d.sensorSize = new Vector2(36.0f, 20.25f);
                cam2d.focalLength = 50.0f;
                cam2d.lensShift = new Vector2(0f, 0.035f);
                cam2d.standardFighterHeight = 1.75f;
                cam2d.targetCharacterScreenFraction = 0.40f;
                cam2d.closeCombatScreenFraction = 0.45f;
                cam2d.wideSpacingScreenFraction = 0.35f;
                cam2d.offset = new Vector3(0f, 1.25f, -10.8f);
                cam2d.pitchAngle = 2.8f;
                cam2d.minDistanceZ = -6.5f;
                cam2d.maxDistanceZ = -13.5f;
                cam2d.ConfigurePhysicalCamera();

                EditorUtility.SetDirty(cam2d);
                EditorUtility.SetDirty(camData);
                EditorUtility.SetDirty(cam);
            }

            // Ensure Global Volume with SuspenseVolumeProfile
            Volume vol = UnityEngine.Object.FindAnyObjectByType<Volume>();
            if (vol == null)
            {
                GameObject vGo = new GameObject("Global Volume");
                vol = vGo.AddComponent<Volume>();
            }
            vol.isGlobal = true;
            vol.weight = 1.0f;

            VolumeProfile prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/ValorantVolumeProfile.asset")
                ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SuspenseVolumeProfile.asset");
            if (prof != null)
            {
                vol.sharedProfile = prof;
            }
            EditorUtility.SetDirty(vol);
        }
    }
}
#endif

#endif
