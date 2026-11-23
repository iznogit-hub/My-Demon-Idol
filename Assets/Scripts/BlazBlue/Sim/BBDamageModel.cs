using System;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Pure mathematical solver for BlazBlue combo damage, proration, guts scaling, and chip damage.
    /// Strictly adheres to section 4.14 and 4.19 of the specification.
    /// Uses deterministic 64-bit integer basis-point (10,000 = 1.0000x) fixed-point math
    /// to guarantee bit-identical damage calculations across ARM64 and x86-64 platforms.
    /// </summary>
    public static class BBDamageModel
    {
        public const long BP_SCALE = 10_000L;
        public const long SAME_MOVE_PENALTY_DEFAULT_BP = 8_000L; // 0.80x
        public const long DANGER_DAMAGE_MULTIPLIER_BP = 12_500L;  // 1.25x
        public const long STYLISH_DAMAGE_MULTIPLIER_BP = 8_000L;  // 0.80x

        public const float SAME_MOVE_PENALTY_DEFAULT = 0.80f;
        public const float DANGER_DAMAGE_MULTIPLIER = 1.25f;
        public const float STYLISH_DAMAGE_MULTIPLIER = 0.80f;

        // Tiered hitstop standards (spec launch pass 1)
        public const int HITSTOP_LIGHT = 9;
        public const int HITSTOP_MEDIUM = 11;
        public const int HITSTOP_HEAVY = 13;
        public const int HITSTOP_SUPER = 16;

        /// <summary>
        /// Standard genre combo scaling in basis points (100% for first 2 hits, -10% per hit, min 20%).
        /// </summary>
        public static long CalculateComboScalingBP(int hitCount)
        {
            if (hitCount <= 2) return BP_SCALE;
            long scaling = BP_SCALE - 1_000L * (hitCount - 2);
            return Math.Max(2_000L, scaling);
        }

        /// <summary>
        /// Standard genre combo scaling: 100% for the first 2 hits, then -10% per hit, minimum 20%.
        /// </summary>
        public static float CalculateComboScaling(int hitCount)
        {
            return CalculateComboScalingBP(hitCount) / (float)BP_SCALE;
        }

        public static int GetTierHitstop(BBButton button, BBMoveKind kind)
        {
            if (kind == BBMoveKind.DistortionDrive || kind == BBMoveKind.AstralHeat) return HITSTOP_SUPER;
            if (kind == BBMoveKind.Special || kind == BBMoveKind.CommandThrow) return HITSTOP_HEAVY;
            if ((button & (BBButton.C | BBButton.D)) != 0) return HITSTOP_HEAVY;
            if ((button & BBButton.B) != 0) return HITSTOP_MEDIUM;
            return HITSTOP_LIGHT;
        }

        /// <summary>
        /// Calculates Guts defense multiplier in basis points (10,000 = 100%) based on defender's remaining HP ratio.
        /// Strictly deterministic 64-bit integer division and comparisons with zero floating-point math.
        /// </summary>
        public static long CalculateGutsBP(int currentHp, int maxHp)
        {
            if (maxHp <= 0) return BP_SCALE;
            long cur = Math.Max(0, currentHp);
            long max = maxHp;
            if (max <= 0L) return BP_SCALE;

            long ratioBP = (cur * BP_SCALE) / max;

            if (ratioBP > 5000L) return 10_000L; // > 50%
            if (ratioBP > 3000L) return 9_000L;  // 30% - 50%
            if (ratioBP > 2000L) return 8_000L;  // 20% - 30%
            if (ratioBP > 1000L) return 7_000L;  // 10% - 20%
            return 6_000L;                       // <= 10%
        }

        /// <summary>
        /// Calculates Guts defense multiplier based on defender's remaining HP ratio.
        /// </summary>
        public static float CalculateGuts(int currentHp, int maxHp)
        {
            return CalculateGutsBP(currentHp, maxHp) / (float)BP_SCALE;
        }

        /// <summary>
        /// Computes final scaled damage for a hit in a combo according to BlazBlue Central Fiction rules.
        /// All intermediate scaling and decay are calculated via 64-bit integer basis-point math (10,000 = 1.0000x).
        /// </summary>
        public static int CalculateDamage(
            float baseDamage,
            float comboP1,               // P1 of the starter move (applied to whole combo)
            float accumulatedP2,         // Product of all previous hits' P2
            int moveUsesInCombo,         // Number of times this move was used in combo (1 = first use)
            float defenderCurrentHp,
            float defenderMaxHp,
            float minDamagePct,          // Guaranteed floor percentage (e.g. 0.08 for B normals, 0.30 for DD)
            bool isDefenderInDanger = false,
            bool isAttackerStylish = false,
            float sameMovePenalty = SAME_MOVE_PENALTY_DEFAULT)
        {
            if (baseDamage <= 0f) return 0;

            long baseDmg = (long)Math.Round(baseDamage);
            if (baseDmg <= 0L) return 0;

            // 1. Initial starter proration (P1 clamped 10% to 100% in BP)
            long p1BP = Math.Clamp((int)Math.Round(comboP1 * BP_SCALE), 1_000, 10_000);

            // 2. Accumulated per-hit decay (P2 clamped 1% to 100% in BP)
            long p2BP = Math.Clamp((int)Math.Round(accumulatedP2 * BP_SCALE), 100, 10_000);

            // 3. Same-move decay in integer BP: penalty ^ (uses - 1)
            long penaltyBP = Math.Clamp((int)Math.Round(sameMovePenalty * BP_SCALE), 1_000, 10_000);
            long repeatFactorBP = BP_SCALE;
            if (moveUsesInCombo > 1)
            {
                for (int i = 1; i < moveUsesInCombo; i++)
                {
                    repeatFactorBP = (repeatFactorBP * penaltyBP) / BP_SCALE;
                }
            }

            // 4. Modifiers in integer BP
            long dangerBP = isDefenderInDanger ? DANGER_DAMAGE_MULTIPLIER_BP : BP_SCALE;
            long stylishBP = isAttackerStylish ? STYLISH_DAMAGE_MULTIPLIER_BP : BP_SCALE;
            int curHpInt = (int)Math.Round(defenderCurrentHp);
            int maxHpInt = (int)Math.Round(defenderMaxHp);
            long gutsBP = CalculateGutsBP(curHpInt, maxHpInt);

            // 5. Total multiplier accumulation in basis points (preserves deterministic fighting-game proration)
            long totalMultiplierBP = BP_SCALE;
            totalMultiplierBP = (totalMultiplierBP * p1BP) / BP_SCALE;
            totalMultiplierBP = (totalMultiplierBP * p2BP) / BP_SCALE;
            totalMultiplierBP = (totalMultiplierBP * repeatFactorBP) / BP_SCALE;
            totalMultiplierBP = (totalMultiplierBP * dangerBP) / BP_SCALE;
            totalMultiplierBP = (totalMultiplierBP * stylishBP) / BP_SCALE;
            totalMultiplierBP = (totalMultiplierBP * gutsBP) / BP_SCALE;

            long scaledDamage = (baseDmg * totalMultiplierBP) / BP_SCALE;

            // 6. Minimum damage floor
            long minDmgBP = Math.Clamp((int)Math.Round(minDamagePct * BP_SCALE), 0, 10_000);
            long minDamage = (baseDmg * minDmgBP) / BP_SCALE;
            long finalDamage = Math.Max(scaledDamage, minDamage);

            // 7. Deterministic integer floor (minimum 1 damage on hit)
            return (int)Math.Max(1L, finalDamage);
        }

        /// <summary>
        /// Calculates chip damage on guard using integer basis-point math.
        /// Specials = 10%, Distortion Drives = 15%, Drive moves = 8%, Normals = 0%.
        /// Barrier blocks 100% of chip damage.
        /// </summary>
        public static int CalculateChipDamage(
            float baseDamage,
            BBMoveKind moveKind,
            bool isBarriering,
            float authoredChipPct = 0f)
        {
            if (isBarriering || baseDamage <= 0f) return 0;

            long baseDmg = (long)Math.Round(baseDamage);
            if (baseDmg <= 0L) return 0;

            long chipRateBP = (long)Math.Round(authoredChipPct * BP_SCALE);
            if (chipRateBP <= 0L)
            {
                switch (moveKind)
                {
                    case BBMoveKind.Special:
                        chipRateBP = 1_000L; // 10%
                        break;
                    case BBMoveKind.DistortionDrive:
                        chipRateBP = 1_500L; // 15%
                        break;
                    case BBMoveKind.DriveMove:
                        chipRateBP = 800L; // 8%
                        break;
                    default:
                        return 0; // Normals deal 0 chip
                }
            }

            return (int)((baseDmg * chipRateBP) / BP_SCALE);
        }

        /// <summary>
        /// Checks if a hit can KO via chip damage.
        /// In BlazBlue, chip can only KO if the attack is a Distortion Drive or Astral Heat.
        /// </summary>
        public static bool CanChipKO(BBMoveKind moveKind)
        {
            return moveKind == BBMoveKind.DistortionDrive || moveKind == BBMoveKind.AstralHeat;
        }

        /// <summary>
        /// Retrieves default P1 starter, P2 per-hit decay, and minimum damage percent for move kind and button.
        /// </summary>
        public static void GetDefaultProration(
            BBMoveKind kind,
            BBButton button,
            out float p1,
            out float p2,
            out float minDamagePct)
        {
            switch (kind)
            {
                case BBMoveKind.Throw:
                case BBMoveKind.CommandThrow:
                    p1 = 0.60f;
                    p2 = 1.00f;
                    minDamagePct = 0.00f;
                    break;
                case BBMoveKind.Special:
                case BBMoveKind.DriveMove:
                    p1 = 0.90f;
                    p2 = 0.90f;
                    minDamagePct = 0.12f;
                    break;
                case BBMoveKind.DistortionDrive:
                    p1 = 1.00f;
                    p2 = 0.95f;
                    minDamagePct = 0.30f;
                    break;
                case BBMoveKind.AstralHeat:
                    p1 = 1.00f;
                    p2 = 1.00f;
                    minDamagePct = 1.00f;
                    break;
                default:
                    // Normals & Command Normals by button strength
                    if ((button & BBButton.A) != 0)
                    {
                        p1 = 0.70f;
                        p2 = 0.85f;
                        minDamagePct = 0.05f;
                    }
                    else if ((button & BBButton.B) != 0)
                    {
                        p1 = 0.80f;
                        p2 = 0.89f;
                        minDamagePct = 0.08f;
                    }
                    else if ((button & BBButton.C) != 0)
                    {
                        p1 = 0.90f;
                        p2 = 0.92f;
                        minDamagePct = 0.10f;
                    }
                    else if ((button & BBButton.D) != 0)
                    {
                        p1 = 0.85f;
                        p2 = 0.90f;
                        minDamagePct = 0.08f;
                    }
                    else
                    {
                        p1 = 0.80f;
                        p2 = 0.89f;
                        minDamagePct = 0.08f;
                    }
                    break;
            }
        }
    }
}
