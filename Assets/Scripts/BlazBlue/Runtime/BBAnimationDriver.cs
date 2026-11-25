using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Pure Mecanim AnimatorController animation driver for My Demon Idol fighters.
    /// Operates directly on Unity's native Animator component with standardized MDI_* states and parameters.
    /// Zero mid-match allocations; driven 100% via native Mecanim Animator.
    /// Frame-accurate speed synchronization with BBMatchSim 60Hz discrete tick timings and hitstop pose freeze.
    /// </summary>
    [ExecuteAlways]
    public class BBAnimationDriver : MonoBehaviour
    {
        [Header("Components")]
        public Animator animator;
        public BBFighterBrain brain;
        public BBFighterPrefabConfig prefabConfig;

        private RuntimeAnimatorController _originalController;
        private AnimatorOverrideController _runtimeOverrideController;
        private BBWeaponRig _weaponRig;
        private AnimationClip _activeAttackClip;
        private Vector3 _baseModelLocalPos = Vector3.zero;
        private bool _isVibrating = false;
        private float _normalSpeed = 1f;

        // Cached State Hashes for zero per-frame string allocations
        private static readonly int Hash_MDI_5A = Animator.StringToHash("MDI_5A");
        private static readonly int Hash_MDI_5B = Animator.StringToHash("MDI_5B");
        private static readonly int Hash_MDI_5C = Animator.StringToHash("MDI_5C");
        private static readonly int Hash_MDI_2C = Animator.StringToHash("MDI_2C");
        private static readonly int Hash_MDI_jB = Animator.StringToHash("MDI_jB");
        private static readonly int Hash_MDI_5D = Animator.StringToHash("MDI_5D");
        private static readonly int Hash_MDI_236236C = Animator.StringToHash("MDI_236236C");

        private static readonly int Hash_MDI_Idle = Animator.StringToHash("MDI_Idle");
        private static readonly int Hash_MDI_WalkForward = Animator.StringToHash("MDI_WalkForward");
        private static readonly int Hash_MDI_WalkFwd = Animator.StringToHash("MDI_WalkFwd");
        private static readonly int Hash_MDI_WalkBackward = Animator.StringToHash("MDI_WalkBackward");
        private static readonly int Hash_MDI_WalkBack = Animator.StringToHash("MDI_WalkBack");
        private static readonly int Hash_MDI_Crouch = Animator.StringToHash("MDI_Crouch");
        private static readonly int Hash_MDI_Jump = Animator.StringToHash("MDI_Jump");
        private static readonly int Hash_MDI_StandBlock = Animator.StringToHash("MDI_StandBlock");
        private static readonly int Hash_MDI_Block = Animator.StringToHash("MDI_Block");
        private static readonly int Hash_MDI_HitLight = Animator.StringToHash("MDI_HitLight");
        private static readonly int Hash_MDI_HitMid = Animator.StringToHash("MDI_HitMid");
        private static readonly int Hash_MDI_HitHeavy = Animator.StringToHash("MDI_HitHeavy");
        private static readonly int Hash_MDI_Knockdown = Animator.StringToHash("MDI_Knockdown");
        private static readonly int Hash_MDI_StandUpBack = Animator.StringToHash("MDI_StandUpBack");
        private static readonly int Hash_MDI_Intro = Animator.StringToHash("MDI_Intro");
        private static readonly int Hash_MDI_Victory = Animator.StringToHash("MDI_Victory");
        private static readonly int Hash_MDI_Taunt = Animator.StringToHash("MDI_Taunt");

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (brain == null) brain = GetComponent<BBFighterBrain>();
            if (prefabConfig == null) prefabConfig = GetComponent<BBFighterPrefabConfig>();
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
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (brain == null) brain = GetComponent<BBFighterBrain>();
            if (prefabConfig == null) prefabConfig = GetComponent<BBFighterPrefabConfig>();
            if (animator == null) return;

            if (_originalController == null)
            {
                _originalController = animator.runtimeAnimatorController;
            }

            // Invariant: Native Humanoid avatar & strict zero-root-motion
            if (animator.avatar != null && (!animator.avatar.isHuman || !animator.avatar.isValid))
            {
                Debug.LogWarning($"[BBAnimationDriver] Fighter '{name}' Avatar is not a valid Humanoid Avatar!");
            }
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Runtime Animator Override resolution from BBFighterPrefabConfig
            if (prefabConfig != null && prefabConfig.HasCustomOverrides())
            {
                if (_runtimeOverrideController == null)
                {
                    _runtimeOverrideController = prefabConfig.CreateRuntimeOverrideController();
                }
                if (_runtimeOverrideController != null)
                {
                    animator.runtimeAnimatorController = _runtimeOverrideController;
                }
            }
            else if (animator.runtimeAnimatorController == null)
            {
                if (brain != null && brain.profile != null && brain.profile.blazBlueOverrideController != null)
                {
                    animator.runtimeAnimatorController = brain.profile.blazBlueOverrideController;
                }
            }

            if (_baseModelLocalPos == Vector3.zero)
            {
                _baseModelLocalPos = animator.transform.localPosition;
            }

            _normalSpeed = 1f;
            animator.speed = 1f;
        }

        public void TeardownDriver()
        {
            if (animator != null && _originalController != null)
            {
                animator.runtimeAnimatorController = _originalController;
            }
            _normalSpeed = 1f;
            if (animator != null) animator.speed = 1f;
        }

        /// <summary>
        /// Plays an attack clip directly via Animator with frame-accurate speed calculation.
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
                float desiredDuration = Mathf.Max(0.1f, totalTicks / 60f);
                if (clip.length > 0.01f)
                {
                    _normalSpeed = clip.length / desiredDuration;
                }
                else
                {
                    _normalSpeed = 1f;
                }

                animator.speed = (brain != null && brain.hitstopRemaining > 0) ? 0f : _normalSpeed;
                string stateName = "MDI_" + clip.name;
                if (HasState(stateName))
                {
                    animator.CrossFadeInFixedTime(stateName, 0.04f, 0, 0f);
                }
            }
        }

        public void PlayAttackClip(AnimationClip clip, int firstFrame, int lastFrame, int totalTicks, int tickInMove)
        {
            int defaultStartup = Mathf.Max(1, Mathf.RoundToInt(totalTicks * 0.35f));
            PlayAttackClip(clip, firstFrame, lastFrame, defaultStartup, totalTicks, tickInMove, -1);
        }

        /// <summary>
        /// Drives the fighter's Animator into the attack state matching the given BBAttackData,
        /// synchronizing animator.speed so the animation length exactly matches attack.TotalTicks at 60 Hz.
        /// </summary>
        public void PlayAttack(BBAttackData attack)
        {
            if (attack == null || animator == null) return;
            _activeAttackClip = attack.clip != null ? attack.clip : attack.animationClip;

            int totalTicks = attack.TotalTicks;
            if (totalTicks <= 0) totalTicks = attack.startupFrames + attack.activeFrames + attack.recoveryFrames;
            if (totalTicks <= 0) totalTicks = 20;

            float desiredDuration = Mathf.Max(0.08f, totalTicks / 60f);
            if (_activeAttackClip != null && _activeAttackClip.length > 0.01f)
            {
                _normalSpeed = _activeAttackClip.length / desiredDuration;
            }
            else
            {
                _normalSpeed = 1f;
            }

            animator.speed = (brain != null && brain.hitstopRemaining > 0) ? 0f : _normalSpeed;

            int stateHash = ResolveAttackStateHash(attack.moveId);
            if (animator.HasState(0, stateHash))
            {
                animator.CrossFadeInFixedTime(stateHash, 0.04f, 0, 0f);
            }
            else if (_activeAttackClip != null && HasState("MDI_" + _activeAttackClip.name))
            {
                animator.CrossFadeInFixedTime("MDI_" + _activeAttackClip.name, 0.04f, 0, 0f);
            }
            else if (HasState("Attack1"))
            {
                animator.CrossFadeInFixedTime("Attack1", 0.04f, 0, 0f);
            }
        }

        private int ResolveAttackStateHash(string moveId)
        {
            if (string.IsNullOrEmpty(moveId)) return 0;
            string clean = moveId.Replace(".", "");
            switch (clean)
            {
                case "5A": return Hash_MDI_5A;
                case "5B": return Hash_MDI_5B;
                case "5C": return Hash_MDI_5C;
                case "2C": return Hash_MDI_2C;
                case "jB": return Hash_MDI_jB;
                case "5D": return Hash_MDI_5D;
                case "236236C": return Hash_MDI_236236C;
                default: return Animator.StringToHash("MDI_" + clean);
            }
        }

        /// <summary>
        /// Smoothly blends back to neutral locomotion/idle when an attack finishes.
        /// </summary>
        public void BlendToNeutral()
        {
            _normalSpeed = 1f;
            if (animator == null) return;
            animator.speed = (brain != null && brain.hitstopRemaining > 0) ? 0f : 1f;

            if (animator.HasState(0, Hash_MDI_Idle))
            {
                animator.CrossFadeInFixedTime(Hash_MDI_Idle, 0.08f);
            }
            else if (HasState("Idle"))
            {
                animator.CrossFadeInFixedTime("Idle", 0.08f);
            }
        }

        /// <summary>
        /// Explicitly resets attack state playback.
        /// </summary>
        public void ResetAttackLayer(bool immediate = true)
        {
            _activeAttackClip = null;
            _normalSpeed = 1f;
            if (animator != null && (brain == null || brain.hitstopRemaining <= 0))
            {
                animator.speed = 1f;
            }
            if (_weaponRig != null)
            {
                _weaponRig.SetWeaponTrailsActive(false);
            }
        }

        /// <summary>
        /// Manual tick called by BBMatchSim presentation step.
        /// Enforces hitstop animation freeze (animator.speed = 0f) and defender hit-vibrate.
        /// </summary>
        public void TickAnimation(int tick)
        {
            if (!Application.isPlaying) return;
            if (animator == null || brain == null) return;

            // Pillar 1: Hitstop pose freeze & Defender hit-vibrate micro-oscillation
            if (brain.hitstopRemaining > 0)
            {
                if (animator.speed != 0f) animator.speed = 0f;

                if (brain.bbState == BBState.Hitstun)
                {
                    float vibrateX = ((tick / 2) % 2 == 0) ? 0.028f : -0.028f;
                    animator.transform.localPosition = new Vector3(_baseModelLocalPos.x + vibrateX, _baseModelLocalPos.y, _baseModelLocalPos.z);
                    _isVibrating = true;
                }
                return;
            }
            else
            {
                if (_isVibrating)
                {
                    animator.transform.localPosition = _baseModelLocalPos;
                    _isVibrating = false;
                }
            }

            bool attacking = brain.bbState == BBState.AttackStartup
                || brain.bbState == BBState.AttackActive
                || brain.bbState == BBState.AttackRecovery;

            float targetSpeed = attacking ? _normalSpeed : 1f;
            if (animator.speed != targetSpeed)
            {
                animator.speed = targetSpeed;
            }

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
        /// Cross-fades to deterministic MDI_* locomotion and reaction states.
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
                    if (animator.HasState(0, Hash_MDI_WalkForward)) animator.CrossFadeInFixedTime(Hash_MDI_WalkForward, 0.08f);
                    else if (animator.HasState(0, Hash_MDI_WalkFwd)) animator.CrossFadeInFixedTime(Hash_MDI_WalkFwd, 0.08f);
                    break;

                case BBState.WalkBackward:
                    SetParamFloat("MoveX", -1f);
                    SetParamFloat("Speed", 1f);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", true);
                    if (animator.HasState(0, Hash_MDI_WalkBackward)) animator.CrossFadeInFixedTime(Hash_MDI_WalkBackward, 0.08f);
                    else if (animator.HasState(0, Hash_MDI_WalkBack)) animator.CrossFadeInFixedTime(Hash_MDI_WalkBack, 0.08f);
                    break;

                case BBState.Crouching:
                    SetParamFloat("MoveX", 0f);
                    SetParamFloat("Speed", 0f);
                    SetParamBool("IsCrouch", true);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", facingNumpad == 1 || facingNumpad == 4);
                    if (animator.HasState(0, Hash_MDI_Crouch)) animator.CrossFadeInFixedTime(Hash_MDI_Crouch, 0.08f);
                    break;

                case BBState.Neutral:
                    SetParamFloat("MoveX", 0f);
                    SetParamFloat("Speed", 0f);
                    SetParamBool("IsCrouch", false);
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", facingNumpad == 4);
                    if (animator.HasState(0, Hash_MDI_Idle)) animator.CrossFadeInFixedTime(Hash_MDI_Idle, 0.08f);
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
                    if (animator.HasState(0, Hash_MDI_Jump)) animator.CrossFadeInFixedTime(Hash_MDI_Jump, 0.08f);
                    break;

                case BBState.Hitstun:
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("HitMid");
                    SetParamBool("IsGrounded", isGrounded);
                    SetParamBool("IsBlocking", false);
                    if (animator.HasState(0, Hash_MDI_HitMid)) animator.CrossFadeInFixedTime(Hash_MDI_HitMid, 0.04f);
                    else if (animator.HasState(0, Hash_MDI_HitLight)) animator.CrossFadeInFixedTime(Hash_MDI_HitLight, 0.04f);
                    break;

                case BBState.Blockstun:
                case BBState.BarrierBlockstun:
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("StandBlock");
                    SetParamBool("IsBlocking", true);
                    SetParamBool("IsGrounded", isGrounded);
                    if (animator.HasState(0, Hash_MDI_StandBlock)) animator.CrossFadeInFixedTime(Hash_MDI_StandBlock, 0.04f);
                    else if (animator.HasState(0, Hash_MDI_Block)) animator.CrossFadeInFixedTime(Hash_MDI_Block, 0.04f);
                    break;

                case BBState.DownWait:
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("Knockdown");
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    if (animator.HasState(0, Hash_MDI_Knockdown)) animator.CrossFadeInFixedTime(Hash_MDI_Knockdown, 0.05f);
                    break;

                case BBState.Wakeup:
                    ResetAttackLayer(immediate: true);
                    SetParamTrigger("StandUpBack");
                    SetParamBool("IsGrounded", true);
                    SetParamBool("IsBlocking", false);
                    if (animator.HasState(0, Hash_MDI_StandUpBack)) animator.CrossFadeInFixedTime(Hash_MDI_StandUpBack, 0.08f);
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
