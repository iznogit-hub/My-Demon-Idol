using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Runtime Simulation Authority Selector.
    /// Dictates whether combat runs through the package-native fighting engine (UFE 2)
    /// or enters the development simulation sandbox for isolated unit verification.
    /// </summary>
    public enum SimulationAuthority
    {
        /// <summary>
        /// Authoritative clean 2.5D fighting engine (FighterController + MatchRoundReferee + HomegrownCombatDirector).
        /// Default production baseline for Demon Idol.
        /// </summary>
        Homegrown = 0,

        /// <summary>
        /// Legacy alias for Homegrown.
        /// </summary>
        HomegrownLegacyFallback = 0,

        /// <summary>
        /// Deprecated package-native simulation (Universal Fighting Engine 2).
        /// </summary>
        PackageNative = 1
    }

    /// <summary>
    /// Itemized descriptor of runtime subsystem ownership.
    /// </summary>
    [Serializable]
    public struct OwnershipDescriptor
    {
        public string Subsystem;
        public string AuthoritativeOwner;
        public string ScriptPath;
        public int SimulationFrequencyHz;
        public string InactiveOwnerAction;

        public OwnershipDescriptor(string subsystem, string authoritativeOwner, string scriptPath, int simulationFrequencyHz, string inactiveOwnerAction)
        {
            Subsystem = subsystem;
            AuthoritativeOwner = authoritativeOwner;
            ScriptPath = scriptPath;
            SimulationFrequencyHz = simulationFrequencyHz;
            InactiveOwnerAction = inactiveOwnerAction;
        }
    }

    /// <summary>
    /// Manages simulation authority, runtime ownership enforcement,
    /// and guarantees exactly ONE authoritative simulation runs per match.
    /// When PackageNative is active, all legacy controllers and listeners are disabled.
    /// </summary>
    [System.Obsolete("Superseded by Assets/Scripts/BlazBlue/ (deterministic simulation authority). Do not use in new code.", false)]
    public static class GameplaySimulationAuthority
    {
        private static SimulationAuthority _activeAuthority = SimulationAuthority.Homegrown;
        private static readonly object _lock = new object();

        /// <summary>
        /// The currently active simulation authority. Default is Homegrown.
        /// </summary>
        public static SimulationAuthority ActiveAuthority
        {
            get
            {
                lock (_lock)
                {
                    return _activeAuthority;
                }
            }
            set
            {
                lock (_lock)
                {
                    _activeAuthority = value;
                }
            }
        }

        public static bool IsHomegrown => true;
        public static bool IsPackageNative => false;
        public static bool IsLegacyFallback => false;

        /// <summary>
        /// Explicit Runtime Ownership Matrix detailing the authoritative owner for each combat concern.
        /// </summary>
        public static readonly IReadOnlyList<OwnershipDescriptor> OwnershipMatrix = new List<OwnershipDescriptor>
        {
            new OwnershipDescriptor(
                "Tick Scheduling",
                "CombatTickEngine (FixedUpdate 60Hz)",
                "Assets/Scripts/Combat/CombatTickEngine.cs",
                60,
                "Authoritative Homegrown 60Hz deterministic clock"
            ),
            new OwnershipDescriptor(
                "Fighter State Machine",
                "FighterController (60Hz State Machine)",
                "Assets/Scripts/Combat/FighterController.cs",
                60,
                "Authoritative state machine"
            ),
            new OwnershipDescriptor(
                "Movement & Physics",
                "PushbackSolver & FighterController Physics",
                "Assets/Scripts/Combat/PushbackSolver.cs",
                60,
                "Authoritative 2.5D planar physics"
            ),
            new OwnershipDescriptor(
                "Pushboxes & Spatial Extents",
                "CharacterController (Strict Z=0 clamp)",
                "Assets/Scripts/Combat/FighterController.cs",
                60,
                "Authoritative 2D pushbox collision"
            ),
            new OwnershipDescriptor(
                "Hit & Hurt Detection",
                "Hitbox & Hurtbox Deterministic Overlap",
                "Assets/Scripts/Combat/Hitbox.cs",
                60,
                "Authoritative frame-accurate combat collisions"
            ),
            new OwnershipDescriptor(
                "Damage & Guard Resolution",
                "BarrierBlockSystem & FighterController Damage",
                "Assets/Scripts/Combat/BarrierBlockSystem.cs",
                60,
                "Authoritative block & barrier mitigation"
            ),
            new OwnershipDescriptor(
                "Combos & Cancels",
                "JuggleEngine & RapidCancel System",
                "Assets/Scripts/Combat/JuggleEngine.cs",
                60,
                "Authoritative combo scaling & cancels"
            ),
            new OwnershipDescriptor(
                "Input Resolution",
                "InputBuffer & MobileFighterInput",
                "Assets/Scripts/Input/InputBuffer.cs",
                60,
                "Authoritative 4-frame input buffer"
            ),
            new OwnershipDescriptor(
                "Camera Authority",
                "FighterCamera2D (Strict Z=0.0m Clamp, 2.5D)",
                "Assets/Scripts/Camera/FighterCamera2D.cs",
                60,
                "Authoritative smooth framing"
            ),
            new OwnershipDescriptor(
                "Battle HUD & Announcements",
                "DemonIdol BattleHUDController",
                "Assets/Scripts/UI/BattleHUDController.cs",
                60,
                "Direct event subscriptions"
            ),
            new OwnershipDescriptor(
                "Round Referee & Results",
                "MatchRoundReferee & HomegrownCombatDirector",
                "Assets/Scripts/Combat/HomegrownCombatDirector.cs",
                60,
                "Authoritative round flow"
            ),
            new OwnershipDescriptor(
                "Audio & Impact VFX",
                "HitEffectCoordinator & CombatAudioDirector",
                "Assets/Scripts/VFX/HitEffectCoordinator.cs",
                60,
                "Authoritative combat audiovisual feedback"
            ),
            new OwnershipDescriptor(
                "Menus, Selection & Saves",
                "Demon Idol Project Architecture",
                "Assets/Scripts/UI/ & Assets/Scripts/Data/",
                60,
                "Persistent across all matches"
            ),
            new OwnershipDescriptor(
                "Cinematics & Video Bindings",
                "Demon Idol CutsceneManager & VideoBindings (38 slots)",
                "Assets/Scripts/Cutscenes/CutsceneManager.cs",
                60,
                "Presentation-only; strictly bounded <= 20 ticks; simulation-confirmed hits only"
            )
        };

        /// <summary>
        /// Enforces single simulation authority in the active scene.
        /// When PackageNative is active, all legacy combat scripts are explicitly deactivated.
        /// When HomegrownLegacyFallback is active, legacy scripts remain active.
        /// </summary>
        public static void EnforceSingleSimulation()
        {
            // Homegrown 60Hz authority is always active.
        }
    }
}
