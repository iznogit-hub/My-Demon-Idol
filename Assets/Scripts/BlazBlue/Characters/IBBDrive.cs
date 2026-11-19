using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Interface for character-specific BlazBlue Drive mechanics.
    /// Each character implements this to govern D button logic, resource banking/spending,
    /// and Overdrive drive-enhancements.
    /// </summary>
    public interface IBBDrive
    {
        string DriveName { get; }
        DriveResourceKind ResourceKind { get; }
        int CurrentResource { get; }
        int MaxResource { get; }
        bool InOverdrive { get; set; }

        void OnMatchStart(BBFighterBrain brain);
        bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick);
        void TickDrive(BBFighterBrain brain, int tick);
        void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked);
        void OnKnockdown(BBFighterBrain brain);
        void ResetResource();
    }
}
