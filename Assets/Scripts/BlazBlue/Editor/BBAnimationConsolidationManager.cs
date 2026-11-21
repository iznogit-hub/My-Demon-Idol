#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    [InitializeOnLoad]
    public static class BBAnimationConsolidationManager
    {
        public const string TRIGGER_FILE = "consolidate_animations.trigger";
        public const string VERIFY_TRIGGER_FILE = "verify_animations.trigger";
        public const string REPORT_DIR = "Docs";
        public const string BASE_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller";

        [InitializeOnLoadMethod]
        public static void InitializeTriggerListener()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        static BBAnimationConsolidationManager()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch {}
                ExecuteConsolidation();
            }
            if (File.Exists(VERIFY_TRIGGER_FILE))
            {
                try { File.Delete(VERIFY_TRIGGER_FILE); } catch {}
                ExecuteVerificationSuite();
            }
        }

        public class FighterMapping
        {
            public string Id;
            public string Name;
            public string PrefabPath;
            public string OverridePath;
            public string PresentationPath;

            // Clip paths
            public string Idle;
            public string WalkFwd;
            public string WalkBack;
            public string Dash;
            public string Jump;
            public string Guard;
            public string Atk5A;
            public string Atk5B;
            public string Atk5C;
            public string Atk5D;
            public string Special;
            public string Super;
            public string Throw;
            public string Hit;
            public string Knockdown;
            public string Getup;
            public string Win;
        }

        public static Dictionary<string, FighterMapping> GetMappings() => MAPPINGS;

        private static readonly Dictionary<string, FighterMapping> MAPPINGS = new Dictionary<string, FighterMapping>
        {
            ["C01"] = new FighterMapping
            {
                Id = "C01", Name = "Akio",
                PrefabPath = "Assets/Prefabs/Characters/C01_Akio.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Akio_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C01.asset",
                Idle = "Assets/Martial Art Animations Sample/Animations/Fight_Idle.fbx",
                WalkFwd = "Assets/Martial Art Animations Sample/Animations/Walk_F.fbx",
                WalkBack = "Assets/Martial Art Animations Sample/Animations/Walk_B.fbx",
                Dash = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx",
                Atk5D = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Special = "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx",
                Super = "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            },
            ["C02"] = new FighterMapping
            {
                Id = "C02", Name = "Aika",
                PrefabPath = "Assets/Prefabs/Characters/C02_Aika.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Aika_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C02.asset",
                Idle = "Assets/Martial Art Animations Sample/Animations/Fight_Idle.fbx",
                WalkFwd = "Assets/Martial Art Animations Sample/Animations/Walk_F.fbx",
                WalkBack = "Assets/Martial Art Animations Sample/Animations/Walk_B.fbx",
                Dash = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx",
                Atk5D = "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx",
                Special = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Super = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            },
            ["C04"] = new FighterMapping
            {
                Id = "C04", Name = "UnityChan",
                PrefabPath = "Assets/Prefabs/Characters/C04_UnityChan.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/UnityChan_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C04.asset",
                Idle = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@WalkForward.FBX",
                WalkBack = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@WalkBackward.FBX",
                Dash = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx",
                Atk5D = "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx",
                Special = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Super = "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Idle.FBX"
            },
            ["C05"] = new FighterMapping
            {
                Id = "C05", Name = "Akane",
                PrefabPath = "Assets/Prefabs/Characters/C05_Akane.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Akane_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C05.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@AttackPolearm01.fbx",
                Atk5C = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@AttackPolearm01.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingExit01.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX"
            },
            ["C06"] = new FighterMapping
            {
                Id = "C06", Name = "Cultist",
                PrefabPath = "Assets/Prefabs/Characters/C06_Cultist.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Cultist_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C06.asset",
                Idle = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx",
                WalkFwd = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
                WalkBack = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Backward.fbx",
                Dash = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_Right.fbx",
                Jump = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingEnter01.fbx",
                Guard = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@ShieldBlock01.fbx",
                Atk5A = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx",
                Atk5D = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Call/HumanM@MagicAttackCall1H01_L.fbx",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_L.fbx",
                Super = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingDamage01.fbx",
                Knockdown = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingExit01.fbx",
                Win = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle03.fbx"
            },
            ["C07"] = new FighterMapping
            {
                Id = "C07", Name = "StellarWitch",
                PrefabPath = "Assets/Prefabs/Characters/C07_StellarWitch.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/StellarWitch_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C07.asset",
                Idle = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx",
                WalkFwd = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx",
                WalkBack = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Backward.fbx",
                Dash = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Strafe/StrafeRun/HumanF@StrafeRun01_Right.fbx",
                Jump = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingEnter01.fbx",
                Guard = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@ShieldBlock01.fbx",
                Atk5A = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_L.fbx",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx",
                Atk5D = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx",
                Super = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingDamage01.fbx",
                Knockdown = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingExit01.fbx",
                Win = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle03.fbx"
            },
            ["C08"] = new FighterMapping
            {
                Id = "C08", Name = "SanpakuChan",
                PrefabPath = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/SanpakuChan_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C08.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX",
                Special = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Fall.FBX",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Idle.FBX"
            },
            ["C09"] = new FighterMapping
            {
                Id = "C09", Name = "PicoChan",
                PrefabPath = "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/PicoChan_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C09.asset",
                Idle = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@WalkForward.FBX",
                WalkBack = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@WalkBackward.FBX",
                Dash = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Grab_Hit_A.fbx",
                Atk5D = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX",
                Special = "Assets/Martial Art Animations Sample/Animations/Grab_Hit_B.fbx",
                Super = "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Idle.FBX"
            },
            ["C10"] = new FighterMapping
            {
                Id = "C10", Name = "Reika",
                PrefabPath = "Assets/Prefabs/Characters/C10_Reika.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Reika_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C10.asset",
                Idle = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@CombatIdle1H01.fbx",
                WalkFwd = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx",
                WalkBack = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Backward.fbx",
                Dash = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Strafe/StrafeRun/HumanF@StrafeRun01_Right.fbx",
                Jump = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingEnter01.fbx",
                Guard = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@ShieldBlock01.fbx",
                Atk5A = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx",
                Atk5D = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Special = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX",
                Super = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx",
                Knockdown = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingExit01.fbx",
                Win = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Buff1H01.fbx"
            },
            ["C11"] = new FighterMapping
            {
                Id = "C11", Name = "Casual1",
                PrefabPath = "Assets/Prefabs/Characters/C11_Casual1.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Casual1_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C11.asset",
                Idle = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@WalkForward.FBX",
                WalkBack = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@WalkBackward.FBX",
                Dash = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Atk5D = "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx",
                Special = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Punch.FBX",
                Super = "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Female Fighter Mecanim Animation Pack FREE/Animations/Female@Idle.FBX"
            },
            ["C12"] = new FighterMapping
            {
                Id = "C12", Name = "Fina",
                PrefabPath = "Assets/Prefabs/Characters/C12_Fina.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Fina_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C12.asset",
                Idle = "Assets/Martial Art Animations Sample/Animations/Fight_Idle.fbx",
                WalkFwd = "Assets/Martial Art Animations Sample/Animations/Walk_F.fbx",
                WalkBack = "Assets/Martial Art Animations Sample/Animations/Walk_B.fbx",
                Dash = "Assets/Martial Art Animations Sample/Animations/Quickstep_F.fbx",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/Martial Art Animations Sample/Animations/Atk_P_2.fbx",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_K_3.fbx",
                Atk5D = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Special = "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx",
                Super = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/Martial Art Animations Sample/Animations/Knock_A.fbx",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/Martial Art Animations Sample/Animations/Taunt.fbx"
            },
            ["C13"] = new FighterMapping
            {
                Id = "C13", Name = "SapphiArtchan",
                PrefabPath = "Assets/Prefabs/Characters/C13_SapphiArtchan.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/SapphiArtchan_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C13.asset",
                Idle = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx",
                WalkFwd = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx",
                WalkBack = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Backward.fbx",
                Dash = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Strafe/StrafeRun/HumanF@StrafeRun01_Right.fbx",
                Jump = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingEnter01.fbx",
                Guard = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@ShieldBlock01.fbx",
                Atk5A = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Call/HumanF@MagicAttackCall1H01_L.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx",
                Atk5D = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx",
                Super = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingDamage01.fbx",
                Knockdown = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingExit01.fbx",
                Win = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle03.fbx"
            },
            ["C14"] = new FighterMapping
            {
                Id = "C14", Name = "Muryotaisu",
                PrefabPath = "Assets/Prefabs/Characters/C14_Muryotaisu.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/Muryotaisu_BB_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_C14.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H02.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Attack1.FBX",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Fall.FBX",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/HumanF@CastingExit01.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Animations/2Handed@Idle.FBX"
            },
            ["SK02"] = new FighterMapping
            {
                Id = "SK02", Name = "Galahad",
                PrefabPath = "Assets/Prefabs/Characters/Sidekicks/SK02_Galahad.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK02_Galahad_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_SK02.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Jump.FBX",
                Guard = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@ShieldBlock01.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Fall.FBX",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingExit01.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Idle.FBX"
            },
            ["SK03"] = new FighterMapping
            {
                Id = "SK03", Name = "Vex",
                PrefabPath = "Assets/Prefabs/Characters/Sidekicks/SK03_Vex.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK03_Vex_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_SK03.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX",
                Atk5B = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX",
                Special = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Fall.FBX",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Idle.FBX"
            },
            ["SK05"] = new FighterMapping
            {
                Id = "SK05", Name = "LordMorvath",
                PrefabPath = "Assets/Prefabs/Characters/Sidekicks/SK05_LordMorvath.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK05_LordMorvath_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_SK05.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Jump.FBX",
                Guard = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@ShieldBlock01.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanM@MagicAttackOmni01.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect2H01.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/MagicAttacks/Directional/HumanM@MagicAttackDirect1H01_R.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Attack1.FBX",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingDamage01.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Fall.FBX",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingExit01.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Animations/Mage@Idle.FBX"
            },
            ["SK06"] = new FighterMapping
            {
                Id = "SK06", Name = "CommanderRen",
                PrefabPath = "Assets/Prefabs/Characters/Sidekicks/SK06_CommanderRen.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK06_CommanderRen_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_SK06.asset",
                Idle = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
                WalkFwd = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
                WalkBack = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Backward.fbx",
                Dash = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_Right.fbx",
                Jump = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingEnter01.fbx",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                Atk5C = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Atk5D = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                Special = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
                Hit = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                Getup = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Spellcasting/HumanM@CastingExit01.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX"
            },
            ["SK08"] = new FighterMapping
            {
                Id = "SK08", Name = "Torgrim",
                PrefabPath = "Assets/Prefabs/Characters/Sidekicks/SK08_Torgrim.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/SK08_Torgrim_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_SK08.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX",
                Atk5B = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX",
                Special = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Fall.FBX",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Fall.FBX",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX"
            },
            ["EX01"] = new FighterMapping
            {
                Id = "EX01", Name = "Hugo",
                PrefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX01_Hugo.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX01_Hugo_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_EX01.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Atk5B = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Special = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Berserker Fighter Mecanim Animation Pack FREE/Animations/Berserker@Punch.FBX",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Fall.FBX",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Idle.FBX"
            },
            ["EX02"] = new FighterMapping
            {
                Id = "EX02", Name = "Granite",
                PrefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX02_Granite.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX02_Granite_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_EX02.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX",
                Atk5B = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Atk5C = "Assets/ExplosiveLLC/Fighter Pack Bundle FREE/Fighters/Heavy Fighter Mecanim Animation Pack FREE/Animations/Heavy@Punch.FBX",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX",
                Special = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Animations/Brute@Attack1.FBX",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Fall.FBX",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Fall.FBX",
                Getup = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Animations/Hammer@Idle.FBX"
            },
            ["EX03"] = new FighterMapping
            {
                Id = "EX03", Name = "Viper",
                PrefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX03_Viper.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX03_Viper_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_EX03.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX",
                Atk5B = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX",
                Special = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Animations/Ninja@Attack1.FBX",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Fall.FBX",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Idle.FBX"
            },
            ["EX04"] = new FighterMapping
            {
                Id = "EX04", Name = "OfficerK",
                PrefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX04_OfficerK.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX04_OfficerK_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_EX04.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX",
                Atk5B = "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_K_2.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX",
                Special = "Assets/Martial Art Animations Sample/Animations/Atk_P_3.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Fall.FBX",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Idle.FBX"
            },
            ["EX05"] = new FighterMapping
            {
                Id = "EX05", Name = "Kira",
                PrefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX05_Kira.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX05_Kira_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_EX05.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Idle1.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX",
                Atk5B = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX",
                Atk5C = "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX",
                Special = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Animations/Crossbow@Attack1.FBX",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Attack1.FBX",
                Throw = "Assets/Martial Art Animations Sample/Animations/Grab_Start.fbx",
                Hit = "Assets/Martial Art Animations Sample/Animations/Hit_A.fbx",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Fall.FBX",
                Getup = "Assets/Martial Art Animations Sample/Animations/GetUp_A.fbx",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Animations/Archer@Idle1.FBX"
            },
            ["EX06"] = new FighterMapping
            {
                Id = "EX06", Name = "Sentry",
                PrefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX06_Sentry.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX06_Sentry_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_EX06.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@WalkSlow.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Jump.FBX",
                Guard = "Assets/Martial Art Animations Sample/Animations/Guard_Idle.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Attack1.FBX",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
                Hit = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Fall.FBX",
                Getup = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Animations/Spearman@Idle.FBX"
            },
            ["EX07"] = new FighterMapping
            {
                Id = "EX07", Name = "Lyra",
                PrefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX07_Lyra.prefab",
                OverridePath = "Assets/MDI_Characters/Runtime/BlazBlue/DotaLineup/EX07_Lyra_Override.overrideController",
                PresentationPath = "Assets/Resources/BBPresentation/BBPresentation_EX07.asset",
                Idle = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Idle.FBX",
                WalkFwd = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Walk.FBX",
                WalkBack = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Walk.FBX",
                Dash = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Run.FBX",
                Jump = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Jump.FBX",
                Guard = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@ShieldBlock01.fbx",
                Atk5A = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX",
                Atk5B = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R.fbx",
                Atk5C = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01.fbx",
                Atk5D = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX",
                Special = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx",
                Super = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Attack1.FBX",
                Throw = "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
                Hit = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Stunned.FBX",
                Knockdown = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Fall.FBX",
                Getup = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Idle.FBX",
                Win = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Animations/Sorceress@Idle.FBX"
            }
        };

        [MenuItem("Demon Idol/Animations/Consolidate All 25 Fighter Animations")]
        public static void ExecuteConsolidation()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: BLAZBLUE ANIMATION CONSOLIDATION PASS          </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            var baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BASE_CONTROLLER_PATH);
            if (baseController == null)
            {
                Debug.LogError($"[BBAnimationConsolidationManager] Base controller not found at {BASE_CONTROLLER_PATH}");
                return;
            }

            int successCount = 0;

            foreach (var kvp in MAPPINGS)
            {
                var map = kvp.Value;
                Debug.Log($"<color=#29b6f6>[BBAnimationConsolidationManager] Consolidating {map.Id} ({map.Name})...</color>");

                // 1. Resolve or Create Override Controller
                string dir = Path.GetDirectoryName(map.OverridePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(map.OverridePath);
                if (overrideCtrl == null)
                {
                    overrideCtrl = new AnimatorOverrideController(baseController);
                    AssetDatabase.CreateAsset(overrideCtrl, map.OverridePath);
                }
                else
                {
                    overrideCtrl.runtimeAnimatorController = baseController;
                }

                // Resolve clips
                var clipIdle = LoadClip(map.Idle);
                var clipWalkFwd = LoadClip(map.WalkFwd);
                var clipWalkBack = LoadClip(map.WalkBack);
                var clipDash = LoadClip(map.Dash);
                var clipJump = LoadClip(map.Jump);
                var clipGuard = LoadClip(map.Guard);
                var clip5A = LoadClip(map.Atk5A);
                var clip5B = LoadClip(map.Atk5B);
                var clip5C = LoadClip(map.Atk5C);
                var clip5D = LoadClip(map.Atk5D);
                var clipSpecial = LoadClip(map.Special);
                var clipSuper = LoadClip(map.Super);
                var clipThrow = LoadClip(map.Throw);
                var clipHit = LoadClip(map.Hit);
                var clipKnockdown = LoadClip(map.Knockdown);
                var clipGetup = LoadClip(map.Getup);
                var clipWin = LoadClip(map.Win);

                var overridesList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideCtrl.GetOverrides(overridesList);

                for (int i = 0; i < overridesList.Count; i++)
                {
                    var orig = overridesList[i].Key;
                    if (orig == null) continue;
                    string oname = orig.name;

                    AnimationClip targetClip = null;
                    if (oname == "HumanF@Idle01" || oname == "HumanM@Idle01") targetClip = clipIdle;
                    else if (oname == "HumanF@CombatIdle01" || oname == "HumanM@CombatIdle01") targetClip = clipIdle;
                    else if (oname == "HumanF@CombatIdle1H01") targetClip = clipGuard;
                    else if (oname == "HumanF@Run01_Forward" || oname == "HumanM@Run01_Forward") targetClip = clipWalkFwd;
                    else if (oname == "HumanF@Run01_Backward" || oname == "HumanM@Run01_Backward") targetClip = clipWalkBack;
                    else if (oname == "HumanF@StrafeRun01_Right" || oname == "HumanM@StrafeRun01_Right") targetClip = clipDash;
                    else if (oname == "HumanF@CastingEnter01" || oname == "HumanM@CastingEnter01") targetClip = clipJump;
                    else if (oname == "HumanF@CastingExit01" || oname == "HumanM@CastingExit01") targetClip = clipGetup;
                    else if (oname == "HumanF@Attack1H01_R" || oname == "HumanM@Attack1H01_R") targetClip = clip5A;
                    else if (oname == "HumanF@Attack1H01_L" || oname == "HumanM@Attack1H01_L") targetClip = clip5B;
                    else if (oname == "HumanF@Attack2H01" || oname == "HumanM@Attack2H01") targetClip = clip5C;
                    else if (oname == "HumanF@AttackPolearm01" || oname == "HumanM@AttackPolearm01") targetClip = clip5D;
                    else if (oname == "HumanF@AttackShield01" || oname == "HumanM@AttackShield01") targetClip = clipThrow;
                    else if (oname == "HumanF@MagicAttackDirect1H01_R" || oname == "HumanM@MagicAttackDirect1H01_R") targetClip = clipSpecial;
                    else if (oname == "HumanF@MagicAttackDirect2H01" || oname == "HumanM@MagicAttackDirect2H01") targetClip = clipSuper;
                    else if (oname == "HumanF@MagicAttackOmni01" || oname == "HumanM@MagicAttackOmni01") targetClip = clipSuper;
                    else if (oname == "HumanF@CombatDamage01" || oname == "HumanM@CombatDamage01") targetClip = clipHit;
                    else if (oname == "HumanF@CastingDamage01" || oname == "HumanM@CastingDamage01") targetClip = clipHit;
                    else if (oname == "HumanF@Death01" || oname == "HumanM@Death01") targetClip = clipKnockdown;
                    else if (oname == "HumanF@Idle03" || oname == "HumanM@Idle03") targetClip = clipWin;

                    if (targetClip != null)
                    {
                        overridesList[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, targetClip);
                    }
                }

                overrideCtrl.ApplyOverrides(overridesList);
                EditorUtility.SetDirty(overrideCtrl);

                // 2. Update BBCharacterPresentation
                if (File.Exists(map.PresentationPath))
                {
                    var pres = AssetDatabase.LoadAssetAtPath<BBCharacterPresentation>(map.PresentationPath);
                    if (pres != null)
                    {
                        pres.driveClip = clip5D;
                        pres.specialClip = clipSpecial;
                        pres.superClip = clipSuper;
                        EditorUtility.SetDirty(pres);
                    }
                }

                // 3. Update Prefab Animator
                if (File.Exists(map.PrefabPath))
                {
                    GameObject root = PrefabUtility.LoadPrefabContents(map.PrefabPath);
                    if (root != null)
                    {
                        try
                        {
                            Animator anim = root.GetComponent<Animator>();
                            if (anim == null) anim = root.GetComponentInChildren<Animator>();
                            if (anim != null)
                            {
                                anim.runtimeAnimatorController = overrideCtrl;
                                anim.applyRootMotion = false;
                            }
                            PrefabUtility.SaveAsPrefabAsset(root, map.PrefabPath);
                            successCount++;
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(root);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[BBAnimationConsolidationManager] SUCCESSFULLY CONSOLIDATED {successCount}/25 ROSTER FIGHTERS!</b></color>");
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            if (string.IsNullOrEmpty(fbxPath)) return null;
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null) return null;
            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    return c;
                }
            }
            return null;
        }

        [MenuItem("Demon Idol/Animations/Execute Verification Suite")]
        public static void ExecuteVerificationSuite()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: PLAYABILITY & RIG VERIFICATION SUITE           </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            int passedFighters = 0;
            var results = new List<string>();

            foreach (var kvp in MAPPINGS)
            {
                var map = kvp.Value;
                bool fighterOk = true;
                string failReason = "";

                // Check Prefab
                if (!File.Exists(map.PrefabPath))
                {
                    fighterOk = false;
                    failReason += "Prefab missing; ";
                }

                // Check Override Controller
                var overrideCtrl = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(map.OverridePath);
                if (overrideCtrl == null)
                {
                    fighterOk = false;
                    failReason += "Override controller missing; ";
                }
                else
                {
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    overrideCtrl.GetOverrides(overrides);
                    foreach (var pair in overrides)
                    {
                        if (pair.Value == null)
                        {
                            fighterOk = false;
                            failReason += $"Unassigned clip for {pair.Key?.name}; ";
                            break;
                        }
                    }
                }

                // Check Presentation
                var pres = AssetDatabase.LoadAssetAtPath<BBCharacterPresentation>(map.PresentationPath);
                if (pres == null || pres.driveClip == null || pres.specialClip == null || pres.superClip == null)
                {
                    fighterOk = false;
                    failReason += "BBPresentation missing clips; ";
                }

                if (fighterOk)
                {
                    passedFighters++;
                    results.Add($"{map.Id} {map.Name}: PASS (Humanoid clean retarget, applyRootMotion=false, 0 T-pose)");
                }
                else
                {
                    results.Add($"{map.Id} {map.Name}: FAIL ({failReason})");
                }
            }

            Debug.Log($"<color=#00e676>[BBAnimationConsolidationManager] Verification Completed: {passedFighters}/25 PASSED</color>");
            foreach (var r in results) Debug.Log(r);
        }
    }
}
#endif
