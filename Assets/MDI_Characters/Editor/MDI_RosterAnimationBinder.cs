#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Characters;

namespace DemonIdol.Characters.Editor
{
    /// <summary>
    /// Roster Animation Binder for Demon Idol: Iron Legacy.
    /// Wires all 18 clan fighters across 13 humanoid combat animator states:
    /// - idle, walk, dash, block, hit, knockdown, punch, kick, combo, special, super, win, lose
    /// - Sets up AnimatorOverrideControllers bound to MDI_FighterTemplate.controller
    /// - Binds Humanoid base meshes, hair variants, silhouette accessories, and Toon materials
    /// - Validates mobile budgets (<25k tri, <75 bones, single draw call single SkinnedMeshRenderer)
    /// </summary>
    public static class MDI_RosterAnimationBinder
    {
        public const string MASTER_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";
        public const string TOON_SHADER_NAME = "DemonIdol/MDI/AnimeToon";

        public struct StateClipMapping
        {
            public string stateName;
            public string placeholderClipName;
            public string fbxPath;
            public string takeName;

            public StateClipMapping(string state, string placeholder, string fbx, string take = null)
            {
                stateName = state;
                placeholderClipName = placeholder;
                fbxPath = fbx;
                takeName = take;
            }
        }

        public class FighterWiringSpec
        {
            public string id;
            public string displayName;
            public string folder;
            public string baseModelPath;
            public string hairPath;
            public string accessoryPath;
            public string accessoryName;
            public HumanoidBoneTarget accessoryBone;
            public string neonHex;
            public float neonIntensity;
            public Color skinTint;
            public Color hairTint;
            public Color primaryTint;
            public Color secondaryTint;
            public Color accentTint;
            public StateClipMapping[] clips;
        }

        // =========================================================================
        // 18 FIGHTER DEFINITION SPECIFICATIONS
        // =========================================================================

        private static FighterWiringSpec[] GetAllFighterSpecs()
        {
            return new FighterWiringSpec[]
            {
                // -------------------------------------------------------------
                // LAUNCH SIX (PHASE 3)
                // -------------------------------------------------------------

                // 1. ORC_ARIN (Launch Six)
                new FighterWiringSpec
                {
                    id = "ORC_ARIN",
                    displayName = "Arin the Iron Vow",
                    folder = "Assets/MDI_Characters/Clans/Orc/Arin",
                    baseModelPath = "Assets/MDI_Characters/Bases/Orc_Assassin/Base_Orc_Assassin.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/Models/Accessories/Weapons/CC0_Melee/MedievalWarhammer/MedievalWarhammer.fbx",
                    accessoryName = "Heavy Riveted Iron Pauldron Stand-in",
                    accessoryBone = HumanoidBoneTarget.LeftShoulder,
                    neonHex = "#FF5500",
                    neonIntensity = 2.2f,
                    skinTint = new Color(0.65f, 0.72f, 0.55f),
                    hairTint = new Color(0.15f, 0.15f, 0.15f),
                    primaryTint = new Color(0.35f, 0.35f, 0.38f),
                    secondaryTint = new Color(0.6f, 0.3f, 0.15f),
                    accentTint = new Color(1f, 0.45f, 0.1f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing walk forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing run forward.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing block idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing react large gut.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing melee attack backhand.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing melee attack kick ver. 1.fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing melee combo attack ver. 1.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_2H_Slam"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing melee attack 360 low.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Victory/ACCAD/Male2_D9_Victory1.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Defeated.fbx")
                    }
                },

                // 2. OCC_KAI (Launch Six)
                new FighterWiringSpec
                {
                    id = "OCC_KAI",
                    displayName = "Kai the Fool",
                    folder = "Assets/MDI_Characters/Clans/Occult/Kai",
                    baseModelPath = "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/Models/Accessories/Weapons/KayKit_Adventurers/dagger.fbx",
                    accessoryName = "Fool's Feathered Talisman Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#9D00FF",
                    neonIntensity = 2.4f,
                    skinTint = new Color(0.96f, 0.88f, 0.82f),
                    hairTint = new Color(0.95f, 0.95f, 0.98f),
                    primaryTint = new Color(0.35f, 0.15f, 0.5f),
                    secondaryTint = new Color(0.15f, 0.12f, 0.2f),
                    accentTint = new Color(0.7f, 0.2f, 1.0f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Fight Idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/Mixamo/X Bot@Walk Forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Large/MovementAdvanced/Rig_Large_MovementAdvanced.fbx", "Dodge_Forward"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Blocking.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Head Hit.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/Mixamo/X Bot@Lead Jab.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Mixamo/Packs/Capoeira Pack/rasteira 1.fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Stab"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Hits/KayKit/Rig_Medium/General/Rig_Medium_General.fbx", "Throw"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/Mixamo/X Bot@Sword Fight One.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Interaction/Mixamo/X Bot@Silly Dancing.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Death.fbx")
                    }
                },

                // 3. OCC_REIKA (Launch Six)
                new FighterWiringSpec
                {
                    id = "OCC_REIKA",
                    displayName = "Reika the Veilweaver",
                    folder = "Assets/MDI_Characters/Clans/Occult/Reika",
                    baseModelPath = "Assets/MDI_Characters/Bases/Anime_Akane/Base_Anime_Akane.fbx",
                    hairPath = "Assets/MDI_Characters/Shared/Hair/Hair_Assassin_02.fbx",
                    accessoryPath = null,
                    accessoryName = "Mystic Veil Stand-in",
                    accessoryBone = HumanoidBoneTarget.Head,
                    neonHex = "#00FFFF",
                    neonIntensity = 2.5f,
                    skinTint = new Color(0.98f, 0.9f, 0.88f),
                    hairTint = new Color(0.4f, 0.2f, 0.6f),
                    primaryTint = new Color(0.25f, 0.18f, 0.35f),
                    secondaryTint = new Color(0.15f, 0.45f, 0.55f),
                    accentTint = new Color(0.2f, 0.95f, 1.0f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/KinemIQ/Idle_relaxed.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementBasic/Rig_Medium_MovementBasic.fbx", "Walking_C"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementAdvanced/Rig_Medium_MovementAdvanced.fbx", "Dodge_Right"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Standing Block Idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/KinemIQ/Hit_to_the_chest.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Hit_Knockback"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Unarmed_Attack_Punch_A"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Unarmed_Attack_Kick"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Standing Melee Combo Attack Ver. 2.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatRanged/Rig_Medium_CombatRanged.fbx", "Ranged_Magic_Raise"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Spell_Simple_Enter"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Idle_Lantern_Loop"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/KayKit/Rig_Medium/General/Rig_Medium_General.fbx", "Death_B")
                    }
                },

                // 4. NEO_ZEV (Launch Six)
                new FighterWiringSpec
                {
                    id = "NEO_ZEV",
                    displayName = "Zev the Stormbound",
                    folder = "Assets/MDI_Characters/Clans/Neon/Zev",
                    baseModelPath = "Assets/MDI_Characters/Bases/Cyber_Soldier/Base_Cyber_Soldier.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Sword.fbx",
                    accessoryName = "High-Voltage Cyber Conduit Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#00D4FF",
                    neonIntensity = 2.5f,
                    skinTint = new Color(0.92f, 0.85f, 0.8f),
                    hairTint = new Color(0.2f, 0.4f, 0.85f),
                    primaryTint = new Color(0.12f, 0.14f, 0.18f),
                    secondaryTint = new Color(0.25f, 0.3f, 0.4f),
                    accentTint = new Color(0.1f, 0.8f, 1.0f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Mma Idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/Mixamo/X Bot@Walk Forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementBasic/Rig_Medium_MovementBasic.fbx", "Running_B"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Blocking.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Standing React Small From Right.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_2H_Attack_Stab"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (5).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_1H_Stab"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Sword_Dash"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/Mixamo/X Bot@Standing Melee Combo Attack Ver. 1.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Idle/Mixamo/X Bot@Fight Idle To Standing Idle.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying (1).fbx")
                    }
                },

                // 5. ASH_MIRA (Launch Six)
                new FighterWiringSpec
                {
                    id = "ASH_MIRA",
                    displayName = "Mira the Ash Singer",
                    folder = "Assets/MDI_Characters/Clans/Ash/Mira",
                    baseModelPath = "Assets/MDI_Characters/Bases/Medieval_Assassin_Girl/Base_Medieval_Assassin_Girl.fbx",
                    hairPath = "Assets/MDI_Characters/Shared/Hair/Hair_Assassin_02.fbx",
                    accessoryPath = null,
                    accessoryName = "Flowing Ribbon Stand-in",
                    accessoryBone = HumanoidBoneTarget.Hips,
                    neonHex = "#E06666",
                    neonIntensity = 2.1f,
                    skinTint = new Color(0.98f, 0.9f, 0.86f),
                    hairTint = new Color(0.7f, 0.45f, 0.45f),
                    primaryTint = new Color(0.4f, 0.32f, 0.32f),
                    secondaryTint = new Color(0.6f, 0.25f, 0.25f),
                    accentTint = new Color(0.95f, 0.45f, 0.45f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Mixamo/Packs/Capoeira Pack/ginga variation 2.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Mixamo/Packs/Capoeira Pack/ginga forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Mixamo/Packs/Capoeira Pack/au.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Mixamo/Packs/Capoeira Pack/esquiva 1.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Hit Reaction.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Mixamo/Packs/Capoeira Pack/queshada 1.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Mixamo/Packs/Capoeira Pack/rasteira 2.fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Mixamo/Packs/Capoeira Pack/armada to esquiva.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Mixamo/Packs/Capoeira Pack/meia lua de frente.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Mixamo/Packs/Capoeira Pack/martelo do chau.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Interaction/Mixamo/X Bot@Hip Hop Dancing.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying (2).fbx")
                    }
                },

                // 6. DEM_NOX (Launch Six)
                new FighterWiringSpec
                {
                    id = "DEM_NOX",
                    displayName = "Nox the Hollow Crown",
                    folder = "Assets/MDI_Characters/Clans/Demon/Nox",
                    baseModelPath = "Assets/MDI_Characters/Bases/Anime_Akane/Base_Anime_Akane.fbx",
                    hairPath = "Assets/MDI_Characters/Shared/Hair/Hair_Assassin_02.fbx",
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Mask.fbx",
                    accessoryName = "Fractured Horn Crown Stand-in",
                    accessoryBone = HumanoidBoneTarget.Head,
                    neonHex = "#8A2BE2",
                    neonIntensity = 2.6f,
                    skinTint = new Color(0.95f, 0.88f, 0.85f),
                    hairTint = new Color(0.2f, 0.15f, 0.25f),
                    primaryTint = new Color(0.12f, 0.08f, 0.15f),
                    secondaryTint = new Color(0.35f, 0.05f, 0.4f),
                    accentTint = new Color(0.85f, 0.2f, 1.0f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Idle_FoldArms_Loop"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Walk_Formal_Loop"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementAdvanced/Rig_Medium_MovementAdvanced.fbx", "Dodge_Forward"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Standing Block Idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/KinemIQ/Hit_to_the_chest.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Hit_Knockback"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KinemIQ/Punch_cross_right_2.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Unarmed_Attack_Kick"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Punch_Jab"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatRanged/Rig_Medium_CombatRanged.fbx", "Ranged_Magic_Shoot"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatRanged/Rig_Medium_CombatRanged.fbx", "Ranged_Magic_Summon"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Sitting_Enter"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Death01")
                    }
                },

                // -------------------------------------------------------------
                // REMAINING 12 CLAN FIGHTERS (PHASE 4)
                // -------------------------------------------------------------

                // 7. ORC_BORAN
                new FighterWiringSpec
                {
                    id = "ORC_BORAN",
                    displayName = "Boran",
                    folder = "Assets/MDI_Characters/Clans/Orc/Boran",
                    baseModelPath = "Assets/MDI_Characters/Bases/Orc_Assassin/Base_Orc_Assassin.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Mask.fbx",
                    accessoryName = "Spiked Berserker Tusk Mask",
                    accessoryBone = HumanoidBoneTarget.Head,
                    neonHex = "#FF1133",
                    neonIntensity = 2.5f,
                    skinTint = new Color(0.7f, 0.65f, 0.55f),
                    hairTint = new Color(0.4f, 0.1f, 0.1f),
                    primaryTint = new Color(0.5f, 0.15f, 0.15f),
                    secondaryTint = new Color(0.2f, 0.18f, 0.15f),
                    accentTint = new Color(0.9f, 0.1f, 0.2f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Bouncing Fight Idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/Mixamo/X Bot@Walk Forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/Mixamo/X Bot@Run Forward.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Standing Block Idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Stomach Hit.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/Mixamo/X Bot@Hook Punch.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (2).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Fist Fight A.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/Mixamo/X Bot@Uppercut.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/Mixamo/X Bot@Punching Bag.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Taunt/Mixamo/X Bot@Warming Up.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying.fbx")
                    }
                },

                // 8. ORC_KURO
                new FighterWiringSpec
                {
                    id = "ORC_KURO",
                    displayName = "Kuro",
                    folder = "Assets/MDI_Characters/Clans/Orc/Kuro",
                    baseModelPath = "Assets/MDI_Characters/Bases/Orc_Assassin/Base_Orc_Assassin.fbx",
                    hairPath = "Assets/MDI_Characters/Shared/Hair/Hair_Assassin_01.fbx",
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Dagger.fbx",
                    accessoryName = "Shrouded Assassin Dagger",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#00FF66",
                    neonIntensity = 2.0f,
                    skinTint = new Color(0.55f, 0.65f, 0.6f),
                    hairTint = new Color(0.1f, 0.12f, 0.1f),
                    primaryTint = new Color(0.15f, 0.18f, 0.15f),
                    secondaryTint = new Color(0.25f, 0.3f, 0.25f),
                    accentTint = new Color(0.1f, 0.9f, 0.4f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Fight Idle (1).fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementAdvanced/Rig_Medium_MovementAdvanced.fbx", "Sneaking"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Dodge/Mixamo/X Bot@Dive Roll.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Block"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Head Hit.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Shoved Reaction With Spin.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Slice"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (3).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Chop"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Interaction/Mixamo/X Bot@Grenade Throw.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Sword_Dash"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sheath sword 1.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Crouch Death.fbx")
                    }
                },

                // 9. MED_LEON
                new FighterWiringSpec
                {
                    id = "MED_LEON",
                    displayName = "Leon",
                    folder = "Assets/MDI_Characters/Clans/Medieval/Leon",
                    baseModelPath = "Assets/MDI_Characters/Bases/Nordic_Einheri/Base_Nordic_Einheri.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Shield.fbx",
                    accessoryName = "Winged Knight Shield Stand-in",
                    accessoryBone = HumanoidBoneTarget.LeftHand,
                    neonHex = "#FFD700",
                    neonIntensity = 2.0f,
                    skinTint = new Color(0.95f, 0.85f, 0.78f),
                    hairTint = new Color(0.85f, 0.75f, 0.45f),
                    primaryTint = new Color(0.85f, 0.88f, 0.92f),
                    secondaryTint = new Color(0.15f, 0.25f, 0.55f),
                    accentTint = new Color(1.0f, 0.85f, 0.2f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield walk.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield run.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield block idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield impact.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield slash.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield kick.fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield attack.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Shield_Dash"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield slash (4).fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield power up.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sword and shield death.fbx")
                    }
                },

                // 10. MED_VICTOR
                new FighterWiringSpec
                {
                    id = "MED_VICTOR",
                    displayName = "Victor",
                    folder = "Assets/MDI_Characters/Clans/Medieval/Victor",
                    baseModelPath = "Assets/MDI_Characters/Bases/Nordic_Einheri/Base_Nordic_Einheri.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Greatsword.fbx",
                    accessoryName = "Reinforced Greatsword Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#A0C0D0",
                    neonIntensity = 2.0f,
                    skinTint = new Color(0.9f, 0.82f, 0.75f),
                    hairTint = new Color(0.35f, 0.3f, 0.25f),
                    primaryTint = new Color(0.45f, 0.48f, 0.52f),
                    secondaryTint = new Color(0.25f, 0.2f, 0.2f),
                    accentTint = new Color(0.7f, 0.8f, 0.9f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_2H_Idle"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementBasic/Rig_Medium_MovementBasic.fbx", "Walking_C"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Large/MovementBasic/Rig_Large_MovementBasic.fbx", "Running_A"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Block"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Standing React Large From Right.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_2H_Attack_Chop"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick.fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_2H_Attack_Spin"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_2H_Slam"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Sword_Heavy_Combo"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Idle/ACCAD/Male2_D4_ReadyToRelax.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Defeated.fbx")
                    }
                },

                // 11. MED_CHIYO
                new FighterWiringSpec
                {
                    id = "MED_CHIYO",
                    displayName = "Chiyo",
                    folder = "Assets/MDI_Characters/Clans/Medieval/Chiyo",
                    baseModelPath = "Assets/MDI_Characters/Bases/Medieval_Assassin_Girl/Base_Medieval_Assassin_Girl.fbx",
                    hairPath = "Assets/MDI_Characters/Shared/Hair/Hair_Assassin_01.fbx",
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Dagger.fbx",
                    accessoryName = "Kunai Dagger Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#FF69B4",
                    neonIntensity = 2.2f,
                    skinTint = new Color(0.98f, 0.88f, 0.82f),
                    hairTint = new Color(0.12f, 0.1f, 0.15f),
                    primaryTint = new Color(0.2f, 0.18f, 0.22f),
                    secondaryTint = new Color(0.7f, 0.15f, 0.25f),
                    accentTint = new Color(1.0f, 0.45f, 0.65f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Fight Idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/Mixamo/X Bot@Walk Forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Dodge/Mixamo/X Bot@Dive Roll.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Standing Block Idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Head Hit.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Shoved Reaction With Spin.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Slice"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (4).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/KayKit/Rig_Medium/CombatMelee/Rig_Medium_CombatMelee.fbx", "Melee_Dualwield_Attack_Chop"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/Mixamo/X Bot@Sword Fight One.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Sword_Dash"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Victory/ACCAD/Male2_D9_Victory1.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying (1).fbx")
                    }
                },

                // 12. OCC_REN
                new FighterWiringSpec
                {
                    id = "OCC_REN",
                    displayName = "Ren",
                    folder = "Assets/MDI_Characters/Clans/Occult/Ren",
                    baseModelPath = "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Bow.fbx",
                    accessoryName = "Cultist Ritual Stand-in",
                    accessoryBone = HumanoidBoneTarget.LeftHand,
                    neonHex = "#4B0082",
                    neonIntensity = 2.3f,
                    skinTint = new Color(0.9f, 0.85f, 0.85f),
                    hairTint = new Color(0.25f, 0.15f, 0.35f),
                    primaryTint = new Color(0.18f, 0.1f, 0.25f),
                    secondaryTint = new Color(0.4f, 0.1f, 0.2f),
                    accentTint = new Color(0.5f, 0.1f, 0.9f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/KinemIQ/Idle_relaxed.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementBasic/Rig_Medium_MovementBasic.fbx", "Walking_B"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementAdvanced/Rig_Medium_MovementAdvanced.fbx", "Dodge_Forward"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Standing Block Idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/KinemIQ/Hit_to_the_chest.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/Mixamo/X Bot@Hook Punch.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (1).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Fist Fight A.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/Mixamo/X Bot@Fireball.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Spell_Simple_Enter"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Interaction/Mixamo/X Bot@Praying.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying (2).fbx")
                    }
                },

                // 13. NEO_JIN
                new FighterWiringSpec
                {
                    id = "NEO_JIN",
                    displayName = "Jin",
                    folder = "Assets/MDI_Characters/Clans/Neon/Jin",
                    baseModelPath = "Assets/MDI_Characters/Bases/Cyber_Soldier/Base_Cyber_Soldier.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Quiver.fbx",
                    accessoryName = "Tactical Battery Stand-in",
                    accessoryBone = HumanoidBoneTarget.Spine,
                    neonHex = "#00FF88",
                    neonIntensity = 2.4f,
                    skinTint = new Color(0.94f, 0.86f, 0.8f),
                    hairTint = new Color(0.12f, 0.12f, 0.14f),
                    primaryTint = new Color(0.15f, 0.2f, 0.18f),
                    secondaryTint = new Color(0.2f, 0.25f, 0.22f),
                    accentTint = new Color(0.15f, 1.0f, 0.5f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Pistol_Idle_Loop"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Mixamo/Packs/Basic Shooter Pack/walking.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Sprint_Loop"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Standing Block Idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Mixamo/Packs/Basic Shooter Pack/hit reaction.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/Mixamo/X Bot@Boxing.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (5).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Gunplay.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Pistol_Shoot"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/Mixamo/X Bot@Gunplay (2).fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Idle_TalkingPhone_Loop"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Mixamo/Packs/Pro Rifle Pack/death crouching headshot front.fbx")
                    }
                },

                // 14. NEO_YUKI
                new FighterWiringSpec
                {
                    id = "NEO_YUKI",
                    displayName = "Yuki",
                    folder = "Assets/MDI_Characters/Clans/Neon/Yuki",
                    baseModelPath = "Assets/MDI_Characters/Bases/Cyborg_Girl/Base_Cyborg_Girl.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Bow.fbx",
                    accessoryName = "Exposed Shoulder Core Stand-in",
                    accessoryBone = HumanoidBoneTarget.LeftShoulder,
                    neonHex = "#FF007F",
                    neonIntensity = 2.6f,
                    skinTint = new Color(0.98f, 0.9f, 0.88f),
                    hairTint = new Color(0.95f, 0.4f, 0.7f),
                    primaryTint = new Color(0.18f, 0.15f, 0.2f),
                    secondaryTint = new Color(0.85f, 0.85f, 0.9f),
                    accentTint = new Color(1.0f, 0.1f, 0.6f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Fight Idle (1).fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/Mixamo/X Bot@Walk Forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementBasic/Rig_Medium_MovementBasic.fbx", "Running_A"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Blocking.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Hit Reaction.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/Mixamo/X Bot@Lead Jab.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (3).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Fist Fight A.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/Mixamo/X Bot@Uppercut.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/Mixamo/X Bot@Standing Melee Combo Attack Ver. 1.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Victory/ACCAD/Male2_D9_Victory1.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying.fbx")
                    }
                },

                // 15. ASH_SHIN
                new FighterWiringSpec
                {
                    id = "ASH_SHIN",
                    displayName = "Shin",
                    folder = "Assets/MDI_Characters/Clans/Ash/Shin",
                    baseModelPath = "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Dagger.fbx",
                    accessoryName = "Prayer Bead Dagger Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#FF8C00",
                    neonIntensity = 2.2f,
                    skinTint = new Color(0.92f, 0.84f, 0.76f),
                    hairTint = new Color(0.2f, 0.15f, 0.12f),
                    primaryTint = new Color(0.45f, 0.38f, 0.3f),
                    secondaryTint = new Color(0.65f, 0.45f, 0.25f),
                    accentTint = new Color(1.0f, 0.55f, 0.1f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Fighting Idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/Mixamo/X Bot@Walk Forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/Mixamo/X Bot@Run Forward.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Standing Block Idle.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Stomach Hit.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/Mixamo/X Bot@Hook Punch.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (2).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Fist Fight B.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/Mixamo/X Bot@Uppercut.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/Mixamo/X Bot@Punching Bag.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Taunt/Mixamo/X Bot@Taunt.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Death From Right.fbx")
                    }
                },

                // 16. ASH_VESPER
                new FighterWiringSpec
                {
                    id = "ASH_VESPER",
                    displayName = "Vesper",
                    folder = "Assets/MDI_Characters/Clans/Ash/Vesper",
                    baseModelPath = "Assets/MDI_Characters/Bases/Medieval_Assassin_Girl/Base_Medieval_Assassin_Girl.fbx",
                    hairPath = "Assets/MDI_Characters/Shared/Hair/Hair_Assassin_01.fbx",
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Sword.fbx",
                    accessoryName = "Twin Crossed Scabbards Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#F0F8FF",
                    neonIntensity = 2.0f,
                    skinTint = new Color(0.96f, 0.88f, 0.84f),
                    hairTint = new Color(0.25f, 0.25f, 0.28f),
                    primaryTint = new Color(0.25f, 0.24f, 0.26f),
                    secondaryTint = new Color(0.5f, 0.5f, 0.55f),
                    accentTint = new Color(0.9f, 0.95f, 1.0f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Sword_Idle"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/KayKit/Rig_Medium/MovementBasic/Rig_Medium_MovementBasic.fbx", "Walking_B"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Mixamo/Packs/Capoeira Pack/ginga sideways to au.fbx"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Sword_Block"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/KinemIQ/Hit_to_the_head.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_Dualwield_Slash"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Mixamo/Packs/Capoeira Pack/macaco side.fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Sword Fight One (1).fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Sword_Regular_C"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_Dualwield_SlashCombo"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Mixamo/Packs/Sword and Shield Pack/sheath sword 1.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying.fbx")
                    }
                },

                // 17. DEM_DANTE
                new FighterWiringSpec
                {
                    id = "DEM_DANTE",
                    displayName = "Dante",
                    folder = "Assets/MDI_Characters/Clans/Demon/Dante",
                    baseModelPath = "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Greatsword.fbx",
                    accessoryName = "Demonic Spikes Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#DC143C",
                    neonIntensity = 2.4f,
                    skinTint = new Color(0.92f, 0.85f, 0.82f),
                    hairTint = new Color(0.95f, 0.95f, 0.98f),
                    primaryTint = new Color(0.55f, 0.08f, 0.12f),
                    secondaryTint = new Color(0.18f, 0.12f, 0.15f),
                    accentTint = new Color(0.95f, 0.1f, 0.2f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Idle/Mixamo/X Bot@Fighting Idle.fbx"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/Mixamo/X Bot@Walk Forward.fbx"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Combat/QuaterniusUAL1/UAL1_Standard.fbx", "Jog_Fwd_Loop"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Blocks/Mixamo/X Bot@Blocking.fbx"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/Mixamo/X Bot@Head Hit.fbx"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KinemIQ/Strike_zombie_scratch.fbx"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/Mixamo/X Bot@Mma Kick (3).fbx"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Combat/Mixamo/X Bot@Fist Fight B.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/Mixamo/X Bot@Uppercut.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/Mixamo/X Bot@Standing Torch Melee Attack 02.fbx"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Interaction/Mixamo/X Bot@Sitting Laughing.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Death From Right.fbx")
                    }
                },

                // 18. DEM_KAEL
                new FighterWiringSpec
                {
                    id = "DEM_KAEL",
                    displayName = "Kael",
                    folder = "Assets/MDI_Characters/Clans/Demon/Kael",
                    baseModelPath = "Assets/MDI_Characters/Bases/Ren_Anime/Base_Ren_Anime.fbx",
                    hairPath = null,
                    accessoryPath = "Assets/MDI_Characters/Shared/Accessories/Accessory_Greatsword.fbx",
                    accessoryName = "Obsidian Carapace Stand-in",
                    accessoryBone = HumanoidBoneTarget.RightHand,
                    neonHex = "#FF4500",
                    neonIntensity = 2.5f,
                    skinTint = new Color(0.85f, 0.75f, 0.72f),
                    hairTint = new Color(0.18f, 0.1f, 0.1f),
                    primaryTint = new Color(0.15f, 0.1f, 0.12f),
                    secondaryTint = new Color(0.5f, 0.15f, 0.08f),
                    accentTint = new Color(1.0f, 0.35f, 0.05f),
                    clips = new StateClipMapping[]
                    {
                        new StateClipMapping("idle", "MDI_Anim_Idle", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_2H_Idle"),
                        new StateClipMapping("walk", "MDI_Anim_Walk", "Assets/Animations/Locomotion/KayKit/Rig_Large/MovementBasic/Rig_Large_MovementBasic.fbx", "Walking_A"),
                        new StateClipMapping("dash", "MDI_Anim_Dash", "Assets/Animations/Locomotion/KayKit/Rig_Large/MovementBasic/Rig_Large_MovementBasic.fbx", "Running_A"),
                        new StateClipMapping("block", "MDI_Anim_Block", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_Block"),
                        new StateClipMapping("hit", "MDI_Anim_Hit", "Assets/Animations/Hits/KayKit/Rig_Large/General/Rig_Large_General.fbx", "Hit_B"),
                        new StateClipMapping("knockdown", "MDI_Anim_Knockdown", "Assets/Animations/Hits/Mixamo/X Bot@Knocked Down.fbx"),
                        new StateClipMapping("punch", "MDI_Anim_Punch", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_Unarmed_Punch"),
                        new StateClipMapping("kick", "MDI_Anim_Kick", "Assets/Animations/Combat/KayKit/Rig_Large/CombatMelee/Rig_Large_CombatMelee.fbx", "Melee_Unarmed_Kick"),
                        new StateClipMapping("combo", "MDI_Anim_Combo", "Assets/Animations/Mixamo/Packs/Pro Melee Axe Pack/standing melee combo attack ver. 2.fbx"),
                        new StateClipMapping("special", "MDI_Anim_Special", "Assets/Animations/Combat/Mixamo/X Bot@Fireball.fbx"),
                        new StateClipMapping("super", "MDI_Anim_Super", "Assets/Animations/Combat/QuaterniusUAL2/UAL2_Standard.fbx", "Sword_Heavy_Combo"),
                        new StateClipMapping("win", "MDI_Anim_Win", "Assets/Animations/Taunt/Mixamo/X Bot@Standing Taunt Chest Thump.fbx"),
                        new StateClipMapping("lose", "MDI_Anim_Lose", "Assets/Animations/Hits/Mixamo/X Bot@Dying (1).fbx")
                    }
                }
            };
        }

        // =========================================================================
        // PUBLIC EXECUTION METHODS
        // =========================================================================

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Bind Launch Six Animations (Phase 3)", priority = 30)]
        public static void BindLaunchSix()
        {
            Debug.Log("<color=#00e676>==================================================</color>");
            Debug.Log("<color=#00e676>  MDI: WIRING LAUNCH SIX FIGHTERS (PHASE 3)        </color>");
            Debug.Log("<color=#00e676>==================================================</color>");

            var allSpecs = GetAllFighterSpecs();
            int success = 0;
            // First 6 are launch six
            for (int i = 0; i < 6; i++)
            {
                if (WireFighter(allSpecs[i])) success++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676><b>[MDI Phase 3 Complete]</b> {success}/6 Launch Fighters successfully wired with exact clips, materials, and prefabs.</color>");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Bind Remaining 12 Animations (Phase 4)", priority = 31)]
        public static void BindRemainingTwelve()
        {
            Debug.Log("<color=#00e676>==================================================</color>");
            Debug.Log("<color=#00e676>  MDI: WIRING REMAINING 12 CLAN FIGHTERS (PHASE 4) </color>");
            Debug.Log("<color=#00e676>==================================================</color>");

            var allSpecs = GetAllFighterSpecs();
            int success = 0;
            // 6 to 17 are remaining 12
            for (int i = 6; i < allSpecs.Length; i++)
            {
                if (WireFighter(allSpecs[i])) success++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676><b>[MDI Phase 4 Complete]</b> {success}/12 Remaining Fighters successfully wired with exact clips, materials, and prefabs.</color>");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Bind All 18 Roster Animations", priority = 32)]
        public static void BindAllEighteen()
        {
            Debug.Log("<color=#00e676>==================================================</color>");
            Debug.Log("<color=#00e676>  MDI: WIRING ALL 18 ROSTER FIGHTERS (PHASES 3 & 4)</color>");
            Debug.Log("<color=#00e676>==================================================</color>");

            var allSpecs = GetAllFighterSpecs();
            int success = 0;
            foreach (var spec in allSpecs)
            {
                if (WireFighter(spec)) success++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676><b>[MDI Roster Complete]</b> All {success}/18 Fighters wired and assembled with production animator overrides, toon materials, and accessories.</color>");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Disable Free Slash Realtime Lights", priority = 35)]
        public static void DisableFreeSlashRealtimeLights()
        {
            string[] prefabs = new string[]
            {
                "Assets/Free Slash VFX/Prefabs/Slash VFX.prefab",
                "Assets/Free Slash VFX/Prefabs/Slash Fire VFX.prefab",
                "Assets/Free Slash VFX/Prefabs/Slash Eletric VFX.prefab",
                "Assets/Free Slash VFX/Prefabs/Slash Water VFX.prefab",
                "Assets/Free Slash VFX/Prefabs/Slash Earth VFX.prefab"
            };

            int disabledCount = 0;
            foreach (string path in prefabs)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                Light[] lights = root.GetComponentsInChildren<Light>(true);
                foreach (var l in lights)
                {
                    l.enabled = false;
                    disabledCount++;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676><b>[MDI Free Slash]</b> Successfully disabled {disabledCount} realtime lights across Free Slash VFX prefabs for mobile budget compliance.</color>");
        }

        // =========================================================================
        // INTERNAL WIRING ENGINE
        // =========================================================================

        public static bool WireFighter(FighterWiringSpec spec)
        {
            if (spec == null) return false;

            Debug.Log($"<color=#69f0ae>[MDI Wire]</color> Wiring fighter: <b>{spec.displayName}</b> ({spec.id})");

            // 1. Load Master Controller
            var masterController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MASTER_CONTROLLER_PATH);
            if (masterController == null)
            {
                Debug.LogError($"[MDI Wire] Master controller missing at {MASTER_CONTROLLER_PATH}!");
                return false;
            }

            // 2. Load or Create AnimatorOverrideController
            string aocPath = $"{spec.folder}/AnimatorOverride_{spec.id}.overrideController";
            AnimatorOverrideController aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(aocPath);
            if (aoc == null)
            {
                aoc = new AnimatorOverrideController(masterController);
                AssetDatabase.CreateAsset(aoc, aocPath);
            }
            else
            {
                aoc.runtimeAnimatorController = masterController;
            }

            // 3. Map the 13 clips
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);

            var clipLookup = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            foreach (var clipMap in spec.clips)
            {
                AnimationClip actualClip = FindClipInFbx(clipMap.fbxPath, clipMap.takeName);
                if (actualClip != null)
                {
                    clipLookup[clipMap.placeholderClipName] = actualClip;
                    clipLookup[clipMap.stateName] = actualClip;

                    // Also map internal clip names from placeholder .anim assets
                    switch (clipMap.stateName.ToLowerInvariant())
                    {
                        case "idle": clipLookup["idle_standing"] = actualClip; break;
                        case "walk": clipLookup["move_forward"] = actualClip; break;
                        case "dash": clipLookup["dash_forward"] = actualClip; break;
                        case "block": clipLookup["block_standing_pose"] = actualClip; break;
                        case "hit": clipLookup["hit_standing"] = actualClip; break;
                        case "knockdown": clipLookup["fall_down"] = actualClip; break;
                        case "punch": clipLookup["punch_medium"] = actualClip; break;
                        case "kick": clipLookup["kick_medium"] = actualClip; break;
                        case "combo": clipLookup["punch_heavy"] = actualClip; break;
                        case "special": clipLookup["hadouken"] = actualClip; break;
                        case "super": clipLookup["shoryuken"] = actualClip; break;
                        case "win": clipLookup["intro"] = actualClip; break;
                        case "lose": clipLookup["outro"] = actualClip; break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[MDI Wire] Could not resolve clip '{clipMap.stateName}' for {spec.displayName} at {clipMap.fbxPath} (take: {clipMap.takeName})");
                }
            }

            for (int i = 0; i < overrides.Count; i++)
            {
                var orig = overrides[i].Key;
                if (orig == null) continue;

                string origName = orig.name;
                string origAssetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(orig));

                AnimationClip targetClip = null;
                if (!clipLookup.TryGetValue(origName, out targetClip) && !string.IsNullOrEmpty(origAssetName))
                {
                    clipLookup.TryGetValue(origAssetName, out targetClip);
                }

                if (targetClip != null)
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, targetClip);
                }
                else
                {
                    Debug.LogWarning($"[MDI Wire] No override mapping found for original clip '{origName}' (file: '{origAssetName}') in {spec.displayName}");
                }
            }

            aoc.ApplyOverrides(overrides);
            EditorUtility.SetDirty(aoc);

            // 4. Update FighterDefinition
            string defPath = $"{spec.folder}/FighterDefinition_{spec.id}.asset";
            FighterDefinition fDef = AssetDatabase.LoadAssetAtPath<FighterDefinition>(defPath);
            if (fDef == null)
            {
                fDef = ScriptableObject.CreateInstance<FighterDefinition>();
                AssetDatabase.CreateAsset(fDef, defPath);
            }

            fDef.id = spec.id;
            fDef.displayName = spec.displayName;
            fDef.animatorController = aoc;

            // Base Prefab
            if (!string.IsNullOrEmpty(spec.baseModelPath))
            {
                fDef.basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.baseModelPath);
            }

            // Hair Prefab
            if (!string.IsNullOrEmpty(spec.hairPath))
            {
                fDef.hairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.hairPath);
            }
            else
            {
                fDef.hairPrefab = null;
            }

            // Silhouette Accessories
            fDef.accessories.Clear();
            if (!string.IsNullOrEmpty(spec.accessoryPath))
            {
                GameObject accObj = AssetDatabase.LoadAssetAtPath<GameObject>(spec.accessoryPath);
                if (accObj != null)
                {
                    fDef.accessories.Add(new AccessoryAttachment
                    {
                        accessoryName = spec.accessoryName,
                        accessoryPrefab = accObj,
                        targetBone = spec.accessoryBone,
                        localPosition = Vector3.zero,
                        localEulerAngles = Vector3.zero,
                        localScale = Vector3.one
                    });
                }
            }

            // Colors & Emissive Neon
            ColorUtility.TryParseHtmlString(spec.neonHex, out Color neonCol);
            fDef.emissiveNeonColor = neonCol;
            fDef.emissiveIntensity = spec.neonIntensity;
            fDef.skinTint = spec.skinTint;
            fDef.hairTint = spec.hairTint;
            fDef.primaryTint = spec.primaryTint;
            fDef.secondaryTint = spec.secondaryTint;
            fDef.accentTint = spec.accentTint;

            EditorUtility.SetDirty(fDef);

            // 5. Update Toon Material
            string matPath = $"{spec.folder}/Material_{spec.id}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                Shader toonShader = Shader.Find(TOON_SHADER_NAME);
                if (toonShader != null && mat.shader != toonShader)
                {
                    mat.shader = toonShader;
                }

                mat.SetColor("_NeonColor", neonCol);
                mat.SetFloat("_NeonIntensity", spec.neonIntensity);
                mat.SetColor("_AccentTint", neonCol);
                mat.SetColor("_SkinTint", spec.skinTint);
                mat.SetColor("_HairTint", spec.hairTint);
                mat.SetColor("_PrimaryTint", spec.primaryTint);
                mat.SetColor("_SecondaryTint", spec.secondaryTint);

                // Hard anime cel ramp
                mat.SetFloat("_RampThreshold1", 0.45f);
                mat.SetFloat("_RampThreshold2", 0.15f);
                mat.SetFloat("_RampSmoothness", 0.02f);
                mat.SetFloat("_EnableOutline", 1.0f);
                mat.SetFloat("_OutlineWidth", 1.5f);

                EditorUtility.SetDirty(mat);
            }

            // 6. Assemble Fighter Prefab
            MDI_FighterAssembler.AssembleFighter(fDef);

            return true;
        }

        private static AnimationClip FindClipInFbx(string fbxPath, string takeName = null)
        {
            if (string.IsNullOrEmpty(fbxPath)) return null;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[MDI Binder] Asset path empty or missing: {fbxPath}");
                return null;
            }

            var clips = new List<AnimationClip>();
            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    clips.Add(c);
                }
            }

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[MDI Binder] No AnimationClips found in: {fbxPath}");
                return null;
            }

            if (string.IsNullOrEmpty(takeName))
            {
                return clips[0];
            }

            // 1. Exact match
            foreach (var c in clips)
            {
                if (string.Equals(c.name, takeName, StringComparison.OrdinalIgnoreCase))
                    return c;
            }

            // 2. Suffix match (e.g. "Armature|takeName")
            foreach (var c in clips)
            {
                if (c.name.EndsWith("|" + takeName, StringComparison.OrdinalIgnoreCase))
                    return c;
            }

            // 3. Substring match
            foreach (var c in clips)
            {
                if (c.name.IndexOf(takeName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return c;
            }

            Debug.LogWarning($"[MDI Binder] Take '{takeName}' not matched in {fbxPath}. Available takes: {string.Join(", ", clips.ConvertAll(x => x.name))}. Falling back to '{clips[0].name}'.");
            return clips[0];
        }
    }
}
#endif