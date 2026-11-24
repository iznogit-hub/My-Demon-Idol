#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Visual;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Master automation pipeline that maps all 12 Launch Fighters strictly 1-to-1
    /// onto 12 unique ExplosiveLLC Warrior Packs with the clean 7-move standard:
    /// (5A, 5B, 5C, 2C, j.B, 5D, 236236C).
    /// </summary>
    public static class BBWarriorBundleMapper
    {
        public struct ClipSliceDef
        {
            public string clipName;
            public string takeName;
            public int firstFrame;
            public int lastFrame;
        }

        public struct WarriorPackConfig
        {
            public string characterId;
            public string fighterName;
            public BBWarriorType warriorType;
            public BBArchetypeClass archetypeClass;
            public float hipHeight;
            public float weaponReach;
            public string driveName;

            public string bundleFolder;
            public string packFolder;
            public string clipPrefix;
            public string attackFbxRelPath;
            public string idleFbxRelPath;
            public string walkFbxRelPath;
            public string walkSlowFbxRelPath;
            public string runFbxRelPath;
            public string jumpFbxRelPath;
            public string fallFbxRelPath;

            public string primaryWeaponPrefab;
            public HumanBodyBones primarySocket;
            public Vector3 primaryPosOffset;
            public Vector3 primaryRotOffset;
            public Vector3 primaryScale;

            public string secondaryWeaponPrefab;
            public HumanBodyBones secondarySocket;
            public Vector3 secondaryPosOffset;
            public Vector3 secondaryRotOffset;
            public Vector3 secondaryScale;

            public bool hasProjectile;
            public ClipSliceDef[] attackClips;
        }

        public static readonly WarriorPackConfig[] PackConfigs = new WarriorPackConfig[]
        {
            // 1. C01 Akio: Bundle 1 — Karate (Rushdown, Unarmed)
            new WarriorPackConfig
            {
                characterId = "C01",
                fighterName = "Akio",
                warriorType = BBWarriorType.Karate,
                archetypeClass = BBArchetypeClass.Rushdown,
                hipHeight = 0.95f,
                weaponReach = 0.85f,
                driveName = "Iron Rush",
                bundleFolder = "Warrior Pack Bundle 1 FREE",
                packFolder = "Karate Warrior Mecanim Animation Pack",
                clipPrefix = "Karate",
                attackFbxRelPath = "Animations/Karate@Attack1.FBX",
                idleFbxRelPath = "Animations/Karate@Idle.FBX",
                walkFbxRelPath = "Animations/Karate@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Karate@WalkSlow.FBX",
                runFbxRelPath = "Animations/Karate@Run.FBX",
                jumpFbxRelPath = "Animations/Karate@Jump.FBX",
                fallFbxRelPath = "Animations/Karate@Fall.FBX",
                primaryWeaponPrefab = null,
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = Vector3.zero,
                primaryRotOffset = Vector3.zero,
                primaryScale = Vector3.one,
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Karate_Jab", takeName = "Take 001", firstFrame = 0, lastFrame = 22 },
                    new ClipSliceDef { clipName = "Karate_Palm", takeName = "Take 001", firstFrame = 22, lastFrame = 45 },
                    new ClipSliceDef { clipName = "Karate_Roundhouse", takeName = "Take 001", firstFrame = 45, lastFrame = 68 },
                    new ClipSliceDef { clipName = "Karate_Sweep", takeName = "Take 001", firstFrame = 45, lastFrame = 64 },
                    new ClipSliceDef { clipName = "Karate_AirKick", takeName = "Take 001", firstFrame = 10, lastFrame = 32 },
                    new ClipSliceDef { clipName = "Karate_IronRush", takeName = "Take 001", firstFrame = 15, lastFrame = 40 },
                    new ClipSliceDef { clipName = "Karate_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 68 }
                }
            },

            // 2. C02 Aika: Bundle 1 — Ninja (Rushdown, Ninja Sword)
            new WarriorPackConfig
            {
                characterId = "C02",
                fighterName = "Aika",
                warriorType = BBWarriorType.Ninja,
                archetypeClass = BBArchetypeClass.Rushdown,
                hipHeight = 0.89f,
                weaponReach = 0.85f,
                driveName = "Resonance Nail",
                bundleFolder = "Warrior Pack Bundle 1 FREE",
                packFolder = "Ninja Warrior Mecanim Animation Pack",
                clipPrefix = "Ninja",
                attackFbxRelPath = "Animations/Ninja@Attack1.FBX",
                idleFbxRelPath = "Animations/Ninja@Idle.FBX",
                walkFbxRelPath = "Animations/Ninja@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Ninja@WalkSlow.FBX",
                runFbxRelPath = "Animations/Ninja@Run.FBX",
                jumpFbxRelPath = "Animations/Ninja@Jump.FBX",
                fallFbxRelPath = "Animations/Ninja@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Ninja_Sword.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = Vector3.zero,
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.80f, 0.80f, 0.80f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Ninja_Slash1", takeName = "Take 001", firstFrame = 0, lastFrame = 18 },
                    new ClipSliceDef { clipName = "Ninja_Slash2", takeName = "Take 001", firstFrame = 18, lastFrame = 36 },
                    new ClipSliceDef { clipName = "Ninja_Cross", takeName = "Take 001", firstFrame = 36, lastFrame = 52 },
                    new ClipSliceDef { clipName = "Ninja_Sweep", takeName = "Take 001", firstFrame = 30, lastFrame = 48 },
                    new ClipSliceDef { clipName = "Ninja_AirStrike", takeName = "Take 001", firstFrame = 10, lastFrame = 28 },
                    new ClipSliceDef { clipName = "Ninja_Drive", takeName = "Take 001", firstFrame = 12, lastFrame = 36 },
                    new ClipSliceDef { clipName = "Ninja_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 52 }
                }
            },

            // 3. C05 Akane: Bundle 1 — Sorceress (Zoner, Arcane Staff)
            new WarriorPackConfig
            {
                characterId = "C05",
                fighterName = "Akane",
                warriorType = BBWarriorType.Sorceress,
                archetypeClass = BBArchetypeClass.Zoner,
                hipHeight = 0.91f,
                weaponReach = 1.10f,
                driveName = "Echoing Mandate",
                bundleFolder = "Warrior Pack Bundle 1 FREE",
                packFolder = "Sorceress Warrior Mecanim Animation Pack",
                clipPrefix = "Sorceress",
                attackFbxRelPath = "Animations/Sorceress@Attack1.FBX",
                idleFbxRelPath = "Animations/Sorceress@Idle.FBX",
                walkFbxRelPath = "Animations/Sorceress@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Sorceress@Walk.FBX",
                runFbxRelPath = "Animations/Sorceress@Run.FBX",
                jumpFbxRelPath = "Animations/Sorceress@Jump.FBX",
                fallFbxRelPath = "Animations/Sorceress@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Mage_Staff.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = new Vector3(0f, -0.12f, 0f),
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.80f, 0.80f, 0.80f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = true,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Sorceress_Burst", takeName = "Take 001", firstFrame = 0, lastFrame = 26 },
                    new ClipSliceDef { clipName = "Sorceress_Wave", takeName = "Take 001", firstFrame = 26, lastFrame = 52 },
                    new ClipSliceDef { clipName = "Sorceress_Nova", takeName = "Take 001", firstFrame = 52, lastFrame = 80 },
                    new ClipSliceDef { clipName = "Sorceress_Sweep", takeName = "Take 001", firstFrame = 45, lastFrame = 70 },
                    new ClipSliceDef { clipName = "Sorceress_Air", takeName = "Take 001", firstFrame = 12, lastFrame = 36 },
                    new ClipSliceDef { clipName = "Sorceress_Drive", takeName = "Take 001", firstFrame = 20, lastFrame = 50 },
                    new ClipSliceDef { clipName = "Sorceress_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 80 }
                }
            },

            // 4. EX04 OfficerK: Bundle 1 — Brute (Powerhouse, Riot Maul)
            new WarriorPackConfig
            {
                characterId = "EX04",
                fighterName = "OfficerK",
                warriorType = BBWarriorType.Brute,
                archetypeClass = BBArchetypeClass.Grappler,
                hipHeight = 0.96f,
                weaponReach = 0.95f,
                driveName = "Discipline",
                bundleFolder = "Warrior Pack Bundle 1 FREE",
                packFolder = "Brute Warrior Mecanim Animation Pack",
                clipPrefix = "Brute",
                attackFbxRelPath = "Animations/Brute@Attack1.FBX",
                idleFbxRelPath = "Animations/Brute@Idle.FBX",
                walkFbxRelPath = "Animations/Brute@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Brute@WalkSlow.FBX",
                runFbxRelPath = "Animations/Brute@Run.FBX",
                jumpFbxRelPath = "Animations/Brute@Jump.FBX",
                fallFbxRelPath = "Animations/Brute@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Armory/Prop_TacticalWeaponDelta.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = Vector3.zero,
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(1.15f, 1.15f, 1.15f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Brute_Hook", takeName = "Take 001", firstFrame = 0, lastFrame = 28 },
                    new ClipSliceDef { clipName = "Brute_Uppercut", takeName = "Take 001", firstFrame = 28, lastFrame = 58 },
                    new ClipSliceDef { clipName = "Brute_DoubleSlam", takeName = "Take 001", firstFrame = 58, lastFrame = 90 },
                    new ClipSliceDef { clipName = "Brute_Sweep", takeName = "Take 001", firstFrame = 50, lastFrame = 80 },
                    new ClipSliceDef { clipName = "Brute_Air", takeName = "Take 001", firstFrame = 15, lastFrame = 42 },
                    new ClipSliceDef { clipName = "Brute_Drive", takeName = "Take 001", firstFrame = 20, lastFrame = 55 },
                    new ClipSliceDef { clipName = "Brute_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 90 }
                }
            },

            // 5. C11 Casual1: Bundle 2 — Knight (All-Rounder, Sword + Shield)
            new WarriorPackConfig
            {
                characterId = "C11",
                fighterName = "Casual1",
                warriorType = BBWarriorType.Knight,
                archetypeClass = BBArchetypeClass.AllRounder,
                hipHeight = 0.92f,
                weaponReach = 0.90f,
                driveName = "Casual Draw",
                bundleFolder = "Warrior Pack Bundle 2 FREE",
                packFolder = "Knight Warrior Mecanim Animation Pack",
                clipPrefix = "Knight",
                attackFbxRelPath = "Animations/Knight@Attack1.FBX",
                idleFbxRelPath = "Animations/Knight@Idle.FBX",
                walkFbxRelPath = "Animations/Knight@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Knight@WalkSlow.FBX",
                runFbxRelPath = "Animations/Knight@Run.FBX",
                jumpFbxRelPath = "Animations/Knight@Jump.FBX",
                fallFbxRelPath = "Animations/Knight@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Knight_Sword.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = new Vector3(0f, 0.04f, 0.04f),
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.90f, 0.90f, 0.90f),
                secondaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Knight_Shield.prefab",
                secondarySocket = HumanBodyBones.LeftLowerArm,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = new Vector3(0.55f, 0.55f, 0.55f),
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Knight_Slash", takeName = "Take 001", firstFrame = 0, lastFrame = 26 },
                    new ClipSliceDef { clipName = "Knight_ShieldBash", takeName = "Take 001", firstFrame = 26, lastFrame = 52 },
                    new ClipSliceDef { clipName = "Knight_Cleave", takeName = "Take 001", firstFrame = 52, lastFrame = 80 },
                    new ClipSliceDef { clipName = "Knight_Sweep", takeName = "Take 001", firstFrame = 45, lastFrame = 72 },
                    new ClipSliceDef { clipName = "Knight_Air", takeName = "Take 001", firstFrame = 10, lastFrame = 34 },
                    new ClipSliceDef { clipName = "Knight_Drive", takeName = "Take 001", firstFrame = 20, lastFrame = 50 },
                    new ClipSliceDef { clipName = "Knight_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 80 }
                }
            },

            // 6. C06 Cultist: Bundle 2 — Mage (Zoner, Void Staff)
            new WarriorPackConfig
            {
                characterId = "C06",
                fighterName = "Cultist",
                warriorType = BBWarriorType.Mage,
                archetypeClass = BBArchetypeClass.Zoner,
                hipHeight = 0.95f,
                weaponReach = 1.10f,
                driveName = "Void Orb",
                bundleFolder = "Warrior Pack Bundle 2 FREE",
                packFolder = "Mage Warrior Mecanim Animation Pack",
                clipPrefix = "Mage",
                attackFbxRelPath = "Animations/Mage@Attack1.FBX",
                idleFbxRelPath = "Animations/Mage@Idle.FBX",
                walkFbxRelPath = "Animations/Mage@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Mage@WalkSlow.FBX",
                runFbxRelPath = "Animations/Mage@Run.FBX",
                jumpFbxRelPath = "Animations/Mage@Jump.FBX",
                fallFbxRelPath = "Animations/Mage@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Mage_Staff.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = new Vector3(0f, -0.22f, 0f),
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.85f, 0.85f, 0.85f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = true,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Mage_StaffPoke", takeName = "Take 001", firstFrame = 0, lastFrame = 26 },
                    new ClipSliceDef { clipName = "Mage_OrbCast", takeName = "Take 001", firstFrame = 26, lastFrame = 52 },
                    new ClipSliceDef { clipName = "Mage_Eruption", takeName = "Take 001", firstFrame = 52, lastFrame = 80 },
                    new ClipSliceDef { clipName = "Mage_Sweep", takeName = "Take 001", firstFrame = 45, lastFrame = 72 },
                    new ClipSliceDef { clipName = "Mage_Air", takeName = "Take 001", firstFrame = 10, lastFrame = 35 },
                    new ClipSliceDef { clipName = "Mage_Drive", takeName = "Take 001", firstFrame = 20, lastFrame = 52 },
                    new ClipSliceDef { clipName = "Mage_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 80 }
                }
            },

            // 7. C09 PicoChan: Bundle 2 — Archer (Zoner, Bow)
            new WarriorPackConfig
            {
                characterId = "C09",
                fighterName = "PicoChan",
                warriorType = BBWarriorType.Archer,
                archetypeClass = BBArchetypeClass.Zoner,
                hipHeight = 0.86f,
                weaponReach = 1.05f,
                driveName = "Clinch",
                bundleFolder = "Warrior Pack Bundle 2 FREE",
                packFolder = "Archer Warrior Mecanim Animation Pack",
                clipPrefix = "Archer",
                attackFbxRelPath = "Animations/Archer@Attack1.FBX",
                idleFbxRelPath = "Animations/Archer@Idle1.FBX",
                walkFbxRelPath = "Animations/Archer@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Archer@WalkSlow.FBX",
                runFbxRelPath = "Animations/Archer@Run.FBX",
                jumpFbxRelPath = "Animations/Archer@Jump.FBX",
                fallFbxRelPath = "Animations/Archer@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Archer_Bow.prefab",
                primarySocket = HumanBodyBones.LeftHand, // Bow held in left hand
                primaryPosOffset = new Vector3(0f, -0.10f, 0f),
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.75f, 0.75f, 0.75f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.RightHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = true,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Archer_QuickDraw", takeName = "Take 001", firstFrame = 0, lastFrame = 24 },
                    new ClipSliceDef { clipName = "Archer_PowerShot", takeName = "Take 001", firstFrame = 24, lastFrame = 48 },
                    new ClipSliceDef { clipName = "Archer_Volley", takeName = "Take 001", firstFrame = 48, lastFrame = 70 },
                    new ClipSliceDef { clipName = "Archer_Sweep", takeName = "Take 001", firstFrame = 40, lastFrame = 65 },
                    new ClipSliceDef { clipName = "Archer_Air", takeName = "Take 001", firstFrame = 10, lastFrame = 32 },
                    new ClipSliceDef { clipName = "Archer_Drive", takeName = "Take 001", firstFrame = 15, lastFrame = 42 },
                    new ClipSliceDef { clipName = "Archer_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 70 }
                }
            },

            // 8. C14 Muryotaisu: Bundle 2 — 2Handed (Heavy Swordsman, Nodachi/Greatsword)
            new WarriorPackConfig
            {
                characterId = "C14",
                fighterName = "Muryotaisu",
                warriorType = BBWarriorType.TwoHanded,
                archetypeClass = BBArchetypeClass.Swordsman,
                hipHeight = 0.98f,
                weaponReach = 1.25f,
                driveName = "Shrine Slash",
                bundleFolder = "Warrior Pack Bundle 2 FREE",
                packFolder = "2 Handed Warrior Mecanim Animation Pack",
                clipPrefix = "2Handed",
                attackFbxRelPath = "Animations/2Handed@Attack1.FBX",
                idleFbxRelPath = "Animations/2Handed@Idle.FBX",
                walkFbxRelPath = "Animations/2Handed@Walk.FBX",
                walkSlowFbxRelPath = "Animations/2Handed@WalkSlow.FBX",
                runFbxRelPath = "Animations/2Handed@Run.FBX",
                jumpFbxRelPath = "Animations/2Handed@Jump.FBX",
                fallFbxRelPath = "Animations/2Handed@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_2Handed_Greatsword.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = Vector3.zero,
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.95f, 0.95f, 0.95f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "TwoHanded_Sweep", takeName = "Take 001", firstFrame = 0, lastFrame = 26 },
                    new ClipSliceDef { clipName = "TwoHanded_Slash", takeName = "Take 001", firstFrame = 26, lastFrame = 52 },
                    new ClipSliceDef { clipName = "TwoHanded_Slam", takeName = "Take 001", firstFrame = 52, lastFrame = 80 },
                    new ClipSliceDef { clipName = "TwoHanded_LowSweep", takeName = "Take 001", firstFrame = 45, lastFrame = 72 },
                    new ClipSliceDef { clipName = "TwoHanded_Air", takeName = "Take 001", firstFrame = 12, lastFrame = 38 },
                    new ClipSliceDef { clipName = "TwoHanded_Drive", takeName = "Take 001", firstFrame = 22, lastFrame = 55 },
                    new ClipSliceDef { clipName = "TwoHanded_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 80 }
                }
            },

            // 9. C08 SanpakuChan: Bundle 3 — Swordsman (Mixup/Assassin, Katana)
            new WarriorPackConfig
            {
                characterId = "C08",
                fighterName = "SanpakuChan",
                warriorType = BBWarriorType.Swordsman,
                archetypeClass = BBArchetypeClass.Rushdown,
                hipHeight = 0.90f,
                weaponReach = 0.90f,
                driveName = "Crow Step",
                bundleFolder = "Warrior Pack Bundle 3 FREE",
                packFolder = "Swordsman Warrior Mecanim Animation Pack",
                clipPrefix = "Swordsman",
                attackFbxRelPath = "Animations/Swordsman@Attack1.FBX",
                idleFbxRelPath = "Animations/Swordsman@Idle.FBX",
                walkFbxRelPath = "Animations/Swordsman@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Swordsman@WalkSlow.FBX",
                runFbxRelPath = "Animations/Swordsman@Run.FBX",
                jumpFbxRelPath = "Animations/Swordsman@Jump.FBX",
                fallFbxRelPath = "Animations/Swordsman@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Swordsman_Sword.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = new Vector3(0f, 0.02f, 0f),
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.90f, 0.90f, 0.90f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Swordsman_Slash1", takeName = "Take 001", firstFrame = 0, lastFrame = 26 },
                    new ClipSliceDef { clipName = "Swordsman_Thrust", takeName = "Take 001", firstFrame = 26, lastFrame = 52 },
                    new ClipSliceDef { clipName = "Swordsman_Cleave", takeName = "Take 001", firstFrame = 52, lastFrame = 80 },
                    new ClipSliceDef { clipName = "Swordsman_Sweep", takeName = "Take 001", firstFrame = 45, lastFrame = 72 },
                    new ClipSliceDef { clipName = "Swordsman_Air", takeName = "Take 001", firstFrame = 10, lastFrame = 34 },
                    new ClipSliceDef { clipName = "Swordsman_Drive", takeName = "Take 001", firstFrame = 18, lastFrame = 48 },
                    new ClipSliceDef { clipName = "Swordsman_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 80 }
                }
            },

            // 10. C12 Fina: Bundle 3 — Crossbow (Zoner/Striker, Plasma Crossbow)
            new WarriorPackConfig
            {
                characterId = "C12",
                fighterName = "Fina",
                warriorType = BBWarriorType.Crossbow,
                archetypeClass = BBArchetypeClass.Zoner,
                hipHeight = 0.91f,
                weaponReach = 1.05f,
                driveName = "Plasma Charge",
                bundleFolder = "Warrior Pack Bundle 3 FREE",
                packFolder = "Crossbow Warrior Mecanim Animation Pack",
                clipPrefix = "Crossbow",
                attackFbxRelPath = "Animations/Crossbow@Attack1.FBX",
                idleFbxRelPath = "Animations/Crossbow@Idle.FBX",
                walkFbxRelPath = "Animations/Crossbow@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Crossbow@WalkSlow.FBX",
                runFbxRelPath = "Animations/Crossbow@Run.FBX",
                jumpFbxRelPath = "Animations/Crossbow@Jump.FBX",
                fallFbxRelPath = "Animations/Crossbow@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Crossbow.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = new Vector3(0f, 0.06f, 0f),
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.72f, 0.72f, 0.72f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = true,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Crossbow_Snapshot", takeName = "Take 001", firstFrame = 0, lastFrame = 24 },
                    new ClipSliceDef { clipName = "Crossbow_Burst", takeName = "Take 001", firstFrame = 24, lastFrame = 48 },
                    new ClipSliceDef { clipName = "Crossbow_Scatter", takeName = "Take 001", firstFrame = 48, lastFrame = 70 },
                    new ClipSliceDef { clipName = "Crossbow_Sweep", takeName = "Take 001", firstFrame = 40, lastFrame = 65 },
                    new ClipSliceDef { clipName = "Crossbow_Air", takeName = "Take 001", firstFrame = 10, lastFrame = 32 },
                    new ClipSliceDef { clipName = "Crossbow_Drive", takeName = "Take 001", firstFrame = 16, lastFrame = 44 },
                    new ClipSliceDef { clipName = "Crossbow_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 70 }
                }
            },

            // 11. EX01 Hugo: Bundle 3 — Spearman (Heavy Mid-Range, Spear/Halberd)
            new WarriorPackConfig
            {
                characterId = "EX01",
                fighterName = "Hugo",
                warriorType = BBWarriorType.Spearman,
                archetypeClass = BBArchetypeClass.Swordsman,
                hipHeight = 1.00f,
                weaponReach = 1.35f,
                driveName = "Highland Swing",
                bundleFolder = "Warrior Pack Bundle 3 FREE",
                packFolder = "Spearman Warrior Mecanim Animation Pack",
                clipPrefix = "Spearman",
                attackFbxRelPath = "Animations/Spearman@Attack1.FBX",
                idleFbxRelPath = "Animations/Spearman@Idle.FBX",
                walkFbxRelPath = "Animations/Spearman@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Spearman@WalkSlow.FBX",
                runFbxRelPath = "Animations/Spearman@Run.FBX",
                jumpFbxRelPath = "Animations/Spearman@Jump.FBX",
                fallFbxRelPath = "Animations/Spearman@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Spearman_Spear.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = Vector3.zero,
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(1.00f, 1.00f, 1.00f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Spearman_Thrust1", takeName = "Take 001", firstFrame = 0, lastFrame = 26 },
                    new ClipSliceDef { clipName = "Spearman_Sweep", takeName = "Take 001", firstFrame = 26, lastFrame = 52 },
                    new ClipSliceDef { clipName = "Spearman_Lance", takeName = "Take 001", firstFrame = 52, lastFrame = 80 },
                    new ClipSliceDef { clipName = "Spearman_LowSweep", takeName = "Take 001", firstFrame = 45, lastFrame = 72 },
                    new ClipSliceDef { clipName = "Spearman_Air", takeName = "Take 001", firstFrame = 12, lastFrame = 38 },
                    new ClipSliceDef { clipName = "Spearman_Drive", takeName = "Take 001", firstFrame = 20, lastFrame = 54 },
                    new ClipSliceDef { clipName = "Spearman_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 80 }
                }
            },

            // 12. SK08 Torgrim: Bundle 3 — Hammer (Boss Bruiser, Colossal Mountain Hammer)
            new WarriorPackConfig
            {
                characterId = "SK08",
                fighterName = "Torgrim",
                warriorType = BBWarriorType.Hammer,
                archetypeClass = BBArchetypeClass.Grappler,
                hipHeight = 0.88f,
                weaponReach = 1.15f,
                driveName = "Storm Break",
                bundleFolder = "Warrior Pack Bundle 3 FREE",
                packFolder = "Hammer Warrior Mecanim Animation Pack",
                clipPrefix = "Hammer",
                attackFbxRelPath = "Animations/Hammer@Attack1.FBX",
                idleFbxRelPath = "Animations/Hammer@Idle.FBX",
                walkFbxRelPath = "Animations/Hammer@Walk.FBX",
                walkSlowFbxRelPath = "Animations/Hammer@WalkSlow.FBX",
                runFbxRelPath = "Animations/Hammer@Run.FBX",
                jumpFbxRelPath = "Animations/Hammer@Jump.FBX",
                fallFbxRelPath = "Animations/Hammer@Fall.FBX",
                primaryWeaponPrefab = "Assets/Prefabs/Weapons/Warrior/Warrior_Hammer_Warhammer.prefab",
                primarySocket = HumanBodyBones.RightHand,
                primaryPosOffset = new Vector3(0f, 0.05f, 0f),
                primaryRotOffset = Vector3.zero,
                primaryScale = new Vector3(0.95f, 0.95f, 0.95f),
                secondaryWeaponPrefab = null,
                secondarySocket = HumanBodyBones.LeftHand,
                secondaryPosOffset = Vector3.zero,
                secondaryRotOffset = Vector3.zero,
                secondaryScale = Vector3.one,
                hasProjectile = false,
                attackClips = new ClipSliceDef[]
                {
                    new ClipSliceDef { clipName = "Hammer_Swing", takeName = "Take 001", firstFrame = 0, lastFrame = 28 },
                    new ClipSliceDef { clipName = "Hammer_Smash", takeName = "Take 001", firstFrame = 28, lastFrame = 58 },
                    new ClipSliceDef { clipName = "Hammer_Quake", takeName = "Take 001", firstFrame = 58, lastFrame = 90 },
                    new ClipSliceDef { clipName = "Hammer_Sweep", takeName = "Take 001", firstFrame = 50, lastFrame = 80 },
                    new ClipSliceDef { clipName = "Hammer_Air", takeName = "Take 001", firstFrame = 14, lastFrame = 42 },
                    new ClipSliceDef { clipName = "Hammer_Drive", takeName = "Take 001", firstFrame = 22, lastFrame = 56 },
                    new ClipSliceDef { clipName = "Hammer_Super", takeName = "Take 001", firstFrame = 0, lastFrame = 90 }
                }
            }
        };

        public static void Apply12FighterWarriorMapping()
        {
            Debug.Log("<color=#00e676>[BBWarriorBundleMapper] ======================================================</color>");
            Debug.Log("<color=#00e676>[BBWarriorBundleMapper] Starting 12-Fighter -> 12-Warrior Bundle Standard Mapping</color>");
            Debug.Log("<color=#00e676>[BBWarriorBundleMapper] ======================================================</color>");

            // Step 1: Slice Attack1.FBX sub-clips and sanitize locomotion for all 12 packs
            for (int i = 0; i < PackConfigs.Length; i++)
            {
                var cfg = PackConfigs[i];
                string baseDir = $"Assets/ExplosiveLLC/{cfg.bundleFolder}/{cfg.packFolder}";
                string attackPath = $"{baseDir}/{cfg.attackFbxRelPath}";

                EnsureAttackClips(attackPath, cfg.attackClips);
                SanitizeLocomotionFBX($"{baseDir}/{cfg.idleFbxRelPath}");
                SanitizeLocomotionFBX($"{baseDir}/{cfg.walkFbxRelPath}");
                if (!string.IsNullOrEmpty(cfg.walkSlowFbxRelPath))
                    SanitizeLocomotionFBX($"{baseDir}/{cfg.walkSlowFbxRelPath}");
                SanitizeLocomotionFBX($"{baseDir}/{cfg.runFbxRelPath}");
                SanitizeLocomotionFBX($"{baseDir}/{cfg.jumpFbxRelPath}");
                SanitizeLocomotionFBX($"{baseDir}/{cfg.fallFbxRelPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Step 2: Configure each fighter
            int successCount = 0;
            for (int i = 0; i < PackConfigs.Length; i++)
            {
                var cfg = PackConfigs[i];
                try
                {
                    ConfigureFighter(cfg);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BBWarriorBundleMapper] Failed configuring {cfg.characterId} ({cfg.fighterName}): {ex.Message}\n{ex.StackTrace}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676>[BBWarriorBundleMapper] SUCCESS: Successfully mapped {successCount}/{PackConfigs.Length} fighters to 12 dedicated Warrior Bundles!</color>");
        }

        private static void EnsureAttackClips(string fbxPath, ClipSliceDef[] subClips)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[BBWarriorBundleMapper] Importer not found for {fbxPath}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;

            var existingClips = importer.clipAnimations;
            bool needsReimport = false;
            if (existingClips == null || existingClips.Length < subClips.Length + 1)
            {
                needsReimport = true;
            }
            else
            {
                // Verify all required clip names exist
                var existingNames = new HashSet<string>();
                for (int i = 0; i < existingClips.Length; i++) existingNames.Add(existingClips[i].name);
                for (int i = 0; i < subClips.Length; i++)
                {
                    if (!existingNames.Contains(subClips[i].clipName))
                    {
                        needsReimport = true;
                        break;
                    }
                }
            }

            if (needsReimport)
            {
                var clipDefs = new List<ModelImporterClipAnimation>();
                clipDefs.Add(CreateClip("Attack1", "Take 001", 0, 34, false));
                for (int i = 0; i < subClips.Length; i++)
                {
                    clipDefs.Add(CreateClip(subClips[i].clipName, subClips[i].takeName, subClips[i].firstFrame, subClips[i].lastFrame, false));
                }
                importer.clipAnimations = clipDefs.ToArray();
                importer.SaveAndReimport();
            }
        }

        private static ModelImporterClipAnimation CreateClip(string name, string takeName, int firstFrame, int lastFrame, bool loop)
        {
            return new ModelImporterClipAnimation
            {
                name = name,
                takeName = takeName,
                firstFrame = firstFrame,
                lastFrame = lastFrame,
                loopTime = loop,
                loop = loop,
                keepOriginalOrientation = true,
                keepOriginalPositionY = false,
                keepOriginalPositionXZ = true,
                heightFromFeet = false,
                lockRootRotation = true,
                lockRootHeightY = true,
                lockRootPositionXZ = true
            };
        }

        private static void SanitizeLocomotionFBX(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].keepOriginalPositionY = false;
                    clips[i].keepOriginalOrientation = true;
                    clips[i].lockRootRotation = true;
                    clips[i].lockRootHeightY = true;
                    clips[i].lockRootPositionXZ = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, AnimationClip> LoadAttackClips(string fbxPath)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var a in allAssets)
            {
                if (a is AnimationClip c)
                {
                    clips[c.name] = c;
                }
            }
            return clips;
        }

        private struct InternalMoveParams
        {
            public string moveId;
            public string defaultAttackName;
            public BBButton button;
            public BBMoveKind kind;
            public BBMotion motion;
            public int s, a, r;
            public int onBlock;
            public int damage;
            public int level;
            public BBGuard guard;
            public bool causesKnockdown;
            public bool airOk;
            public bool groundOk;
            public int heatCost;
            public string[] gatlingTargets;
            public int clipSubIndex; // 0..6 matching cfg.attackClips
        }

        private static void ConfigureFighter(WarriorPackConfig cfg)
        {
            var def = FindDefinition(cfg.characterId);
            if (def == null)
            {
                Debug.LogError($"[BBWarriorBundleMapper] CharacterDefinition for {cfg.characterId} is null!");
                return;
            }

            var profile = def.blazBlueProfile as BBCharacterProfile;
            if (profile == null)
            {
                Debug.LogError($"[BBWarriorBundleMapper] BBCharacterProfile for {cfg.characterId} is null!");
                return;
            }

            var moveSet = profile.moveSet as BBMoveSet;
            if (moveSet == null)
            {
                Debug.LogError($"[BBWarriorBundleMapper] BBMoveSet for {cfg.characterId} is null!");
                return;
            }

            var overrideCtrl = profile.blazBlueOverrideController as AnimatorOverrideController;
            if (overrideCtrl == null && def.animatorController is AnimatorOverrideController aoc)
            {
                overrideCtrl = aoc;
                profile.blazBlueOverrideController = overrideCtrl;
                EditorUtility.SetDirty(profile);
            }

            string baseDir = $"Assets/CombatData/BlazBlue/{cfg.fighterName}";
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            string boxDir = $"{baseDir}/BoxTracks";
            if (!Directory.Exists(boxDir))
            {
                Directory.CreateDirectory(boxDir);
            }

            // Existing moves dictionary
            var existingMoves = new Dictionary<string, BBAttackData>();
            if (moveSet.moves != null)
            {
                foreach (var m in moveSet.moves)
                {
                    if (m != null && !string.IsNullOrEmpty(m.moveId))
                        existingMoves[m.moveId] = m;
                }
            }

            // Frame data tuning by Archetype Group
            InternalMoveParams[] moveConfigs;
            if (cfg.archetypeClass == BBArchetypeClass.Rushdown)
            {
                // Fast / Rushdown: C01, C02, C08
                moveConfigs = new InternalMoveParams[]
                {
                    new InternalMoveParams { moveId = "5A", defaultAttackName = "Light Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 5, a = 3, r = 8, onBlock = 1, damage = 280, level = 1, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5B", "5C", "2C", "5D", "236236C" }, clipSubIndex = 0 },
                    new InternalMoveParams { moveId = "5B", defaultAttackName = "Medium Strike", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 7, a = 4, r = 12, onBlock = -2, damage = 520, level = 2, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5C", "2C", "5D", "236236C" }, clipSubIndex = 1 },
                    new InternalMoveParams { moveId = "5C", defaultAttackName = "Heavy Strike", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 11, a = 4, r = 17, onBlock = -4, damage = 820, level = 3, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5D", "236236C" }, clipSubIndex = 2 },
                    new InternalMoveParams { moveId = "2C", defaultAttackName = "Low Sweep", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None, s = 10, a = 3, r = 22, onBlock = -8, damage = 700, level = 3, guard = BBGuard.Low, causesKnockdown = true, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "236236C" }, clipSubIndex = 3 },
                    new InternalMoveParams { moveId = "j.B", defaultAttackName = "Jump Attack", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 7, a = 4, r = 14, onBlock = 0, damage = 520, level = 2, guard = BBGuard.Overhead, causesKnockdown = false, airOk = true, groundOk = false, heatCost = 0, gatlingTargets = new string[0], clipSubIndex = 4 },
                    new InternalMoveParams { moveId = "5D", defaultAttackName = cfg.driveName, button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None, s = 13, a = 4, r = 17, onBlock = -4, damage = 750, level = 3, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "236236C" }, clipSubIndex = 5 },
                    new InternalMoveParams { moveId = "236236C", defaultAttackName = "Super Finisher", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236, s = 9, a = 4, r = 26, onBlock = -14, damage = 2200, level = 4, guard = BBGuard.Mid, causesKnockdown = true, airOk = false, groundOk = true, heatCost = 50, gatlingTargets = new string[0], clipSubIndex = 6 }
                };
            }
            else if (cfg.archetypeClass == BBArchetypeClass.Grappler || cfg.characterId == "EX04" || cfg.characterId == "C14" || cfg.characterId == "EX01" || cfg.characterId == "SK08")
            {
                // Heavy / Boss / Powerhouse: EX04, C14, EX01, SK08
                moveConfigs = new InternalMoveParams[]
                {
                    new InternalMoveParams { moveId = "5A", defaultAttackName = "Heavy Jab", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 7, a = 3, r = 9, onBlock = 0, damage = 320, level = 1, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5B", "5C", "2C", "5D", "236236C" }, clipSubIndex = 0 },
                    new InternalMoveParams { moveId = "5B", defaultAttackName = "Heavy Medium", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 9, a = 4, r = 13, onBlock = -2, damage = 560, level = 2, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5C", "2C", "5D", "236236C" }, clipSubIndex = 1 },
                    new InternalMoveParams { moveId = "5C", defaultAttackName = "Colossal Strike", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 13, a = 4, r = 19, onBlock = -5, damage = 900, level = 3, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5D", "236236C" }, clipSubIndex = 2 },
                    new InternalMoveParams { moveId = "2C", defaultAttackName = "Heavy Sweep", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None, s = 12, a = 3, r = 24, onBlock = -9, damage = 750, level = 3, guard = BBGuard.Low, causesKnockdown = true, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "236236C" }, clipSubIndex = 3 },
                    new InternalMoveParams { moveId = "j.B", defaultAttackName = "Heavy Drop", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 9, a = 4, r = 15, onBlock = 0, damage = 560, level = 2, guard = BBGuard.Overhead, causesKnockdown = false, airOk = true, groundOk = false, heatCost = 0, gatlingTargets = new string[0], clipSubIndex = 4 },
                    new InternalMoveParams { moveId = "5D", defaultAttackName = cfg.driveName, button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None, s = 15, a = 4, r = 19, onBlock = -4, damage = 800, level = 3, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "236236C" }, clipSubIndex = 5 },
                    new InternalMoveParams { moveId = "236236C", defaultAttackName = "Super Finisher", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236, s = 10, a = 4, r = 28, onBlock = -16, damage = 2400, level = 4, guard = BBGuard.Mid, causesKnockdown = true, airOk = false, groundOk = true, heatCost = 50, gatlingTargets = new string[0], clipSubIndex = 6 }
                };
            }
            else
            {
                // Mid-Range / Zoner / All-Rounder: C05, C06, C09, C11, C12
                moveConfigs = new InternalMoveParams[]
                {
                    new InternalMoveParams { moveId = "5A", defaultAttackName = "Light Strike", button = BBButton.A, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 6, a = 3, r = 9, onBlock = 0, damage = 280, level = 1, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5B", "5C", "2C", "5D", "236236C" }, clipSubIndex = 0 },
                    new InternalMoveParams { moveId = "5B", defaultAttackName = "Medium Strike", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 8, a = 4, r = 13, onBlock = -2, damage = 520, level = 2, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5C", "2C", "5D", "236236C" }, clipSubIndex = 1 },
                    new InternalMoveParams { moveId = "5C", defaultAttackName = "Heavy Strike", button = BBButton.C, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 12, a = 4, r = 18, onBlock = -4, damage = 820, level = 3, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "5D", "236236C" }, clipSubIndex = 2 },
                    new InternalMoveParams { moveId = "2C", defaultAttackName = "Low Sweep", button = BBButton.C, kind = BBMoveKind.CommandNormal, motion = BBMotion.None, s = 11, a = 3, r = 22, onBlock = -8, damage = 700, level = 3, guard = BBGuard.Low, causesKnockdown = true, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "236236C" }, clipSubIndex = 3 },
                    new InternalMoveParams { moveId = "j.B", defaultAttackName = "Air Strike", button = BBButton.B, kind = BBMoveKind.Normal, motion = BBMotion.None, s = 8, a = 4, r = 14, onBlock = 0, damage = 520, level = 2, guard = BBGuard.Overhead, causesKnockdown = false, airOk = true, groundOk = false, heatCost = 0, gatlingTargets = new string[0], clipSubIndex = 4 },
                    new InternalMoveParams { moveId = "5D", defaultAttackName = cfg.driveName, button = BBButton.D, kind = BBMoveKind.DriveMove, motion = BBMotion.None, s = 14, a = 4, r = 18, onBlock = -4, damage = 750, level = 3, guard = BBGuard.Mid, causesKnockdown = false, airOk = false, groundOk = true, heatCost = 0, gatlingTargets = new string[] { "236236C" }, clipSubIndex = 5 },
                    new InternalMoveParams { moveId = "236236C", defaultAttackName = "Super Finisher", button = BBButton.C, kind = BBMoveKind.DistortionDrive, motion = BBMotion.M236236, s = 9, a = 4, r = 26, onBlock = -14, damage = 2200, level = 4, guard = BBGuard.Mid, causesKnockdown = true, airOk = false, groundOk = true, heatCost = 50, gatlingTargets = new string[0], clipSubIndex = 6 }
                };
            }

            // Load sliced attack clips
            string attackFbxPath = $"Assets/ExplosiveLLC/{cfg.bundleFolder}/{cfg.packFolder}/{cfg.attackFbxRelPath}";
            var attackClips = LoadAttackClips(attackFbxPath);

            var finalSevenMoves = new List<BBAttackData>();

            for (int m = 0; m < moveConfigs.Length; m++)
            {
                var mc = moveConfigs[m];
                BBAttackData attack = null;

                if (existingMoves.TryGetValue(mc.moveId, out var existingAtk) && existingAtk != null)
                {
                    attack = existingAtk;
                }
                else
                {
                    string assetPath = $"{baseDir}/BB_{cfg.fighterName}_{mc.moveId}.asset";
                    attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(assetPath);
                    if (attack == null)
                    {
                        attack = ScriptableObject.CreateInstance<BBAttackData>();
                        AssetDatabase.CreateAsset(attack, assetPath);
                    }
                }

                attack.moveId = mc.moveId;
                attack.attackName = mc.defaultAttackName;
                attack.button = mc.button;
                attack.kind = mc.kind;
                attack.motion = mc.motion;
                attack.startupFrames = mc.s;
                attack.activeFrames = mc.a;
                attack.recoveryFrames = mc.r;
                attack.damage = mc.damage;
                attack.attackLevel = mc.level;
                attack.guard = mc.guard;
                attack.causesKnockdown = mc.causesKnockdown;
                attack.hardKnockdown = mc.causesKnockdown;
                attack.airOk = mc.airOk;
                attack.groundOk = mc.groundOk;
                attack.heatCost = mc.heatCost;
                attack.gatlingInto = mc.gatlingTargets;

                // Dustloop formula: blockstun = recovery + (active - 1) + onBlock
                int blockstun = mc.r + (mc.a - 1) + mc.onBlock;
                attack.blockstunTicks = blockstun;
                attack.advantageOnBlock = mc.onBlock;

                int hitstun = mc.causesKnockdown ? 36 : (blockstun + 2);
                attack.hitstunTicks = hitstun;
                attack.advantageOnHit = hitstun - (mc.a - 1 + mc.r);

                int hitstop = mc.level == 1 ? 9 : (mc.level == 2 ? 10 : (mc.level == 3 ? 11 : 13));
                attack.hitstopAttacker = hitstop;
                attack.hitstopDefender = hitstop;
                attack.hitstopOnBlock = hitstop;
                attack.counterHitStopBonus = 4;

                attack.specialCancel = true;
                attack.ddCancel = true;
                attack.rapidCancelable = mc.kind != BBMoveKind.DistortionDrive;

                // Animation Clip mapping from dedicated warrior pack
                string targetSubClipName = cfg.attackClips[mc.clipSubIndex].clipName;
                AnimationClip assignedClip = null;
                if (attackClips.TryGetValue(targetSubClipName, out var foundClip))
                {
                    assignedClip = foundClip;
                }
                else if (attackClips.TryGetValue("Attack1", out var fbClip))
                {
                    assignedClip = fbClip;
                }

                attack.clip = assignedClip;
                attack.animationClip = assignedClip;
                attack.clipName = assignedClip != null ? assignedClip.name : targetSubClipName;
                attack.clipFirstFrame = 0;
                attack.clipLastFrame = assignedClip != null ? Mathf.RoundToInt(assignedClip.length * 60f) : 30;
                attack.measuredImpactFrame = mc.s;

                // Bake BoxTrack with archetype reach scaling & Delta=0f peakLimbTick
                BakeBoxTrack(attack, mc, baseDir, cfg.fighterName, cfg.characterId, cfg.weaponReach);

                EditorUtility.SetDirty(attack);
                finalSevenMoves.Add(attack);
            }

            // Update BBMoveSet
            moveSet.moves = finalSevenMoves.ToArray();
            EditorUtility.SetDirty(moveSet);

            // Update BBProfile
            var atk5D = finalSevenMoves.Find(m => m.moveId == "5D");
            var atkSuper = finalSevenMoves.Find(m => m.moveId == "236236C");
            if (atk5D != null && atk5D.clip != null) profile.signatureDriveClip = atk5D.clip;
            if (atkSuper != null && atkSuper.clip != null) profile.signatureSuperClip = atkSuper.clip;
            profile.driveName = cfg.driveName;
            EditorUtility.SetDirty(profile);

            // Update AnimatorOverrideController
            if (overrideCtrl != null)
            {
                UpdateOverrideController(overrideCtrl, cfg);
            }

            // Step 3: Configure BBWeaponRig on character prefab
            ConfigureWeaponRigOnPrefab(cfg, def);

            Debug.Log($"<color=#00e676>[BBWarriorBundleMapper] Configured {cfg.characterId} ({cfg.fighterName}) -> {cfg.warriorType} with 7 moves & WeaponRig (Reach: {cfg.weaponReach:F2}m)</color>");
        }

        private static void BakeBoxTrack(BBAttackData attack, InternalMoveParams cfg, string baseDir, string charName, string charId, float weaponReach)
        {
            string boxDir = $"{baseDir}/BoxTracks";
            if (!Directory.Exists(boxDir)) Directory.CreateDirectory(boxDir);

            string boxPath = $"{boxDir}/BoxTrack_{charName}_{cfg.moveId}.asset";
            var track = AssetDatabase.LoadAssetAtPath<BBBoxTrack>(boxPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<BBBoxTrack>();
                AssetDatabase.CreateAsset(track, boxPath);
            }

            int totalTicks = cfg.s + cfg.a + cfg.r;
            track.tickCount = totalTicks;

            float cy = 1.05f;
            float hy = 0.35f;
            float farEdge = 1.10f;
            float reachDelta = Mathf.Max(0f, weaponReach - 0.85f);

            if (cfg.moveId == "2C")
            {
                cy = 0.45f;
                hy = 0.25f;
                farEdge = 1.35f + reachDelta;
            }
            else if (cfg.moveId == "j.B")
            {
                cy = 0.75f;
                hy = 0.35f;
                farEdge = 1.10f + reachDelta;
            }
            else if (cfg.moveId == "5A")
            {
                farEdge = 0.95f + reachDelta * 0.5f;
            }
            else if (cfg.moveId == "5B")
            {
                farEdge = 1.15f + reachDelta * 0.7f;
            }
            else if (cfg.moveId == "5C")
            {
                farEdge = 1.55f + reachDelta;
            }
            else if (cfg.moveId == "5D")
            {
                farEdge = 1.95f + reachDelta;
            }
            else if (cfg.moveId == "236236C")
            {
                farEdge = 2.45f + reachDelta;
            }

            float minX = 0.15f;
            float hx = (farEdge - minX) * 0.5f;
            float cx = minX + hx;

            var activeBox = new BBBox(cx, cy, hx, hy, BBBoxKind.Hit, 0, BBAttribute.Body);

            track.frameStart = new int[totalTicks + 1];
            var boxList = new List<BBBox>();
            int currentOffset = 0;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentOffset;
                if (t >= cfg.s && t < cfg.s + cfg.a)
                {
                    boxList.Add(activeBox);
                    currentOffset++;
                }

                // Standard full body hurtbox
                float bodyY = cfg.moveId == "2C" ? 0.60f : (cfg.moveId == "j.B" ? 1.25f : 0.95f);
                float bodyH = cfg.moveId == "2C" ? 0.45f : 0.70f;
                var hurtBox = new BBBox(0f, bodyY, 0.40f, bodyH, BBBoxKind.Hurt, 0, BBAttribute.Body);
                boxList.Add(hurtBox);
                currentOffset++;
            }
            track.frameStart[totalTicks] = currentOffset;
            track.boxes = boxList.ToArray();

            // Delta = 0f alignment: peak limb tick matches first active tick exactly
            track.keyTicks = new int[] { cfg.s };
            track.bakeHash = $"Bake_{charId}_{cfg.moveId}_{cfg.s}_{cfg.a}_{cfg.r}";

            EditorUtility.SetDirty(track);
            attack.boxes = track;
        }

        private static void UpdateOverrideController(AnimatorOverrideController overrideCtrl, WarriorPackConfig cfg)
        {
            string baseDir = $"Assets/ExplosiveLLC/{cfg.bundleFolder}/{cfg.packFolder}";
            var idleFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseDir}/{cfg.idleFbxRelPath}");
            var walkFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseDir}/{cfg.walkFbxRelPath}");
            var walkSlowFBX = !string.IsNullOrEmpty(cfg.walkSlowFbxRelPath)
                ? AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseDir}/{cfg.walkSlowFbxRelPath}")
                : walkFBX;
            var runFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseDir}/{cfg.runFbxRelPath}");
            var fallFBX = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{baseDir}/{cfg.fallFbxRelPath}");

            var jumpAssets = AssetDatabase.LoadAllAssetsAtPath($"{baseDir}/{cfg.jumpFbxRelPath}");
            AnimationClip jumpClip = null;
            AnimationClip landClip = null;
            foreach (var a in jumpAssets)
            {
                if (a is AnimationClip c)
                {
                    if (c.name.Contains("Land")) landClip = c;
                    else if (c.name.Contains("Jump") || jumpClip == null) jumpClip = c;
                }
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideCtrl.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                var orig = overrides[i].Key;
                if (orig == null) continue;
                string name = orig.name.ToLowerInvariant();

                if (name.Contains("idle"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, idleFBX != null ? idleFBX : overrides[i].Value);
                }
                else if (name.Contains("walk") || name.Contains("locomotion") || name.Contains("forward"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, walkFBX != null ? walkFBX : overrides[i].Value);
                }
                else if (name.Contains("run") || name.Contains("dash"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, runFBX != null ? runFBX : overrides[i].Value);
                }
                else if (name.Contains("jump"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, jumpClip != null ? jumpClip : overrides[i].Value);
                }
                else if (name.Contains("fall"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, fallFBX != null ? fallFBX : overrides[i].Value);
                }
                else if (name.Contains("land"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, landClip != null ? landClip : overrides[i].Value);
                }
                else if (name.Contains("guard") || name.Contains("block"))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, idleFBX != null ? idleFBX : overrides[i].Value);
                }
            }

            overrideCtrl.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideCtrl);
        }

        private static void ConfigureWeaponRigOnPrefab(WarriorPackConfig cfg, CharacterDefinition def)
        {
            if (def.modelPrefab == null) return;
            string prefabPath = AssetDatabase.GetAssetPath(def.modelPrefab);
            if (string.IsNullOrEmpty(prefabPath)) return;

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[BBWarriorBundleMapper] Could not load prefab contents at {prefabPath}");
                return;
            }

            try
            {
                var rig = root.GetComponent<BBWeaponRig>();
                if (rig == null)
                {
                    rig = root.AddComponent<BBWeaponRig>();
                }

                rig.warriorType = cfg.warriorType;

                if (!string.IsNullOrEmpty(cfg.primaryWeaponPrefab))
                {
                    rig.primaryWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.primaryWeaponPrefab);
                    rig.primarySocket = cfg.primarySocket;
                    rig.primaryPositionOffset = cfg.primaryPosOffset;
                    rig.primaryRotationOffset = cfg.primaryRotOffset;
                    rig.primaryScale = cfg.primaryScale;
                }
                else
                {
                    rig.primaryWeaponPrefab = null;
                    rig.primaryScale = Vector3.one;
                }

                if (!string.IsNullOrEmpty(cfg.secondaryWeaponPrefab))
                {
                    rig.secondaryWeaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cfg.secondaryWeaponPrefab);
                    rig.secondarySocket = cfg.secondarySocket;
                    rig.secondaryPositionOffset = cfg.secondaryPosOffset;
                    rig.secondaryRotationOffset = cfg.secondaryRotOffset;
                    rig.secondaryScale = cfg.secondaryScale;
                }
                else
                {
                    rig.secondaryWeaponPrefab = null;
                    rig.secondaryScale = Vector3.one;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static CharacterDefinition FindDefinition(string id)
        {
            string[] found = AssetDatabase.FindAssets($"CharacterDefinition_{id}_ t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
            if (found.Length == 0)
            {
                found = AssetDatabase.FindAssets($"CharacterDefinition_{id} t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
            }

            if (found.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(found[0]);
                return AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            }
            return null;
        }
    }
}
#endif
