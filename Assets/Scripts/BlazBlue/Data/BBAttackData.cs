using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    [CreateAssetMenu(fileName = "BB_Move", menuName = "DemonIdol/BlazBlue/Attack Data")]
    public class BBAttackData : AttackData
    {
        public string moveId = "5A";
        public BBButton button = BBButton.A;
        public BBMoveKind kind = BBMoveKind.Normal;
        public BBMotion motion = BBMotion.None;
        public bool airOk = false;
        public bool groundOk = true;
        public byte stance = 0;
        public int priority = 0;
        public BBGuard guard = BBGuard.Mid;
        public BBAttribute attribute = BBAttribute.Body;
        public bool fatalCounter = false;
        public bool hardKnockdown = false;
        public int untechableTicks = 18;
        public float airHitX = 1.5f;
        public float airHitY = 5f;
        public int landingRecovery = 4;
        public float minDamagePct = 0.08f;
        public float chipPct = 0f;
        public int heatCost = 0;
        public string[] gatlingInto = new string[0];
        public bool specialCancel = true;
        public bool ddCancel = true;
        public bool jumpCancelOnHit = false;
        public bool rapidCancelable = true;
        public string[] followUps = new string[0];
        public int boxTickCount = 0;
        public BBBoxSample[] boxSamples = new BBBoxSample[0];
        public string clipName = "";
        public AnimationClip clip;
        public int clipFirstFrame;
        public int clipLastFrame;
        public float hitLocalX = 0.6f;
        public float hitLocalY = 1.0f;
        public float hitLocalW = 0.4f;
        public float hitLocalH = 0.3f;

        public BBMoveDef ToDef(int id)
        {
            BBMoveDef m = new BBMoveDef();
            m.id = id;
            m.button = button;
            m.kind = kind;
            m.motion = motion;
            m.stance = stance;
            m.airOk = airOk;
            m.groundOk = groundOk;
            m.startup = startupFrames;
            m.active = activeFrames;
            m.recovery = recoveryFrames;
            m.damage = (int)damage;
            m.p1 = initialProration;
            m.p2 = comboDamageDecay;
            m.minPct = minDamagePct;
            m.hitstun = hitstunTicks;
            m.blockstun = blockstunTicks;
            m.guard = guard;
            m.fatalCounter = fatalCounter;
            m.jumpCancelOnHit = jumpCancelOnHit;
            m.specialCancel = specialCancel;
            m.ddCancel = ddCancel;
            m.rapidCancelable = rapidCancelable;
            m.heatCost = heatCost;
            m.chipPct = chipPct;
            m.priority = priority;
            m.hitX = hitLocalX;
            m.hitY = hitLocalY;
            m.hitW = hitLocalW;
            m.hitH = hitLocalH;
            return m;
        }
    }
}
