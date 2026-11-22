using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Deterministic animation playback engine using Unity's Playables API.
    /// Blends Layer 0 (locomotion / hit reactions on AnimatorController) with
    /// Layer 1 (deterministic attack clips via AnimationClipPlayable.SetTime).
    /// Zero mid-match allocations; does not touch Classic controllers.
    /// </summary>
    [ExecuteAlways]
    public class BBAnimationDriver : MonoBehaviour
    {
        [Header("Components")]
        public Animator animator;
        public BBFighterBrain brain;

        private PlayableGraph _graph;
        private AnimationLayerMixerPlayable _mixer;
        private AnimatorControllerPlayable _controllerPlayable;
        private AnimationClipPlayable _attackPlayableA;
        private AnimationClipPlayable _attackPlayableB;
        private bool _usingPlayableA = true;

        private RuntimeAnimatorController _originalController;
        private bool _isGraphValid = false;
        private BBWeaponRig _weaponRig;

        private float _attackWeight = 0f;
        private const float BLEND_SPEED_TICKS = 3f;
        private AnimationClip _activeAttackClip;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (brain == null) brain = GetComponent<BBFighterBrain>();
            if (_weaponRig == null) _weaponRig = GetComponent<BBWeaponRig>();
        }

        private void OnEnable()
        {
            InitializeGraph();
        }

        private void OnDisable()
        {
            TeardownGraph();
        }

        public void InitializeGraph()
        {
            if (_isGraphValid || animator == null) return;

            _originalController = animator.runtimeAnimatorController;

            // In BlazBlue mode, switch to the fighter's BlazBlue override controller if assigned
            RuntimeAnimatorController targetController = _originalController;
            if (brain != null && brain.profile != null && brain.profile.blazBlueOverrideController != null)
            {
                targetController = brain.profile.blazBlueOverrideController;
                animator.runtimeAnimatorController = targetController;
            }

            _graph = PlayableGraph.Create($"BBAnim_{gameObject.name}");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            var animOutput = AnimationPlayableOutput.Create(_graph, "BBAnimationOutput", animator);
            _mixer = AnimationLayerMixerPlayable.Create(_graph, 2);

            // Layer 0: Base Controller Playable
            if (targetController != null)
            {
                _controllerPlayable = AnimatorControllerPlayable.Create(_graph, targetController);
                _graph.Connect(_controllerPlayable, 0, _mixer, 0);
                _mixer.SetInputWeight(0, 1.0f);
            }

            animOutput.SetSourcePlayable(_mixer);
            _graph.Play();
            _isGraphValid = true;
        }

        public void TeardownGraph()
        {
            if (!_isGraphValid) return;

            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
            _isGraphValid = false;

            // Restore original Classic controller
            if (animator != null && _originalController != null)
            {
                animator.runtimeAnimatorController = _originalController;
            }
        }

        /// <summary>
        /// Plays an attack clip deterministically seeked to the exact frame time.
        /// </summary>
        public void PlayAttackClip(AnimationClip clip, int firstFrame, int lastFrame, int totalTicks, int tickInMove)
        {
            if (!_isGraphValid || clip == null) return;
            _activeAttackClip = clip;

            // Ping-pong playables if clip changed
            ref AnimationClipPlayable activePlayable = ref (_usingPlayableA ? ref _attackPlayableA : ref _attackPlayableB);

            if (!activePlayable.IsValid() || activePlayable.GetAnimationClip() != clip)
            {
                if (activePlayable.IsValid())
                {
                    _graph.Disconnect(_mixer, 1);
                    _graph.DestroyPlayable(activePlayable);
                }

                activePlayable = AnimationClipPlayable.Create(_graph, clip);
                activePlayable.SetApplyFootIK(false);
                _graph.Connect(activePlayable, 0, _mixer, 1);
            }

            // Calculate deterministic seek time using real clip frameRate and scaled playback speed
            float fps = clip.frameRate > 0f ? clip.frameRate : 30f;
            float startSec = firstFrame > 0 ? (firstFrame / fps) : 0f;
            float clipDuration = (lastFrame > firstFrame && lastFrame > 0) ? ((lastFrame - firstFrame) / fps) : (clip.length - startSec);
            float targetDuration = totalTicks > 0 ? (totalTicks / 60.0f) : clipDuration;
            float speed = (clipDuration > 0f && targetDuration > 0f) ? Mathf.Clamp(clipDuration / targetDuration, 0.85f, 1.15f) : 1.0f;
            float seekTime = startSec + ((tickInMove / 60.0f) * speed);
            seekTime = Mathf.Clamp(seekTime, 0f, clip.length);

            activePlayable.SetTime(seekTime);

            // Blend weight towards 1.0 during active attack
            _attackWeight = Mathf.MoveTowards(_attackWeight, 1.0f, 1.0f / BLEND_SPEED_TICKS);
            _mixer.SetInputWeight(1, _attackWeight);
            _mixer.SetInputWeight(0, 1.0f - _attackWeight);

            _graph.Evaluate(CombatTickEngine.TICK_DELTA_TIME);
        }

        public void PlayAttack(BBAttackData attack)
        {
            if (attack == null) return;
            var clipToPlay = attack.clip != null ? attack.clip : attack.animationClip;
            if (clipToPlay != null)
            {
                PlayAttackClip(clipToPlay, attack.clipFirstFrame, attack.clipLastFrame, attack.TotalTicks, 0);
            }
        }

        /// <summary>
        /// Smoothly blends back to Layer 0 base locomotion/idle when an attack finishes.
        /// </summary>
        public void BlendToNeutral()
        {
            if (!_isGraphValid) return;

            if (_attackWeight > 0f)
            {
                _attackWeight = Mathf.MoveTowards(_attackWeight, 0f, 1.0f / BLEND_SPEED_TICKS);
                _mixer.SetInputWeight(1, _attackWeight);
                _mixer.SetInputWeight(0, 1.0f - _attackWeight);
                _graph.Evaluate(CombatTickEngine.TICK_DELTA_TIME);
            }
        }

        /// <summary>
        /// Manual tick called by BBMatchSim presentation step.
        /// </summary>
        public void TickAnimation(int tick)
        {
            if (!_isGraphValid) InitializeGraph();
            if (!_isGraphValid || brain == null) return;

            bool attacking = brain.bbState == BBState.AttackStartup
                || brain.bbState == BBState.AttackActive
                || brain.bbState == BBState.AttackRecovery;
            AnimationClip attackClip = null;
            if (attacking && brain.currentAttack != null)
            {
                attackClip = brain.currentAttack.clip != null ? brain.currentAttack.clip : brain.currentAttack.animationClip;
                if (attackClip == null) attackClip = _activeAttackClip;
            }

            if (attacking && attackClip != null && brain.currentAttack != null)
            {
                PlayAttackClip(
                    attackClip,
                    brain.currentAttack.clipFirstFrame,
                    brain.currentAttack.clipLastFrame,
                    brain.currentAttack.TotalTicks,
                    brain.attackTick
                );

                if (_weaponRig == null) _weaponRig = GetComponent<BBWeaponRig>();
                if (_weaponRig != null)
                {
                    bool isActiveWindow = brain.attackTick >= brain.currentAttack.startupFrames &&
                                          brain.attackTick < (brain.currentAttack.startupFrames + brain.currentAttack.activeFrames);
                    _weaponRig.SetWeaponTrailsActive(isActiveWindow);

                    if (brain.attackTick == Mathf.Max(0, brain.currentAttack.startupFrames - 1))
                    {
                        if (BBAudioBridge.Instance != null)
                        {
                            BBAudioBridge.Instance.PlayWeaponSwing(_weaponRig.warriorType, transform.position);
                        }
                    }
                }
                return;
            }

            _activeAttackClip = null;
            if (_attackWeight > 0f)
            {
                _attackWeight = Mathf.MoveTowards(_attackWeight, 0f, 1.0f / BLEND_SPEED_TICKS);
                if (_mixer.IsValid())
                {
                    _mixer.SetInputWeight(1, _attackWeight);
                    _mixer.SetInputWeight(0, 1.0f - _attackWeight);
                }
            }
            if (_weaponRig != null)
                _weaponRig.SetWeaponTrailsActive(false);

            // The graph is manual. Without this, walk/crouch/jump parameters never advance and the pose stays idle.
            if (_graph.IsValid())
                _graph.Evaluate(CombatTickEngine.TICK_DELTA_TIME);
        }

        public string CurrentClipLabel()
        {
            if (_attackWeight > 0.05f && _activeAttackClip != null)
                return _activeAttackClip.name;
            if (animator != null && animator.GetCurrentAnimatorClipInfoCount(0) > 0)
            {
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0 && clips[0].clip != null)
                    return clips[0].clip.name;
            }
            return brain != null ? brain.bbState.ToString() : "none";
        }

        public Quaternion[] CaptureBoneRotations(out Transform[] bones)
        {
            if (animator == null)
            {
                bones = System.Array.Empty<Transform>();
                return System.Array.Empty<Quaternion>();
            }
            var bList = new System.Collections.Generic.List<Transform>();
            var rList = new System.Collections.Generic.List<Quaternion>();
            if (animator.isHuman)
            {
                HumanBodyBones[] testBones = new HumanBodyBones[]
                {
                    HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                    HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                    HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftUpperLeg,
                    HumanBodyBones.Spine
                };
                foreach (var b in testBones)
                {
                    var t = animator.GetBoneTransform(b);
                    if (t != null)
                    {
                        bList.Add(t);
                        rList.Add(t.localRotation);
                    }
                }
            }
            else
            {
                var allTransforms = animator.GetComponentsInChildren<Transform>();
                int sampleCount = Mathf.Min(8, allTransforms.Length);
                for (int i = 1; i < sampleCount; i++)
                {
                    bList.Add(allTransforms[i]);
                    rList.Add(allTransforms[i].localRotation);
                }
            }
            bones = bList.ToArray();
            return rList.ToArray();
        }

        public static bool IsPoseEqualToBind(Transform[] bones, Quaternion[] bindRotations, float thresholdAngle = 0.8f)
        {
            if (bones == null || bindRotations == null || bones.Length == 0 || bones.Length != bindRotations.Length) return false;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    float angle = Quaternion.Angle(bones[i].localRotation, bindRotations[i]);
                    if (angle > thresholdAngle) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Updates Layer 0 locomotion parameters (Speed, MoveX, IsGrounded, IsCrouch, IsBlocking, Jump, Dash)
        /// on both AnimatorControllerPlayable and the underlying Animator.
        /// </summary>
        public void UpdateLocomotionState(BBState state, sbyte facingNumpad, bool isGrounded)
        {
            if (animator == null) return;

            switch (state)
            {
                case BBState.WalkForward:
                    SetParamFloat("MoveX", 1f);
                    SetParamFloat("Speed", 1f);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    break;

                case BBState.WalkBackward:
                    SetParamFloat("MoveX", -1f);
                    SetParamFloat("Speed", 1f);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", true);
                    break;

                case BBState.Crouching:
                    SetParamFloat("MoveX", 0f);
                    SetParamFloat("Speed", 0f);
                    SetParamBool("IsCrouch", true);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", facingNumpad == 1 || facingNumpad == 4);
                    break;

                case BBState.Neutral:
                    SetParamFloat("MoveX", 0f);
                    SetParamFloat("Speed", 0f);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", facingNumpad == 4);
                    break;

                case BBState.ForwardDash:
                    SetParamTrigger("ForwardDash");
                    SetParamFloat("Speed", 2f);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    break;

                case BBState.Backdash:
                    SetParamTrigger("Backdash");
                    SetParamFloat("Speed", 2f);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    break;

                case BBState.JumpStartup:
                case BBState.Airborne:
                    SetParamTrigger("Jump");
                    SetParamBool("IsGrounded", false);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsBlocking", false);
                    break;

                case BBState.Hitstun:
                    SetParamTrigger("HitMid");
                    SetParamBool("IsGrounded", isGrounded);
                    SetParamBool("IsBlocking", false);
                    break;

                case BBState.Blockstun:
                case BBState.BarrierBlockstun:
                    SetParamTrigger("StandBlock");
                    SetParamBool("IsBlocking", true);
                    SetParamBool("IsGrounded", isGrounded);
                    break;

                case BBState.DownWait:
                    SetParamTrigger("Knockdown");
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    break;

                case BBState.Wakeup:
                    SetParamTrigger("StandUpBack");
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    break;
            }
        }

        private void SetParamFloat(string name, float val)
        {
            if (_controllerPlayable.IsValid()) _controllerPlayable.SetFloat(name, val);
            if (animator != null) animator.SetFloat(name, val);
        }

        private void SetParamBool(string name, bool val)
        {
            if (_controllerPlayable.IsValid()) _controllerPlayable.SetBool(name, val);
            if (animator != null) animator.SetBool(name, val);
        }

        private void SetParamTrigger(string name)
        {
            if (_controllerPlayable.IsValid()) _controllerPlayable.SetTrigger(name);
            if (animator != null) animator.SetTrigger(name);
        }
    }
}
