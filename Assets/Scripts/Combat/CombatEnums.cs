using System;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Unambiguous guard requirement for 2.5D combat:
    /// Normal: Blockable standing or crouching.
    /// Low: Blockable crouching only (down-back).
    /// Overhead: Blockable standing only (back).
    /// Throw: Unblockable by standard guard (capture rules).
    /// </summary>
    public enum GuardRequirement
    {
        Normal,
        Low,
        Overhead,
        Throw
    }

    /// <summary>
    /// Three-button combat input commands:
    /// Projectile (P), Melee (M), Special (S).
    /// Chords: Ultra/Super (P+M+S), CommandGrab (66 close), GrabBreak (P+M Tech).
    /// Maintains backward-compatible aliases for legacy Light/Heavy/Skill code.
    /// </summary>
    public enum ThreeButtonCommand
    {
        None = 0,
        Projectile = 1,
        Melee = 2,
        Special = 3,
        Ultra = 4,
        CommandGrab = 5,
        GrabBreak = 6,

        // Backward compatibility aliases
        Light = Projectile,
        Heavy = Melee,
        Skill = Special,
        Throw = CommandGrab,
        Super = Ultra,
        Burst = GrabBreak,
        Escape = GrabBreak
    }

    public enum FighterArchetype
    {
        Rushdown,
        Footsies,
        Grappler,
        Zoner
    }

    [Flags]
    public enum InvulnerabilityMask
    {
        None = 0,
        Strike = 1 << 0,
        Throw = 1 << 1,
        AirborneStrikeOnly = 1 << 2,
        All = Strike | Throw
    }

    public enum HitLevel
    {
        High,        // Duckable (whiffs crouching). Must be blocked standing. Plus or small minus on block.
        Mid,         // The core check. Hits crouching opponents! Must be blocked standing. Primary launchers.
        Low,         // Hits standing! Bypasses neutral auto-guard; must crouch-block, parry, or hopkick.
        SpecialMid,  // Blockable standing or crouching (like d/b+1 / dickjab).
        Unblockable, // Bypasses all standard guards. Must be interrupted, ducked, or low-crushed.
        Overhead,    // High arc overhead smash. Hits crouching opponents; MUST be blocked standing!
        Throw        // Tactical grab. Bypasses standard block; broken via tech or evaded by ducking/jumping.
    }

    public enum CrushProperty
    {
        None,
        HighCrush,   // Tech Crouch: Completely ignores High attacks during active crush window.
        LowCrush,    // Tech Jump / Hopkick: Completely airborne & ignores Low attacks during active crush window.
        PowerCrush   // Super Armor: Absorbs Highs & Mids during startup (takes chip/white damage); vulnerable to Lows & Parries.
    }

    public enum AttackType
    {
        Light,       // Fast startup (i10), low damage, safe on block, chain starter.
        Medium,      // Balanced mid-range poke (i14), hit confirm bridge.
        Heavy,       // Void Cleave / launcher (i18-i20), high damage, punishable on block.
        DriveSkill,  // Character Drive ability (Arcana 0 Card projectile / kinetic rush).
        JumpAttack,  // Low-crush overhead arc strike.
        DomainSuper, // 100% Super Overdrive cinematic finisher.
        Throw        // Tactical grab (5-frame startup, 1.1m range, tech window 6 ticks).
    }

    public enum FighterState
    {
        Idle,            // Auto-blocks High and Mid. Susceptible to Low.
        WalkCreep,       // Slow intentional micro-spacing.
        ForwardDash,     // Burst closing distance.
        Backdash,        // Quick evasive hop backwards (short invulnerability window).
        Crouch,          // Blocks Low, ducks High, crushed by Mid.
        AttackStartup,   // Wind-up frames before active hitbox.
        AttackActive,    // Hitbox enabled.
        AttackRecovery,  // Vulnerable cooldown frames (cancelable on hit/block confirm).
        ParryActive,     // Tight read window (5 frames) catching incoming strikes.
        ParryRecovery,   // Whiffed parry penalty frames.
        ParrySuccess,    // Attacker frozen in ParryStun; defender free to punish.
        Blockstun,       // Guarding hit, minus frames based on move data.
        Hitstun,         // Staggered by an attack, comboable.
        AirborneJuggle,  // Juggled in air; subject to gravity scaling and juggle physics.
        BoundTornado,    // Slammed onto ground / tailspin rebound (1 per combo).
        WallSplat,       // Pushed against stage boundary; slides down wall.
        WallBounce,      // Rebound off wall boundary for juggle extension.
        Knockdown,       // Knocked down on pavers.
        Wakeup,          // Ground recovery options (tech-roll, wakeup kicks).
        Sidestep,        // Tekken 3D Z-axis lateral evasion against linear attacks.
        BurstActive,     // BlazBlue Psychic Burst invincible shockwave.
        BarrierActive,   // BlazBlue Barrier pushblock guard.
        Dead             // Defeated.
    }

    public enum HitResult
    {
        Whiff,
        Hit,
        CounterHit,
        Blocked,
        BarrierBlocked,
        Parried,
        Crushed,
        Armored,
        BurstBreak,
        Teched,
        Grabbed
    }

    public enum BurstType
    {
        None,
        BlueDefensive,   // Combo breaker while in hitstun/juggle
        GoldOffensive    // Neutral launcher + instant 100% Super Meter
    }

    public enum WakeupAction
    {
        None,
        QuickTechRoll,        // Fast roll backwards to safety.
        SidestepTechRollUp,   // Tekken 3D roll into background (Z > 0).
        SidestepTechRollDown, // Tekken 3D roll into foreground (Z < 0).
        WakeupLowKick,        // High-crushing low kick from ground.
        WakeupMidKick,        // Safe mid check from ground.
        StandUpBack           // Safe retreat to neutral.
    }

    public enum CommandMotion
    {
        None,
        ForwardDash,         // f, f
        Backdash,            // b, b
        KoreanBackdashLoop,  // b, b, d/b, b
        QuarterCircleForward,// d, d/f, f
        DragonPunch          // f, d, d/f
    }
}
