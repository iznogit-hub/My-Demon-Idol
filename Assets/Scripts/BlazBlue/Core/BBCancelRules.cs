using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Interface allowing cancel rule evaluations inside MDI.BlazBlue.Core
    /// without coupling to Unity's Assembly-CSharp AttackData hierarchy.
    /// </summary>
    public interface IBBCancelableMove
    {
        string MoveId { get; }
        BBButton Button { get; }
        BBMoveKind Kind { get; }
        bool AirOk { get; }
        bool GroundOk { get; }
        bool SpecialCancel { get; }
        bool DdCancel { get; }
        bool JumpCancelOnHit { get; }
        bool JumpCancelOnBlock { get; }
        bool RapidCancelable { get; }
        string[] GatlingInto { get; }
    }

    /// <summary>
    /// Tracks active chain state during a combo or blockstring.
    /// Enforces once-per-chain usage, A-normal repeat limit, and jump/special cancel availability.
    /// </summary>
    [Serializable]
    public struct BBChainState
    {
        public ulong usedMask;           // Bitmask of move indices used in current chain
        public byte aRepeatCount;        // Count of A normals used consecutively in this chain (max 3)
        public bool hasJumpCanceled;     // Jump cancel used in this ground chain
        public bool hasSpecialCanceled;  // Special cancel used in this chain
        public bool madeContact;         // Attack made contact (hit or block)
        public int contactTick;          // Tick when attack connected

        public void Reset()
        {
            usedMask = 0UL;
            aRepeatCount = 0;
            hasJumpCanceled = false;
            hasSpecialCanceled = false;
            madeContact = false;
            contactTick = 0;
        }

        public bool IsUsed(int moveIndex)
        {
            if (moveIndex < 0 || moveIndex >= 64) return false;
            return (usedMask & (1UL << moveIndex)) != 0;
        }

        public void MarkUsed(int moveIndex)
        {
            if (moveIndex >= 0 && moveIndex < 64)
            {
                usedMask |= (1UL << moveIndex);
            }
        }
    }

    /// <summary>
    /// Pure logic solver for BlazBlue Revolver Action gatlings and cancel windows.
    /// Zero allocation.
    /// </summary>
    public static class BBCancelRules
    {
        public const int DEFAULT_MAX_A_REPEATS = 3;

        /// <summary>
        /// Evaluates whether an active attack can cancel into the next requested attack.
        /// </summary>
        public static bool CanCancel(
            in BBChainState chain,
            IBBCancelableMove current,
            IBBCancelableMove next,
            int nextMoveIndex,
            int currentTickInMove,
            bool onHit,
            bool onBlock,
            bool isGrounded,
            int maxARepeats = DEFAULT_MAX_A_REPEATS,
            bool allowWhiffACancels = true)
        {
            if (current == null || next == null) return false;

            // 1. Check ground/air compatibility
            if (isGrounded && !next.GroundOk) return false;
            if (!isGrounded && !next.AirOk) return false;

            bool madeContact = onHit || onBlock;

            // 2. Cancel window check (must be inside cancel window or after minimum contact ticks)
            if (madeContact)
            {
                int ticksSinceContact = currentTickInMove - chain.contactTick;
                // Hard minimum of 6 ticks after contact so hitstop doesn't swallow cancel
                if (ticksSinceContact < 0) return false;
            }
            else
            {
                // Whiff cancel check: only A into A allowed on whiff
                if (!allowWhiffACancels) return false;
                if (current.Button != BBButton.A || next.Button != BBButton.A) return false;
            }

            // 3. Special / Distortion / Astral cancels
            if (next.Kind == BBMoveKind.Special || next.Kind == BBMoveKind.DriveMove)
            {
                return current.SpecialCancel && madeContact;
            }
            if (next.Kind == BBMoveKind.DistortionDrive)
            {
                return current.DdCancel && madeContact;
            }
            if (next.Kind == BBMoveKind.AstralHeat)
            {
                return current.SpecialCancel && onHit;
            }

            // 4. Revolver Action (Gatling) Normals
            if (next.Kind == BBMoveKind.Normal || next.Kind == BBMoveKind.CommandNormal)
            {
                // A-repeat rule (5A / 2A can chain into themselves or each other up to maxARepeats)
                if (current.Button == BBButton.A && next.Button == BBButton.A)
                {
                    if (chain.aRepeatCount >= maxARepeats) return false;
                    return true;
                }

                // Check once-per-chain mask
                if (nextMoveIndex >= 0 && chain.IsUsed(nextMoveIndex))
                {
                    return false;
                }

                // Standard Revolver Action hierarchy: A -> B -> C -> D
                if (next.Button > current.Button)
                {
                    return true;
                }

                // Check explicit gatling overrides authored on the move
                if (current.GatlingInto != null && current.GatlingInto.Length > 0)
                {
                    for (int i = 0; i < current.GatlingInto.Length; i++)
                    {
                        if (current.GatlingInto[i] == next.MoveId)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the current move can be jump canceled on hit.
        /// </summary>
        public static bool CanJumpCancel(
            in BBChainState chain,
            IBBCancelableMove current,
            bool onHit,
            bool onBlock)
        {
            if (current == null) return false;
            if (chain.hasJumpCanceled) return false;

            if (onHit && current.JumpCancelOnHit) return true;
            if (onBlock && current.JumpCancelOnBlock) return true;

            return false;
        }

        /// <summary>
        /// Checks whether Rapid Cancel can be performed (50 Heat, contact required, once per combo).
        /// </summary>
        public static bool CanRapidCancel(
            IBBCancelableMove current,
            bool madeContact,
            float currentHeat,
            int rcUsedInCombo,
            bool allowWhiffRC = false)
        {
            if (current == null) return false;
            if (!current.RapidCancelable) return false;
            if (currentHeat < 50f) return false;
            if (rcUsedInCombo >= 1) return false;

            // Cannot RC during Distortion Drive or Astral Heat
            if (current.Kind == BBMoveKind.DistortionDrive || current.Kind == BBMoveKind.AstralHeat)
            {
                return false;
            }

            if (!madeContact && !allowWhiffRC) return false;

            return true;
        }
    }
}
