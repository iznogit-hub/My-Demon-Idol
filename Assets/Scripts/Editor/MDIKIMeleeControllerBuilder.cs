#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DemonIdol.Characters.Editor
{
    /// <summary>
    /// Reapplies the shared Kevin Iglesias melee controller to the 11 girl fighters.
    /// The committed YAML at MDI_KI_Melee_Fighter.controller is the primary asset.
    /// This menu rebuilds that controller only when the asset is missing, then
    /// reassigns prefabs. It does not delete existing files.
    /// </summary>
    public static class MDIKIMeleeControllerBuilder
    {
        const string ControllerPath = "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller";
        const string Female = "Assets/Kevin Iglesias/Human Animations/Animations/Female/";

        static readonly string[] Roster =
        {
            "C01_Akio", "C02_Aika", "C04_UnityChan", "C05_Akane", "C06_Cultist",
            "C07_StellarWitch", "C08_SanpakuChan", "C09_PicoChan", "C10_Reika",
            "C12_Fina", "C14_Muryotaisu"
        };

        [MenuItem("Demon Idol/Animation/Rebuild KI Melee Controller")]
        public static void Rebuild()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = BuildController();
                Debug.Log("Created " + ControllerPath);
            }
            else
            {
                Debug.Log("Using the authored controller at " + ControllerPath + ". Prefab assignments were refreshed.");
            }

            int assigned = 0;
            foreach (var name in Roster)
            {
                string path = "Assets/Prefabs/Characters/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    Debug.LogWarning("Missing prefab " + path);
                    continue;
                }
                var animators = root.GetComponentsInChildren<Animator>(true);
                var rootAnimator = root.GetComponent<Animator>();
                foreach (var animator in animators)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    if (rootAnimator != null && animator != rootAnimator)
                        animator.enabled = false;
                }
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    string typeName = behaviour.GetType().Name;
                    if (typeName == "PlayerController" || typeName == "MuryotaisuController")
                        behaviour.enabled = false;
                }
                var body = root.GetComponent<Rigidbody>();
                if (body != null && root.name.Contains("Reika"))
                {
                    body.isKinematic = true;
                    body.useGravity = false;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                assigned++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("KI melee controller assigned on " + assigned + " roster prefabs. Root motion off.");
        }

        static AnimatorController BuildController()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath) ?? "Assets");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCrouch", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.AddState("Locomotion");
            var tree = new BlendTree
            {
                name = "WalkBlend",
                blendParameter = "MoveX",
                blendType = BlendTreeType.Simple1D,
                useAutomaticThresholds = false
            };
            tree.AddChild(Clip(Female + "Movement/Run/HumanF@Run01_Backward.fbx"), -1f);
            tree.AddChild(Clip(Female + "Idles/HumanF@Idle01.fbx"), 0f);
            tree.AddChild(Clip(Female + "Movement/Run/HumanF@Run01_Forward.fbx"), 1f);
            AssetDatabase.AddObjectToAsset(tree, controller);
            locomotion.motion = tree;
            machine.defaultState = locomotion;

            AddTrigger(controller, machine, locomotion, "LightAttack", Female + "Combat/1H/HumanF@Attack1H01_R.fbx", 4.667f);
            AddTrigger(controller, machine, locomotion, "HeavyAttack", Female + "Combat/2H/HumanF@Attack2H01.fbx", 2.824f);
            AddTrigger(controller, machine, locomotion, "DriveSkill", Female + "Combat/Polearm/HumanF@AttackPolearm01.fbx", 2.103f);
            AddTrigger(controller, machine, locomotion, "DomainSuper", Female + "Combat/Spellcasting/MagicAttacks/Directional/HumanF@MagicAttackDirect2H01.fbx", 2f);
            AddTrigger(controller, machine, locomotion, "Defeat", Female + "Combat/HumanF@Death01.fbx", 1f, false);
            AddTrigger(controller, machine, locomotion, "Victory", Female + "Idles/HumanF@Idle03.fbx", 1f, false);
            AddTrigger(controller, machine, locomotion, "Knockdown", Female + "Combat/HumanF@Death01.fbx", 1.222f, false);
            AddTrigger(controller, machine, locomotion, "StandUpBack", Female + "Combat/Spellcasting/HumanF@CastingExit01.fbx", 5.143f, true);

            var idleBack = machine.AddAnyStateTransition(locomotion);
            idleBack.AddCondition(AnimatorConditionMode.If, 0, "Idle");
            idleBack.hasExitTime = false;
            idleBack.duration = 0.05f;
            controller.AddParameter("Idle", AnimatorControllerParameterType.Trigger);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void AddTrigger(AnimatorController controller, AnimatorStateMachine machine, AnimatorState back, string trigger, string clipPath, float speed, bool exit = true)
        {
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
            var state = machine.AddState(trigger);
            state.motion = Clip(clipPath);
            state.speed = speed;
            var enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0, trigger);
            enter.hasExitTime = false;
            enter.duration = 0.04f;
            enter.canTransitionToSelf = true;
            if (exit)
            {
                var leave = state.AddTransition(back);
                leave.hasExitTime = true;
                leave.exitTime = 0.92f;
                leave.duration = 0.06f;
                leave.hasFixedDuration = true;
            }
        }

        static AnimationClip Clip(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                    return clip;
            }
            Debug.LogWarning("Missing KI clip at " + path);
            return null;
        }
    }
}
#endif
