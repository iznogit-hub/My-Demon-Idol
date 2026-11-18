using UnityEngine;

namespace DemonIdol.Combat
{
    [CreateAssetMenu(fileName = "NewAttackData", menuName = "DemonIdol/Combat/AttackData")]
    public class AttackData : ScriptableObject
    {
        [Header("Identity")]
        public string attackName = "Light Attack";
        public AttackType attackType = AttackType.Light;
        public HitLevel hitLevel = HitLevel.Mid;
        public GuardRequirement guardRequirement = GuardRequirement.Normal;
        public bool isDuckableHigh = false; // Whiffs on crouching opponents if true (default false)
        public CrushProperty crush = CrushProperty.None;

        [Header("Crush & Armor Windows (in 60FPS Ticks)")]
        [Tooltip("Tick within attack startup when crush or armor activates")]
        public int crushStartTick = 4;
        [Tooltip("Tick within attack when crush or armor expires")]
        public int crushEndTick = 18;
        public bool hasArmor = false; // Power Crush: absorbs High/Mid hits during startup
        public bool isAirborneStrikeProtected = false; // Down+S Anti-Air Launcher protection against airborne strikes
        public int protectionStartTick = 5;
        public int protectionEndTick = 12;

        [Header("Frame Data (60 FPS Ticks - S/A/R Convention)")]
        [Tooltip("Startup-only frames before first active frame (e.g., age 0 to S-1)")]
        public int startupFrames = 5;
        [Tooltip("Frames the hitbox stays lethal (e.g., S to S+A-1)")]
        public int activeFrames = 3;
        [Tooltip("Cooldown frames after hitbox expires (e.g., S+A to S+A+R-1)")]
        public int recoveryFrames = 10;

        public int StartupOnlyTicks => startupFrames;
        public int ActiveTicks => activeFrames;
        public int RecoveryTicks => recoveryFrames;
        public int TotalTicks => startupFrames + activeFrames + recoveryFrames;

        [Header("Stun Ticks (excluding hitstop)")]
        public int hitstunTicks = 16;
        public int blockstunTicks = 10;
        public int knockdownDurationTicks = 36;

        [Header("Hitstop & Crunch (60 FPS Ticks)")]
        public int hitstopAttacker = 5;
        public int hitstopDefender = 5;
        public int hitstopOnBlock = 4;
        public int counterHitStopBonus = 4;

        [Header("Frame Advantage (+/- on 60 FPS)")]
        public int advantageOnBlock = -2;
        public int advantageOnHit = +4;
        public int advantageOnCounterHit = +8;

        public int CalculateHitAdvantage() => hitstunTicks - ((activeFrames - 1) + recoveryFrames);
        public int CalculateBlockAdvantage() => blockstunTicks - ((activeFrames - 1) + recoveryFrames);

        [Header("Damage & BlazBlue Proration")]
        public float damage = 40f;
        public float chipDamageOnBlock = 0f;
        [Tooltip("Initial combo scaling if combo starts with this move (e.g. 0.7 for light jabs, 1.0 for heavy launchers)")]
        [Range(0.1f, 1f)]
        public float initialProration = 0.85f;
        [Tooltip("Damage multiplier per subsequent hit in combo")]
        [Range(0.5f, 1f)]
        public float comboDamageDecay = 0.92f;

        [Header("Tekken Juggle & Physics")]
        public float pushbackOnHit = 1.2f;
        public float pushbackOnBlock = 0.8f;
        public bool isLauncher = false;
        public bool isBoundTornado = false; // Extends juggle once per combo
        public bool causesWallSplat = false;
        public bool causesWallBounce = false; // Rebounds off wall for aerial juggle
        public bool causesKnockdown = false;
        [Tooltip("Linear moves can be evaded via 3D Sidestep. Non-linear moves track sidesteps.")]
        public bool isLinear = true;
        [Tooltip("Tech window duration in ticks for throw grabs")]
        public int throwTechWindowTicks = 6;
        public Vector2 launchVelocity = Vector2.zero; // X = horizontal push, Y = vertical lift
        [Range(0f, 1f)]
        public float screenShakeTrauma = 0.1f;

        [Header("Cancels (Gatling & Rapid)")]
        public bool canRapidCancel = true; // Consumes 50% meter to reset recovery to neutral
        public AttackType[] gatlingCancels = new AttackType[]
        {
            AttackType.Medium,
            AttackType.Heavy,
            AttackType.DriveSkill
        };

        [Header("Animation Hook & Clip Reference")]
        public AnimationClip animationClip;
        public string animationTrigger = "LightAttack";

        [Header("Hitbox Spatial Geometry (1.25x Scaled)")]
        public Vector3 hitboxOffset = new Vector3(0.8f, 1.0f, 0f);
        public Vector3 hitboxSize = new Vector3(1.25f, 1.25f, 1.25f);

        [Header("Cancel Window & Archetype")]
        public Vector2Int cancelWindow = new Vector2Int(5, 12);
        public FighterArchetype archetype = FighterArchetype.Rushdown;

        public bool CanGatlingCancelInto(AttackType nextAttack)
        {
            if (gatlingCancels == null) return false;
            for (int i = 0; i < gatlingCancels.Length; i++)
            {
                if (gatlingCancels[i] == nextAttack) return true;
            }
            return false;
        }

        public bool IsCrushActive(int currentAttackTick)
        {
            if (crush == CrushProperty.None && !hasArmor) return false;
            return currentAttackTick >= crushStartTick && currentAttackTick <= crushEndTick;
        }
    }
}
