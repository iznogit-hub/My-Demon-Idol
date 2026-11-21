#if MDI_ARCHIVE
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using DemonIdol.Characters;

namespace DemonIdol.Characters.Editor
{
    public static class MDI_AnimationTemplateBuilder
    {
        public const string MASTER_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";
        public const string ANIM_DIR = "Assets/MDI_Animations";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/MDI/Build Animation Template & Overrides", priority = 2)]
        public static void BuildMasterControllerAndOverrides()
        {
            Debug.Log("<color=#00e676>[MDI Animation]</color> Building master 14-state Humanoid combat animator template...");

            // 1. Create or load master AnimatorController
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MASTER_CONTROLLER_PATH);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(MASTER_CONTROLLER_PATH);
            }

            // Parameters - Standard Template
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsBlocking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Knockdown", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "AttackIndex", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "ActionState", AnimatorControllerParameterType.Int);

            // Parameters - Combat Engine Direct Triggers for instant zero-latency response
            EnsureParameter(controller, "LightAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "MediumAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "HeavyAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "DriveSkill", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "DomainSuper", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "StandBlock", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "CrouchBlock", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "HitMid", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "HitLow", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "ForwardDash", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Backdash", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Defeat", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "Victory", AnimatorControllerParameterType.Trigger);

            // Load clips
            AnimationClip clipIdle = LoadClip("MDI_Anim_Idle");
            AnimationClip clipWalk = LoadClip("MDI_Anim_Walk");
            AnimationClip clipDash = LoadClip("MDI_Anim_Dash");
            AnimationClip clipBlock = LoadClip("MDI_Anim_Block");
            AnimationClip clipHit = LoadClip("MDI_Anim_Hit");
            AnimationClip clipKnockdown = LoadClip("MDI_Anim_Knockdown");
            AnimationClip clipPunch = LoadClip("MDI_Anim_Punch");
            AnimationClip clipKick = LoadClip("MDI_Anim_Kick");
            AnimationClip clipCombo = LoadClip("MDI_Anim_Combo");
            AnimationClip clipSpecial = LoadClip("MDI_Anim_Special");
            AnimationClip clipSuper = LoadClip("MDI_Anim_Super");
            AnimationClip clipWin = LoadClip("MDI_Anim_Win");
            AnimationClip clipLose = LoadClip("MDI_Anim_Lose");

            var rootStateMachine = controller.layers[0].stateMachine;

            // Clear old states
            var existingStates = rootStateMachine.states;
            for (int i = existingStates.Length - 1; i >= 0; i--)
            {
                rootStateMachine.RemoveState(existingStates[i].state);
            }

            // Create states
            var stateIdle = rootStateMachine.AddState("idle", new Vector3(200, 0, 0));
            stateIdle.motion = clipIdle;
            rootStateMachine.defaultState = stateIdle;

            var stateWalk = rootStateMachine.AddState("walk", new Vector3(200, 70, 0));
            stateWalk.motion = clipWalk;

            var stateDash = rootStateMachine.AddState("dash", new Vector3(200, 140, 0));
            stateDash.motion = clipDash;

            var stateBlock = rootStateMachine.AddState("block", new Vector3(450, -70, 0));
            stateBlock.motion = clipBlock;

            var stateHit = rootStateMachine.AddState("hit", new Vector3(450, 0, 0));
            stateHit.motion = clipHit;

            var stateKnockdown = rootStateMachine.AddState("knockdown", new Vector3(450, 70, 0));
            stateKnockdown.motion = clipKnockdown;

            var statePunch = rootStateMachine.AddState("punch", new Vector3(-50, -70, 0));
            statePunch.motion = clipPunch;

            var stateKick = rootStateMachine.AddState("kick", new Vector3(-50, 0, 0));
            stateKick.motion = clipKick;

            var stateCombo = rootStateMachine.AddState("combo", new Vector3(-50, 70, 0));
            stateCombo.motion = clipCombo;

            var stateSpecial = rootStateMachine.AddState("special", new Vector3(-50, 140, 0));
            stateSpecial.motion = clipSpecial;

            var stateSuper = rootStateMachine.AddState("super", new Vector3(-50, 210, 0));
            stateSuper.motion = clipSuper;

            var stateWin = rootStateMachine.AddState("win", new Vector3(200, -100, 0));
            stateWin.motion = clipWin;

            var stateLose = rootStateMachine.AddState("lose", new Vector3(450, -140, 0));
            stateLose.motion = clipLose;

            // Add standard transitions
            var idleToWalk = stateIdle.AddTransition(stateWalk);
            idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            idleToWalk.hasExitTime = false;
            idleToWalk.duration = 0.1f;

            var walkToIdle = stateWalk.AddTransition(stateIdle);
            walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            walkToIdle.hasExitTime = false;
            walkToIdle.duration = 0.1f;

            // Block transitions via IsBlocking bool
            var idleToBlock = stateIdle.AddTransition(stateBlock);
            idleToBlock.AddCondition(AnimatorConditionMode.If, 0, "IsBlocking");
            idleToBlock.hasExitTime = false;
            idleToBlock.duration = 0.05f;

            var blockToIdle = stateBlock.AddTransition(stateIdle);
            blockToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsBlocking");
            blockToIdle.hasExitTime = false;
            blockToIdle.duration = 0.05f;

            // AnyState transitions - Hits & Knockdowns
            AddAnyStateTransition(rootStateMachine, stateHit, "Hit", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateHit, "HitMid", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateHit, "HitLow", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateKnockdown, "Knockdown", 0.05f);

            // AnyState transitions - Direct Combat Triggers
            AddAnyStateTransition(rootStateMachine, statePunch, "LightAttack", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateKick, "MediumAttack", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateCombo, "HeavyAttack", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateSpecial, "DriveSkill", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateSuper, "DomainSuper", 0.05f);

            // AnyState transitions - Generic Attack Trigger with AttackIndex (0=Punch, 1=Kick, 2=Combo, 3=Special, 4=Super)
            AddAnyStateIndexedAttackTransition(rootStateMachine, statePunch, 0, 0.05f);
            AddAnyStateIndexedAttackTransition(rootStateMachine, stateKick, 1, 0.05f);
            AddAnyStateIndexedAttackTransition(rootStateMachine, stateCombo, 2, 0.05f);
            AddAnyStateIndexedAttackTransition(rootStateMachine, stateSpecial, 3, 0.05f);
            AddAnyStateIndexedAttackTransition(rootStateMachine, stateSuper, 4, 0.05f);

            // AnyState transitions - Movement, Blocks & Match Outcomes
            AddAnyStateTransition(rootStateMachine, stateDash, "ForwardDash", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateDash, "Backdash", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateBlock, "StandBlock", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateBlock, "CrouchBlock", 0.05f);
            AddAnyStateTransition(rootStateMachine, stateWin, "Victory", 0.1f);
            AddAnyStateTransition(rootStateMachine, stateLose, "Defeat", 0.1f);

            // Exit transitions back to idle upon animation completion
            AddExitToIdleTransition(stateHit, stateIdle, 0.85f, 0.1f);
            AddExitToIdleTransition(stateKnockdown, stateIdle, 0.90f, 0.15f);
            AddExitToIdleTransition(statePunch, stateIdle, 0.85f, 0.05f);
            AddExitToIdleTransition(stateKick, stateIdle, 0.85f, 0.05f);
            AddExitToIdleTransition(stateCombo, stateIdle, 0.90f, 0.05f);
            AddExitToIdleTransition(stateSpecial, stateIdle, 0.90f, 0.05f);
            AddExitToIdleTransition(stateSuper, stateIdle, 0.95f, 0.05f);
            AddExitToIdleTransition(stateDash, stateIdle, 0.85f, 0.05f);

            EditorUtility.SetDirty(controller);

            // 2. Build 18 AnimatorOverrideControllers (one per fighter)
            string[] fighterDefGuids = AssetDatabase.FindAssets("t:FighterDefinition", new string[] { "Assets/MDI_Characters/Clans" });
            int overrideCount = 0;

            foreach (string guid in fighterDefGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FighterDefinition fDef = AssetDatabase.LoadAssetAtPath<FighterDefinition>(path);
                if (fDef == null) continue;

                string folder = Path.GetDirectoryName(path);
                string overridePath = $"{folder}/AnimatorOverride_{fDef.id}.overrideController";

                AnimatorOverrideController aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
                if (aoc == null)
                {
                    aoc = new AnimatorOverrideController(controller);
                    AssetDatabase.CreateAsset(aoc, overridePath);
                }
                else
                {
                    aoc.runtimeAnimatorController = controller;
                }

                EditorUtility.SetDirty(aoc);

                // Assign to FighterDefinition
                fDef.animatorController = aoc;
                EditorUtility.SetDirty(fDef);
                overrideCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[MDI Animation] Complete:</b> Master controller and {overrideCount} AnimatorOverrideControllers created & bound.</color>");
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == paramName) return;
            }
            controller.AddParameter(paramName, type);
        }

        private static AnimationClip LoadClip(string clipName)
        {
            string path = $"{ANIM_DIR}/{clipName}.anim";
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState targetState, string triggerName, float duration)
        {
            var transition = sm.AddAnyStateTransition(targetState);
            transition.AddCondition(AnimatorConditionMode.If, 0, triggerName);
            transition.duration = duration;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
        }

        private static void AddAnyStateIndexedAttackTransition(AnimatorStateMachine sm, AnimatorState targetState, int attackIndex, float duration)
        {
            var transition = sm.AddAnyStateTransition(targetState);
            transition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            transition.AddCondition(AnimatorConditionMode.Equals, attackIndex, "AttackIndex");
            transition.duration = duration;
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
        }

        private static void AddExitToIdleTransition(AnimatorState fromState, AnimatorState idleState, float exitTime, float duration)
        {
            var transition = fromState.AddTransition(idleState);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.hasFixedDuration = false;
        }
    }
}
#endif

#endif
