using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Pure, read-only presentation snapshot of a fighter's state at a specific simulation tick.
    /// Exposes all state needed by visual/audio/camera systems without exposing mutable simulation internals.
    /// </summary>
    [Serializable]
    public struct BBFighterSnapshot
    {
        public int tick;
        public int playerIndex; // 0 for P1, 1 for P2
        public Vector3 position;
        public int facingDirection; // +1 or -1
        public bool isGrounded;
        public BBState state;
        public string currentMoveId;
        public int stateTick;
        public int attackTick;
        public int hitstopRemaining;
        public int hitstunRemaining;
        public int blockstunRemaining;
        public float health;
        public float maxHealth;
        public float heat;
        public float barrier;
        public float burst;
        public bool inOverdrive;
        public int overdriveTicksRemaining;
        public int comboHitCount;
        public float comboDamage;
        public bool isGuarding;
        public bool isInvulnerable;

        public bool IsDead => health <= 0f || state == BBState.Dead;
        public bool IsActionable => state == BBState.Neutral || state == BBState.Crouching || state == BBState.WalkForward || state == BBState.WalkBackward;
        public bool InHitstun => state == BBState.Hitstun;
        public bool InBlockstun => state == BBState.Blockstun || state == BBState.BarrierBlockstun;
    }
}
