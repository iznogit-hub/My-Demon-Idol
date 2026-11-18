using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Pure zero-allocation static collision solver for the BlazBlue 2D fight plane (Z=0).
    /// Enforces strict AABB overlap, multi-hit masking, invulnerability filtering,
    /// and deterministic pushbox separation.
    /// </summary>
    public static class BBCollision
    {
        /// <summary>
        /// Strict AABB overlap test on the Z=0 plane.
        /// Touching edges do not hit (strict inequality).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlap(in BBWorldBox a, in BBWorldBox b)
        {
            return a.minX < b.maxX && b.minX < a.maxX && a.minY < b.maxY && b.minY < a.maxY;
        }

        /// <summary>
        /// Offensive strike pass checking attacker Hit boxes against defender Hurt boxes.
        /// Filters out previously landed hitIndices via BBHitMask and defender invulnerability.
        /// Appends contacts into the preallocated results array. Zero allocation.
        /// </summary>
        public static int StrikePass(
            BBBoxBuffer attacker,
            BBBoxBuffer defender,
            ref BBHitMask alreadyHit,
            BBContact[] results,
            ref int contactCount,
            int attackerId,
            int defenderId,
            BBInvuln defenderInvuln = BBInvuln.None)
        {
            if (attacker == null || defender == null || results == null) return 0;
            int initialCount = contactCount;

            // Defender completely strike invulnerable
            bool fullStrikeInvuln = (defenderInvuln & BBInvuln.Strike) != 0;

            for (int a = 0; a < attacker.count; a++)
            {
                ref BBWorldBox hitBox = ref attacker.boxes[a];
                if (hitBox.kind != BBBoxKind.Hit) continue;

                // Multi-hit check: has this hitIndex already landed during this attack instance?
                if (alreadyHit.HasHit(hitBox.hitIndex)) continue;

                // Invulnerability attribute check
                if (fullStrikeInvuln) continue;
                if ((hitBox.attribute & BBAttribute.Head) != 0 && (defenderInvuln & BBInvuln.Head) != 0) continue;
                if ((hitBox.attribute & BBAttribute.Body) != 0 && (defenderInvuln & BBInvuln.Body) != 0) continue;
                if ((hitBox.attribute & BBAttribute.Foot) != 0 && (defenderInvuln & BBInvuln.Foot) != 0) continue;
                if ((hitBox.attribute & BBAttribute.Projectile) != 0 && (defenderInvuln & BBInvuln.Projectile) != 0) continue;

                // Test against defender hurtboxes
                for (int d = 0; d < defender.count; d++)
                {
                    ref BBWorldBox hurtBox = ref defender.boxes[d];
                    if (hurtBox.kind != BBBoxKind.Hurt) continue;

                    if (Overlap(in hitBox, in hurtBox))
                    {
                        // Mark hitIndex so this active phase cannot hit again
                        alreadyHit.MarkHit(hitBox.hitIndex);

                        // Calculate intersection midpoint as contact point
                        float cx = (Mathf.Max(hitBox.minX, hurtBox.minX) + Mathf.Min(hitBox.maxX, hurtBox.maxX)) * 0.5f;
                        float cy = (Mathf.Max(hitBox.minY, hurtBox.minY) + Mathf.Min(hitBox.maxY, hurtBox.maxY)) * 0.5f;

                        if (contactCount < results.Length)
                        {
                            results[contactCount++] = new BBContact(
                                BBContactType.Strike,
                                attackerId,
                                defenderId,
                                hitBox.hitIndex,
                                hitBox.attribute,
                                cx,
                                cy,
                                in hitBox,
                                in hurtBox
                            );
                        }

                        // One hitBox connects at most once per tick
                        break;
                    }
                }
            }

            return contactCount - initialCount;
        }

        /// <summary>
        /// Throw pass checking attacker Throw boxes against defender ThrowHurt boxes.
        /// Zero allocation.
        /// </summary>
        public static int ThrowPass(
            BBBoxBuffer attacker,
            BBBoxBuffer defender,
            BBContact[] results,
            ref int contactCount,
            int attackerId,
            int defenderId,
            BBInvuln defenderInvuln = BBInvuln.None)
        {
            if (attacker == null || defender == null || results == null) return 0;
            if ((defenderInvuln & BBInvuln.Throw) != 0) return 0;

            int initialCount = contactCount;

            for (int a = 0; a < attacker.count; a++)
            {
                ref BBWorldBox throwBox = ref attacker.boxes[a];
                if (throwBox.kind != BBBoxKind.Throw) continue;

                for (int d = 0; d < defender.count; d++)
                {
                    ref BBWorldBox throwHurtBox = ref defender.boxes[d];
                    if (throwHurtBox.kind != BBBoxKind.ThrowHurt) continue;

                    if (Overlap(in throwBox, in throwHurtBox))
                    {
                        float cx = (Mathf.Max(throwBox.minX, throwHurtBox.minX) + Mathf.Min(throwBox.maxX, throwHurtBox.maxX)) * 0.5f;
                        float cy = (Mathf.Max(throwBox.minY, throwHurtBox.minY) + Mathf.Min(throwBox.maxY, throwHurtBox.maxY)) * 0.5f;

                        if (contactCount < results.Length)
                        {
                            results[contactCount++] = new BBContact(
                                BBContactType.Throw,
                                attackerId,
                                defenderId,
                                throwBox.hitIndex,
                                BBAttribute.Throw,
                                cx,
                                cy,
                                in throwBox,
                                in throwHurtBox
                            );
                        }

                        break;
                    }
                }
            }

            return contactCount - initialCount;
        }

        /// <summary>
        /// Deterministic pushbox separation on the Z=0 plane.
        /// 50/50 overlap split; 100% push if opponent is cornered against stage bounds;
        /// grounded vs airborne pushes the airborne fighter;
        /// exact center tie breaks deterministically by pushing P1 to the left.
        /// </summary>
        public static void ResolvePush(
            ref float p1x,
            ref float p2x,
            in BBWorldBox p1Push,
            in BBWorldBox p2Push,
            float arenaMin,
            float arenaMax,
            bool p1Grounded = true,
            bool p2Grounded = true)
        {
            // If either box has zero width/height or they don't overlap, no push needed
            if (!Overlap(in p1Push, in p2Push)) return;

            float overlapX = Mathf.Min(p1Push.maxX, p2Push.maxX) - Mathf.Max(p1Push.minX, p2Push.minX);
            if (overlapX <= 0f) return;

            float p1Center = (p1Push.minX + p1Push.maxX) * 0.5f;
            float p2Center = (p2Push.minX + p2Push.maxX) * 0.5f;

            // Deterministic tie-break: if centers match exactly, P1 is on the left
            bool p1IsLeft;
            if (Mathf.Abs(p1Center - p2Center) < 0.0001f)
            {
                p1IsLeft = true;
            }
            else
            {
                p1IsLeft = p1Center < p2Center;
            }

            // Check cornered status (within 1mm of arena boundaries)
            bool leftCornered = p1IsLeft ? (p1Push.minX <= arenaMin + 0.001f) : (p2Push.minX <= arenaMin + 0.001f);
            bool rightCornered = p1IsLeft ? (p2Push.maxX >= arenaMax - 0.001f) : (p1Push.maxX >= arenaMax - 0.001f);

            float pushLeft = 0f;
            float pushRight = 0f;

            // Ground vs Air rule: airborne fighter is pushed 100%
            if (p1Grounded && !p2Grounded)
            {
                // P2 is airborne: P2 takes full displacement
                if (p1IsLeft) pushRight = overlapX;
                else pushLeft = overlapX;
            }
            else if (!p1Grounded && p2Grounded)
            {
                // P1 is airborne: P1 takes full displacement
                if (p1IsLeft) pushLeft = overlapX;
                else pushRight = overlapX;
            }
            else
            {
                // Both grounded or both airborne
                if (leftCornered && !rightCornered)
                {
                    // Left fighter is pinned to the wall, right fighter absorbs 100%
                    pushRight = overlapX;
                }
                else if (rightCornered && !leftCornered)
                {
                    // Right fighter is pinned to the wall, left fighter absorbs 100%
                    pushLeft = overlapX;
                }
                else
                {
                    // Symmetric 50/50 split
                    pushLeft = overlapX * 0.5f;
                    pushRight = overlapX * 0.5f;
                }
            }

            if (p1IsLeft)
            {
                p1x -= pushLeft;
                p2x += pushRight;
            }
            else
            {
                p2x -= pushLeft;
                p1x += pushRight;
            }

            // Clamp fighter positions so their pushboxes remain inside arena bounds
            float p1HalfW = p1Push.HalfWidth;
            float p2HalfW = p2Push.HalfWidth;

            p1x = Mathf.Clamp(p1x, arenaMin + p1HalfW, arenaMax - p1HalfW);
            p2x = Mathf.Clamp(p2x, arenaMin + p2HalfW, arenaMax - p2HalfW);

            // Secondary separation check if edge clamp re-introduced overlap
            if (p1IsLeft)
            {
                float p1Right = p1x + p1HalfW;
                float p2Left = p2x - p2HalfW;
                if (p1Right > p2Left)
                {
                    float residual = p1Right - p2Left;
                    if (p2x + p2HalfW + residual <= arenaMax)
                    {
                        p2x += residual;
                    }
                    else if (p1x - p1HalfW - residual >= arenaMin)
                    {
                        p1x -= residual;
                    }
                }
            }
            else
            {
                float p2Right = p2x + p2HalfW;
                float p1Left = p1x - p1HalfW;
                if (p2Right > p1Left)
                {
                    float residual = p2Right - p1Left;
                    if (p1x + p1HalfW + residual <= arenaMax)
                    {
                        p1x += residual;
                    }
                    else if (p2x - p2HalfW - residual >= arenaMin)
                    {
                        p2x -= residual;
                    }
                }
            }
        }
    }
}
