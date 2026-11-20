#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Phase 1 Assembler: Assembles and clothes the 8 Synty Sidekick characters.
    /// Clothe the naked base bodies (HumanSpecies 01-04) using modular outfit meshes,
    /// configures Animator, CharacterController, FighterController, BBWeaponRig, Hitbox and Hurtbox,
    /// and saves clean standalone prefabs in Assets/Prefabs/Characters/Sidekicks/.
    /// </summary>
    public static class BBSyntyCharacterAssembler
    {
        public const string SIDEKICK_PREFAB_DIR = "Assets/Prefabs/Characters/Sidekicks";
        public const string CHAR_DATA_DIR = "Assets/CombatData/Characters";
        public const string MASTER_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller";

        public class SidekickSpec
        {
            public string Id;
            public string Name;
            public string Title;
            public string Archetype;
            public string BasePrefabPath;
            public string HeadFbxPath;
            public string MaterialPath;
            public string[] OutfitFbxPaths;
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
            public string WarriorStanceName;

            public SidekickSpec(
                string id, string name, string title, string archetype,
                string basePrefab, string headFbx, string material,
                string[] outfitFbxs,
                string primWep, HumanBodyBones primBone, Vector3 primPos, Vector3 primRot, Vector3 primScale,
                string secWep = "", HumanBodyBones secBone = HumanBodyBones.LeftHand, Vector3 secPos = default, Vector3 secRot = default, Vector3 secScale = default,
                string stance = "Swordsman")
            {
                Id = id;
                Name = name;
                Title = title;
                Archetype = archetype;
                BasePrefabPath = basePrefab;
                HeadFbxPath = headFbx;
                MaterialPath = material;
                OutfitFbxPaths = outfitFbxs ?? new string[0];
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
                WarriorStanceName = stance;
            }
        }

        private static readonly string HEAD_FBX_BASE = "Assets/Synty/SidekickCharacters/Resources/Meshes/Species/Humans/SK_HUMN_BASE_01_01HEAD_HU01.fbx";

        public static readonly SidekickSpec[] SIDEKICK_SPECS = new SidekickSpec[]
        {
            // SK01: Valen (Rogue / Ninja)
            new SidekickSpec(
                "SK01", "Valen", "Shadow Scout", "Rogue / Ninja",
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_01/Starter_01.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_01/Materials/Starter_01.mat",
                null,
                "Warrior_Ninja_Sword", HumanBodyBones.RightHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.35f, 0.35f, 0.35f),
                "Warrior_Ninja_Sword", HumanBodyBones.LeftHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.35f, 0.35f, 0.35f),
                "Ninja"
            ),

            // SK02: Galahad (Knight / Shield)
            new SidekickSpec(
                "SK02", "Galahad", "Sun Templar", "Knight / Shield",
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_02/Starter_02.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_02/Materials/Starter_02.mat",
                null,
                "Warrior_Knight_Sword", HumanBodyBones.RightHand, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(0.65f, 0.65f, 0.65f),
                "Warrior_Knight_Shield", HumanBodyBones.LeftLowerArm, new Vector3(0f, 0f, 0.05f), new Vector3(0f, 90f, 0f), new Vector3(0.58f, 0.58f, 0.58f),
                "Knight"
            ),

            // SK03: Vex (Tech Ranger / Crossbow)
            new SidekickSpec(
                "SK03", "Vex", "Outland Ranger", "Ranger / Crossbow",
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_03/Starter_03.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_03/Materials/Starter_03.mat",
                null,
                "Warrior_Crossbow_Crossbow", HumanBodyBones.RightHand, new Vector3(0.04f, 0.04f, 0f), Vector3.zero, new Vector3(0.60f, 0.60f, 0.60f),
                "Warrior_Crossbow_Arrow", HumanBodyBones.LeftHand, new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.60f, 0.60f, 0.60f),
                "Crossbow"
            ),

            // SK04: Jin (Iron Fist / Karate)
            new SidekickSpec(
                "SK04", "Jin", "Iron Fist", "Brawler / Martial Arts",
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_04/Starter_04.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_04/Materials/Starter_04.mat",
                null,
                "", HumanBodyBones.RightHand, Vector3.zero, Vector3.zero, Vector3.one,
                "", HumanBodyBones.LeftHand, Vector3.zero, Vector3.zero, Vector3.one,
                "Karate"
            ),

            // SK05: Lord Morvath (Black Iron Vanguard / Two-Handed Greatsword)
            new SidekickSpec(
                "SK05", "LordMorvath", "Black Iron Vanguard", "Vanguard / Greatsword",
                "Assets/Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_01/HumanSpecies_01.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_01/Materials/Starter_01.mat",
                new string[]
                {
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_10TORS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_11AUPL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_12AUPR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_13ALWL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_14ALWR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_15HNDL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_16HNDR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_17HIPS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_18LEGL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_19LEGR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_20FOTL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_21FOTR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_22AHED_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_29ASHL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_30ASHR_HU01.fbx"
                },
                "Warrior_2Handed_Greatsword", HumanBodyBones.RightHand, new Vector3(0f, 0.06f, 0f), Vector3.zero, new Vector3(0.72f, 0.72f, 0.72f),
                "", HumanBodyBones.LeftHand, Vector3.zero, Vector3.zero, Vector3.one,
                "TwoHanded"
            ),

            // SK06: Commander Ren (Exo-Dragoon / Spearman)
            new SidekickSpec(
                "SK06", "CommanderRen", "Exo-Dragoon", "Tactical / Spearman",
                "Assets/Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_02/HumanSpecies_02.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_02/Materials/Starter_02.mat",
                new string[]
                {
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_10TORS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_11AUPL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_12AUPR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_13ALWL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_14ALWR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_15HNDL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_16HNDR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_17HIPS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_18LEGL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_19LEGR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_20FOTL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_21FOTR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_22AHED_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_29ASHL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_SCFI_CIVL_09_30ASHR_HU01.fbx"
                },
                "Warrior_Spearman_Spear", HumanBodyBones.RightHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f),
                "", HumanBodyBones.LeftHand, Vector3.zero, Vector3.zero, Vector3.one,
                "Spearman"
            ),

            // SK07: Malakar (Abyssal Vizier / Mage)
            new SidekickSpec(
                "SK07", "Malakar", "Abyssal Vizier", "Dark Arcane / Mage",
                "Assets/Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_03/HumanSpecies_03.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_04/Materials/Starter_04.mat",
                new string[]
                {
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_HORR_VILN_01_22AHED_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_10TORS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_17HIPS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_18LEGL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_19LEGR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_20FOTL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_21FOTR_HU01.fbx"
                },
                "Warrior_Mage_Staff", HumanBodyBones.RightHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.65f, 0.65f, 0.65f),
                "", HumanBodyBones.LeftHand, Vector3.zero, Vector3.zero, Vector3.one,
                "Mage"
            ),

            // SK08: Torgrim (Storm Breaker / Hammer)
            new SidekickSpec(
                "SK08", "Torgrim", "Storm Breaker", "Heavy Bruiser / Hammer",
                "Assets/Synty/SidekickCharacters/Characters/HumanSpecies/HumanSpecies_04/HumanSpecies_04.prefab",
                HEAD_FBX_BASE,
                "Assets/Synty/SidekickCharacters/Characters/Starter/Starter_03/Materials/Starter_03.mat",
                new string[]
                {
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_29ASHL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_30ASHR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_33AKNL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_34AKNR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_10TORS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_17HIPS_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_18LEGL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_19LEGR_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_20FOTL_HU01.fbx",
                    "Assets/Synty/SidekickCharacters/Resources/Meshes/Outfits/Starter/SK_FANT_KNGT_17_21FOTR_HU01.fbx"
                },
                "Warrior_Hammer_Hammer", HumanBodyBones.RightHand, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.60f, 0.60f, 0.60f),
                "", HumanBodyBones.LeftHand, Vector3.zero, Vector3.zero, Vector3.one,
                "Hammer"
            )
        };

        public const string TRIGGER_FILE = "assemble_sidekicks.trigger";

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
                AssembleAllSidekicks();
            }
        }

        [MenuItem("Demon Idol/🚀 Playability/Assemble 8 Sidekick Heroes", priority = 10)]
        public static void AssembleAllSidekicks()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: ASSEMBLE & CLOTHE 8 SYNTY SIDEKICK HEROES      </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            if (!Directory.Exists(SIDEKICK_PREFAB_DIR))
                Directory.CreateDirectory(SIDEKICK_PREFAB_DIR);

            // Load shared attack data assets
            var lightAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
            var lowAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");
            var medAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
            var hvyAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
            var jmpAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");

            int assembledCount = 0;

            foreach (var spec in SIDEKICK_SPECS)
            {
                string targetPrefabPath = $"{SIDEKICK_PREFAB_DIR}/{spec.Id}_{spec.Name}.prefab";
                Debug.Log($"[BBSyntyCharacterAssembler] Assembling {spec.Id} {spec.Name} ({spec.Title})...");

                GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.BasePrefabPath);
                if (sourcePrefab == null)
                {
                    Debug.LogError($"[BBSyntyCharacterAssembler] Base prefab not found at {spec.BasePrefabPath} for {spec.Id}!");
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
                instance.name = $"{spec.Id}_{spec.Name}";
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                // Build bone mapping dictionary for skeleton
                Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                {
                    if (!boneMap.ContainsKey(t.name))
                        boneMap[t.name] = t;
                }

                Material targetMat = AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath);

                // If this spec has modular outfit meshes to attach (e.g. SK05-SK08)
                if (spec.OutfitFbxPaths != null && spec.OutfitFbxPaths.Length > 0)
                {
                    // Disable base naked mesh to prevent clipping
                    Transform baseMeshTrans = instance.transform.Find("mesh");
                    if (baseMeshTrans != null)
                    {
                        baseMeshTrans.gameObject.SetActive(false);
                    }

                    // Attach head mesh first if not already present
                    AttachMeshPart(instance, spec.HeadFbxPath, boneMap, targetMat);

                    // Attach all outfit meshes
                    foreach (var fbxPath in spec.OutfitFbxPaths)
                    {
                        AttachMeshPart(instance, fbxPath, boneMap, targetMat);
                    }
                }
                else
                {
                    // For SK01-SK04, ensure material is assigned to existing mesh
                    Transform existingMeshTrans = instance.transform.Find("mesh");
                    if (existingMeshTrans != null && targetMat != null)
                    {
                        var smr = existingMeshTrans.GetComponent<SkinnedMeshRenderer>();
                        if (smr != null)
                        {
                            smr.sharedMaterial = targetMat;
                        }
                    }
                }

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
                Debug.Log($"<color=#00e676>[BBSyntyCharacterAssembler] Successfully authored: {targetPrefabPath}</color>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[BBSyntyCharacterAssembler] COMPLETED: {assembledCount}/8 SIDEKICK HEROES ASSEMBLED!</b></color>");
        }

        private static void AttachMeshPart(GameObject characterRoot, string fbxPath, Dictionary<string, Transform> boneMap, Material mat)
        {
            if (string.IsNullOrEmpty(fbxPath)) return;
            GameObject fbxGo = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxGo == null)
            {
                Debug.LogWarning($"[BBSyntyCharacterAssembler] Outfit FBX not found: {fbxPath}");
                return;
            }

            SkinnedMeshRenderer sourceSmr = fbxGo.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (sourceSmr == null || sourceSmr.sharedMesh == null)
            {
                // Might be regular MeshRenderer
                MeshFilter mf = fbxGo.GetComponentInChildren<MeshFilter>(true);
                if (mf != null && mf.sharedMesh != null)
                {
                    GameObject staticChild = new GameObject(Path.GetFileNameWithoutExtension(fbxPath));
                    staticChild.transform.SetParent(characterRoot.transform, false);
                    MeshFilter newMf = staticChild.AddComponent<MeshFilter>();
                    newMf.sharedMesh = mf.sharedMesh;
                    MeshRenderer newMr = staticChild.AddComponent<MeshRenderer>();
                    newMr.sharedMaterial = mat;
                }
                return;
            }

            GameObject partGo = new GameObject(Path.GetFileNameWithoutExtension(fbxPath));
            partGo.transform.SetParent(characterRoot.transform, false);
            SkinnedMeshRenderer smr = partGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = sourceSmr.sharedMesh;
            smr.sharedMaterial = mat;

            Transform[] mappedBones = new Transform[sourceSmr.bones.Length];
            for (int i = 0; i < sourceSmr.bones.Length; i++)
            {
                if (sourceSmr.bones[i] != null && boneMap.TryGetValue(sourceSmr.bones[i].name, out Transform b))
                {
                    mappedBones[i] = b;
                }
                else
                {
                    mappedBones[i] = characterRoot.transform;
                }
            }
            smr.bones = mappedBones;

            if (sourceSmr.rootBone != null && boneMap.TryGetValue(sourceSmr.rootBone.name, out Transform r))
            {
                smr.rootBone = r;
            }
            else
            {
                smr.rootBone = boneMap.ContainsKey("root") ? boneMap["root"] : characterRoot.transform;
            }
        }

        private static void ConfigureFighterComponents(
            GameObject root, SidekickSpec spec,
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

            // Resolve authentic Avatar from Head FBX
            if (!string.IsNullOrEmpty(spec.HeadFbxPath))
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(spec.HeadFbxPath);
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
            var zoneField = typeof(Hurtbox).GetField("_zone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
            if (Enum.TryParse<BBWarriorType>(spec.WarriorStanceName, out var wt))
            {
                rig.warriorType = wt;
            }
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

        private static void UpdateCharacterDefinition(SidekickSpec spec, string prefabPath)
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
            def.description = $"Dota 2 style hero: {spec.Name}, the {spec.Title}. Equipped with {spec.WarriorStanceName} stance.";
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
