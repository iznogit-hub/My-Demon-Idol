using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Cutscenes;
using DemonIdol.Audio;
using DemonIdol.BlazBlue;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Thin visual/rig presentation facade (Option A: Zero Prefab Breakage).
    /// Preserves existing prefab serialized fields and rig references while delegating all
    /// simulation authority (movement, attacks, HP, meters, boxes) to BBMatchSim & BBFighterBrain.
    /// Eliminates legacy dual-mode branching, CharacterController movement, and redundant attack states.
    /// </summary>
    public class FighterController : MonoBehaviour
    {
        [Header("Fighter Identity")]
        public string fighterName = "Kai";
        public CharacterDefinition appliedDefinition;
        public bool isPlayerControlled = true;
        public BBFighterBrain brain { get; private set; }

        [Header("Combat Stats")]
        public float maxHealth
        {
            get => brain != null ? brain.maxHealth : _maxHealth;
            set
            {
                if (brain != null && !Mathf.Approximately(brain.maxHealth, value))
                {
                    brain.maxHealth = value;
                }
                _maxHealth = value;
            }
        }
        [SerializeField] private float _maxHealth = 1000f;

        public float currentHealth
        {
            get => brain != null ? brain.health : _currentHealth;
            set
            {
                if (brain != null)
                {
                    if (!Mathf.Approximately(brain.health, value))
                    {
                        brain.SetAuthoritativeHealth(value);
                    }
                    _currentHealth = brain.health;
                }
                else
                {
                    _currentHealth = value;
                }
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
        [SerializeField] private float _currentHealth = 1000f;

        internal void SetHealthDirect(float value)
        {
            _currentHealth = value;
        }

        internal void SetMaxHealthDirect(float value)
        {
            _maxHealth = value;
        }

        public float maxSuperMeter = 100f;
        public float currentSuperMeter
        {
            get => brain != null ? brain.heat : _currentSuperMeter;
            set
            {
                if (brain != null)
                {
                    if (!Mathf.Approximately(brain.heat, value))
                    {
                        brain.heat = value;
                    }
                    _currentSuperMeter = brain.heat;
                }
                else
                {
                    _currentSuperMeter = value;
                }
                OnSuperMeterChanged?.Invoke(currentSuperMeter, maxSuperMeter);
            }
        }
        [SerializeField] private float _currentSuperMeter = 0f;

        [Header("Locomotion Tuning (Preserved for Prefab Serialization)")]
        public float forwardWalkSpeed = 3.6f;
        public float backwardWalkSpeed = 2.8f;
        public float jumpVerticalLaunch = 8.0f;
        public float jumpHorizontalSpeed = 3.2f;
        public float gravity = 20.0f;
        public float creepSpeed = 1.2f;
        public float forwardDashSpeed = 6.5f;
        public int forwardDashDurationTicks = 12;
        public float backdashSpeed = 7.0f;
        public int backdashDurationTicks = 14;
        public int backdashInvincibleTicks = 5;

        [Header("Jump Attack Physics")]
        public float jumpAttackVelocityX = 3.2f;
        public float jumpAttackVelocityY = 6.0f;

        [Header("Parry Window")]
        public int parryActiveTicks = 5;
        public int parryRecoveryTicks = 14;
        public int parryStunTicksOnAttacker = 18;

        [Header("MoveSet Contract")]
        public MoveSet moveSet;

        [Header("Attack Data Assets (Default Presets)")]
        public AttackData lightAttackData;
        public AttackData lowAttackData;
        public AttackData mediumAttackData;
        public AttackData heavyAttackData;
        public AttackData driveSkillData;
        public AttackData jumpAttackData;
        public AttackData domainSuperData;
        public AttackData throwAttackData;

        [Header("References")]
        public FighterController opponent;
        public Hitbox activeHitbox;
        public Animator animator;
        public float sidestepDistanceZ = 0.85f;
        public int sidestepDurationTicks = 14;

        // Current State
        public FighterState State
        {
            get
            {
                if (brain != null)
                {
                    switch (brain.bbState)
                    {
                        case BBState.Neutral: return FighterState.Idle;
                        case BBState.WalkForward:
                        case BBState.WalkBackward: return FighterState.WalkCreep;
                        case BBState.ForwardDash: return FighterState.ForwardDash;
                        case BBState.Backdash: return FighterState.Backdash;
                        case BBState.Crouching: return FighterState.Crouch;
                        case BBState.JumpStartup: return FighterState.Idle;
                        case BBState.Airborne: return FighterState.AirborneJuggle;
                        case BBState.AttackStartup: return FighterState.AttackStartup;
                        case BBState.AttackActive: return FighterState.AttackActive;
                        case BBState.AttackRecovery: return FighterState.AttackRecovery;
                        case BBState.Hitstun: return FighterState.Hitstun;
                        case BBState.Blockstun:
                        case BBState.BarrierBlockstun: return FighterState.Blockstun;
                        case BBState.DownWait:
                        case BBState.Thrown: return FighterState.Knockdown;
                        case BBState.Wakeup: return FighterState.Wakeup;
                        case BBState.Dead: return FighterState.Dead;
                        default: return FighterState.Idle;
                    }
                }
                return _state;
            }
            private set => _state = value;
        }
        private FighterState _state = FighterState.Idle;

        public AttackData CurrentAttack => null;
        public int FacingDirection { get; private set; } = 1;

        public bool IsActionable => (State == FighterState.Idle || State == FighterState.WalkCreep || State == FighterState.Crouch) && !IsBeingThrown && State != FighterState.Dead;
        public bool IsGrounded => brain != null ? brain.isGrounded : true;
        public bool IsGuardable => (IsActionable || State == FighterState.Blockstun) && !IsBeingThrown && IsGrounded;
        public bool IsThrowable => IsGrounded && IsActionable && !IsBeingThrown && RecoveryProtectionTicks <= 0;
        public bool IsCounterHitVulnerable => State == FighterState.AttackStartup || State == FighterState.AttackActive;
        public InvulnerabilityMask InvulnMask { get; set; } = InvulnerabilityMask.None;
        public int RemainingTicks => brain != null ? brain.stateTick : 0;
        public int RecoveryProtectionTicks => 0;
        public bool IsBeingThrown => brain != null && brain.bbState == BBState.Thrown;

        public float ProjectileMeter => 1.0f;
        public int MeleeComboStep => 0;
        public bool IsBlockedByDefender => false;
        public Vector2 MoveInput { get; private set; }

        // Subsystems (Presentation facades)
        private InputBuffer _inputBuf;
        public InputBuffer InputBuf => _inputBuf ??= new InputBuffer(leniencyTicks: 5);

        // Legacy reflection hook compatibility
        private CharacterController _charController;

        // Events
        public event Action<FighterState> OnStateChanged;
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnSuperMeterChanged;
        public event Action<HitResult, AttackData, Vector3> OnHitProcessed;
        public event Action OnRapidCanceled;
        public event Action OnDomainTriggered;

        public void RaiseHealthChanged() => OnHealthChanged?.Invoke(currentHealth, maxHealth);

        public void SetFacing(int direction)
        {
            FacingDirection = direction >= 0 ? 1 : -1;
            transform.rotation = Quaternion.Euler(0, FacingDirection == 1 ? 90f : -90f, 0);
        }

        public void SetMoveInput(Vector2 dir) => MoveInput = dir;

        public void ResetToIdle()
        {
            _state = FighterState.Idle;
            if (brain != null) brain.ResetForRound();
        }

        public void ReceiveAuthoritativeDamage(float amount)
        {
            if (brain != null) brain.ReceiveAuthoritativeDamage(amount);
            else
            {
                currentHealth = Mathf.Max(0f, currentHealth - amount);
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default) => ReceiveAuthoritativeDamage(amount);

        public HitResult ReceiveIncomingStrike(FighterController attacker, AttackData attack, Hurtbox hurtbox)
        {
            float dmg = attack != null ? attack.damage : 300f;
            ReceiveAuthoritativeDamage(dmg);
            return HitResult.Hit;
        }

        public void OnHitboxLanded(FighterController defender, AttackData attack, Hurtbox hurtbox, Vector3 contactPoint)
        {
            if (defender != null)
            {
                defender.ReceiveAuthoritativeDamage(attack != null ? attack.damage : 300f);
            }
        }

        public void ApplyHorizontalDisplacement(float amount)
        {
            transform.position = new Vector3(transform.position.x + amount, transform.position.y, transform.position.z);
        }

        public void AddSuperMeter(float amount) => currentSuperMeter = Mathf.Clamp(currentSuperMeter + amount, 0f, maxSuperMeter);
        public bool SpendSuperMeter(float amount)
        {
            if (currentSuperMeter >= amount)
            {
                currentSuperMeter -= amount;
                return true;
            }
            return false;
        }

        public void EnterKnockdown()
        {
            if (brain != null) brain.bbState = BBState.DownWait;
        }

        public void ForceCancelToNeutral() => ResetToIdle();
        public void SetState(FighterState newState, int ticks = 0) => _state = newState;

        public bool CanExecuteBurst() => brain != null ? brain.burst >= 100f : true;
        public bool ExecuteDefensiveBurst() => brain != null ? brain.TryExecuteDefensiveBurst(brain.stateTick) : false;

        public void ExecuteParry() {}
        public void ExecuteBackdash() {}
        public void ExecuteSidestep(float dir = 1f) {}
        public void ExecuteDomainCinematic(AttackData domainData = null, int freezeTicks = 20) {}

        public void ConfirmHit() {}
        public void SetBlockRecoveryBonusTicks(int bonusTicks) {}

        public void ApplyCharacterDefinition(CharacterDefinition def)
        {
            if (def == null) return;
            appliedDefinition = def;
            fighterName = !string.IsNullOrEmpty(def.displayName) ? def.displayName : def.id;
        }

        public void ApplyMoveSet(MoveSet ms)
        {
            if (ms == null) return;
            moveSet = ms;
        }

        public void CacheAnimatorParams()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void TriggerAnimation(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName) || animator == null) return;
            animator.SetTrigger(triggerName);
        }

        public void FindOpponentIfMissing()
        {
            if (opponent != null) return;
            var fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            foreach (var f in fighters)
            {
                if (f != this)
                {
                    opponent = f;
                    if (f.opponent == null) f.opponent = this;
                    break;
                }
            }
        }

        private void Awake()
        {
            brain = GetComponent<BBFighterBrain>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _charController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            if (brain == null) brain = GetComponent<BBFighterBrain>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            FindOpponentIfMissing();
        }

        // Stub for legacy reflection callers
        private void HandleCombatTick(int currentTick) {}
    }
}
