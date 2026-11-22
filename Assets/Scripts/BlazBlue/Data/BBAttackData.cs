using System;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Specialized attack data container for BlazBlue 4-button combat.
    /// Subclasses existing AttackData for full compatibility with presentation and inspector tools
    /// while adding deterministic box tracks, gatlings, motions, and attributes.
    /// </summary>
    [CreateAssetMenu(fileName = "BB_Move", menuName = "DemonIdol/BlazBlue/Attack Data")]
    public class BBAttackData : AttackData, IBBCancelableMove
    {
        // IBBCancelableMove implementation
        public string MoveId => moveId;
        public BBButton Button => button;
        public BBMoveKind Kind => kind;
        public bool AirOk => airOk;
        public bool GroundOk => groundOk;
        public bool SpecialCancel => specialCancel;
        public bool DdCancel => ddCancel;
        public bool JumpCancelOnHit => jumpCancelOnHit;
        public bool JumpCancelOnBlock => jumpCancelOnBlock;
        public bool RapidCancelable => rapidCancelable;
        public string[] GatlingInto => gatlingInto;

        [Header("BB Identity")]
        public string moveId = "5A";              // Unique per character: 5A, 2A, j.A, 5B, 236A, 632146C, etc.
        public BBButton button = BBButton.A;
        public BBMoveKind kind = BBMoveKind.Normal;
        public BBMotion motion = BBMotion.None;
        public bool airOk = false;
        public bool groundOk = true;

        [Header("BB Attack Level")]
        public int attackLevel = -1; // 0 to 5. -1 = derived from kind/button

        public int AttackLevel
        {
            get
            {
                if (attackLevel >= 0) return Mathf.Clamp(attackLevel, 0, 5);
                if (kind == BBMoveKind.DistortionDrive) return 5;
                if (kind == BBMoveKind.Special) return 4;
                if (button == BBButton.C || button == BBButton.D) return 3;
                if (button == BBButton.B) return 2;
                if (moveId == "2A") return 0;
                return 1;
            }
        }

        [Header("BB Guard / Attribute / Invuln")]
        public BBGuard guard = BBGuard.Mid;
        public BBAttribute attribute = BBAttribute.Body;
        public BBInvulnWindow[] invuln = new BBInvulnWindow[0];
        public bool fatalCounter = false;
        public bool hardKnockdown = false;
        public bool otg = false;
        public bool groundBounce = false;
        public bool wallBounce = false;
        public bool wallStick = false;
        public bool groundSlide = false;
        public int untechableTicks = 18;            // Air hit untechable duration
        public int untechableTicksCH = 27;          // Counter-hit untechable duration
        public Vector2 airHitVelocity = new Vector2(1.5f, 5.0f);
        public int landingRecovery = 4;

        [Header("BB Damage")]
        [Range(0f, 1f)] public float minDamagePct = 0.08f;   // Guaranteed minimum damage percentage
        public float chipPct = 0f;
        public int heatGainOverride = -1;          // -1 = use BBSystemTuning default for kind/button
        public int heatCost = 0;                   // 50 for Distortion Drive, 100 for Astral Heat

        [Header("BB Cancels")]
        public string[] gatlingInto = new string[0];      // Revolver Action target moveIds
        public bool specialCancel = true;
        public bool ddCancel = true;
        public bool jumpCancelOnHit = false;
        public bool jumpCancelOnBlock = false;
        public bool rapidCancelable = true;
        public string[] followUps = new string[0];        // Rekka/stance follow-up moveIds
        public Vector2Int followUpWindow = new Vector2Int(0, 0);

        [Header("BB Boxes (per tick, baked)")]
        public BBBoxTrack boxes;                          // Baked per-tick hitboxes/hurtboxes
        public BBMotionTrack motionTrack;                 // Baked root displacement per tick

        [Header("BB Presentation")]
        public string clipName = "";                      // e.g. "Akio_BB_5A" or "Akio_5P"
        public AnimationClip clip;                        // Sub-clip asset inside FBX
        public int clipFirstFrame;
        public int clipLastFrame;
        public BBEvent[] events = new BBEvent[0];         // SFX/VFX/Camera events at tick N
    }

    [Serializable]
    public struct BBEvent
    {
        public int tick;
        public BBEventType type;
        public string key;
        public Vector3 offset;

        public BBEvent(int tick, BBEventType type, string key, Vector3 offset = default)
        {
            this.tick = tick;
            this.type = type;
            this.key = key;
            this.offset = offset;
        }
    }
}
