using System;

namespace DemonIdol.BlazBlue
{
    public enum ControlScheme
    {
        Classic3Button = 0,
        BlazBlue4Button = 1
    }

    [Flags]
    public enum BBButton : byte
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4,
        D = 8,
        Taunt = 16
    }

    public struct BBInputFrame
    {
        public byte held;
        public byte pressed;
        public byte released;
        public sbyte numpad;
    }

    [Flags]
    public enum BBInvuln : ushort
    {
        None = 0,
        Strike = 1,
        Throw = 2,
        Projectile = 4,
        Head = 8,
        Body = 16,
        Foot = 32,
        Armor1 = 64,
        ArmorMulti = 128,
        Guard = 256
    }

    [Flags]
    public enum BBAttribute : byte
    {
        None = 0,
        Head = 1,
        Body = 2,
        Foot = 4,
        Projectile = 8,
        Throw = 16
    }

    public enum BBGuard : byte
    {
        Mid,
        Low,
        Overhead,
        Unblockable,
        AirUnblockable,
        Throw
    }

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

    public enum BBDashType : byte
    {
        Run,
        Step,
        Hop
    }

    public enum DriveResourceKind : byte
    {
        None,
        Stocks,
        Gauge,
        Stance,
        Install
    }

    public enum BBCommandKind : byte
    {
        None,
        Normal,
        CommandNormal,
        Special,
        Distortion,
        Astral,
        Throw,
        Barrier,
        CrushTrigger,
        CounterAssault,
        RapidCancel,
        Burst,
        Overdrive
    }

    public struct BBTuning
    {
        public int bufferTicks;
        public int chordWindowTicks;
        public int negativeEdgeTicks;
        public int motionWindow236;
        public int motionWindowHalfCircle;
        public int motionWindow360;
        public int motionWindowDD;
        public int motionWindow22;
        public int chargeHoldTicks;
        public int chargeReleaseTicks;
        public float stickDeadzone;
        public bool allow323Shortcut;
        public int heatMax;
        public int costRC;
        public int costDD;
        public int costCA;
        public int costCT;
        public int costAstral;
        public float heatOnHitA;
        public float heatOnHitB;
        public float heatOnHitC;
        public float heatOnHitD;
        public float heatOnHitSpecial;
        public float heatPerDamageTaken;
        public float lowHpThreshold;
        public float lowHpRegenPerTick;
        public int barrierMax;
        public int barrierHoldCost;
        public int barrierRegen;
        public int barrierRegenDelay;
        public int dangerTicks;
        public float barrierHitCostPerDamage;
        public float barrierPushback;
        public float instantBarrierPushback;
        public float dangerDamageScale;
        public int instantBlockWindow;
        public int burstMax;
        public int burstRegenPerTick;
        public int burstInvulnTicks;
        public float burstRadius;
        public int odMinTicks;
        public int odMaxTicks;
        public float activeODPenalty;
        public bool allowWhiffRC;
        public int rcSlowTicks;
        public float rcSlowFactor;
        public int rcPerCombo;
        public int throwTechTicks;
        public int throwTechTicksCH;
        public int throwWhiffRecovery;
        public int throwProtectTicks;
        public float throwRange;
        public float sameMovePenalty;
        public float gravityScalePerHit;
        public float gravityCap;
        public int fatalCounterDecayShift;
        public float fatalCounterGravity;
        public float counterHitHitstunScale;
        public bool enableAstral;
        public bool negativePenalty;
        public float stylishDamageScale;
        public bool allowWhiffACancels;
        public int maxARepeats;

        public static BBTuning Default()
        {
            BBTuning t = new BBTuning();
            t.bufferTicks = 5;
            t.chordWindowTicks = 3;
            t.negativeEdgeTicks = 3;
            t.motionWindow236 = 22;
            t.motionWindowHalfCircle = 30;
            t.motionWindow360 = 40;
            t.motionWindowDD = 36;
            t.motionWindow22 = 16;
            t.chargeHoldTicks = 40;
            t.chargeReleaseTicks = 12;
            t.stickDeadzone = 0.20f;
            t.allow323Shortcut = true;
            t.heatMax = 100;
            t.costRC = 50;
            t.costDD = 50;
            t.costCA = 50;
            t.costCT = 25;
            t.costAstral = 100;
            t.heatOnHitA = 2f;
            t.heatOnHitB = 3f;
            t.heatOnHitC = 4f;
            t.heatOnHitD = 3f;
            t.heatOnHitSpecial = 5f;
            t.barrierMax = 1000;
            t.barrierHoldCost = 2;
            t.barrierRegen = 3;
            t.barrierRegenDelay = 30;
            t.dangerTicks = 300;
            t.barrierHitCostPerDamage = 0.5f;
            t.barrierPushback = 2.2f;
            t.instantBarrierPushback = 2.6f;
            t.dangerDamageScale = 1.25f;
            t.instantBlockWindow = 8;
            t.burstMax = 1000;
            t.burstRegenPerTick = 1;
            t.burstInvulnTicks = 26;
            t.burstRadius = 1.8f;
            t.odMinTicks = 180;
            t.odMaxTicks = 480;
            t.activeODPenalty = 0.6f;
            t.allowWhiffRC = false;
            t.rcSlowTicks = 20;
            t.rcSlowFactor = 0.75f;
            t.rcPerCombo = 1;
            t.throwTechTicks = 13;
            t.throwTechTicksCH = 6;
            t.throwWhiffRecovery = 24;
            t.throwProtectTicks = 6;
            t.throwRange = 1.0f;
            t.sameMovePenalty = 0.80f;
            t.gravityScalePerHit = 0.06f;
            t.gravityCap = 1.9f;
            t.fatalCounterDecayShift = 120;
            t.fatalCounterGravity = 0.8f;
            t.counterHitHitstunScale = 1.5f;
            t.heatPerDamageTaken = 0.03f;
            t.lowHpThreshold = 0.35f;
            t.lowHpRegenPerTick = 0.05f;
            t.enableAstral = false;
            t.negativePenalty = false;
            t.stylishDamageScale = 0.8f;
            t.allowWhiffACancels = true;
            t.maxARepeats = 3;
            return t;
        }
    }

    [Serializable]
    public struct BBBoxSample
    {
        public float x;
        public float y;
        public float w;
        public float h;
        public byte kind;
    }

    public struct BBAabb
    {
        public float x0, x1, y0, y1;
    }

    public struct BBMoveDef
    {
        public int id;
        public BBButton button;
        public BBMoveKind kind;
        public BBMotion motion;
        public byte stance;
        public bool airOk;
        public bool groundOk;
        public int startup;
        public int active;
        public int recovery;
        public int damage;
        public float p1;
        public float p2;
        public float minPct;
        public int hitstun;
        public int blockstun;
        public BBGuard guard;
        public bool fatalCounter;
        public bool jumpCancelOnHit;
        public bool specialCancel;
        public bool ddCancel;
        public bool rapidCancelable;
        public int heatCost;
        public float chipPct;
        public int gatling0, gatling1, gatling2, gatling3, gatling4, gatling5, gatling6, gatling7;
        public float hitX, hitY, hitW, hitH;
        public int priority;
    }
}
