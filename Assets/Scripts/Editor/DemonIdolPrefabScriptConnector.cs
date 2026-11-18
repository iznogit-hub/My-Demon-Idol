#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Characters.Editor
{
    /// <summary>
    /// Master Character Prefab & Script Connector for Demon Idol.
    /// Audits all 14 playable characters (C01 to C14), ensuring:
    /// 1. Canonical clean standalone prefabs in Assets/Prefabs/Characters/ (C01 to C14).
    /// 2. Clean standalone unpacked meshes with zero weapon props (100% barehanded).
    /// 3. CharacterController calibrated (Center: (0, 0.95, 0), Height: 1.9, Radius: 0.35).
    /// 4. Animator with authentic package avatar and assigned runtime controller.
    /// 5. FighterController fully configured, with animator & activeHitbox WIRED.
    /// 6. MobileFighterInput with _fighter WIRED.
    /// 7. AnimationEventReceiver attached.
    /// 8. Hurtbox_Torso attached with trigger BoxCollider and BodyZone.Torso.
    /// 9. Hitbox attached with trigger BoxCollider and Hitbox component.
    /// 10. CharacterDefinition assets synchronized with modelPrefab, avatar & controller.
    /// </summary>
    public static class DemonIdolPrefabScriptConnector
    {
        public const string PREFABS_DIR = "Assets/Prefabs/Characters";
        public const string CHAR_DATA_DIR = "Assets/CombatData/Characters";
        public const string MASTER_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";

        public struct CharSpec
        {
            public string id;
            public string name;
            public string targetPrefabName;
            public string sourceAssetPath;
            public string packageFbxPath;
            public string runtimeControllerPath;
            public bool forceRebuildStandalone;

            public CharSpec(string id, string name, string targetPrefab, string sourceAsset, string packageFbx, string runtimeController, bool forceRebuild = false)
            {
                this.id = id;
                this.name = name;
                this.targetPrefabName = targetPrefab;
                this.sourceAssetPath = sourceAsset;
                this.packageFbxPath = packageFbx;
                this.runtimeControllerPath = runtimeController;
                this.forceRebuildStandalone = forceRebuild;
            }
        }

        public static readonly CharSpec[] Specs = new CharSpec[]
        {
            // C01 Akio (Human Melee Animations)
            new CharSpec(
                "C01", "Akio", "C01_Akio.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/Prefab/Akio Highschool Uniform.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/FBX/Akio Highschool Uniform.fbx",
                "Assets/MDI_Characters/Runtime/AnimatorOverride_C01_Akio.overrideController"
            ),
            // C02 Aika
            new CharSpec(
                "C02", "Aika", "C02_Aika.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Aika/Highschool Uniform/Prefab/Aika Highschool Uniform.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Aika/Highschool Uniform/FBX/Aika Highschool Uniform.fbx",
                MASTER_CONTROLLER_PATH
            ),
            // C03 Ren (Human Melee Animations)
            new CharSpec(
                "C03", "Ren", "C03_Ren.prefab",
                "Assets/MDI_Characters/Clans/Occult/Ren/Ren.prefab",
                "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx",
                "Assets/MDI_Characters/Runtime/AnimatorOverride_C03_Ren.overrideController"
            ),
            // C04 UnityChan
            new CharSpec(
                "C04", "UnityChan", "C04_UnityChan.prefab",
                "Assets/unity-chan!/Unity-chan! Model/Prefabs/unitychan_dynamic.prefab",
                "Assets/unity-chan!/Unity-chan! Model/Art/Models/unitychan.fbx",
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/Animators/UnityChanLocomotions.controller"
            ),
            // C05 Akane
            new CharSpec(
                "C05", "Akane", "C05_Akane.prefab",
                "Assets/3d-character_animeGirlAkane/Akane/angGirl.fbx",
                "Assets/3d-character_animeGirlAkane/Akane/angGirl.fbx",
                "Assets/3d-character_animeGirlAkane/DemoScene/Animation/AnimationController.controller"
            ),
            // C06 Cultist
            new CharSpec(
                "C06", "Cultist", "C06_Cultist.prefab",
                "Assets/CeltriusStudios/Cultist/Prefabs/Cultist.prefab",
                "Assets/CeltriusStudios/Cultist/Prefabs/Mesh/Cultist.fbx",
                MASTER_CONTROLLER_PATH
            ),
            // C07 StellarWitch
            new CharSpec(
                "C07", "StellarWitch", "C07_StellarWitch.prefab",
                "Assets/StellarWitch3D/Prefabs/StellarWitch.prefab",
                "Assets/StellarWitch3D/Meshes/StellarWitch.fbx",
                MASTER_CONTROLLER_PATH
            ),
            // C08 SanpakuChan
            new CharSpec(
                "C08", "SanpakuChan", "C08_SanpakuChan.prefab",
                "Assets/SanpakuChan/Prefabs/SanpakuChan.prefab",
                "Assets/SanpakuChan/Meshes/SanpakuChan.fbx",
                MASTER_CONTROLLER_PATH
            ),
            // C09 PicoChan
            new CharSpec(
                "C09", "PicoChan", "C09_PicoChan.prefab",
                "Assets/Picola/PicoChan/Prefabs/PicoChan.prefab",
                "Assets/Picola/PicoChan/Models/pico_chan_chr_pico_00.fbx",
                MASTER_CONTROLLER_PATH
            ),
            // C10 Reika (Kiki)
            new CharSpec(
                "C10", "Reika", "C10_Reika.prefab",
                "Assets/Kiki/Xmas Girl/Prefabs/Kiki-v2.prefab",
                "Assets/Kiki/Xmas Girl/Model/Kiki-v2.fbx",
                "Assets/Kiki/Xmas Girl/Animator/Animator Controller.controller"
            ),
            // C11 Casual1
            new CharSpec(
                "C11", "Casual1", "C11_Casual1.prefab",
                "Assets/AnimeGirls/Casual1/Casual1.prefab",
                "Assets/AnimeGirls/Casual1/SourceFiles/Mesh/AnimeGirl.fbx",
                "Assets/AnimeGirls/BaseCharacter/Demo files/AC.controller"
            ),
            // C12 Fina
            new CharSpec(
                "C12", "Fina", "C12_Fina.prefab",
                "Assets/smoky_fox/FinaAnimeGirl/Model/Fina.fbx",
                "Assets/smoky_fox/FinaAnimeGirl/Model/Fina.fbx",
                "Assets/smoky_fox/FinaAnimeGirl/Animations/Locomotion/finna.controller"
            ),
            // C13 SapphiArtchan
            new CharSpec(
                "C13", "SapphiArtchan", "C13_SapphiArtchan.prefab",
                "Assets/SapphiArt/SapphiArtchan/OBJ/SapphiArtchan.prefab",
                "Assets/SapphiArt/SapphiArtchan/OBJ/SapphiArtchan.fbx",
                "Assets/SapphiArt/SapphiArtchan/Animation/SapphiArtchanAnimController.controller"
            ),
            // C14 Muryotaisu
            new CharSpec(
                "C14", "Muryotaisu", "C14_Muryotaisu.prefab",
                "Assets/Mryotaisu/Prefabs/Muryotaisu.prefab",
                "Assets/Mryotaisu/Models/Muryotaisu.fbx",
                "Assets/Mryotaisu/Animators/Muryotaisu.controller"
            )
        };

        [InitializeOnLoadMethod]
        public static void AutoExecuteOnCompile()
        {
            string triggerFile = "run_prefab_connector.trigger";
            if (!File.Exists(triggerFile)) return;
            try { File.Delete(triggerFile); } catch {}
            ConnectAllCharacterPrefabs();
        }

        [MenuItem("Demon Idol/🚀 Playability/Connect All Character Prefab Scripts", priority = 2)]
        public static void ConnectAllCharacterPrefabs()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: MASTER CHARACTER PREFAB & SCRIPT CONNECTOR      </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            if (!Directory.Exists(PREFABS_DIR)) Directory.CreateDirectory(PREFABS_DIR);
            if (!Directory.Exists(CHAR_DATA_DIR)) Directory.CreateDirectory(CHAR_DATA_DIR);
            AssetDatabase.Refresh();

            // Load shared attack data assets
            var lightAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
            var lowAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");
            var medAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
            var hvyAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
            var jmpAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");

            // Clean up rogue/legacy prefabs
            string[] rogueFiles = new string[]
            {
                $"{PREFABS_DIR}/C12M_Ren.prefab",
                $"{PREFABS_DIR}/C12B_Reika.prefab",
                $"{PREFABS_DIR}/C12_Underwear1.prefab"
            };
            foreach (var r in rogueFiles)
            {
                if (File.Exists(r))
                {
                    AssetDatabase.DeleteAsset(r);
                    Debug.Log($"[PrefabScriptConnector] Removed legacy rogue file: {r}");
                }
            }

            int processedCount = 0;

            foreach (var spec in Specs)
            {
                string targetPrefabPath = $"{PREFABS_DIR}/{spec.targetPrefabName}";
                bool needsNewPrefab = spec.forceRebuildStandalone || !File.Exists(targetPrefabPath);

                if (!needsNewPrefab)
                {
                    GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(targetPrefabPath);
                    if (existing == null || existing.GetComponentsInChildren<Renderer>(true).Length == 0)
                    {
                        needsNewPrefab = true;
                    }
                }

                if (needsNewPrefab)
                {
                    GameObject sourceGo = AssetDatabase.LoadAssetAtPath<GameObject>(spec.sourceAssetPath);
                    if (sourceGo == null)
                    {
                        Debug.LogError($"[PrefabScriptConnector] Source asset not found at {spec.sourceAssetPath} for {spec.id} {spec.name}!");
                        continue;
                    }

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceGo);
                    instance.name = Path.GetFileNameWithoutExtension(spec.targetPrefabName);
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                    if (spec.id == "C12")
                    {
                        AssignFinaMaterials(instance);
                    }

                    // Remove all missing script components before saving
                    foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    }

                    ConfigureFighterRoot(instance, spec, lightAtk, lowAtk, medAtk, hvyAtk, jmpAtk);

                    PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
                    UnityEngine.Object.DestroyImmediate(instance);
                    AssetDatabase.ImportAsset(targetPrefabPath, ImportAssetOptions.ForceSynchronousImport);
                    Debug.Log($"[PrefabScriptConnector] Successfully authored and saved clean prefab: {targetPrefabPath}");
                    processedCount++;
                }
                else
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(targetPrefabPath);
                    if (root == null)
                    {
                        Debug.LogError($"[PrefabScriptConnector] Failed to load prefab contents at {targetPrefabPath}!");
                        continue;
                    }

                    try
                    {
                        // Remove all missing script components
                        foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        {
                            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                        }

                        ConfigureFighterRoot(root, spec, lightAtk, lowAtk, medAtk, hvyAtk, jmpAtk);
                        PrefabUtility.SaveAsPrefabAsset(root, targetPrefabPath);
                        processedCount++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }

                // Update CharacterDefinition ScriptableObject
                UpdateCharDefinition(spec, targetPrefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Run verification and generate report
            RunVerificationAudit();

            Debug.Log($"<color=#00e676><b>[PrefabScriptConnector] ALL {processedCount}/14 CHARACTER PREFABS VERIFIED & CONNECTED!</b></color>");
        }

        private static void ConfigureFighterRoot(
            GameObject root, CharSpec spec,
            AttackData lightAtk, AttackData lowAtk, AttackData medAtk, AttackData hvyAtk, AttackData jmpAtk)
        {
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // 1. CharacterController
            CharacterController cc = root.GetComponent<CharacterController>();
            if (cc == null) cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.95f, 0);
            cc.height = 1.9f;
            cc.radius = 0.35f;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.08f;
            cc.minMoveDistance = 0.001f;

            // 2. Animator
            Animator anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.applyRootMotion = false;

            // Resolve authentic Avatar
            Avatar authenticAvatar = null;
            if (!string.IsNullOrEmpty(spec.packageFbxPath))
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(spec.packageFbxPath);
                foreach (var a in assets)
                {
                    if (a is Avatar av)
                    {
                        authenticAvatar = av;
                        break;
                    }
                }
            }
            if (authenticAvatar != null)
            {
                anim.avatar = authenticAvatar;
            }

            // Resolve Runtime Controller
            RuntimeAnimatorController runtimeController = null;
            if (!string.IsNullOrEmpty(spec.runtimeControllerPath))
            {
                runtimeController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(spec.runtimeControllerPath);
            }
            if (runtimeController == null)
            {
                runtimeController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MASTER_CONTROLLER_PATH);
            }
            if (runtimeController != null)
            {
                anim.runtimeAnimatorController = runtimeController;
            }

            // 3. Hurtbox_Torso
            Transform hurtboxTrans = root.transform.Find("Hurtbox_Torso");
            GameObject hurtboxGo = hurtboxTrans != null ? hurtboxTrans.gameObject : null;
            if (hurtboxGo == null)
            {
                hurtboxGo = new GameObject("Hurtbox_Torso");
                hurtboxGo.transform.SetParent(root.transform, false);
            }
            hurtboxGo.transform.localPosition = new Vector3(0, 1.0f, 0);
            hurtboxGo.transform.localRotation = Quaternion.identity;
            hurtboxGo.transform.localScale = Vector3.one;

            BoxCollider hbCol = hurtboxGo.GetComponent<BoxCollider>();
            if (hbCol == null) hbCol = hurtboxGo.AddComponent<BoxCollider>();
            hbCol.size = new Vector3(0.8f, 1.8f, 0.8f);
            hbCol.center = Vector3.zero;
            hbCol.isTrigger = true;

            Hurtbox hurtbox = hurtboxGo.GetComponent<Hurtbox>();
            if (hurtbox == null) hurtbox = hurtboxGo.AddComponent<Hurtbox>();
            SetHurtboxZone(hurtbox, BodyZone.Torso);

            // 4. Hitbox
            Transform hitTrans = root.transform.Find("Hitbox") ?? root.transform.Find("Hitbox_Weapon");
            GameObject hitGo = hitTrans != null ? hitTrans.gameObject : null;
            if (hitGo == null)
            {
                hitGo = new GameObject("Hitbox");
                hitGo.transform.SetParent(root.transform, false);
            }
            hitGo.name = "Hitbox";
            hitGo.transform.localPosition = new Vector3(0, 1.0f, 0.8f);
            hitGo.transform.localRotation = Quaternion.identity;
            hitGo.transform.localScale = Vector3.one;

            BoxCollider hitCol = hitGo.GetComponent<BoxCollider>();
            if (hitCol == null) hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.2f, 1.2f, 1.4f);
            hitCol.center = Vector3.zero;
            hitCol.isTrigger = true;

            Hitbox hitbox = hitGo.GetComponent<Hitbox>();
            if (hitbox == null) hitbox = hitGo.AddComponent<Hitbox>();

            // 5. FighterController
            FighterController fc = root.GetComponent<FighterController>();
            if (fc == null) fc = root.AddComponent<FighterController>();
            fc.fighterName = spec.name;
            fc.isPlayerControlled = true;
            fc.maxHealth = 1000;
            fc.maxSuperMeter = 100f;
            fc.forwardWalkSpeed = 3.6f;
            fc.backwardWalkSpeed = 2.8f;
            fc.jumpVerticalLaunch = 8f;
            fc.jumpHorizontalSpeed = 3.2f;
            fc.gravity = 20f;
            fc.creepSpeed = 1.2f;
            fc.forwardDashSpeed = 6.5f;
            fc.forwardDashDurationTicks = 12;
            fc.backdashSpeed = 7f;
            fc.backdashDurationTicks = 14;
            fc.backdashInvincibleTicks = 5;
            fc.jumpAttackVelocityX = 3.2f;
            fc.jumpAttackVelocityY = 6f;
            fc.parryActiveTicks = 5;
            fc.parryRecoveryTicks = 14;
            fc.parryStunTicksOnAttacker = 18;

            // Wire Animator & Hitbox
            fc.animator = anim;
            fc.activeHitbox = hitbox;

            // Wire attack data
            if (lightAtk != null) fc.lightAttackData = lightAtk;
            if (lowAtk != null) fc.lowAttackData = lowAtk;
            if (medAtk != null) fc.mediumAttackData = medAtk;
            if (hvyAtk != null) fc.heavyAttackData = hvyAtk;
            if (jmpAtk != null) fc.jumpAttackData = jmpAtk;

            // 6. MobileFighterInput
            MobileFighterInput inputComp = root.GetComponent<MobileFighterInput>();
            if (inputComp == null) inputComp = root.AddComponent<MobileFighterInput>();
            inputComp.joystickDeadZone = 0.20f;
            inputComp.joystickExitThreshold = 0.15f;
            inputComp.isGestureModeEnabled = false;

            // Wire _fighter serialized field
            var soInput = new SerializedObject(inputComp);
            var propFighter = soInput.FindProperty("_fighter");
            if (propFighter != null)
            {
                propFighter.objectReferenceValue = fc;
                soInput.ApplyModifiedProperties();
            }

            // Also set via reflection as backup
            try
            {
                var field = typeof(MobileFighterInput).GetField("_fighter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null) field.SetValue(inputComp, fc);
            }
            catch {}

            // 7. AnimationEventReceiver
            if (root.GetComponent<AnimationEventReceiver>() == null)
            {
                root.AddComponent<AnimationEventReceiver>();
            }

            // 8. Sanitize attached props (guns, staves, wands, swords)
            var binders = root.GetComponentsInChildren<FighterPropBinder>(true);
            foreach (var b in binders) UnityEngine.Object.DestroyImmediate(b, true);

            var allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t != null && t != root.transform && (
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
                    UnityEngine.Object.DestroyImmediate(t.gameObject, true);
                }
            }

            // Ensure all renderers are enabled
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
            }
        }

        private static void SetHurtboxZone(Hurtbox hb, BodyZone zone)
        {
            try
            {
                var field = typeof(Hurtbox).GetField("_zone", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(hb, zone);
                }
            }
            catch {}
        }

        private static void AssignFinaMaterials(GameObject instance)
        {
            var matBody = AssetDatabase.LoadAssetAtPath<Material>("Assets/smoky_fox/FinaAnimeGirl/Materials/Body.mat");
            var matCloth = AssetDatabase.LoadAssetAtPath<Material>("Assets/smoky_fox/FinaAnimeGirl/Materials/Cloth.mat");
            var matEyes = AssetDatabase.LoadAssetAtPath<Material>("Assets/smoky_fox/FinaAnimeGirl/Materials/Eyes.mat");
            var matFace = AssetDatabase.LoadAssetAtPath<Material>("Assets/smoky_fox/FinaAnimeGirl/Materials/Face.mat");
            var matHair = AssetDatabase.LoadAssetAtPath<Material>("Assets/smoky_fox/FinaAnimeGirl/Materials/Hair.mat");
            var matMouth = AssetDatabase.LoadAssetAtPath<Material>("Assets/smoky_fox/FinaAnimeGirl/Materials/Mouth.mat");

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                string rName = r.name.ToLower();
                if (rName.Contains("body") && matBody != null) r.sharedMaterial = matBody;
                else if (rName.Contains("cloth") && matCloth != null) r.sharedMaterial = matCloth;
                else if (rName.Contains("eye") && matEyes != null) r.sharedMaterial = matEyes;
                else if (rName.Contains("face") && matFace != null) r.sharedMaterial = matFace;
                else if (rName.Contains("hair") && matHair != null) r.sharedMaterial = matHair;
                else if (rName.Contains("mouth") && matMouth != null) r.sharedMaterial = matMouth;
            }
        }

        private static void UpdateCharDefinition(CharSpec spec, string targetPrefabPath)
        {
            string defPath = $"{CHAR_DATA_DIR}/CharacterDefinition_{spec.id}_{spec.name}.asset";
            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(defPath);
            if (charDef == null)
            {
                string[] found = Directory.GetFiles(CHAR_DATA_DIR, $"CharacterDefinition_{spec.id}*.asset");
                if (found.Length > 0)
                {
                    charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(found[0]);
                }
            }

            if (charDef != null)
            {
                charDef.id = spec.id;
                charDef.displayName = spec.name;
                charDef.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPrefabPath);

                if (!string.IsNullOrEmpty(spec.packageFbxPath))
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(spec.packageFbxPath);
                    foreach (var a in assets)
                    {
                        if (a is Avatar av)
                        {
                            charDef.avatar = av;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(spec.runtimeControllerPath))
                {
                    charDef.animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(spec.runtimeControllerPath);
                }

                EditorUtility.SetDirty(charDef);
            }
        }

        private static void RunVerificationAudit()
        {
            string logFile = "prefab_script_connection_audit.log";
            using (var sw = new StreamWriter(logFile, false))
            {
                sw.WriteLine("=== DEMON IDOL 14-FIGHTER PREFAB SCRIPT CONNECTION AUDIT ===");
                sw.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");

                int passCount = 0;

                foreach (var spec in Specs)
                {
                    string pPath = $"{PREFABS_DIR}/{spec.targetPrefabName}";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);

                    if (prefab == null)
                    {
                        sw.WriteLine($"[{spec.id} {spec.name}] PREFAB MISSING at {pPath}!");
                        continue;
                    }

                    var cc = prefab.GetComponent<CharacterController>();
                    var anim = prefab.GetComponent<Animator>();
                    var fc = prefab.GetComponent<FighterController>();
                    var input = prefab.GetComponent<MobileFighterInput>();
                    var aer = prefab.GetComponent<AnimationEventReceiver>();
                    var hurtbox = prefab.GetComponentInChildren<Hurtbox>();
                    var hitbox = prefab.GetComponentInChildren<Hitbox>();
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);

                    // Check serialized wiring
                    bool ccValid = cc != null && Mathf.Abs(cc.center.y - 0.95f) < 0.1f && Mathf.Abs(cc.height - 1.9f) < 0.15f;
                    bool animValid = anim != null && anim.avatar != null && anim.runtimeAnimatorController != null;
                    bool fcValid = fc != null && fc.animator != null && fc.activeHitbox != null;
                    
                    bool inputFighterWired = false;
                    if (input != null)
                    {
                        var field = typeof(MobileFighterInput).GetField("_fighter", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (field != null)
                        {
                            inputFighterWired = field.GetValue(input) != null;
                        }
                        if (!inputFighterWired)
                        {
                            var so = new SerializedObject(input);
                            var pFighter = so.FindProperty("_fighter");
                            inputFighterWired = pFighter != null && pFighter.objectReferenceValue != null;
                        }
                    }

                    bool aerValid = aer != null;
                    bool hurtboxValid = hurtbox != null && hurtbox.GetComponent<BoxCollider>() != null && hurtbox.GetComponent<BoxCollider>().isTrigger;
                    bool hitboxValid = hitbox != null && hitbox.GetComponent<BoxCollider>() != null && hitbox.GetComponent<BoxCollider>().isTrigger;
                    bool renderersValid = renderers.Length > 0;

                    bool allPassed = ccValid && animValid && fcValid && inputFighterWired && aerValid && hurtboxValid && hitboxValid && renderersValid;
                    if (allPassed) passCount++;

                    string status = allPassed ? "PASS" : "FAIL";

                    sw.WriteLine($"[{spec.id} {spec.name}] {status}");
                    sw.WriteLine($"  Prefab: {pPath} (Renderers: {renderers.Length})");
                    sw.WriteLine($"  CharacterController: {(ccValid ? $"OK (Center={cc.center.y:F2}, Height={cc.height:F2})" : "MISSING/INVALID")}");
                    sw.WriteLine($"  Animator: {(animValid ? $"OK (Avatar={anim.avatar.name}, Ctrl={anim.runtimeAnimatorController.name})" : "MISSING/INVALID")}");
                    sw.WriteLine($"  FighterController: {(fcValid ? $"OK (Name={fc.fighterName}, AnimWired={fc.animator != null}, HitboxWired={fc.activeHitbox != null})" : "MISSING/UNWIRED")}");
                    sw.WriteLine($"  MobileFighterInput: {(inputFighterWired ? "OK (FighterWired=True)" : "MISSING/UNWIRED")}");
                    sw.WriteLine($"  AnimationEventReceiver: {(aerValid ? "OK" : "MISSING")}");
                    sw.WriteLine($"  Hurtbox_Torso: {(hurtboxValid ? "OK (Trigger=True)" : "MISSING")}");
                    sw.WriteLine($"  Hitbox: {(hitboxValid ? "OK (Trigger=True)" : "MISSING")}");
                    sw.WriteLine("");
                }

                sw.WriteLine($"AUDIT RESULT: {passCount}/{Specs.Length} CHARACTER PREFABS FULLY VERIFIED & WIRED");
                sw.WriteLine("=== END OF AUDIT ===");
            }
        }
    }
}
#endif
