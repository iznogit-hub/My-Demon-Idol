#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Phase 2 Assembler: Assembles and configures the 8 External Dota 2 Heroes:
    /// EX01 Hugo (Zombie Gorilla - Brute)
    /// EX02 Granite (Sanctuary Rock Golem - Juggernaut)
    /// EX03 Viper (Apocalyptic Soldier - Tactical)
    /// EX04 OfficerK (Security Officer - Karate / Enforcer)
    /// EX05 Kira (Stylized Female Fighter - Duelist)
    /// EX06 Sentry (Royal Army Guard - Spearman / Shield)
    /// EX07 Lyra (Low Poly Female - Arcane Sorceress)
    /// EX08 Garrick (Berserker Marauder - Berserker Fighter)
    /// </summary>
    public static class BBDotaHeroAssembler
    {
        public const string DOTA_PREFAB_DIR = "Assets/Prefabs/Characters/DotaHeroes";
        public const string CHAR_DATA_DIR = "Assets/CombatData/Characters";
        public const string MASTER_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller";
        public const string TRIGGER_FILE = "assemble_dota_heroes.trigger";

        public class DotaHeroSpec
        {
            public string Id;
            public string Name;
            public string Title;
            public string Archetype;
            public string SourceAssetPath;
            public string FbxPath;
            public BBWarriorType WarriorType;
            public string PrimaryWeapon;
            public HumanBodyBones PrimaryBone;
            public Vector3 PrimaryPos;
            public Vector3 PrimaryRot;
            public Vector3 PrimaryScale;
            public string SecondaryWeapon;
            public HumanBodyBones SecondaryBone;
            public Vector3 SecondaryPos;
            public Vector3 SecondaryRot;
            public Vector3 SecondaryScale;

            public DotaHeroSpec(
                string id, string name, string title, string archetype,
                string sourceAsset, string fbx, BBWarriorType warriorType,
                string primWep, HumanBodyBones primBone, Vector3 primPos, Vector3 primRot, Vector3 primScale,
                string secWep = "", HumanBodyBones secBone = HumanBodyBones.LeftHand, Vector3 secPos = default, Vector3 secRot = default, Vector3 secScale = default)
            {
                Id = id;
                Name = name;
                Title = title;
                Archetype = archetype;
                SourceAssetPath = sourceAsset;
                FbxPath = fbx;
                WarriorType = warriorType;
                PrimaryWeapon = primWep;
                PrimaryBone = primBone;
                PrimaryPos = primPos;
                PrimaryRot = primRot;
                PrimaryScale = primScale;
                SecondaryWeapon = secWep;
                SecondaryBone = secBone;
                SecondaryPos = secPos;
                SecondaryRot = secRot;
                SecondaryScale = secScale == default ? Vector3.one : secScale;
            }
        }

        public static readonly DotaHeroSpec[] HERO_SPECS = new DotaHeroSpec[]
        {
            // EX01: Hugo (Bionic Zombie Gorilla)
            new DotaHeroSpec(
                "EX01", "Hugo", "Primal Terror", "Heavy / Brute",
                "Assets/GAMWILL Character Pack Monster  Bionic Cartoon Zombie Gorilla/Prefab/HUGO_T_Pose.prefab",
                "Assets/GAMWILL Character Pack Monster  Bionic Cartoon Zombie Gorilla/Mesh/HUGO_T_Pose.fbx",
                BBWarriorType.Brute,
                "", HumanBodyBones.RightHand, Vector3.zero, Vector3.zero, Vector3.one
            ),

            // EX02: Granite (Sanctuary Rock Golem)
            new DotaHeroSpec(
                "EX02", "Granite", "Earth Vanguard", "Juggernaut / Hammer",
                "Assets/Santuary/Characters/SenctuaryGuardian_M.FBX",
                "Assets/Santuary/Characters/SenctuaryGuardian_M.FBX",
                BBWarriorType.Hammer,
                "Warrior_Hammer_Hammer", HumanBodyBones.RightHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.70f, 0.70f, 0.70f)
            ),

            // EX03: Viper (Apocalyptic Soldier)
            new DotaHeroSpec(
                "EX03", "Viper", "Wasteland Commando", "Tactical / Trench Knife",
                "Assets/Shady_3d/PREFAB/Apocalyptic character (Helmet +hood).prefab",
                "Assets/Shady_3d/FBX/Apocalyptic character.fbx",
                BBWarriorType.Swordsman,
                "Warrior_Swordsman_Sword", HumanBodyBones.RightHand, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(0.65f, 0.65f, 0.65f)
            ),

            // EX04: Officer K (Security Officer)
            new DotaHeroSpec(
                "EX04", "OfficerK", "Martial Enforcer", "Discipline / Karate",
                "Assets/JBlue/Security Officer/Prefab/Security Officer.prefab",
                "Assets/JBlue/Security Officer/3D Model/Security Officer.fbx",
                BBWarriorType.Karate,
                "", HumanBodyBones.RightHand, Vector3.zero, Vector3.zero, Vector3.one
            ),

            // EX05: Kira (Stylized Female Fighter)
            new DotaHeroSpec(
                "EX05", "Kira", "Crimson Duelist", "Agile / Duelist",
                "Assets/RetroStyleGames/StylizedCharacters/Stylized_Female_Character/Prefabs/P_Stylized_Female_Character_Free.prefab",
                "Assets/RetroStyleGames/StylizedCharacters/Stylized_Female_Character/Models/SK_Stylized_Female_Character.fbx",
                BBWarriorType.Swordsman,
                "Warrior_Swordsman_Sword", HumanBodyBones.RightHand, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(0.60f, 0.60f, 0.60f)
            ),

            // EX06: Sentry (Royal Army Guard)
            new DotaHeroSpec(
                "EX06", "Sentry", "Phalanx Sentinel", "Defender / Spearman",
                "Assets/SimpleAssets/Humans/Guard/Prefab/Guard_Yellow (Mobile).prefab",
                "Assets/SimpleAssets/Humans/Guard/FBX/Guard_Rig.fbx",
                BBWarriorType.Spearman,
                "Warrior_Spearman_Spear", HumanBodyBones.RightHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.50f, 0.50f, 0.50f)
            ),

            // EX07: Lyra (Low Poly Female - Arcane Sorceress)
            new DotaHeroSpec(
                "EX07", "Lyra", "Aether Weaver", "Arcane / Sorceress",
                "Assets/Shokubutsu Studio/Low Poly Female Free/URP/Prefabs/Smooth Shaded/Female 1 Smooth URP Prefab.prefab",
                "Assets/Shokubutsu Studio/Low Poly Female Free/Meshes/Smooth Shaded/Female 1.fbx",
                BBWarriorType.Sorceress,
                "Warrior_Mage_Staff", HumanBodyBones.RightHand, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(0.60f, 0.60f, 0.60f)
            ),

            // EX08: Garrick (Berserker Marauder)
            new DotaHeroSpec(
                "EX08", "Garrick", "Iron Berserker", "Frenzy / Marauder",
                "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Prefabs/Berserker.prefab",
                "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Characters/Berserker.fbx",
                BBWarriorType.Brute,
                "Warrior_Ninja_Sword", HumanBodyBones.RightHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.40f, 0.40f, 0.40f),
                "Warrior_Ninja_Sword", HumanBodyBones.LeftHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.40f, 0.40f, 0.40f)
            )
        };

        [InitializeOnLoadMethod]
        public static void InitializeTriggerListener()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch {}
                AssembleAllDotaHeroes();
            }
        }

        [MenuItem("Demon Idol/🚀 Playability/Assemble 8 External Dota Heroes", priority = 11)]
        public static void AssembleAllDotaHeroes()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: ASSEMBLE 8 EXTERNAL DOTA 2 HEROES              </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            if (!Directory.Exists(DOTA_PREFAB_DIR))
                Directory.CreateDirectory(DOTA_PREFAB_DIR);

            // Load shared attack data assets
            var lightAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
            var lowAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");
            var medAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
            var hvyAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
            var jmpAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");

            int assembledCount = 0;

            foreach (var spec in HERO_SPECS)
            {
                string targetPrefabPath = $"{DOTA_PREFAB_DIR}/{spec.Id}_{spec.Name}.prefab";
                Debug.Log($"[BBDotaHeroAssembler] Assembling {spec.Id} {spec.Name} ({spec.Title})...");

                GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.SourceAssetPath);
                if (sourcePrefab == null)
                {
                    Debug.LogError($"[BBDotaHeroAssembler] Source asset not found at {spec.SourceAssetPath} for {spec.Id}!");
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
                instance.name = $"{spec.Id}_{spec.Name}";
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                // Remove any missing script components
                foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                }

                // Configure standard Demon Idol components
                ConfigureFighterComponents(instance, spec, lightAtk, lowAtk, medAtk, hvyAtk, jmpAtk);

                // Save clean prefab
                PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
                UnityEngine.Object.DestroyImmediate(instance);
                AssetDatabase.ImportAsset(targetPrefabPath, ImportAssetOptions.ForceSynchronousImport);

                // Update CharacterDefinition ScriptableObject
                UpdateCharacterDefinition(spec, targetPrefabPath);

                assembledCount++;
                Debug.Log($"<color=#00e676>[BBDotaHeroAssembler] Successfully authored: {targetPrefabPath}</color>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[BBDotaHeroAssembler] COMPLETED: {assembledCount}/8 EXTERNAL DOTA HEROES ASSEMBLED!</b></color>");
        }

        private static void ConfigureFighterComponents(
            GameObject root, DotaHeroSpec spec,
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
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Resolve authentic Avatar from FBX if available
            if (!string.IsNullOrEmpty(spec.FbxPath))
            {
                AssetDatabase.ImportAsset(spec.FbxPath, ImportAssetOptions.ForceUpdate);
                var assets = AssetDatabase.LoadAllAssetsAtPath(spec.FbxPath);
                foreach (var a in assets)
                {
                    if (a is Avatar av)
                    {
                        anim.avatar = av;
                        break;
                    }
                }
            }

            RuntimeAnimatorController masterCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MASTER_CONTROLLER_PATH);
            if (masterCtrl != null)
            {
                anim.runtimeAnimatorController = masterCtrl;
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
            var zoneField = typeof(Hurtbox).GetField("_zone", BindingFlags.NonPublic | BindingFlags.Instance);
            if (zoneField != null) zoneField.SetValue(hurtbox, BodyZone.Torso);

            // 4. Hitbox
            Transform hitTrans = root.transform.Find("Hitbox");
            GameObject hitGo = hitTrans != null ? hitTrans.gameObject : null;
            if (hitGo == null)
            {
                hitGo = new GameObject("Hitbox");
                hitGo.transform.SetParent(root.transform, false);
            }
            hitGo.transform.localPosition = new Vector3(0, 1.0f, 0.8f);
            hitGo.transform.localRotation = Quaternion.identity;
            hitGo.transform.localScale = Vector3.one;

            BoxCollider hitCol = hitGo.GetComponent<BoxCollider>();
            if (hitCol == null) hitCol = hitGo.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.2f, 1.2f, 1.4f);
            hitCol.center = Vector3.zero;
            hitCol.isTrigger = true;

            Rigidbody hitRb = hitGo.GetComponent<Rigidbody>();
            if (hitRb == null) hitRb = hitGo.AddComponent<Rigidbody>();
            hitRb.isKinematic = true;
            hitRb.useGravity = false;
            hitRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Hitbox hitbox = hitGo.GetComponent<Hitbox>();
            if (hitbox == null) hitbox = hitGo.AddComponent<Hitbox>();

            // 5. FighterController
            FighterController fc = root.GetComponent<FighterController>();
            if (fc == null) fc = root.AddComponent<FighterController>();
            fc.fighterName = spec.Name;
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

            fc.animator = anim;
            fc.activeHitbox = hitbox;

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

            // 7. AnimationEventReceiver
            AnimationEventReceiver aer = root.GetComponent<AnimationEventReceiver>();
            if (aer == null) aer = root.AddComponent<AnimationEventReceiver>();

            // 8. BBWeaponRig
            BBWeaponRig rig = root.GetComponent<BBWeaponRig>();
            if (rig == null) rig = root.AddComponent<BBWeaponRig>();
            rig.warriorType = spec.WarriorType;

            if (!string.IsNullOrEmpty(spec.PrimaryWeapon))
            {
                rig.primaryWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Weapons/Warrior/{spec.PrimaryWeapon}.prefab");
            }
            rig.primarySocket = spec.PrimaryBone;
            rig.primaryPositionOffset = spec.PrimaryPos;
            rig.primaryRotationOffset = spec.PrimaryRot;
            rig.primaryScale = spec.PrimaryScale;

            if (!string.IsNullOrEmpty(spec.SecondaryWeapon))
            {
                rig.secondaryWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Weapons/Warrior/{spec.SecondaryWeapon}.prefab");
            }
            rig.secondarySocket = spec.SecondaryBone;
            rig.secondaryPositionOffset = spec.SecondaryPos;
            rig.secondaryRotationOffset = spec.SecondaryRot;
            rig.secondaryScale = spec.SecondaryScale;
        }

        private static void UpdateCharacterDefinition(DotaHeroSpec spec, string prefabPath)
        {
            if (!Directory.Exists(CHAR_DATA_DIR))
                Directory.CreateDirectory(CHAR_DATA_DIR);

            string defPath = $"{CHAR_DATA_DIR}/CharacterDefinition_{spec.Id}_{spec.Name}.asset";
            CharacterDefinition def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(defPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(def, defPath);
            }

            def.id = spec.Id;
            def.displayName = spec.Name;
            def.title = spec.Title;
            def.archetype = spec.Archetype;
            def.description = $"Dota 2 style hero: {spec.Name}, the {spec.Title}. Equipped with {spec.WarriorType} stance.";
            def.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            def.speed = 1.0f;
            def.power = 1.0f;
            def.reach = 1.0f;
            def.defense = 1.0f;
            def.signatureStageId = "ST01";

            EditorUtility.SetDirty(def);
        }
    }
}
#endif
