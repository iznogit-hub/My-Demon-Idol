using System;

namespace DemonIdol.BlazBlue
{
    public enum BBCommandType
    {
        None = 0,

        // System Macros & Chords (Highest Priority)
        BurstOrOverdrive,   // A+B+C+D
        RapidCancel,        // A+B+C
        CounterAssault,     // 6+A+B (in blockstun)
        Barrier,            // A+B (held while guarding)
        CrushTrigger,       // A+B (neutral)
        Throw,              // B+C (or 4BC, 6BC, j.BC)

        // Super & Cinematic Specials
        AstralHeat,         // Character specific or 222D
        Distortion632146,   // 632146 + Button
        Distortion236236,   // 236236 + Button
        Distortion214214,   // 214214 + Button

        // Special Motions
        Special360,         // 360 + Button
        Special623,         // 623 + Button (DP)
        Special421,         // 421 + Button (Reverse DP)
        Special41236,       // 41236 + Button (HCF)
        Special63214,       // 63214 + Button (HCB)
        Special236,         // 236 + Button (QCF)
        Special214,         // 214 + Button (QCB)
        Special22,          // 22 + Button (Down-Down)
        SpecialChargeBF,     // [4]6 + Button
        SpecialChargeDU,     // [2]8 + Button

        // Locomotion
        ForwardDash,        // 66
        Backdash,           // 44
        SuperJump,          // 28 / 29 / 27
        AirDashForward,     // j.66
        AirDashBack,        // j.44

        // Command Normals
        Normal6A,
        Normal2A,
        Normal6B,
        Normal2B,
        Normal6C,
        Normal2C,
        Normal3C,
        Normal6D,
        Normal2D,
        Normal4D,

        // Neutral Normals
        Normal5A,
        Normal5B,
        Normal5C,
        Normal5D,

        // Airborne Normals
        NormalAirA,
        NormalAirB,
        NormalAirC,
        NormalAirD
    }

    public struct BBEvaluatedCommand
    {
        public BBCommandType commandType;
        public BBButton button;
        public sbyte facingNumpad;
        public int tick;
        public bool isNegativeEdge;

        public bool IsValid => commandType != BBCommandType.None;
    }

    /// <summary>
    /// Interprets buffered raw inputs and directional edges into prioritized BlazBlue combat commands.
    /// Strict zero-allocation execution.
    /// </summary>
    public class BBCommandInterpreter
    {
        private readonly BBMotionEdge[] _edgeBuffer = new BBMotionEdge[BBInputHistory.CAPACITY];

        public BBEvaluatedCommand Evaluate(
            BBInputHistory history,
            int currentTick,
            int facingDirection,
            bool isGrounded,
            bool isInAttack,
            bool isInBlockstun,
            bool isGuarding)
        {
            var result = new BBEvaluatedCommand();
            if (history == null) return result;

            int edgeCount = history.CopyEdges(_edgeBuffer, BBInputHistory.CAPACITY);
            sbyte currentFacingNumpad = BBInputFrame.WorldToFacingNumpad(history.LatestFrame.numpad, facingDirection);

            // ==========================================
            // 1. SYSTEM MACROS (A+B+C+D, A+B+C, A+B, B+C)
            // ==========================================

            // A+B+C+D: Burst / Overdrive
            if (history.IsQuadChordBuffered(chordWindowTicks: 3, bufferTicks: 5))
            {
                result.commandType = BBCommandType.BurstOrOverdrive;
                result.button = BBButton.A | BBButton.B | BBButton.C | BBButton.D;
                result.facingNumpad = currentFacingNumpad;
                result.tick = currentTick;
                return result;
            }

            // A+B+C: Rapid Cancel (only during an attack)
            if (isInAttack && history.IsTripleChordBuffered(BBButton.A, BBButton.B, BBButton.C, chordWindowTicks: 3, bufferTicks: 5))
            {
                result.commandType = BBCommandType.RapidCancel;
                result.button = BBButton.A | BBButton.B | BBButton.C;
                result.facingNumpad = currentFacingNumpad;
                result.tick = currentTick;
                return result;
            }

            // 6+A+B: Counter Assault (in blockstun)
            if (isInBlockstun && currentFacingNumpad == 6 && history.IsChordBuffered(BBButton.A, BBButton.B, chordWindowTicks: 3, bufferTicks: 5))
            {
                result.commandType = BBCommandType.CounterAssault;
                result.button = BBButton.A | BBButton.B;
                result.facingNumpad = currentFacingNumpad;
                result.tick = currentTick;
                return result;
            }

            // A+B: Barrier (while guarding) or Crush Trigger (neutral grounded)
            if (history.IsChordBuffered(BBButton.A, BBButton.B, chordWindowTicks: 3, bufferTicks: 5))
            {
                result.commandType = isGuarding ? BBCommandType.Barrier : BBCommandType.CrushTrigger;
                result.button = BBButton.A | BBButton.B;
                result.facingNumpad = currentFacingNumpad;
                result.tick = currentTick;
                return result;
            }

            // B+C: Throw (neutral, 4, 6, or air)
            if (history.IsChordBuffered(BBButton.B, BBButton.C, chordWindowTicks: 3, bufferTicks: 5))
            {
                result.commandType = BBCommandType.Throw;
                result.button = BBButton.B | BBButton.C;
                result.facingNumpad = currentFacingNumpad;
                result.tick = currentTick;
                return result;
            }

            // ==========================================
            // 2. MOTION SPECIALS & DISTORTIONS
            // ==========================================
            // Evaluate newest buffered button (or negative edge for specials)
            BBButton activeBtn = BBButton.None;
            bool isNegEdge = false;

            // Check A, B, C, D in priority order
            BBButton[] checkButtons = { BBButton.D, BBButton.C, BBButton.B, BBButton.A };
            foreach (var b in checkButtons)
            {
                if (history.IsButtonBuffered(b, bufferTicks: 5))
                {
                    activeBtn = b;
                    isNegEdge = false;
                    break;
                }
                if (history.IsNegativeEdgeBuffered(b, negativeEdgeTicks: 3))
                {
                    activeBtn = b;
                    isNegEdge = true;
                    break;
                }
            }

            if (activeBtn != BBButton.None)
            {
                // Distortion 632146
                if (BBMotionMatcher.Is632146(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Distortion632146, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // Distortion 236236
                if (BBMotionMatcher.Is236236(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Distortion236236, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // Distortion 214214
                if (BBMotionMatcher.Is214214(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Distortion214214, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 360 Command Grab
                if (BBMotionMatcher.Is360(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special360, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 623 Dragon Punch
                if (BBMotionMatcher.Is623(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special623, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 421 Reverse Dragon Punch
                if (BBMotionMatcher.Is421(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special421, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 41236 Half-Circle Forward
                if (BBMotionMatcher.Is41236(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special41236, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 63214 Half-Circle Back
                if (BBMotionMatcher.Is63214(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special63214, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 236 Quarter-Circle Forward
                if (BBMotionMatcher.Is236(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special236, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 214 Quarter-Circle Back
                if (BBMotionMatcher.Is214(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special214, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // 22 Down-Down
                if (BBMotionMatcher.Is22(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.Special22, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // [4]6 Charge Back-Forward
                if (BBMotionMatcher.IsChargeBackForward(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.SpecialChargeBF, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // [2]8 Charge Down-Up
                if (BBMotionMatcher.IsChargeDownUp(_edgeBuffer, edgeCount, currentTick))
                {
                    return CreateCommand(BBCommandType.SpecialChargeDU, activeBtn, currentFacingNumpad, currentTick, isNegEdge);
                }

                // Negative-edge only triggers specials above, not normals
                if (isNegEdge) return result;

                // ==========================================
                // 3. NORMALS & COMMAND NORMALS
                // ==========================================
                if (!isGrounded)
                {
                    switch (activeBtn)
                    {
                        case BBButton.A: return CreateCommand(BBCommandType.NormalAirA, activeBtn, currentFacingNumpad, currentTick, false);
                        case BBButton.B: return CreateCommand(BBCommandType.NormalAirB, activeBtn, currentFacingNumpad, currentTick, false);
                        case BBButton.C: return CreateCommand(BBCommandType.NormalAirC, activeBtn, currentFacingNumpad, currentTick, false);
                        case BBButton.D: return CreateCommand(BBCommandType.NormalAirD, activeBtn, currentFacingNumpad, currentTick, false);
                    }
                }
                else
                {
                    // Grounded command normals
                    if (currentFacingNumpad == 6)
                    {
                        switch (activeBtn)
                        {
                            case BBButton.A: return CreateCommand(BBCommandType.Normal6A, activeBtn, currentFacingNumpad, currentTick, false);
                            case BBButton.B: return CreateCommand(BBCommandType.Normal6B, activeBtn, currentFacingNumpad, currentTick, false);
                            case BBButton.C: return CreateCommand(BBCommandType.Normal6C, activeBtn, currentFacingNumpad, currentTick, false);
                            case BBButton.D: return CreateCommand(BBCommandType.Normal6D, activeBtn, currentFacingNumpad, currentTick, false);
                        }
                    }
                    else if (currentFacingNumpad == 2 || currentFacingNumpad == 1)
                    {
                        switch (activeBtn)
                        {
                            case BBButton.A: return CreateCommand(BBCommandType.Normal2A, activeBtn, currentFacingNumpad, currentTick, false);
                            case BBButton.B: return CreateCommand(BBCommandType.Normal2B, activeBtn, currentFacingNumpad, currentTick, false);
                            case BBButton.C: return CreateCommand(BBCommandType.Normal2C, activeBtn, currentFacingNumpad, currentTick, false);
                            case BBButton.D: return CreateCommand(BBCommandType.Normal2D, activeBtn, currentFacingNumpad, currentTick, false);
                        }
                    }
                    else if (currentFacingNumpad == 3)
                    {
                        if (activeBtn == BBButton.C)
                            return CreateCommand(BBCommandType.Normal3C, activeBtn, currentFacingNumpad, currentTick, false);
                    }
                    else if (currentFacingNumpad == 4)
                    {
                        if (activeBtn == BBButton.D)
                            return CreateCommand(BBCommandType.Normal4D, activeBtn, currentFacingNumpad, currentTick, false);
                    }

                    // Standing neutral normals
                    switch (activeBtn)
                    {
                        case BBButton.A: return CreateCommand(BBCommandType.Normal5A, activeBtn, currentFacingNumpad, currentTick, false);
                        case BBButton.B: return CreateCommand(BBCommandType.Normal5B, activeBtn, currentFacingNumpad, currentTick, false);
                        case BBButton.C: return CreateCommand(BBCommandType.Normal5C, activeBtn, currentFacingNumpad, currentTick, false);
                        case BBButton.D: return CreateCommand(BBCommandType.Normal5D, activeBtn, currentFacingNumpad, currentTick, false);
                    }
                }
            }

            // ==========================================
            // 4. LOCOMOTION / DASHES (No button pressed)
            // ==========================================
            if (isGrounded)
            {
                if (BBMotionMatcher.IsDoubleTap(_edgeBuffer, edgeCount, currentTick, 6))
                {
                    return CreateCommand(BBCommandType.ForwardDash, BBButton.None, currentFacingNumpad, currentTick, false);
                }
                if (BBMotionMatcher.IsDoubleTap(_edgeBuffer, edgeCount, currentTick, 4))
                {
                    return CreateCommand(BBCommandType.Backdash, BBButton.None, currentFacingNumpad, currentTick, false);
                }
            }
            else
            {
                if (BBMotionMatcher.IsDoubleTap(_edgeBuffer, edgeCount, currentTick, 6))
                {
                    return CreateCommand(BBCommandType.AirDashForward, BBButton.None, currentFacingNumpad, currentTick, false);
                }
                if (BBMotionMatcher.IsDoubleTap(_edgeBuffer, edgeCount, currentTick, 4))
                {
                    return CreateCommand(BBCommandType.AirDashBack, BBButton.None, currentFacingNumpad, currentTick, false);
                }
            }

            return result;
        }

        private static BBEvaluatedCommand CreateCommand(BBCommandType type, BBButton btn, sbyte numpad, int tick, bool isNeg)
        {
            return new BBEvaluatedCommand
            {
                commandType = type,
                button = btn,
                facingNumpad = numpad,
                tick = tick,
                isNegativeEdge = isNeg
            };
        }
    }
}
