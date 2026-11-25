using System;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Direct Mecanim animation driver.
    /// Operates directly on Unity's native Animator component with standardized MDI_* states and parameters.
    /// Zero mid-match allocations; completely stripped of PlayableGraph / AnimationPlayableOutput.
    /// </summary>
    [ExecuteAlways]
    public class BBAnimationDriver : MonoBehaviour
    {
        [Header("Components")]
        public Animator animator;
        public BBFighterBrain brain;

        private RuntimeAnimatorController _originalController;
        private BBWeaponRig _weaponRig;
        private AnimationClip _activeAttackClip;
        private Vector3 _baseModelLocalPos = Vector3.zero;
        private bool _isVibrating = false;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (brain == null) brain = GetComponent<BBFighterBrain>();
            if (_weaponRig == null) _weaponRig = GetComponent<BBWeaponRig>();
            if (animator != null)
            {
                _baseModelLocalPos = animator.transform.localPosition;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private void OnEnable()
        {
            InitializeDriver();
        }

        private void OnDisable()
        {
            TeardownDriver();
        }

        public void InitializeGraph() => InitializeDriver();
        public void TeardownGraph() => TeardownDriver();

        public void InitializeDriver()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null) return;

            if (_originalController == null)
            {
                _originalController = animator.runtimeAnimatorController;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (brain != null && brain.profile != null && brain.profile.blazBlueOverrideController != null)
            {
                animator.runtimeAnimatorController = brain.profile.blazBlueOverrideController;
            }

            if (_baseModelLocalPos == Vector3.zero)
            {
                _baseModelLocalPos = animator.transform.localPosition;
            }
        }

        public void TeardownDriver()
        {
            if (animator != null && _originalController != null)
            {
                animator.runtimeAnimatorController = _originalController;
            }
        }

        /// <summary>
        /// Plays an attack clip directly via Animator.
        /// </summary>
        public void PlayAttackClip(AnimationClip clip, int firstFrame, int lastFrame, int startupTicks, int totalTicks, int tickInMove, int measuredImpactFrame)
        {
            if (animator == null || clip == null) return;
            _activeAttackClip = clip;

            if (brain != null && brain.currentAttack != null)
            {
                PlayAttack(brain.currentAttack);
            }
            else
            {
                string stateName = "MDI_" + clip.name;
                if (HasState(stateName))
                {
                    animator.CrossFadeInFixedTime(stateName, 0.05f);
                }
            }
        }

        public void PlayAttackClip(AnimationClip clip, int firstFrame, int lastFrame, int totalTicks, int tickInMove)
        {
            int defaultStartup = Mathf.Max(1, Mathf.RoundToInt(totalTicks * 0.35f));
            PlayAttackClip(clip, firstFrame, lastFrame, defaultStartup, totalTicks, tickInMove, -1);
        }

        public void PlayAttack(BBAttackData attack)
        {
            if (attack == null || animator == null) return;
            _activeAttackClip = attack.clip != null ? attack.clip : attack.animationClip;

            string mdiState = "MDI_" + attack.moveId;
            if (HasState(mdiState))
            {
                animator.CrossFadeInFixedTime(mdiState, 0.05f);
            }
            else if (_activeAttackClip != null && HasState("MDI_" + _activeAttackClip.name))
            {
                animator.CrossFadeInFixedTime("MDI_" + _activeAttackClip.name, 0.05f);
            }
            else if (HasState("Attack1"))
            {
                animator.CrossFadeInFixedTime("Attack1", 0.05f);
            }
        }

        /// <summary>
        /// Smoothly blends back to neutral locomotion/idle when an attack finishes.
        /// </summary>
        public void BlendToNeutral()
        {
            if (animator == null) return;
            if (HasState("MDI_Idle"))
            {
                animator.CrossFadeInFixedTime("MDI_Idle", 0.1f);
            }
            else if (HasState("Idle"))
            {
                animator.CrossFadeInFixedTime("Idle", 0.1f);
            }
        }

        /// <summary>
        /// Explicitly resets attack state playback.
        /// </summary>
        public void ResetAttackLayer(bool immediate = true)
        {
            _activeAttackClip = null;
            if (_weaponRig != null)
            {
                _weaponRig.SetWeaponTrailsActive(false);
            }
        }

        /// <summary>
        /// Manual tick called by BBMatchSim presentation step.
        /// </summary>
        public void TickAnimation(int tick)
        {
            if (!Application.isPlaying) return;
            if (animator == null || brain == null) return;

            // Pillar 1: Defender hit-vibrate during hitstop (Model horizontal micro-oscillation)
            if (brain.hitstopRemaining > 0 && brain.bbState == BBState.Hitstun)
            {
                float vibrateX = ((tick / 2) % 2 == 0) ? 0.028f : -0.028f;
                animator.transform.localPosition = new Vector3(_baseModelLocalPos.x + vibrateX, _baseModelLocalPos.y, _baseModelLocalPos.z);
                _isVibrating = true;
            }
            else if (_isVibrating)
            {
                animator.transform.localPosition = _baseModelLocalPos;
                _isVibrating = false;
            }

            bool attacking = brain.bbState == BBState.AttackStartup
                || brain.bbState == BBState.AttackActive
                || brain.bbState == BBState.AttackRecovery;

            if (attacking && brain.currentAttack != null)
            {
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

            // Not attacking: cleanly reset attack layer
            bool isInterrupted = brain.bbState == BBState.Hitstun
                || brain.bbState == BBState.Blockstun
                || brain.bbState == BBState.BarrierBlockstun
                || brain.bbState == BBState.DownWait
                || brain.bbState == BBState.Wakeup
                || brain.bbState == BBState.Dead;

            if (isInterrupted)
            {
                ResetAttackLayer(immediate: true);
            }
            else
            {
                _activeAttackClip = null;
                if (_weaponRig != null)
                {
                    _weaponRig.SetWeaponTrailsActive(false);
                }
            }
        }

        public string CurrentClipLabel()
        {
            if (_activeAttackClip != null) return _activeAttackClip.name;
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
        /// Updates locomotion parameters and reaction triggers directly on the Animator.
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
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("HitMid");
                    SetParamBool("IsGrounded", isGrounded);
                    SetParamBool("IsBlocking", false);
                    if (HasState("MDI_HitMid")) animator.CrossFadeInFixedTime("MDI_HitMid", 0.05f);
                    break;

                case BBState.Blockstun:
                case BBState.BarrierBlockstun:
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("StandBlock");
                    SetParamBool("IsBlocking", true);
                    SetParamBool("IsGrounded", isGrounded);
                    if (HasState("MDI_StandBlock")) animator.CrossFadeInFixedTime("MDI_StandBlock", 0.05f);
                    break;

                case BBState.DownWait:
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("Knockdown");
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    if (HasState("MDI_Knockdown")) animator.CrossFadeInFixedTime("MDI_Knockdown", 0.05f);
                    break;

                case BBState.Wakeup:
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("StandUpBack");
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    if (HasState("MDI_StandUpBack")) animator.CrossFadeInFixedTime("MDI_StandUpBack", 0.05f);
                    break;
            }
        }

        private bool HasState(string stateName)
        {
            if (animator == null) return false;
            return animator.HasState(0, Animator.StringToHash(stateName));
        }

        private void SetParamFloat(string name, float val)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(name, val);
            }
        }

        private void SetParamBool(string name, bool val)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(name, val);
            }
        }

        private void SetParamTrigger(string name)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(name);
            }
        }
    }
}
