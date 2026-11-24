using UnityEngine;
using DemonIdol.Audio;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Bridges FBX / Mecanim animation events directly into combat audio effects.
    /// Attach this to the character model holding the Animator (e.g. Model_C01).
    /// Provides concrete receiver handlers for all ExplosiveLLC Warrior Pack animation events
    /// ('FootL', 'FootR', 'Hit', 'Shoot', 'Land', 'WeaponSwitch', 'NewEvent') to silence console errors.
    /// Hitboxes remain strictly authoritative on the 60Hz tick engine (BBMatchSim).
    /// </summary>
    public class AnimationEventReceiver : MonoBehaviour
    {
        #region Classic Combat Audio Triggers

        // Called via Animation Events on Walk/Dash clips
        public void OnFootstep()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayFootstep();
            }
        }

        // Called on wind-up / swing frames
        public void OnSwingLight()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlaySwingLight();
            }
        }

        public void OnSwingHeavy()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlaySwingHeavy();
            }
        }

        public void OnShoot()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayProjectileFire();
            }
        }

        public void OnHitImpact()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlaySwingHeavy();
            }
        }

        public void OnLand()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayLand();
            }
        }

        #endregion

        #region ExplosiveLLC Warrior Pack Event Handlers

        // Footstep Left
        public void FootL() => OnFootstep();
        public void FootL(AnimationEvent evt) => OnFootstep();
        public void FootL(string s) => OnFootstep();
        public void FootL(int i) => OnFootstep();
        public void FootL(float f) => OnFootstep();

        // Footstep Right
        public void FootR() => OnFootstep();
        public void FootR(AnimationEvent evt) => OnFootstep();
        public void FootR(string s) => OnFootstep();
        public void FootR(int i) => OnFootstep();
        public void FootR(float f) => OnFootstep();

        // Melee / Weapon Swing Contact -> heavy/impact swing
        public void Hit() => OnHitImpact();
        public void Hit(AnimationEvent evt) => OnHitImpact();
        public void Hit(string s) => OnHitImpact();
        public void Hit(int i) => OnHitImpact();
        public void Hit(float f) => OnHitImpact();

        // Projectile Release / Bow Shoot -> ranged/projectile launch SFX
        public void Shoot() => OnShoot();
        public void Shoot(AnimationEvent evt) => OnShoot();
        public void Shoot(string s) => OnShoot();
        public void Shoot(int i) => OnShoot();
        public void Shoot(float f) => OnShoot();

        // Landing Impact
        public void Land() => OnLand();
        public void Land(AnimationEvent evt) => OnLand();
        public void Land(string s) => OnLand();
        public void Land(int i) => OnLand();
        public void Land(float f) => OnLand();

        // Weapon Switch / Sheathe
        public void WeaponSwitch() { }
        public void WeaponSwitch(AnimationEvent evt) { }
        public void WeaponSwitch(string s) { }
        public void WeaponSwitch(int i) { }
        public void WeaponSwitch(float f) { }

        // Generic FBX Event
        public void NewEvent() { }
        public void NewEvent(AnimationEvent evt) { }
        public void NewEvent(string s) { }
        public void NewEvent(int i) { }
        public void NewEvent(float f) { }

        // Attack Swing -> weight-appropriate swing
        public void Attack() => OnSwingLight();
        public void Attack(AnimationEvent evt) => OnSwingLight();
        public void Attack(string s) => OnSwingLight();
        public void Attack(int i) => OnSwingLight();
        public void Attack(float f) => OnSwingLight();

        public void Step() => OnFootstep();
        public void Step(AnimationEvent evt) => OnFootstep();
        public void Step(string s) => OnFootstep();
        public void Step(int i) => OnFootstep();
        public void Step(float f) => OnFootstep();

        #endregion
    }
}
