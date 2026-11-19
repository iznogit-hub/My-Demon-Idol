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

            // Calculate deterministic seek time (spec 7.6)
            float clipSeconds = Mathf.Max(0.01f, (lastFrame - firstFrame) / 24.0f);
            float moveSeconds = Mathf.Max(0.01f, totalTicks / 60.0f);
            float playbackRate = clipSeconds / moveSeconds;
            float seekTime = (tickInMove / 60.0f) * playbackRate;

            activePlayable.SetTime(seekTime);

            // Blend weight towards 1.0 during active attack
            _attackWeight = Mathf.MoveTowards(_attackWeight, 1.0f, 1.0f / BLEND_SPEED_TICKS);
            _mixer.SetInputWeight(1, _attackWeight);
            _mixer.SetInputWeight(0, 1.0f - _attackWeight);

            _graph.Evaluate(0f);
        }

        public void PlayAttack(BBAttackData attack)
        {
            if (attack == null) return;
            if (attack.clip != null)
            {
                PlayAttackClip(attack.clip, attack.clipFirstFrame, attack.clipLastFrame, attack.TotalTicks, 0);
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
                _graph.Evaluate(0f);
            }
        }

        /// <summary>
        /// Manual tick called by BBMatchSim presentation step.
        /// </summary>
        public void TickAnimation(int tick)
        {
            if (!_isGraphValid || brain == null) return;

            if (brain.currentAttack != null && brain.currentAttack.clip != null)
            {
                PlayAttackClip(
                    brain.currentAttack.clip,
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
            }
            else
            {
                BlendToNeutral();
                if (_weaponRig != null)
                {
                    _weaponRig.SetWeaponTrailsActive(false);
                }
            }
        }
    }
}
