using System;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Invulnerability types for BlazBlue combat.
    /// Bitmask allowing moves to ignore specific attack attributes.
    /// </summary>
    [Flags]
    public enum BBInvuln : ushort
    {
        None = 0,
        Strike = 1 << 0,       // Fully strike invulnerable
        Throw = 1 << 1,        // Throw invulnerable
        Projectile = 1 << 2,   // Projectile invulnerable
        Head = 1 << 3,         // High/airborne strike invulnerable ("head attribute")
        Body = 1 << 4,         // Mid strike invulnerable ("body attribute")
        Foot = 1 << 5,         // Low strike invulnerable ("foot attribute")
        Armor1 = 1 << 6,       // Absorbs 1 hit
        ArmorMulti = 1 << 7,   // Absorbs multiple hits
        Guard = 1 << 8         // Guard point: counters/parries on contact
    }

    /// <summary>
    /// Attack and hurtbox attribute classification in BlazBlue.
    /// Used by invulnerability windows to determine interaction.
    /// </summary>
    [Flags]
    public enum BBAttribute : byte
    {
        None = 0,
        Head = 1 << 0,        // Upper body / aerial attack
        Body = 1 << 1,        // Mid body attack
        Foot = 1 << 2,        // Low / sweep attack
        Projectile = 1 << 3,  // Projectile entity
        Throw = 1 << 4        // Normal or command grab
    }

    /// <summary>
    /// Guard requirement for incoming attacks.
    /// </summary>
    public enum BBGuard : byte
    {
        Mid,             // Blockable standing or crouching
        Low,             // Must be blocked crouching (2/1/3)
        Overhead,        // Must be blocked standing (4/7)
        Unblockable,     // Cannot be blocked normally
        AirUnblockable,  // Cannot be blocked while airborne unless barriering
        Throw            // Must be teched or avoided
    }

    /// <summary>
    /// Active window of invulnerability during a move.
    /// </summary>
    [Serializable]
    public struct BBInvulnWindow
    {
        public int startTick; // Relative to attack start
        public int endTick;   // Inclusive
        public BBInvuln mask;

        public BBInvulnWindow(int startTick, int endTick, BBInvuln mask)
        {
            this.startTick = startTick;
            this.endTick = endTick;
            this.mask = mask;
        }

        public bool IsActiveAt(int tick) => tick >= startTick && tick <= endTick;
    }

    /// <summary>
    /// Character dash archetype.
    /// </summary>
    public enum BBDashType : byte
    {
        Run,  // Continuous sprint forward (Akio, Aika, UnityChan)
        Step, // Fixed-distance burst dash
        Hop   // Short airborne hop dash
    }

    /// <summary>
    /// Kind of unique Drive resource tracked by HUD and Drive logic.
    /// </summary>
    public enum DriveResourceKind : byte
    {
        None,
        Stocks,  // Discrete charges / bullets / coins
        Gauge,   // Continuous meter (0..100)
        Stance,  // State machine index
        Install  // Timed transformation
    }

    /// <summary>
    /// Attack category in BlazBlue.
    /// </summary>
    public enum BBMoveKind : byte
    {
        Normal,
        CommandNormal,
        Special,
        DriveMove,
        DistortionDrive,
        AstralHeat,
        Throw,
        CommandThrow,
        System
    }

    /// <summary>
    /// Recognized stick motions.
    /// </summary>
    public enum BBMotion : byte
    {
        None,
        M236,
        M214,
        M623,
        M421,
        M41236,
        M63214,
        M236236,
        M214214,
        M632146,
        M360,
        M720,
        M22,
        Charge46,
        Charge28
    }

    /// <summary>
    /// Presentation and combat event types spawned at specific ticks.
    /// </summary>
    public enum BBEventType : byte
    {
        Sfx,
        Vfx,
        CameraShake,
        CameraZoom,
        SpawnProjectile,
        Voice,
        Hitstop,
        SuperFlash
    }

    /// <summary>
    /// Discrete states for the BlazBlue deterministic state machine.
    /// </summary>
    public enum BBState : byte
    {
        Neutral,
        Crouching,
        WalkForward,
        WalkBackward,
        ForwardDash,
        Backdash,
        JumpStartup,
        Airborne,
        AttackStartup,
        AttackActive,
        AttackRecovery,
        Hitstun,
        Blockstun,
        BarrierBlockstun,
        GuardCrush,
        Thrown,
        DownWait,
        Wakeup,
        Dead
    }
}
