using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Pure mathematical solver for BlazBlue combo damage, proration, guts scaling, and chip damage.
    /// Strictly adheres to section 4.14 and 4.19 of the specification.
    /// Integer-quantized output for deterministic rollback authority.
    /// </summary>
    public static class BBDamageModel
    {
        public const float SAME_MOVE_PENALTY_DEFAULT = 0.80f;
        public const float DANGER_DAMAGE_MULTIPLIER = 1.25f;
        public const float STYLISH_DAMAGE_MULTIPLIER = 0.80f;

        // Tiered hitstop standards (spec launch pass 1)
        public const int HITSTOP_LIGHT = 9;
        public const int HITSTOP_MEDIUM = 11;
        public const int HITSTOP_HEAVY = 13;
        public const int HITSTOP_SUPER = 16;

        /// <summary>
        /// Standard genre combo scaling: 100% for the first 2 hits, then -10% per hit, minimum 20%.
        /// </summary>
        public static float CalculateComboScaling(int hitCount)
        {
            if (hitCount <= 2) return 1.0f;
            float scaling = 1.0f - 0.10f * (hitCount - 2);
            return Mathf.Max(0.20f, scaling);
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
        /// Calculates Guts defense multiplier based on defender's remaining HP ratio.
        /// </summary>
        public static float CalculateGuts(float currentHp, float maxHp)
        {
            if (maxHp <= 0f) return 1.00f;
            float ratio = currentHp / maxHp;

            if (ratio > 0.50f) return 1.00f;
            if (ratio > 0.30f) return 0.90f;
            if (ratio > 0.20f) return 0.80f;
            if (ratio > 0.10f) return 0.70f;
            return 0.60f;
        }

        /// <summary>
        /// Computes final scaled damage for a hit in a combo according to BlazBlue Central Fiction rules.
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

            // 1. Initial starter proration (P1)
            float p1 = Mathf.Clamp(comboP1, 0.1f, 1.0f);

            // 2. Accumulated per-hit decay (P2)
            float p2 = Mathf.Clamp(accumulatedP2, 0.01f, 1.0f);

            // 3. Same-move decay: sameMovePenalty ^ (uses - 1)
            float repeatFactor = 1.0f;
            if (moveUsesInCombo > 1)
            {
                repeatFactor = Mathf.Pow(sameMovePenalty, moveUsesInCombo - 1);
            }

            // 4. Modifiers
            float dangerFactor = isDefenderInDanger ? DANGER_DAMAGE_MULTIPLIER : 1.0f;
            float stylishFactor = isAttackerStylish ? STYLISH_DAMAGE_MULTIPLIER : 1.0f;
            float gutsFactor = CalculateGuts(defenderCurrentHp, defenderMaxHp);

            // 5. Scaled damage calculation
            float scaledDamage = baseDamage * p1 * p2 * repeatFactor * dangerFactor * stylishFactor * gutsFactor;

            // 6. Minimum damage floor
            float minDamage = baseDamage * Mathf.Clamp01(minDamagePct);
            float finalDamage = Mathf.Max(scaledDamage, minDamage);

            // 7. Deterministic integer floor
            return Mathf.Max(1, Mathf.FloorToInt(finalDamage));
        }

        /// <summary>
        /// Calculates chip damage on guard.
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

            float chipRate = authoredChipPct;
            if (chipRate <= 0f)
            {
                switch (moveKind)
                {
                    case BBMoveKind.Special:
                        chipRate = 0.10f;
                        break;
                    case BBMoveKind.DistortionDrive:
                        chipRate = 0.15f;
                        break;
                    case BBMoveKind.DriveMove:
                        chipRate = 0.08f;
                        break;
                    default:
                        return 0; // Normals deal 0 chip
                }
            }

            return Mathf.FloorToInt(baseDamage * chipRate);
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
