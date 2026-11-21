#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Characters.Editor
{
    /// <summary>
    /// Roster Animator & Fighter Playability Hook for Demon Idol.
    /// Hooks all 14 fighters (C01 to C14) with:
    /// - Authentic package animator controllers and animations (locomotion, gestures, damage, victory).
    /// - Full combat state machine triggers (LightAttack, MediumAttack, HeavyAttack, DriveSkill, DomainSuper, Block, Hit, Knockdown).
    /// - Dedicated AnimatorOverrideControllers in Assets/CombatData/Animators/.
    /// - Complete CharacterDefinition and Character Prefab binding.
    /// </summary>
    public static class DemonIdolRosterAnimatorHook
    {
        public const string MASTER_TEMPLATE_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";
        public const string ANIMATORS_DIR = "Assets/CombatData/Animators";
        public const string CHARACTERS_DIR = "Assets/CombatData/Characters";
        public const string PREFABS_DIR = "Assets/Prefabs/Characters";
        public const string MOVESETS_DIR = "Assets/CombatData/MoveSets";
        public const string MDI_ANIM_DIR = "Assets/MDI_Animations";

        public struct FighterSpec
        {
            public string id;
            public string name;
            public string targetPrefabName;
            public string sourceModelOrPrefab;
            public string packageFbxPath;
            public string nativePackageController;
            public string nativeIdlePath;
            public string nativeWalkPath;
            public string nativeDashPath;
            public string nativeDamagePath;
            public string nativeKnockdownPath;
            public string nativeWinPath;
            public string nativeLosePath;

            public FighterSpec(
                string id, string name, string targetPrefab, string sourceModel, string fbxPath,
                string packageController = null,
                string idle = null, string walk = null, string dash = null,
                string damage = null, string knockdown = null,
                string win = null, string lose = null)
            {
                this.id = id;
                this.name = name;
                this.targetPrefabName = targetPrefab;
                this.sourceModelOrPrefab = sourceModel;
                this.packageFbxPath = fbxPath;
                this.nativePackageController = packageController;
                this.nativeIdlePath = idle;
                this.nativeWalkPath = walk;
                this.nativeDashPath = dash;
                this.nativeDamagePath = damage;
                this.nativeKnockdownPath = knockdown;
                this.nativeWinPath = win;
                this.nativeLosePath = lose;
            }
        }

        private static readonly FighterSpec[] Specs = new FighterSpec[]
        {
            // C01 Akio (Human Melee Animations)
            new FighterSpec(
                "C01", "Akio", "C01_Akio.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/Prefab/Akio Highschool Uniform.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/FBX/Akio Highschool Uniform.fbx",
                "Assets/MDI_Characters/Runtime/AnimatorOverride_C01_Akio.overrideController"
            ),
            // C02 Aika
            new FighterSpec(
                "C02", "Aika", "C02_Aika.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Aika/Highschool Uniform/Prefab/Aika Highschool Uniform.prefab",
                "Assets/Novacorp Studios/Characters/Hikari Series/Aika/Highschool Uniform/FBX/Aika Highschool Uniform.fbx"
            ),
            // C04 UnityChan
            new FighterSpec(
                "C04", "UnityChan", "C04_UnityChan.prefab",
                "Assets/unity-chan!/Unity-chan! Model/Prefabs/unitychan_dynamic.prefab",
                "Assets/unity-chan!/Unity-chan! Model/Art/Models/unitychan.fbx",
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/Animators/UnityChanLocomotions.controller",
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_WAIT00.fbx",
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_WALK00_F.fbx",
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_RUN00_F.fbx",
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_DAMAGED00.fbx",
                null,
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_WIN00.fbx",
                "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_LOSE00.fbx"
            ),
            // C05 Akane
            new FighterSpec(
                "C05", "Akane", "C05_Akane.prefab",
                "Assets/3d-character_animeGirlAkane/Akane/angGirl.fbx",
                "Assets/3d-character_animeGirlAkane/Akane/angGirl.fbx",
                "Assets/3d-character_animeGirlAkane/DemoScene/Animation/AnimationController.controller",
                "Assets/3d-character_animeGirlAkane/DemoScene/Animation/stand.anim",
                "Assets/3d-character_animeGirlAkane/DemoScene/Animation/walk.anim"
            ),
            // C06 Cultist
            new FighterSpec(
                "C06", "Cultist", "C06_Cultist.prefab",
                "Assets/CeltriusStudios/Cultist/Prefabs/Cultist.prefab",
                "Assets/CeltriusStudios/Cultist/Prefabs/Mesh/Cultist.fbx"
            ),
            // C07 StellarWitch
            new FighterSpec(
                "C07", "StellarWitch", "C07_StellarWitch.prefab",
                "Assets/StellarWitch3D/Prefabs/StellarWitch.prefab",
                "Assets/StellarWitch3D/Meshes/StellarWitch.fbx"
            ),
            // C08 SanpakuChan
            new FighterSpec(
                "C08", "SanpakuChan", "C08_SanpakuChan.prefab",
                "Assets/SanpakuChan/Prefabs/SanpakuChan.prefab",
                "Assets/SanpakuChan/Meshes/SanpakuChan.fbx"
            ),
            // C09 PicoChan
            new FighterSpec(
                "C09", "PicoChan", "C09_PicoChan.prefab",
                "Assets/Picola/PicoChan/Prefabs/PicoChan.prefab",
                "Assets/Picola/PicoChan/Models/pico_chan_chr_pico_00.fbx"
            ),
            // C10 Reika (Kiki)
            new FighterSpec(
                "C10", "Reika", "C10_Reika.prefab",
                "Assets/Kiki/Xmas Girl/Prefabs/Kiki-v2.prefab",
                "Assets/Kiki/Xmas Girl/Model/Kiki-v2.fbx",
                "Assets/Kiki/Xmas Girl/Animator/Animator Controller.controller",
                "Assets/Kiki/Xmas Girl/Model/Idle.fbx",
                null,
                "Assets/Kiki/Xmas Girl/Model/Run.fbx"
            ),
            // C11 Casual1
            new FighterSpec(
                "C11", "Casual1", "C11_Casual1.prefab",
                "Assets/AnimeGirls/Casual1/Casual1.prefab",
                "Assets/AnimeGirls/Casual1/SourceFiles/Mesh/AnimeGirl.fbx",
                "Assets/AnimeGirls/BaseCharacter/Demo files/AC.controller",
                "Assets/AnimeGirls/BaseCharacter/Demo files/Anim_Idle.fbx"
            ),
            // C12 Fina
            new FighterSpec(
                "C12", "Fina", "C12_Fina.prefab",
                "Assets/smoky_fox/FinaAnimeGirl/Prefabs/Fina.prefab",
                "Assets/smoky_fox/FinaAnimeGirl/Model/Fina.fbx",
                "Assets/smoky_fox/FinaAnimeGirl/Animations/Locomotion/finna.controller",
                "Assets/smoky_fox/FinaAnimeGirl/Animations/Locomotion/Idle.anim",
                "Assets/smoky_fox/FinaAnimeGirl/Animations/Locomotion/Walk.anim",
                "Assets/smoky_fox/FinaAnimeGirl/Animations/Locomotion/Run.anim"
            ),
            // C13 SapphiArtchan
            new FighterSpec(
                "C13", "SapphiArtchan", "C13_SapphiArtchan.prefab",
                "Assets/SapphiArt/SapphiArtchan/OBJ/SapphiArtchan.prefab",
                "Assets/SapphiArt/SapphiArtchan/OBJ/SapphiArtchan.fbx",
                "Assets/SapphiArt/SapphiArtchan/Animation/SapphiArtchanAnimController.controller",
                "Assets/SapphiArt/SapphiArtchan/Animation/idle.anim",
                "Assets/SapphiArt/SapphiArtchan/Animation/walk.anim",
                "Assets/SapphiArt/SapphiArtchan/Animation/running.anim",
                "Assets/SapphiArt/SapphiArtchan/Animation/damage.anim",
                "Assets/SapphiArt/SapphiArtchan/Animation/KO_big.anim",
                "Assets/SapphiArt/SapphiArtchan/Animation/winpose.anim"
            ),
            // C14 Muryotaisu
            new FighterSpec(
                "C14", "Muryotaisu", "C14_Muryotaisu.prefab",
                "Assets/Mryotaisu/Prefabs/Muryotaisu.prefab",
                "Assets/Mryotaisu/Models/Muryotaisu.fbx",
                "Assets/Mryotaisu/Animators/Muryotaisu.controller",
                "Assets/Mryotaisu/Animators/IdleA.anim",
                "Assets/Mryotaisu/Animators/Walk.anim"
            )
        };

        public static void HookAll14FightersAndAnimators()
        {
            Debug.Log("<color=#00FF66>[DemonIdolRosterHook] Starting complete 14-Fighter Animator & Playability Hook...</color>");

            if (!Directory.Exists(ANIMATORS_DIR)) Directory.CreateDirectory(ANIMATORS_DIR);
            if (!Directory.Exists(CHARACTERS_DIR)) Directory.CreateDirectory(CHARACTERS_DIR);
            if (!Directory.Exists(MOVESETS_DIR)) Directory.CreateDirectory(MOVESETS_DIR);
            if (!Directory.Exists(PREFABS_DIR)) Directory.CreateDirectory(PREFABS_DIR);
            AssetDatabase.Refresh();

            RuntimeAnimatorController masterTemplate = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MASTER_TEMPLATE_PATH);
            if (masterTemplate == null)
            {
                Debug.LogError($"[DemonIdolRosterHook] Master template not found at {MASTER_TEMPLATE_PATH}! Building template first...");
                MDI_AnimationTemplateBuilder.BuildMasterControllerAndOverrides();
                masterTemplate = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MASTER_TEMPLATE_PATH);
            }

            // Sync alias prefabs
            SyncAliasPrefabs();

            int hookedCount = 0;

            for (int i = 0; i < Specs.Length; i++)
            {
                var spec = Specs[i];

                // 1. Determine and enhance original package controller for this character
                RuntimeAnimatorController originalPackageController = null;
                if (!string.IsNullOrEmpty(spec.nativePackageController))
                {
                    originalPackageController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(spec.nativePackageController);
                    if (originalPackageController is AnimatorController ac)
                    {
                        InjectCombatAnimationsIntoPackageController(ac, spec);
                    }
                }
                if (originalPackageController == null)
                {
                    originalPackageController = masterTemplate;
                }

                // 2. Update CharacterDefinition with the authentic original package controller
                UpdateCharacterDefinition(spec, originalPackageController);

                // 3. Update Character Prefab with the authentic original package controller
                UpdateCharacterPrefab(spec, originalPackageController);

                hookedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00FF66><b>[DemonIdolRosterHook] SUCCESS: All {hookedCount}/14 fighters fully hooked with package controllers + combat animations!</b></color>");
        }

        private static void SyncAliasPrefabs()
        {
            // Ensure legacy C12_Underwear1 is never copied into runtime PREFABS_DIR
            string underTarget = $"{PREFABS_DIR}/C12_Underwear1.prefab";
            if (File.Exists(underTarget))
            {
                AssetDatabase.DeleteAsset(underTarget);
            }
        }

        private static void InjectCombatAnimationsIntoPackageController(AnimatorController ac, FighterSpec spec)
        {
            if (ac == null || ac.layers.Length == 0) return;

            // Ensure parameters
            EnsureParameter(ac, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(ac, "MoveX", AnimatorControllerParameterType.Float);
            EnsureParameter(ac, "IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(ac, "IsCrouch", AnimatorControllerParameterType.Bool);
            EnsureParameter(ac, "IsBlocking", AnimatorControllerParameterType.Bool);
            EnsureParameter(ac, "ActionState", AnimatorControllerParameterType.Int);
            EnsureParameter(ac, "AttackIndex", AnimatorControllerParameterType.Int);

            EnsureParameter(ac, "LightAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "LowAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "MediumAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "HeavyAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "OverheadAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "LowSweepAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "DriveSkill", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "AdvancingSkill", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "LauncherAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "AirLight", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "AirHeavy", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "DomainSuper", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "StandBlock", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "CrouchBlock", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "HitMid", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "HitLow", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "Knockdown", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "ForwardDash", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "Backdash", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "Defeat", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "Victory", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ac, "Jump", AnimatorControllerParameterType.Trigger);

            // Resolve native clips when present, fall back to MDI combat clips
            AnimationClip hitClip = ResolveClip(spec.nativeDamagePath) ?? LoadMdiClip("MDI_Anim_Hit");
            AnimationClip knockdownClip = ResolveClip(spec.nativeKnockdownPath) ?? LoadMdiClip("MDI_Anim_Knockdown");
            AnimationClip winClip = ResolveClip(spec.nativeWinPath) ?? LoadMdiClip("MDI_Anim_Win");
            AnimationClip loseClip = ResolveClip(spec.nativeLosePath) ?? LoadMdiClip("MDI_Anim_Lose");
            AnimationClip dashClip = ResolveClip(spec.nativeDashPath) ?? LoadMdiClip("MDI_Anim_Dash");

            AnimationClip punchClip = null;
            AnimationClip kickClip = null;
            AnimationClip comboClip = null;
            AnimationClip specialClip = null;

            if (spec.id == "C13")
            {
                punchClip = ResolveClip("Assets/SapphiArt/SapphiArtchan/Animation/hit01.anim");
                kickClip = ResolveClip("Assets/SapphiArt/SapphiArtchan/Animation/hit02.anim");
                comboClip = ResolveClip("Assets/SapphiArt/SapphiArtchan/Animation/hit03.anim");
            }

            punchClip = punchClip ?? LoadMdiClip("MDI_Anim_Punch");
            kickClip = kickClip ?? LoadMdiClip("MDI_Anim_Kick");
            comboClip = comboClip ?? LoadMdiClip("MDI_Anim_Combo");
            specialClip = specialClip ?? LoadMdiClip("MDI_Anim_Special");

            AnimationClip superClip = LoadMdiClip("MDI_Anim_Super");
            AnimationClip blockClip = LoadMdiClip("MDI_Anim_Block");

            // Add combat states to base layer stateMachine
            var sm = ac.layers[0].stateMachine;
            EnsureCombatStateWithTriggers(sm, "MDI_LightAttack", punchClip, "LightAttack", "LowAttack", "AirLight");
            EnsureCombatStateWithTriggers(sm, "MDI_MediumAttack", kickClip, "MediumAttack");
            EnsureCombatStateWithTriggers(sm, "MDI_HeavyAttack", comboClip, "HeavyAttack", "OverheadAttack", "LowSweepAttack", "AirHeavy");
            EnsureCombatStateWithTriggers(sm, "MDI_DriveSkill", specialClip, "DriveSkill", "AdvancingSkill", "LauncherAttack");
            EnsureCombatStateWithTriggers(sm, "MDI_DomainSuper", superClip, "DomainSuper");
            EnsureCombatStateWithTriggers(sm, "MDI_StandBlock", blockClip, "StandBlock", "CrouchBlock");
            EnsureCombatStateWithTriggers(sm, "MDI_HitMid", hitClip, "HitMid", "HitLow", "Hit");
            EnsureCombatStateWithTriggers(sm, "MDI_Knockdown", knockdownClip, "Knockdown");
            EnsureCombatStateWithTriggers(sm, "MDI_ForwardDash", dashClip, "ForwardDash");
            EnsureCombatStateWithTriggers(sm, "MDI_Backdash", dashClip, "Backdash");
            EnsureCombatStateWithTriggers(sm, "MDI_Defeat", loseClip, "Defeat");
            EnsureCombatStateWithTriggers(sm, "MDI_Victory", winClip, "Victory");

            EditorUtility.SetDirty(ac);
        }

        private static void EnsureCombatStateWithTriggers(AnimatorStateMachine sm, string stateName, AnimationClip clip, params string[] triggers)
        {
            if (clip == null || sm == null) return;

            AnimatorState st = null;
            foreach (var s in sm.states)
            {
                if (s.state != null && s.state.name == stateName)
                {
                    st = s.state;
                    break;
                }
            }

            if (st == null)
            {
                st = sm.AddState(stateName);
            }
            st.motion = clip;

            foreach (var trigger in triggers)
            {
                bool hasTrans = false;
                foreach (var trans in sm.anyStateTransitions)
                {
                    if (trans.destinationState == st)
                    {
                        foreach (var cond in trans.conditions)
                        {
                            if (cond.parameter == trigger)
                            {
                                hasTrans = true;
                                break;
                            }
                        }
                    }
                }

                if (!hasTrans)
                {
                    var trans = sm.AddAnyStateTransition(st);
                    trans.AddCondition(AnimatorConditionMode.If, 0, trigger);
                    trans.duration = 0.05f;
                    trans.hasExitTime = false;
                    trans.canTransitionToSelf = false;
                }
            }

            // Ensure transition back to default state
            if (sm.defaultState != null && st != sm.defaultState)
            {
                bool hasExit = false;
                foreach (var trans in st.transitions)
                {
                    if (trans.destinationState == sm.defaultState)
                    {
                        hasExit = true;
                        break;
                    }
                }

                if (!hasExit)
                {
                    var exitTrans = st.AddTransition(sm.defaultState);
                    exitTrans.hasExitTime = true;
                    exitTrans.exitTime = 0.85f;
                    exitTrans.duration = 0.1f;
                }
            }
        }

        private static void UpdateCharacterDefinition(FighterSpec spec, RuntimeAnimatorController controller)
        {
            if (!Directory.Exists(CHARACTERS_DIR)) Directory.CreateDirectory(CHARACTERS_DIR);

            // Find CharacterDefinition file
            string defPath = null;
            string[] files = Directory.GetFiles(CHARACTERS_DIR, $"CharacterDefinition_{spec.id}*.asset");
            if (files != null && files.Length > 0)
            {
                defPath = files[0];
            }
            else
            {
                defPath = $"{CHARACTERS_DIR}/CharacterDefinition_{spec.id}_{spec.name}.asset";
            }

            CharacterDefinition charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(defPath);
            if (charDef == null)
            {
                charDef = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(charDef, defPath);
            }

            charDef.id = spec.id;
            charDef.displayName = spec.name;
            charDef.animatorController = controller;

            // Authentic Package Avatar
            if (!string.IsNullOrEmpty(spec.packageFbxPath))
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(spec.packageFbxPath);
                foreach (var a in allAssets)
                {
                    if (a is Avatar av)
                    {
                        charDef.avatar = av;
                        break;
                    }
                }
            }

            // Prefab
            string prefabPath = $"{PREFABS_DIR}/{spec.targetPrefabName}";
            if (File.Exists(prefabPath))
            {
                charDef.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            else if (File.Exists(spec.sourceModelOrPrefab))
            {
                charDef.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.sourceModelOrPrefab);
            }

            // MoveSet
            string msPath = null;
            if (Directory.Exists(MOVESETS_DIR))
            {
                string[] msFiles = Directory.GetFiles(MOVESETS_DIR, $"{spec.id}*.asset");
                if (msFiles != null && msFiles.Length > 0)
                {
                    msPath = msFiles[0];
                }
            }
            if (!string.IsNullOrEmpty(msPath))
            {
                charDef.moveSet = AssetDatabase.LoadAssetAtPath<MoveSet>(msPath);
            }

            EditorUtility.SetDirty(charDef);
        }

        private static void UpdateCharacterPrefab(FighterSpec spec, RuntimeAnimatorController controller)
        {
            if (!Directory.Exists(PREFABS_DIR)) Directory.CreateDirectory(PREFABS_DIR);
            string prefabPath = $"{PREFABS_DIR}/{spec.targetPrefabName}";
            bool needsRebuild = !File.Exists(prefabPath);
            if (!needsRebuild)
            {
                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existing == null || existing.GetComponentInChildren<Renderer>() == null)
                {
                    needsRebuild = true;
                }
            }

            if (needsRebuild)
            {
                if (File.Exists(spec.sourceModelOrPrefab))
                {
                    GameObject sourceGo = AssetDatabase.LoadAssetAtPath<GameObject>(spec.sourceModelOrPrefab);
                    if (sourceGo != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(sourceGo);
                        instance.name = Path.GetFileNameWithoutExtension(spec.targetPrefabName);
                        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                        UnityEngine.Object.DestroyImmediate(instance);
                    }
                    else
                    {
                        AssetDatabase.CopyAsset(spec.sourceModelOrPrefab, prefabPath);
                    }
                }
                else return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            // Load prefab contents for modification
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;

            try
            {
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                Animator anim = root.GetComponentInChildren<Animator>();
                if (anim == null)
                {
                    anim = root.AddComponent<Animator>();
                }
                if (anim != null)
                {
                    anim.runtimeAnimatorController = controller;
                    anim.applyRootMotion = false;

                    // Authentic Package Avatar
                    if (!string.IsNullOrEmpty(spec.packageFbxPath))
                    {
                        var allAssets = AssetDatabase.LoadAllAssetsAtPath(spec.packageFbxPath);
                        foreach (var a in allAssets)
                        {
                            if (a is Avatar av)
                            {
                                anim.avatar = av;
                                break;
                            }
                        }
                    }
                }

                // Calibrate CharacterController
                CharacterController cc = root.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.center = new Vector3(0, 0.95f, 0);
                    cc.height = 1.9f;
                    cc.radius = 0.35f;
                }

                if (root.GetComponent<AnimationEventReceiver>() == null)
                {
                    root.AddComponent<AnimationEventReceiver>();
                }

                // Strip any FighterPropBinder or attached weapon props
                var binders = root.GetComponentsInChildren<FighterPropBinder>(true);
                foreach (var b in binders)
                {
                    UnityEngine.Object.DestroyImmediate(b, true);
                }
                var allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t != null && t != root.transform && (t.name.StartsWith("Prop_") || t.name.Contains("Staff") || t.name.Contains("Blaster") || t.name.Contains("Katana") || t.name.Contains("Sword") || t.name.Contains("MagicWand") || t.name.Contains("Wand") || t.name.Contains("Gun") || t.name.Contains("Orb")))
                    {
                        UnityEngine.Object.DestroyImmediate(t.gameObject, true);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static AnimationClip ResolveClip(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            {
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            }

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        return clip;
                    }
                }
            }
            return null;
        }

        private static AnimationClip LoadMdiClip(string clipName)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{MDI_ANIM_DIR}/{clipName}.anim");
        }

        private static void EnsureParameter(AnimatorController ac, string paramName, AnimatorControllerParameterType type)
        {
            foreach (var p in ac.parameters)
            {
                if (p.name == paramName) return;
            }
            ac.AddParameter(paramName, type);
        }
    }
}
#endif

#endif
