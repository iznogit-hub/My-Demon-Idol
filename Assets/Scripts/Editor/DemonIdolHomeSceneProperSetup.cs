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
using DemonIdol.Cutscenes;
using DemonIdol.UI;
using DemonIdol.Audio;
using DemonIdol.VFX;
using DemonIdol.Input;
using DemonIdol.Launch;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master Authoritative Setup & Sanitation Tool for Assets/Scenes/Home.unity.
    /// Cleans up all unused, legacy, or broken elements and sets up the production-grade Home scene:
    /// 1. Strips unused legacy clutter: CCVFree Demo, leftover point lights, legacy PostProcessing, static stage instances, Home_ShowcaseRoster.
    /// 2. Purges all missing scripts across all scene GameObjects.
    /// 3. Sets up Arena Platform (Ground_CombatPlatform) with collision, texture, and Pushback bounds.
    /// 4. Calibrates URP Indoor Template Lighting: Directional Light sun, active Global Volume with URP profile, Realtime Reflection Probe.
    /// 5. Configures SF6 Physical Camera (Super 35, 50mm prime, 40% fighter screen height calibration).
    /// 6. Configures P1 (Akio) and P2 (Aika) with weapons, 3-button combat stack, hitboxes, hurtboxes, and AnimeToon materials.
    /// 7. Configures CombatManager, HomegrownCombatDirector, AceDeathReplayDirector, and Narrator.
    /// 8. Rebuilds Master UI Canvas (14-Character Select with C12 mugshots/shields, 6-Stage Select, Battle HUD, Touch Dial & Super Joysticks).
    /// </summary>
    public static class DemonIdolHomeSceneProperSetup
    {
        private const string SCENE_PATH = "Assets/Scenes/Home.unity";
        private const string INDOOR_PROFILE_PATH = "Assets/Settings/URP_IndoorTemplateVolumeProfile.asset";
        private const string MDI_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Home/Setup Home Scene Properly (Clean & Calibrate)", priority = 0)]
        public static void SetupAndCleanHomeScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[DemonIdolHomeCleanup] Please exit Play Mode before running setup.");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += SetupAndCleanHomeScene;
                return;
            }

            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: SANITIZING & PROPERLY SETTING UP HOME SCENE    </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            // 1. Open Home scene
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            int deletedCount = 0;

            // 2. Remove Unused / Deprecated Legacy Elements
            deletedCount += RemoveUnusedLegacyObjects(scene);

            // 3. Purge Missing Scripts Across All Scene Objects
            int missingScriptsRemoved = PurgeMissingScripts(scene);

            // 4. Setup Lighting, URP Volume & Reflection Probe
            SetupLightingAndEnvironment(scene);

            // 5. Setup Arena Ground Platform
            SetupArenaPlatform();

            // 6. Setup Combat Fighters (Player1_Akio and Player2_Aika)
            SetupFighters();

            // 7. Setup SF6 Cinematic Physical Camera (40% screen framing)
            SetupPhysicalCamera();

            // 8. Setup CombatManager & Directors
            SetupCombatManagerAndDirectors();

            // 9. Rebuild and Verify Master UI
            DemonIdolUIBuilder.BuildMasterUI();

            // 10. Re-bind camera targets
            BindCameraTargets();

            // 11. Purge any remaining missing scripts
            missingScriptsRemoved += PurgeMissingScripts(scene);

            // 12. Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[DemonIdolHomeCleanup] SUCCESS!</b> Removed {deletedCount} unused legacy objects, stripped {missingScriptsRemoved} missing scripts. Home.unity is now 100% clean, calibrated, and production ready!</color>");
            Debug.Log("<color=#00e676>=============================================================</color>");
        }

        private static int RemoveUnusedLegacyObjects(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();

            string[] legacyNames = new string[]
            {
                "CCVFree Demo",
                "Light", // Parent with 4 duplicate point lights from CCVFree Demo
                "PostProcessing", // Legacy post-processing GameObject from CCVFree Demo
                "ST04_InkFortressCathedral", // Old static cathedral stage left in scene
                "ST01_ReactorCoreSilo",
                "ST02_HeavyCyberHangar",
                "ST03_NeoShibuyaColosseum",
                "Stage_CombatArena_Base",
                "Stage_Diorama_Base",
                "Home_ShowcaseRoster", // Legacy backdrop holding 38 overlapping/floating hair & model prefabs
                "[BloodlinesUIAtlas]"  // Empty placeholder
            };

            foreach (GameObject root in roots)
            {
                if (root == null) continue;

                bool shouldDelete = false;
                foreach (string leg in legacyNames)
                {
                    if (string.Equals(root.name, leg, StringComparison.OrdinalIgnoreCase))
                    {
                        shouldDelete = true;
                        break;
                    }
                }

                if (shouldDelete)
                {
                    Debug.Log($"<color=#ffab00>[DemonIdolHomeCleanup] Removing unused legacy object: '{root.name}'</color>");
                    UnityEngine.Object.DestroyImmediate(root);
                    count++;
                }
            }

            return count;
        }

        private static int PurgeMissingScripts(Scene scene)
        {
            int totalRemoved = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                totalRemoved += CleanMissingScriptsRecursively(root);
            }
            return totalRemoved;
        }

        private static int CleanMissingScriptsRecursively(GameObject go)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                if (child != null)
                {
                    removed += CleanMissingScriptsRecursively(child.gameObject);
                }
            }
            return removed;
        }

        private static void SetupLightingAndEnvironment(Scene scene)
        {
            // 1. Directional Light (Sun)
            Light dirLight = null;
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    dirLight = l;
                    break;
                }
            }

            if (dirLight == null)
            {
                GameObject dlGo = new GameObject("Directional Light");
                dirLight = dlGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.name = "Directional Light";
            dirLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            dirLight.color = new Color(1.0f, 0.97f, 0.94f); // Warm anime sunlight
            dirLight.intensity = 1.15f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowNormalBias = 0.4f;
            dirLight.shadowBias = 0.05f;

            // 2. Global Volume (Indoor URP Template Profile)
            Volume volume = UnityEngine.Object.FindAnyObjectByType<Volume>(FindObjectsInactive.Include);
            if (volume == null)
            {
                GameObject vGo = new GameObject("Global Volume");
                volume = vGo.AddComponent<Volume>();
            }

            volume.name = "Global Volume";
            volume.gameObject.SetActive(true);
            volume.isGlobal = true;
            volume.weight = 1.0f;

            VolumeProfile indoorProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(INDOOR_PROFILE_PATH)
                ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            if (indoorProfile != null)
            {
                volume.sharedProfile = indoorProfile;
            }
            EditorUtility.SetDirty(volume);

            // 3. Realtime Reflection Probe for Vibrant Anime Highlights
            ReflectionProbe probe = UnityEngine.Object.FindAnyObjectByType<ReflectionProbe>(FindObjectsInactive.Include);
            if (probe == null)
            {
                GameObject probeGo = new GameObject("Reflection Probe");
                probe = probeGo.AddComponent<ReflectionProbe>();
            }

            probe.name = "Reflection Probe";
            probe.gameObject.SetActive(true);
            probe.transform.position = new Vector3(0f, 2.5f, 0f);
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.boxProjection = true;
            probe.size = new Vector3(36f, 16f, 24f);
            probe.center = Vector3.zero;
            probe.intensity = 1.0f;
            EditorUtility.SetDirty(probe);

            Debug.Log("[DemonIdolHomeCleanup] Calibrated Directional Light, URP Indoor Global Volume, and Reflection Probe.");
        }

        private static void SetupArenaPlatform()
        {
            GameObject ground = GameObject.Find("Ground_CombatPlatform");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
                mr.enabled = true;
                Material groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/charaxters/Ground.mat")
                    ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Floor.mat");
                if (groundMat != null)
                {
                    mr.sharedMaterial = groundMat;
                }
            }

            PushbackSolver.SetArenaBounds(14f, 13.2f);
            Debug.Log("[DemonIdolHomeCleanup] Arena platform established with bounds ±14.0m and enabled MeshRenderer.");
        }

        private static void SetupFighters()
        {
            // P1: Akio
            FighterController p1 = SetupCombatFighter("Player1_Akio", "C01", new Vector3(-2.5f, 0f, 0f), 90f, true);
            // P2: Aika
            FighterController p2 = SetupCombatFighter("Player2_Aika", "C02", new Vector3(2.5f, 0f, 0f), -90f, false);

            // Wire Director
            HomegrownCombatDirector director = HomegrownCombatDirector.Instance;
            if (director != null)
            {
                SerializedObject soDir = new SerializedObject(director);
                var f1Prop = soDir.FindProperty("_fighter1");
                var f2Prop = soDir.FindProperty("_fighter2");
                if (f1Prop != null) f1Prop.objectReferenceValue = p1;
                if (f2Prop != null) f2Prop.objectReferenceValue = p2;
                soDir.ApplyModifiedProperties();
                director.SetMatchContext("C01", "C02", "ST01", true, false);
            }
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

            // Visual Model Binding
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
                        model.AddComponent<AnimationEventReceiver>();

                    if (model.GetComponent<FighterPropBinder>() == null)
                        model.AddComponent<FighterPropBinder>();
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

        private static void SetupPhysicalCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            cam.transform.position = new Vector3(0f, 1.25f, -5.5f);
            cam.transform.rotation = Quaternion.Euler(2.8f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.043f, 0.051f, 0.078f, 1f);

            UniversalAdditionalCameraData camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;

            FighterCamera2D cam2d = cam.GetComponent<FighterCamera2D>();
            if (cam2d == null) cam2d = cam.gameObject.AddComponent<FighterCamera2D>();

            // SF6 Cinematic Physical Camera Calibration
            cam2d.usePhysicalCamera = true;
            cam2d.sensorSize = new Vector2(36.0f, 20.25f); // Super 35 Full Frame 16:9
            cam2d.focalLength = 50.0f; // 50mm portrait prime
            cam2d.lensShift = new Vector2(0f, 0.035f); // Heroic grounded rise

            // Exact 40% Character Screen Height Framing
            cam2d.standardFighterHeight = 1.75f;
            cam2d.targetCharacterScreenFraction = 0.40f;
            cam2d.closeCombatScreenFraction = 0.45f;
            cam2d.wideSpacingScreenFraction = 0.35f;

            // SF6 Dynamic Handling
            cam2d.offset = new Vector3(0f, 1.25f, -5.5f);
            cam2d.pitchAngle = 2.8f;
            cam2d.aggressionLeadAmount = 0.35f;
            cam2d.verticalLaunchThresholdY = 2.5f;
            cam2d.minDistanceZ = -4.5f;
            cam2d.maxDistanceZ = -10.5f;

            cam2d.ConfigurePhysicalCamera();
            EditorUtility.SetDirty(cam2d);
            EditorUtility.SetDirty(cam);

            Debug.Log("[DemonIdolHomeCleanup] Configured Main Camera with SF6 40% Physical Camera calibration.");
        }

        private static void BindCameraTargets()
        {
            FighterCamera2D cam2d = UnityEngine.Object.FindAnyObjectByType<FighterCamera2D>();
            if (cam2d != null)
            {
                GameObject p1Go = GameObject.Find("Player1_Akio");
                GameObject p2Go = GameObject.Find("Player2_Aika");
                if (p1Go != null && p2Go != null)
                {
                    cam2d.SetTargets(p1Go.transform, p2Go.transform);
                }
            }
        }

        private static void SetupCombatManagerAndDirectors()
        {
            // 1. CombatManager
            GameObject managerGo = GameObject.Find("CombatManager");
            if (managerGo == null) managerGo = new GameObject("CombatManager");

            GetOrAdd<CombatTickEngine>(managerGo);
            GetOrAdd<CombatAudioDirector>(managerGo);
            var vfxCoord = GetOrAdd<HitEffectCoordinator>(managerGo);
            GetOrAdd<MatchRoundReferee>(managerGo);
            var domainAoe = GetOrAdd<DomainExpansionAOE>(managerGo);
            GetOrAdd<CrossPlatformOptimizer>(managerGo);
            GetOrAdd<InkBloodSplatterManager>(managerGo);

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

            // 2. AceDeathReplayDirector
            GameObject replayGo = GameObject.Find("AceDeathReplayDirector");
            if (replayGo == null) replayGo = new GameObject("AceDeathReplayDirector");
            GetOrAdd<AceDeathReplayDirector>(replayGo);

            // 3. DemonLordNarratorManager
            GameObject narratorGo = GameObject.Find("DemonLordNarratorManager");
            if (narratorGo == null) narratorGo = new GameObject("DemonLordNarratorManager");
            GetOrAdd<DemonLordNarratorManager>(narratorGo);

            Debug.Log("[DemonIdolHomeCleanup] CombatManager, AceDeathReplayDirector, and Narrator configured.");
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
#endif
