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

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
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
