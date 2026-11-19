#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Phase 1 Builder: Extracts authentic weapon sub-meshes from ExplosiveLLC Warrior Bundles 1, 2, and 3,
    /// bakes standalone weapon prefabs in Assets/Prefabs/Weapons/Warrior/, and configures BBWeaponRig on all roster fighters.
    /// </summary>
    public static class BBWarriorWeaponBuilder
    {
        public const string WEAPON_PREFAB_DIR = "Assets/Prefabs/Weapons/Warrior";

        public struct WeaponSpec
        {
            public string prefabName;
            public string fbxPath;
            public string meshName;
            public string matPath;
            public Vector3 pivotOffset;
            public Vector3 rotOffset;
            public Vector3 scale;
        }

        private static readonly WeaponSpec[] WEAPON_SPECS = new WeaponSpec[]
        {
            // Swordsman Sword
            new WeaponSpec
            {
                prefabName = "Warrior_Swordsman_Sword",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Characters/Swordsman.FBX",
                meshName = "Mesh-Swordsman-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Materials/Swordsman.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Knight Sword
            new WeaponSpec
            {
                prefabName = "Warrior_Knight_Sword",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Characters/Knight.FBX",
                meshName = "Mesh-Knight-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Materials/Knight.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Knight Shield
            new WeaponSpec
            {
                prefabName = "Warrior_Knight_Shield",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Characters/Knight.FBX",
                meshName = "Mesh-Knight-Shield",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Materials/Knight.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // 2-Handed Greatsword
            new WeaponSpec
            {
                prefabName = "Warrior_2Handed_Greatsword",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Characters/2Handed.FBX",
                meshName = "Mesh-TwoHanded-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Materials/2Handed.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Spearman Spear
            new WeaponSpec
            {
                prefabName = "Warrior_Spearman_Spear",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Characters/Spearman.FBX",
                meshName = "Mesh-Spearman-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Materials/Spearman.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Hammer
            new WeaponSpec
            {
                prefabName = "Warrior_Hammer_Warhammer",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Characters/Hammer.FBX",
                meshName = "Mesh-Hammer-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Materials/Hammer.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Crossbow
            new WeaponSpec
            {
                prefabName = "Warrior_Crossbow",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Characters/Crossbow.FBX",
                meshName = "Mesh-Crossbow-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Materials/Crossbow.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Archer Bow
            new WeaponSpec
            {
                prefabName = "Warrior_Archer_Bow",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Characters/Archer.FBX",
                meshName = "Mesh-Archer-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Materials/Archer.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Archer Arrow
            new WeaponSpec
            {
                prefabName = "Warrior_Archer_Arrow",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Characters/Archer.FBX",
                meshName = "Mesh-Archer-Arrow",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Materials/Archer.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Mage Staff
            new WeaponSpec
            {
                prefabName = "Warrior_Mage_Staff",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Characters/Mage.FBX",
                meshName = "Mesh-Mage-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Materials/Mage.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            },
            // Ninja Dual Swords
            new WeaponSpec
            {
                prefabName = "Warrior_Ninja_Sword",
                fbxPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Characters/Ninja.FBX",
                meshName = "Mesh-Ninja-Weapon",
                matPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Materials/Ninja.mat",
                pivotOffset = Vector3.zero,
                rotOffset = Vector3.zero,
                scale = Vector3.one
            }
        };

        [MenuItem("Demon Idol/BlazBlue/Build All Warrior Weapon Prefabs", priority = 50)]
        public static void BuildAllWeaponPrefabs()
        {
            if (!Directory.Exists(WEAPON_PREFAB_DIR))
            {
                Directory.CreateDirectory(WEAPON_PREFAB_DIR);
            }

            int count = 0;
            foreach (var spec in WEAPON_SPECS)
            {
                Mesh mesh = FindMesh(spec.fbxPath, spec.meshName);
                if (mesh == null)
                {
                    Debug.LogWarning($"[BBWarriorWeaponBuilder] Mesh '{spec.meshName}' not found in '{spec.fbxPath}'!");
                    continue;
                }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(spec.matPath);
                if (mat == null)
                {
                    Debug.LogWarning($"[BBWarriorWeaponBuilder] Material not found at '{spec.matPath}'!");
                    continue;
                }

                string prefabPath = $"{WEAPON_PREFAB_DIR}/{spec.prefabName}.prefab";
                GameObject root = new GameObject(spec.prefabName);
                
                // Visual Mesh Child
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = spec.pivotOffset;
                visual.transform.localRotation = Quaternion.Euler(spec.rotOffset);
                visual.transform.localScale = spec.scale;

                MeshFilter mf = visual.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                MeshRenderer mr = visual.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676>[BBWarriorWeaponBuilder] SUCCESS: Built {count} Warrior Weapon Prefabs in {WEAPON_PREFAB_DIR}!</color>");
        }

        private static Mesh FindMesh(string fbxPath, string targetName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is Mesh m)
                {
                    Debug.Log($"[FindMesh] Found Mesh in '{System.IO.Path.GetFileName(fbxPath)}': '{m.name}'");
                    if (string.Equals(m.name, targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        return m;
                    }
                }
            }
            // Secondary search for contains
            foreach (var a in assets)
            {
                if (a is Mesh m && m.name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return m;
                }
            }
            // Tertiary search in SkinnedMeshRenderers or MeshFilters on sub-GameObjects
            foreach (var a in assets)
            {
                if (a is GameObject go)
                {
                    var smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr != null && smr.sharedMesh != null)
                    {
                        Debug.Log($"[FindMesh] Found SMR Mesh on GO '{go.name}': '{smr.sharedMesh.name}'");
                        if (string.Equals(smr.sharedMesh.name, targetName, StringComparison.OrdinalIgnoreCase) ||
                            smr.sharedMesh.name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return smr.sharedMesh;
                        }
                    }
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        Debug.Log($"[FindMesh] Found MF Mesh on GO '{go.name}': '{mf.sharedMesh.name}'");
                        if (string.Equals(mf.sharedMesh.name, targetName, StringComparison.OrdinalIgnoreCase) ||
                            mf.sharedMesh.name.IndexOf(targetName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return mf.sharedMesh;
                        }
                    }
                }
            }
            return null;
        }

        [MenuItem("Demon Idol/BlazBlue/Configure All Character Weapon Rigs", priority = 51)]
        public static void ConfigureAllCharacterWeaponRigs()
        {
            BuildAllWeaponPrefabs();

            var charConfigs = new[]
            {
                new { Id = "C01", Prefab = "Assets/Prefabs/Characters/C01_Akio.prefab", Warrior = BBWarriorType.Swordsman, Primary = "Warrior_Swordsman_Sword", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, 0.04f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.75f, 0.75f, 0.75f), Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C02", Prefab = "Assets/Prefabs/Characters/C02_Aika.prefab", Warrior = BBWarriorType.Karate, Primary = "", PrimBone = HumanBodyBones.RightHand, PrimPos = Vector3.zero, PrimRot = Vector3.zero, PrimScale = Vector3.one, Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C03", Prefab = "Assets/Prefabs/Characters/C03_Ren.prefab", Warrior = BBWarriorType.Brute, Primary = "", PrimBone = HumanBodyBones.RightHand, PrimPos = Vector3.zero, PrimRot = Vector3.zero, PrimScale = Vector3.one, Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C04", Prefab = "Assets/Prefabs/Characters/C04_UnityChan.prefab", Warrior = BBWarriorType.Archer, Primary = "Warrior_Archer_Arrow", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, 0.02f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.55f, 0.55f, 0.55f), Sec = "Warrior_Archer_Bow", SecBone = HumanBodyBones.LeftHand, SecPos = new Vector3(0f, 0.04f, 0f), SecRot = new Vector3(0f, 90f, 0f), SecScale = new Vector3(0.58f, 0.58f, 0.58f) },
                new { Id = "C05", Prefab = "Assets/Prefabs/Characters/C05_Akane.prefab", Warrior = BBWarriorType.Spearman, Primary = "Warrior_Spearman_Spear", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, -0.12f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.45f, 0.45f, 0.45f), Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C06", Prefab = "Assets/Prefabs/Characters/C06_Cultist.prefab", Warrior = BBWarriorType.Mage, Primary = "Warrior_Mage_Staff", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, -0.22f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.85f, 0.85f, 0.85f), Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C07", Prefab = "Assets/Prefabs/Characters/C07_StellarWitch.prefab", Warrior = BBWarriorType.Sorceress, Primary = "", PrimBone = HumanBodyBones.RightHand, PrimPos = Vector3.zero, PrimRot = Vector3.zero, PrimScale = Vector3.one, Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C08", Prefab = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab", Warrior = BBWarriorType.Ninja, Primary = "Warrior_Ninja_Sword", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, 0.02f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.35f, 0.35f, 0.35f), Sec = "Warrior_Ninja_Sword", SecBone = HumanBodyBones.LeftHand, SecPos = new Vector3(0f, 0.02f, 0f), SecRot = Vector3.zero, SecScale = new Vector3(0.35f, 0.35f, 0.35f) },
                new { Id = "C09", Prefab = "Assets/Prefabs/Characters/C09_PicoChan.prefab", Warrior = BBWarriorType.Hammer, Primary = "Warrior_Hammer_Warhammer", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, -0.10f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.60f, 0.60f, 0.60f), Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C10", Prefab = "Assets/Prefabs/Characters/C10_Reika.prefab", Warrior = BBWarriorType.Knight, Primary = "Warrior_Knight_Sword", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, 0.04f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.62f, 0.62f, 0.62f), Sec = "Warrior_Knight_Shield", SecBone = HumanBodyBones.LeftLowerArm, SecPos = new Vector3(0f, 0.06f, -0.04f), SecRot = new Vector3(0f, 90f, 0f), SecScale = new Vector3(0.58f, 0.58f, 0.58f) },
                new { Id = "C11", Prefab = "Assets/Prefabs/Characters/C11_Casual1.prefab", Warrior = BBWarriorType.Crossbow, Primary = "Warrior_Crossbow", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, 0.04f, 0.04f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.52f, 0.52f, 0.52f), Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
                new { Id = "C12", Prefab = "Assets/Prefabs/Characters/C12_Fina.prefab", Warrior = BBWarriorType.TwoHanded, Primary = "Warrior_2Handed_Greatsword", PrimBone = HumanBodyBones.RightHand, PrimPos = new Vector3(0f, 0.06f, 0f), PrimRot = Vector3.zero, PrimScale = new Vector3(0.72f, 0.72f, 0.72f), Sec = "", SecBone = HumanBodyBones.LeftHand, SecPos = Vector3.zero, SecRot = Vector3.zero, SecScale = Vector3.one },
            };

            int updated = 0;
            foreach (var cfg in charConfigs)
            {
                if (!File.Exists(cfg.Prefab))
                {
                    Debug.LogWarning($"[BBWarriorWeaponBuilder] Character prefab not found at '{cfg.Prefab}'");
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(cfg.Prefab);
                if (root == null) continue;

                BBWeaponRig rig = root.GetComponent<BBWeaponRig>();
                if (rig == null)
                {
                    rig = root.AddComponent<BBWeaponRig>();
                }

                rig.warriorType = cfg.Warrior;
                rig.primarySocket = cfg.PrimBone;
                rig.primaryPositionOffset = cfg.PrimPos;
                rig.primaryRotationOffset = cfg.PrimRot;
                rig.primaryScale = cfg.PrimScale;

                if (!string.IsNullOrEmpty(cfg.Primary))
                {
                    string pPath = $"{WEAPON_PREFAB_DIR}/{cfg.Primary}.prefab";
                    rig.primaryWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                }
                else
                {
                    rig.primaryWeaponPrefab = null;
                }

                rig.secondarySocket = cfg.SecBone;
                rig.secondaryPositionOffset = cfg.SecPos;
                rig.secondaryRotationOffset = cfg.SecRot;
                rig.secondaryScale = cfg.SecScale;

                if (!string.IsNullOrEmpty(cfg.Sec))
                {
                    string sPath = $"{WEAPON_PREFAB_DIR}/{cfg.Sec}.prefab";
                    rig.secondaryWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sPath);
                }
                else
                {
                    rig.secondaryWeaponPrefab = null;
                }

                PrefabUtility.SaveAsPrefabAsset(root, cfg.Prefab);
                PrefabUtility.UnloadPrefabContents(root);
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676>[BBWarriorWeaponBuilder] SUCCESS: Configured BBWeaponRig on {updated} character prefabs!</color>");
        }

        [MenuItem("Demon Idol/BlazBlue/Execute Phase 1 (Weapons and Rigs)", priority = 49)]
        public static void ExecutePhase1()
        {
            BuildAllWeaponPrefabs();
            ConfigureAllCharacterWeaponRigs();
        }
    }
}
#endif
