using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    public enum WeaponArchetype
    {
        Gun,
        Sword,
        MageStaff,
        Shield
    }

    /// <summary>
    /// Attach-point descriptor and runtime controller for a 3D weapon prop in Demon Idol.
    /// Supports guns (muzzle flashes), swords (blade trails), and mage staffs (glowing casting foci).
    /// </summary>
    [DisallowMultipleComponent]
    public class FighterWeaponProp : MonoBehaviour
    {
        [Header("Weapon Identity")]
        public WeaponArchetype archetype = WeaponArchetype.Sword;
        public string weaponName = "Fantasy Weapon";
        public HumanBodyBones targetBone = HumanBodyBones.RightHand;

        [Header("Socket Offsets")]
        public Vector3 localPositionOffset = Vector3.zero;
        public Vector3 localRotationOffset = Vector3.zero;
        public Vector3 localScale = Vector3.one;

        [Header("Combat FX Anchor Points")]
        public Transform muzzlePoint;
        public Transform bladeTip;
        public Transform bladeBase;
        public Transform staffOrbPoint;
        public TrailRenderer slashTrail;
        public ParticleSystem ambientAura;

        public void ActivateTrail(bool active)
        {
            if (slashTrail != null)
            {
                slashTrail.emitting = active;
            }
        }

        public void TriggerMuzzleFlash()
        {
            if (muzzlePoint != null)
            {
                var ps = muzzlePoint.GetComponentInChildren<ParticleSystem>();
                if (ps != null) ps.Play();
            }
        }

        public void TriggerStaffCast()
        {
            if (staffOrbPoint != null)
            {
                var ps = staffOrbPoint.GetComponentInChildren<ParticleSystem>();
                if (ps != null) ps.Play();
            }
        }
    }
}
// trigger Tue Nov 17 00:47:11 IST 2026
