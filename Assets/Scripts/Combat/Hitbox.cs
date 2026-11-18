using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        public FighterController Owner { get; private set; }
        public AttackData CurrentAttack { get; private set; }

        private Collider _collider;
        private readonly HashSet<FighterController> _hitTargets = new HashSet<FighterController>();
        private Vector3 _savedLocalPos;
        private Quaternion _savedLocalRot;
        private bool _posed;
        private bool _follow;
        private HumanBodyBones _followBone;
        private Vector3 _followOffset;
        private Transform _followRoot;
        private Animator _followAnimator;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
            _savedLocalPos = transform.localPosition;
            _savedLocalRot = transform.localRotation;
        }

        public void Initialize(FighterController owner)
        {
            Owner = owner;
        }

        public void Activate(AttackData attack)
        {
            CurrentAttack = attack;
            _hitTargets.Clear();
            _collider.enabled = true;
        }

        public void Deactivate()
        {
            _collider.enabled = false;
            CurrentAttack = null;
            _hitTargets.Clear();
            _follow = false;
            if (_posed)
            {
                transform.localPosition = _savedLocalPos;
                transform.localRotation = _savedLocalRot;
                _posed = false;
            }
        }

        /// <summary>
        /// Applies a move's offset, size, and optional bone follow. No-ops unless the attack opted in,
        /// so every other fighter's hitbox stays where the prefab put it.
        /// </summary>
        public void ApplyAuthored(AttackData attack, int tick, FighterController owner)
        {
            if (attack == null || !attack.akioApplyHitbox || _collider == null) return;
            Vector3 offset = attack.hitboxOffset;
            Vector3 size = attack.hitboxSize;
            bool follow = attack.akioFollowBone;
            HumanBodyBones bone = attack.akioBone;
            if (attack.akioHitPhases != null)
            {
                for (int i = 0; i < attack.akioHitPhases.Length; i++)
                {
                    var phase = attack.akioHitPhases[i];
                    if (phase == null) continue;
                    if (tick < phase.startTick || tick > phase.endTick) continue;
                    offset = phase.offset;
                    size = phase.size;
                    follow = phase.followBone;
                    bone = phase.bone;
                    break;
                }
            }

            if (_collider is BoxCollider box)
            {
                box.size = size;
                box.center = follow ? Vector3.zero : offset;
            }

            _follow = follow && owner != null && owner.animator != null;
            _followBone = bone;
            _followOffset = offset;
            _followRoot = owner != null ? owner.transform : null;
            _followAnimator = owner != null ? owner.animator : null;
            if (_follow)
            {
                _posed = true;
                SnapToBone();
            }
        }

        private void LateUpdate()
        {
            if (!_follow || _collider == null || !_collider.enabled) return;
            SnapToBone();
        }

        private void SnapToBone()
        {
            if (_followAnimator == null || _followRoot == null) return;
            Transform bone = _followAnimator.GetBoneTransform(_followBone);
            if (bone == null) return;
            transform.position = bone.position
                + _followRoot.forward * _followOffset.x
                + Vector3.up * _followOffset.y
                + _followRoot.right * _followOffset.z;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (CurrentAttack == null || Owner == null) return;

            Hurtbox hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox == null) return;

            // Don't hit self
            if (hurtbox.Owner == null || hurtbox.Owner == Owner) return;

            // Don't hit the same target twice in one active swing
            if (_hitTargets.Contains(hurtbox.Owner)) return;

            _hitTargets.Add(hurtbox.Owner);

            // Execute hit resolution
            Owner.OnHitboxLanded(hurtbox.Owner, CurrentAttack, hurtbox, transform.position);
        }
    }
}
