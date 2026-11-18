using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Content Contract: MoveSet ScriptableObject.
    /// Encapsulates a character's complete tactical moveset for 3-button 2.5D combat
    /// (Light, Heavy, Skill, Throw, Super, Burst) with frame data, hitboxes,
    /// damage, stun, advantages, proration, and cancel routes.
    /// Authored by combat designers without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMoveSet", menuName = "DemonIdol/Combat/MoveSet")]
    public class MoveSet : ScriptableObject
    {
        [Header("Identity")]
        public string characterId = "Kai";
        public string moveSetName = "Standard Shinobi Art";
        public FighterArchetype archetype = FighterArchetype.Rushdown;

        [Header("Three-Button Grounded Moves (L, H, S)")]
        public AttackData standingL;        // L (neutral/forward/back): standing Light (NORMAL)
        public AttackData crouchingL;       // Down/down-diagonals+L: low crouching Light (LOW)
        public AttackData standingH;        // H (neutral/back): standing Heavy (NORMAL)
        public AttackData forwardH;         // Forward+H: overhead (OVERHEAD)
        public AttackData crouchingH;       // Down/down-diagonals+H: low sweep (LOW, KD)
        public AttackData neutralS;         // S (neutral/back): signature skill (NORMAL)
        public AttackData forwardS;         // Forward+S: advancing skill (NORMAL)
        public AttackData downS;            // Down/down-diagonals+S: anti-air launcher (AIR, protection ages 5-12)

        [Header("Three-Button Airborne Moves")]
        public AttackData airL;             // Air L: air Light (OVERHEAD)
        public AttackData airH;             // Air H: air Heavy (OVERHEAD)
        public AttackData airS;             // Air S: signature air skill (alias for airH in sample)

        [Header("Chords & System Actions")]
        public AttackData tacticalThrow;    // L+H chord: throw / throw tech
        public AttackData domainSuper;      // H+S chord: super finisher (100 meter)
        public AttackData defensiveBurst;   // L+S chord: defensive burst (50 meter)
        public AttackData escapeAction => defensiveBurst; // L+S Escape action alias

        [Header("Legacy & Content Contract Bindings")]
        public AttackData lightAttack;
        public AttackData mediumAttack;
        public AttackData heavyAttack;
        public AttackData driveSkill;
        public AttackData overheadAttack;
        public AttackData lowSweepAttack;
        public AttackData antiairAttack;

        [Header("Additional Move List")]
        public List<AttackData> additionalMoves = new List<AttackData>();

        [Header("Akio-only routes (null on every other moveset)")]
        public AttackData akioRekka1;
        public AttackData akioRekka2;
        public AttackData akioRekka3;
        public AttackData akioFireball;
        public AttackData akioDragonPunch;

        /// <summary>
        /// Unambiguously resolves an accepted 3-button command into the authored AttackData.
        /// Facing-relative direction: forward is direction.x * facingDirection > 0.20f.
        /// Down is direction.y < -0.20f.
        /// </summary>
        public AttackData ResolveAttack(ThreeButtonCommand cmd, bool isGrounded, Vector2 direction, int facingDirection)
        {
            // 1. Chords & Macro Commands (override directional attack variants)
            switch (cmd)
            {
                case ThreeButtonCommand.CommandGrab:
                    return tacticalThrow;
                case ThreeButtonCommand.Ultra:
                    return domainSuper;
                case ThreeButtonCommand.GrabBreak:
                    return defensiveBurst;
            }

            // 2. Airborne Moves
            if (!isGrounded)
            {
                switch (cmd)
                {
                    case ThreeButtonCommand.Light:
                        return airL ?? lightAttack;
                    case ThreeButtonCommand.Heavy:
                        return airH ?? heavyAttack;
                    case ThreeButtonCommand.Skill:
                        return airS ?? airH ?? heavyAttack ?? driveSkill;
                    default:
                        return airL ?? lightAttack;
                }
            }

            // 3. Grounded Moves (Quantized direction: down, forward, neutral/back)
            bool isDown = direction.y < -0.20f;
            float relX = direction.x * facingDirection;
            bool isForward = relX > 0.20f;

            if (isDown)
            {
                switch (cmd)
                {
                    case ThreeButtonCommand.Light:
                        return crouchingL ?? lowSweepAttack ?? lightAttack;
                    case ThreeButtonCommand.Heavy:
                        return crouchingH ?? lowSweepAttack ?? heavyAttack;
                    case ThreeButtonCommand.Skill:
                        return downS ?? antiairAttack ?? driveSkill;
                }
            }
            else if (isForward)
            {
                switch (cmd)
                {
                    case ThreeButtonCommand.Light:
                        return standingL ?? lightAttack;
                    case ThreeButtonCommand.Heavy:
                        return forwardH ?? overheadAttack ?? heavyAttack;
                    case ThreeButtonCommand.Skill:
                        return forwardS ?? driveSkill;
                }
            }
            else
            {
                // Neutral or holding Back
                switch (cmd)
                {
                    case ThreeButtonCommand.Light:
                        return standingL ?? lightAttack;
                    case ThreeButtonCommand.Heavy:
                        return standingH ?? heavyAttack;
                    case ThreeButtonCommand.Skill:
                        return neutralS ?? driveSkill;
                }
            }

            return standingL ?? lightAttack;
        }

        public AttackData GetAttack(AttackType type)
        {
            switch (type)
            {
                case AttackType.Light: return standingL ?? lightAttack;
                case AttackType.Medium: return mediumAttack ?? forwardH ?? standingH;
                case AttackType.Heavy: return standingH ?? heavyAttack;
                case AttackType.DriveSkill: return neutralS ?? driveSkill;
                case AttackType.DomainSuper: return domainSuper;
                case AttackType.Throw: return tacticalThrow;
                default: return standingL ?? lightAttack;
            }
        }

        /// <summary>
        /// Synchronizes legacy field pointers so inspectors and legacy harnesses remain 100% valid.
        /// </summary>
        public void SyncLegacyBindings()
        {
            if (lightAttack == null) lightAttack = standingL;
            if (standingL == null) standingL = lightAttack;

            if (heavyAttack == null) heavyAttack = standingH;
            if (standingH == null) standingH = heavyAttack;

            if (driveSkill == null) driveSkill = neutralS;
            if (neutralS == null) neutralS = driveSkill;

            if (overheadAttack == null) overheadAttack = forwardH;
            if (forwardH == null) forwardH = overheadAttack;

            if (lowSweepAttack == null) lowSweepAttack = crouchingH;
            if (crouchingH == null) crouchingH = lowSweepAttack;

            if (antiairAttack == null) antiairAttack = downS;
            if (downS == null) downS = antiairAttack;
        }

        public static T CreateSafeInstance<T>() where T : ScriptableObject
        {
            try
            {
                var method = typeof(ScriptableObject).GetMethod("CreateInstance", Type.EmptyTypes);
                if (method != null)
                {
                    return (T)method.MakeGenericMethod(typeof(T)).Invoke(null, null);
                }
            }
            catch
            {
                // Fallback for standalone non-Unity host environments
            }

            return (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
        }

        /// <summary>
        /// Factory creating the exact editable sample preset from Section 4 of the project prompt.
        /// </summary>
        public static MoveSet CreateSamplePreset(string charId = "SampleShinobi")
        {
            MoveSet ms = CreateSafeInstance<MoveSet>();
            ms.characterId = charId;
            ms.moveSetName = "3-Button Shinobi Tactical Preset";

            // Standing L: 5/3/10, dmg 35, HS 16, BS 10, advantage +4/-2 (NORMAL)
            ms.standingL = CreateAttack("Standing L", AttackType.Light, GuardRequirement.Normal, 5, 3, 10, 35f, 16, 10, 0.18f, +4, -2, "LightAttack");
            
            // Crouching L: 6/2/12, dmg 30, HS 16, BS 10, advantage +3/-3 (LOW)
            ms.crouchingL = CreateAttack("Crouching L", AttackType.Light, GuardRequirement.Low, 6, 2, 12, 30f, 16, 10, 0.18f, +3, -3, "LowAttack");

            // Standing H: 11/3/20, dmg 80, HS 26, BS 15, advantage +4/-7 (NORMAL)
            ms.standingH = CreateAttack("Standing H", AttackType.Heavy, GuardRequirement.Normal, 11, 3, 20, 80f, 26, 15, 0.40f, +4, -7, "HeavyAttack");

            // Forward H (Overhead): 23/3/22, dmg 70, HS 27, BS 16, advantage +3/-8 (OVERHEAD)
            ms.forwardH = CreateAttack("Forward H (Overhead)", AttackType.Heavy, GuardRequirement.Overhead, 23, 3, 22, 70f, 27, 16, 0.40f, +3, -8, "OverheadAttack");

            // Crouching H (Low Sweep): 15/3/25, dmg 65, HS KD, BS 15, advantage KD/-12 (LOW, KD 36 ticks)
            ms.crouchingH = CreateAttack("Crouching H (Sweep)", AttackType.Heavy, GuardRequirement.Low, 15, 3, 25, 65f, 36, 15, 0.40f, 0, -12, "LowSweepAttack");
            ms.crouchingH.causesKnockdown = true;
            ms.crouchingH.knockdownDurationTicks = 36;

            // Neutral S (Signature Skill): 13/4/22, dmg 90, HS 30, BS 17, advantage +5/-8 (NORMAL)
            ms.neutralS = CreateAttack("Neutral S (Signature)", AttackType.DriveSkill, GuardRequirement.Normal, 13, 4, 22, 90f, 30, 17, 0.55f, +5, -8, "DriveSkill");

            // Forward S (Advancing Skill): 17/3/26, dmg 95, HS 31, BS 16, advantage +3/-12 (NORMAL)
            ms.forwardS = CreateAttack("Forward S (Advancing)", AttackType.DriveSkill, GuardRequirement.Normal, 17, 3, 26, 95f, 31, 16, 0.55f, +3, -12, "AdvancingSkill");

            // Down S (Anti-Air Launcher): 10/4/28, dmg 70, HS AIR, BS 16, advantage AIR/-15 (NORMAL, launcher, airborne strike protection ages 5-12)
            ms.downS = CreateAttack("Down S (Launcher)", AttackType.DriveSkill, GuardRequirement.Normal, 10, 4, 28, 70f, 35, 16, 0.55f, +8, -15, "LauncherAttack");
            ms.downS.isLauncher = true;
            ms.downS.launchVelocity = new Vector2(1.5f, 9.0f);
            ms.downS.isAirborneStrikeProtected = true;
            ms.downS.protectionStartTick = 5;
            ms.downS.protectionEndTick = 12;

            // Air L: 7/5/*, dmg 35, HS 18, BS 12 (OVERHEAD)
            ms.airL = CreateAttack("Air L", AttackType.JumpAttack, GuardRequirement.Overhead, 7, 5, 12, 35f, 18, 12, 0.18f, +4, -2, "AirLight");

            // Air H: 12/5/*, dmg 65, HS 22, BS 15 (OVERHEAD)
            ms.airH = CreateAttack("Air H", AttackType.JumpAttack, GuardRequirement.Overhead, 12, 5, 12, 65f, 22, 15, 0.40f, +5, -5, "AirHeavy");
            ms.airS = ms.airH; // Alias for sample

            // Throw: 6/2/25, dmg 100, KD 40, capture rules (THROW)
            ms.tacticalThrow = CreateAttack("Tactical Throw", AttackType.Throw, GuardRequirement.Throw, 6, 2, 25, 100f, 40, 0, 1.2f, 0, 0, "Throw");
            ms.tacticalThrow.causesKnockdown = true;
            ms.tacticalThrow.knockdownDurationTicks = 40;
            ms.tacticalThrow.throwTechWindowTicks = 12;

            // Super: 10/3/35, dmg 240, KD 48, BS 20, advantage KD/-17 (NORMAL)
            ms.domainSuper = CreateAttack("Super Finisher", AttackType.DomainSuper, GuardRequirement.Normal, 10, 3, 35, 240f, 48, 20, 1.5f, 0, -17, "DomainSuper");
            ms.domainSuper.causesKnockdown = true;
            ms.domainSuper.knockdownDurationTicks = 48;

            // Burst: 6/3/30, dmg 0, BS 14, advantage -18, strike protection ages 0-8
            ms.defensiveBurst = CreateAttack("Defensive Burst", AttackType.DriveSkill, GuardRequirement.Normal, 6, 3, 30, 0f, 20, 14, 2.5f, 0, -18, "BurstShockwave");
            ms.defensiveBurst.crush = CrushProperty.PowerCrush;
            ms.defensiveBurst.crushStartTick = 0;
            ms.defensiveBurst.crushEndTick = 8;

            ms.SyncLegacyBindings();
            return ms;
        }

        private static AttackData CreateAttack(string name, AttackType type, GuardRequirement guardReq, 
            int s, int a, int r, float dmg, int hs, int bs, float pushback, int hitAdv, int blockAdv, string anim)
        {
            AttackData atk = CreateSafeInstance<AttackData>();
            atk.attackName = name;
            atk.attackType = type;
            atk.guardRequirement = guardReq;
            atk.hitLevel = (guardReq == GuardRequirement.Low) ? HitLevel.Low 
                         : (guardReq == GuardRequirement.Overhead) ? HitLevel.Overhead 
                         : (guardReq == GuardRequirement.Throw) ? HitLevel.Throw 
                         : HitLevel.Mid;
            atk.startupFrames = s;
            atk.activeFrames = a;
            atk.recoveryFrames = r;
            atk.damage = dmg;
            atk.hitstunTicks = hs;
            atk.blockstunTicks = bs;
            atk.pushbackOnHit = pushback;
            atk.pushbackOnBlock = pushback * 0.70f;
            atk.advantageOnHit = hitAdv;
            atk.advantageOnBlock = blockAdv;
            atk.animationTrigger = anim;
            atk.hitboxOffset = new Vector3(0.8f, 1.0f, 0f);
            atk.hitboxSize = new Vector3(1.25f, 1.25f, 1.25f);
            atk.cancelWindow = new Vector2Int(s, s + a + 3);
            atk.archetype = FighterArchetype.Rushdown;

            // Hitstop: L=5, H=8, S=9, Super=12. On block=4/6/6/8
            if (type == AttackType.Light) { atk.hitstopAttacker = 5; atk.hitstopDefender = 5; atk.hitstopOnBlock = 4; }
            else if (type == AttackType.Heavy) { atk.hitstopAttacker = 8; atk.hitstopDefender = 8; atk.hitstopOnBlock = 6; }
            else if (type == AttackType.DriveSkill) { atk.hitstopAttacker = 9; atk.hitstopDefender = 9; atk.hitstopOnBlock = 6; }
            else if (type == AttackType.DomainSuper) { atk.hitstopAttacker = 12; atk.hitstopDefender = 12; atk.hitstopOnBlock = 8; }
            else { atk.hitstopAttacker = 5; atk.hitstopDefender = 5; atk.hitstopOnBlock = 4; }

            return atk;
        }
    }
}
