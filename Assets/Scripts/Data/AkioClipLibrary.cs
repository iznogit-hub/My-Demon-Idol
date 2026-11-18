using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Clip handles for Akio's runtime override. Assigned only from her character definition.
    /// Original clips are the KI motions the shared controller already plays. Sub-clips are CMU windows.
    /// </summary>
    [CreateAssetMenu(fileName = "AkioClipLibrary", menuName = "DemonIdol/Combat/Akio Clip Library")]
    public class AkioClipLibrary : ScriptableObject
    {
        [Header("KI source clips (override keys)")]
        public AnimationClip originalAtkR;
        public AnimationClip originalAtkL;
        public AnimationClip originalAtk2;
        public AnimationClip originalPole;
        public AnimationClip originalShield;
        public AnimationClip originalMagic1;
        public AnimationClip originalMagic2;
        public AnimationClip originalDeath;
        public AnimationClip originalRise;
        public AnimationClip originalHop;
        public AnimationClip originalRunF;
        public AnimationClip originalGuard;
        public AnimationClip originalCrouch;

        [Header("CMU sub-clips")]
        public AnimationClip clipWalk;
        public AnimationClip clipDash;
        public AnimationClip clipJump;
        public AnimationClip clipIntro;
        public AnimationClip clipCrouch;
        public AnimationClip clipStandBlock;
        public AnimationClip clipCrouchBlock;
        public AnimationClip clipFall;
        public AnimationClip clipGetup;

        [Header("Animator.speed while that state is playing")]
        public float dashAnimatorSpeed = 1.458f;
        public float fallAnimatorSpeed = 1.62f;
        public float getupAnimatorSpeed = 0.664f;
    }
}
