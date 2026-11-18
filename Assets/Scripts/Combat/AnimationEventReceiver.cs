using UnityEngine;
using DemonIdol.Audio;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Bridges FBX / Mecanim animation events directly into combat audio effects.
    /// Attach this to the character model holding the Animator.
    /// Hitboxes are strictly authoritative on the 60Hz tick engine (FighterController).
    /// </summary>
    public class AnimationEventReceiver : MonoBehaviour
    {
        #region Animation Event Triggers

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

        public void OnLand()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayLand();
            }
        }

        #endregion
    }
}
