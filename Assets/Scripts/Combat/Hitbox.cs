using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    [RequireComponent(typeof(Collider))]
    [System.Obsolete("Superseded by BBBoxTrack and BBBoxBuffer in Assets/Scripts/BlazBlue/. Do not use in new code.", false)]
    public class Hitbox : MonoBehaviour
    {
        public FighterController Owner { get; private set; }
        public AttackData CurrentAttack { get; private set; }

        private Collider _collider;
        private BoxCollider _boxCollider;
        private Rigidbody _rigidbody;
        private readonly HashSet<FighterController> _hitTargets = new HashSet<FighterController>();
        private readonly Collider[] _sweepResults = new Collider[16];

        private Vector3 _defaultCenter = new Vector3(0f, 1.0f, 0.8f);
        private Vector3 _defaultSize = new Vector3(1.2f, 1.2f, 1.4f);

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;

            _boxCollider = _collider as BoxCollider;
            if (_boxCollider != null)
            {
                _defaultCenter = _boxCollider.center;
                _defaultSize = _boxCollider.size;
            }

            // Ensure Kinematic Rigidbody so Unity PhysX reliably dispatches OnTriggerEnter
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Initialize(FighterController owner)
        {
            Owner = owner;
        }

        public void Activate(AttackData attack)
        {
            CurrentAttack = attack;
            _hitTargets.Clear();

            // Dynamically tune hitbox reach and elevation based on attack and weapon type
            AdjustHitboxForAttack(attack);

            _collider.enabled = true;

            // Perform immediate spatial sweep on activation frame to catch direct point-blank contacts
            PerformSpatialSweep();
        }

        public void Deactivate()
        {
            if (_collider != null) _collider.enabled = false;
            CurrentAttack = null;
            if (_hitTargets != null) _hitTargets.Clear();

            // Reset back to default dimensions
            if (_boxCollider != null)
            {
                _boxCollider.center = _defaultCenter;
                _boxCollider.size = _defaultSize;
            }
        }

        private void Update()
        {
            // Continuous spatial sweep during active attack frames to prevent high-speed tunneling
            if (_collider != null && _collider.enabled && CurrentAttack != null && Owner != null)
            {
                PerformSpatialSweep();
            }
        }

        private void AdjustHitboxForAttack(AttackData attack)
        {
            if (_boxCollider == null || attack == null) return;

            Vector3 center = _defaultCenter;
            Vector3 size = _defaultSize;

            // Elevation tuning based on hit level
            if (attack.hitLevel == HitLevel.Low)
            {
                center.y = 0.35f;
                size.y = 0.70f;
                size.z = 1.6f;
            }
            else if (attack.hitLevel == HitLevel.High || attack.guardRequirement == GuardRequirement.Overhead)
            {
                center.y = 1.45f;
                size.y = 1.10f;
                size.z = 1.5f;
            }

            // Reach tuning based on attack intensity
            if (attack.attackType == AttackType.Heavy || attack.isLauncher)
            {
                size.x *= 1.25f;
                size.z *= 1.35f;
                center.z += 0.25f;
            }
            else if (attack.attackType == AttackType.DomainSuper)
            {
                size = new Vector3(2.2f, 2.2f, 2.8f);
                center.z += 0.5f;
            }

            // Weapon archetype scaling
            Component weaponRig = Owner != null ? Owner.GetComponent("BBWeaponRig") : null;
            if (weaponRig != null)
            {
                var typeField = weaponRig.GetType().GetField("warriorType");
                if (typeField != null)
                {
                    string warriorTypeStr = typeField.GetValue(weaponRig)?.ToString();
                    if (warriorTypeStr == "TwoHanded" || warriorTypeStr == "Hammer")
                    {
                        size.x *= 1.3f;
                        size.z *= 1.4f;
                        center.z += 0.35f;
                    }
                    else if (warriorTypeStr == "Spearman")
                    {
                        size.z *= 1.75f;
                        center.z += 0.6f;
                    }
                    else if (warriorTypeStr == "Ninja" || warriorTypeStr == "Karate")
                    {
                        size.x *= 1.1f;
                    }
                }
            }

            _boxCollider.center = center;
            _boxCollider.size = size;
        }

        private void PerformSpatialSweep()
        {
            if (_boxCollider == null || CurrentAttack == null || Owner == null) return;

            Vector3 worldCenter = transform.TransformPoint(_boxCollider.center);
            Vector3 halfExtents = Vector3.Scale(_boxCollider.size * 0.5f, transform.lossyScale);
            Quaternion orientation = transform.rotation;

            int hitCount = Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, _sweepResults, orientation, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _sweepResults[i];
                if (col == null || col == _collider) continue;

                Hurtbox hurtbox = col.GetComponent<Hurtbox>();
                if (hurtbox != null)
                {
                    ProcessHitOnHurtbox(hurtbox, col.ClosestPoint(worldCenter));
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (CurrentAttack == null || Owner == null) return;

            Hurtbox hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox != null)
            {
                ProcessHitOnHurtbox(hurtbox, transform.position);
            }
        }

        private void ProcessHitOnHurtbox(Hurtbox hurtbox, Vector3 contactPoint)
        {
            if (hurtbox == null || CurrentAttack == null || Owner == null) return;

            // Don't hit self
            if (hurtbox.Owner == null || hurtbox.Owner == Owner) return;

            // Don't hit the same target twice in one active swing
            if (_hitTargets.Contains(hurtbox.Owner)) return;

            _hitTargets.Add(hurtbox.Owner);

            // Execute authoritative hit resolution on FighterController
            Owner.OnHitboxLanded(hurtbox.Owner, CurrentAttack, hurtbox, contactPoint);
        }
    }
}
