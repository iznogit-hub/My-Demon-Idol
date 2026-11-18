using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Swaps the shared-controller source clip and the animator speed for Akio only.
    /// The base controller is never edited. State speeds stay baked; animator.speed
    /// is desiredClipRate / bakedStateSpeed for the move that is about to play.
    /// </summary>
    public static class AkioPresentation
    {
        public static void Prepare(FighterController fc, string trigger)
        {
            if (fc == null || !fc.UsesAkioMovelist) return;
            var ov = fc.AkioRuntimeOverride;
            var lib = Library(fc);
            if (ov == null || lib == null) return;

            float speed = 1f;
            AttackData atk = fc.CurrentAttack;
            if (atk != null && atk.animationTrigger == trigger && atk.animationClip != null)
            {
                Bind(ov, Slot(lib, atk.akioClipSlot), atk.animationClip);
                if (atk.akioAnimatorSpeed > 0f) speed = atk.akioAnimatorSpeed;
            }

            switch (trigger)
            {
                case "ThrowCard":
                    Clear(ov, lib.originalMagic1);
                    speed = 1f;
                    break;
                case "Parry":
                    Clear(ov, lib.originalAtkR);
                    speed = 1f;
                    break;
                case "ForwardDash":
                    Bind(ov, lib.originalRunF, lib.clipDash);
                    speed = lib.dashAnimatorSpeed > 0f ? lib.dashAnimatorSpeed : 1f;
                    break;
                case "Backdash":
                    speed = 1f;
                    break;
                case "Jump":
                    Bind(ov, lib.originalHop, lib.clipJump);
                    speed = 1f;
                    break;
                case "Intro":
                    Bind(ov, lib.originalHop, lib.clipIntro);
                    speed = 1f;
                    break;
                case "Knockdown":
                    Bind(ov, lib.originalDeath, lib.clipFall);
                    speed = lib.fallAnimatorSpeed > 0f ? lib.fallAnimatorSpeed : 1f;
                    break;
                case "Defeat":
                    Bind(ov, lib.originalDeath, lib.clipFall);
                    speed = 1f;
                    break;
                case "StandUpBack":
                    Bind(ov, lib.originalRise, lib.clipGetup);
                    speed = lib.getupAnimatorSpeed > 0f ? lib.getupAnimatorSpeed : 1f;
                    break;
                case "RapidCancel":
                    Clear(ov, lib.originalRise);
                    speed = 1f;
                    break;
                case "Idle":
                case "Crouch":
                    RestoreLocomotion(fc);
                    return;
            }

            if (fc.animator != null) fc.animator.speed = speed;
        }

        public static void RestoreLocomotion(FighterController fc)
        {
            if (fc == null || !fc.UsesAkioMovelist) return;
            var ov = fc.AkioRuntimeOverride;
            var lib = Library(fc);
            if (ov == null || lib == null) return;
            Bind(ov, lib.originalRunF, lib.clipWalk);
            Bind(ov, lib.originalCrouch, lib.clipCrouch);
            Bind(ov, lib.originalGuard, lib.clipStandBlock);
            Clear(ov, lib.originalAtkR);
            Clear(ov, lib.originalAtkL);
            Clear(ov, lib.originalAtk2);
            Clear(ov, lib.originalPole);
            if (fc.animator != null) fc.animator.speed = 1f;
        }

        public static void SetGuardClip(FighterController fc, bool crouching)
        {
            if (fc == null || !fc.UsesAkioMovelist) return;
            var ov = fc.AkioRuntimeOverride;
            var lib = Library(fc);
            if (ov == null || lib == null) return;
            Bind(ov, lib.originalGuard, crouching ? lib.clipCrouchBlock : lib.clipStandBlock);
        }

        static AkioClipLibrary Library(FighterController fc)
        {
            return fc.AppliedDefinition != null ? fc.AppliedDefinition.akioClips : null;
        }

        static AnimationClip Slot(AkioClipLibrary lib, string slot)
        {
            switch (slot)
            {
                case "atkR": return lib.originalAtkR;
                case "atkL": return lib.originalAtkL;
                case "atk2": return lib.originalAtk2;
                case "pole": return lib.originalPole;
                case "shield": return lib.originalShield;
                case "magic1": return lib.originalMagic1;
                case "magic2": return lib.originalMagic2;
                default: return null;
            }
        }

        static void Bind(AnimatorOverrideController ov, AnimationClip original, AnimationClip replacement)
        {
            if (ov == null || original == null || replacement == null) return;
            ov[original] = replacement;
        }

        static void Clear(AnimatorOverrideController ov, AnimationClip original)
        {
            if (ov == null || original == null) return;
            ov[original] = null;
        }
    }
}
