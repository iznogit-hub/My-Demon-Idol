using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Cutscenes;
using DemonIdol.Audio;

namespace DemonIdol.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public class FighterController : MonoBehaviour
    {
        [Header("Fighter Identity")]
        public string fighterName = "Kai";
        public bool isPlayerControlled = true;
        [System.NonSerialized] public bool externalSimulation = false;

        [Header("Combat Stats")]
        public float maxHealth = 1000f;
        public float currentHealth { get; set; } = 1000f;
        public float maxSuperMeter = 100f;
        public float currentSuperMeter { get; set; } = 0f;

        [Header("Locomotion Tuning (Vishaka 2.5D Starting Preset)")]
        public float forwardWalkSpeed = 3.6f;
        public float backwardWalkSpeed = 2.8f;
        public float jumpVerticalLaunch = 8.0f;
        public float jumpHorizontalSpeed = 3.2f;
        public float gravity = 20.0f;

        [Tooltip("Ultra-slow intentional creep walk for micro-spacing footsies")]
        public float creepSpeed = 1.2f;
        [Tooltip("Forward dash burst velocity")]
        public float forwardDashSpeed = 6.5f;
        public int forwardDashDurationTicks = 12;
        [Tooltip("Backdash evasive burst velocity")]
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

        [Header("MoveSet Contract (Three-Button Combat)")]
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
        private readonly HashSet<string> _animatorParams = new HashSet<string>();

        // Current State
        public FighterState State { get; private set; } = FighterState.Idle;
        public AttackData CurrentAttack { get; private set; }
        public int FacingDirection { get; private set; } = 1; // 1 = Facing Right, -1 = Facing Left

        // Explicit Action & Guard Properties (Section 3 Contract)
        public bool IsActionable => (State == FighterState.Idle || State == FighterState.WalkCreep || State == FighterState.Crouch) && !_isBeingThrown && State != FighterState.Dead;
        public bool IsGrounded => _charController != null ? _charController.isGrounded : true;
        public bool IsGuardable => (IsActionable || State == FighterState.Blockstun) && !_isBeingThrown && IsGrounded;
        public bool IsThrowable => IsGrounded && IsActionable && !_isBeingThrown && !_isInvincible && _recoveryProtectionTicks <= 0;
        public bool IsCounterHitVulnerable => State == FighterState.AttackStartup || State == FighterState.AttackActive;
        public InvulnerabilityMask InvulnMask { get; set; } = InvulnerabilityMask.None;
        public int RemainingTicks => _stateTicksRemaining;
        public int RecoveryProtectionTicks => _recoveryProtectionTicks;

        // Subsystems
        private InputBuffer _inputBuf;
        public InputBuffer InputBuf
        {
            get => _inputBuf ??= new InputBuffer(leniencyTicks: 5);
            private set => _inputBuf = value;
        }

        private CommandParser _cmdParser;
        public CommandParser CmdParser
        {
            get => _cmdParser ??= new CommandParser();
            private set => _cmdParser = value;
        }

        private JuggleEngine _juggle;
        public JuggleEngine Juggle
        {
            get => _juggle ??= new JuggleEngine();
            private set => _juggle = value;
        }

        private OkizemeWakeup _wakeupHandler;
        public OkizemeWakeup WakeupHandler
        {
            get => _wakeupHandler ??= new OkizemeWakeup();
            private set => _wakeupHandler = value;
        }

        public PsychicBurstSystem Burst { get; private set; }
        private BarrierBlockSystem _barrier;
        public BarrierBlockSystem Barrier
        {
            get
            {
                if (_barrier == null) _barrier = GetComponent<BarrierBlockSystem>() ?? gameObject.AddComponent<BarrierBlockSystem>();
                return _barrier;
            }
            private set => _barrier = value;
        }

        [Header("Tekken 3D Sidestep Tuning (Disabled for 2.5D Fixed Z)")]
        public float sidestepDistanceZ = 0.85f;
        public int sidestepDurationTicks = 14;

        // Tick counters for state transitions
        private int _stateTicksRemaining = 0;
        private int _hitstopTicksRemaining = 0;
        private int _superFlashTicksRemaining = 0;
        private int _recoveryProtectionTicks = 0;
        private int _currentAttackTick = 0;
        private int _hitContactTick = -999;
        private int _hitCancelCount = 0;
        private bool _burstUsedThisRound = false;
        private bool _hasAttackedInJump = false;
        private int _lastBackdashStartTick = -999;
        private bool _isInvincible = false;
        private bool _hitConfirmed = false;
        private readonly HashSet<string> _consumedDamageTransactions = new HashSet<string>();

        [Header("3-Button System: Projectile Meter & 3-Hit Melee Combo")]
        public float projectileMeter = 1.0f;
        public const float PROJECTILE_RECHARGE_DURATION = 1.8f;
        public float ProjectileMeter => projectileMeter;
        private int _meleeComboStep = 0; // 0=None, 1=M1, 2=M2, 3=M3
        public int MeleeComboStep => _meleeComboStep;
        private bool _isBlockedByDefender = false;
        public bool IsBlockedByDefender => _isBlockedByDefender;
        private int _blockRecoveryBonusTicks = 0;

        public void SetBlockRecoveryBonusTicks(int bonusTicks)
        {
            _blockRecoveryBonusTicks = bonusTicks;
            if (State == FighterState.AttackRecovery)
            {
                _stateTicksRemaining += bonusTicks;
            }
        }

        public void ConfirmHit() => _hitConfirmed = true;

        // Delayed Throw Tech State
        private bool _isBeingThrown = false;
        private int _throwBreakTicksRemaining = 0;
        private int _throwGrabTick = 0;
        private FighterController _throwAttacker;
        private AttackData _pendingThrowAttack;
        public bool IsBeingThrown => _isBeingThrown;

        // Movement & Physics
        private CharacterController _charController;
        private Vector2 _moveInput;
        public Vector2 MoveInput => _moveInput;
        private Vector3 _velocity;
        private bool _isGrounded => _charController != null ? _charController.isGrounded : true;

        // Events
        public event Action<FighterState> OnStateChanged;
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnSuperMeterChanged;
        public event Action<HitResult, AttackData, Vector3> OnHitProcessed;
        public event Action OnRapidCanceled;
        public event Action OnDomainTriggered;

        private void Awake()
        {
            if (GameplaySimulationAuthority.IsPackageNative)
            {
                enabled = false;
                return;
            }

            _charController = GetComponent<CharacterController>();
            InputBuf = new InputBuffer(leniencyTicks: 5);
            CmdParser = new CommandParser();
            Juggle = new JuggleEngine();
            WakeupHandler = new OkizemeWakeup();

            Burst = GetComponent<PsychicBurstSystem>();
            if (Burst == null) Burst = gameObject.AddComponent<PsychicBurstSystem>();

            Barrier = GetComponent<BarrierBlockSystem>();
            if (Barrier == null) Barrier = gameObject.AddComponent<BarrierBlockSystem>();

            currentHealth = maxHealth;
            currentSuperMeter = 0f;

            if (moveSet == null)
            {
                moveSet = MoveSet.CreateSamplePreset(fighterName);
            }
            ApplyMoveSet(moveSet);

            Hurtbox[] hurtboxes = GetComponentsInChildren<Hurtbox>();
            foreach (var h in hurtboxes)
            {
                h.Initialize(this);
            }

            if (activeHitbox != null)
            {
                activeHitbox.Initialize(this);
            }
        }

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            CacheAnimatorParams();

            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
        }

        public void CacheAnimatorParams()
        {
            _animatorParams.Clear();
            if (animator == null) return;
            foreach (var p in animator.parameters)
            {
                _animatorParams.Add(p.name);
            }
        }

        public void TriggerAnimation(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName)) return;
            if (animator != null && _animatorParams.Contains(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }

        private void OnDestroy()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        private void Update()
        {
            if (externalSimulation) return;
            if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen) return;

            // Projectile Meter Rapid Recharge (1.8s)
            if (projectileMeter < 1.0f)
            {
                projectileMeter = Mathf.Min(1.0f, projectileMeter + (Time.deltaTime / PROJECTILE_RECHARGE_DURATION));
            }

            // 2.5D Arena bounds & Strict Z=0.0m fighting plane & Ground Y>=0.0m clamp
            Vector3 pos = transform.position;
            pos.x = PushbackSolver.ClampToArena(pos.x);
            pos.z = 0f;
            if (pos.y < 0f || float.IsNaN(pos.y)) pos.y = 0f;
            transform.position = pos;
        }

        public void SetFacing(int direction)
        {
            FacingDirection = direction >= 0 ? 1 : -1;
            transform.rotation = Quaternion.Euler(0, FacingDirection == 1 ? 90f : -90f, 0);
        }

        private void UpdateFacingDirection()
        {
            float dirX = opponent.transform.position.x - transform.position.x;
            FacingDirection = dirX >= 0 ? 1 : -1;
            transform.rotation = Quaternion.Euler(0, FacingDirection == 1 ? 90f : -90f, 0);
        }

        #region Combat Tick Simulation (60Hz Fixed)

        private void HandleCombatTick(int currentTick)
        {
            if (externalSimulation) return;

            // Global superfreeze check (Domain Expansion, etc.)
            if (CombatTickEngine.Instance != null && CombatTickEngine.Instance.IsSuperfrozen)
            {
                InputBuf.FreezeAge(1);
                return;
            }

            // Superflash freeze: 18-tick superflash pauses startup advances
            if (_superFlashTicksRemaining > 0)
            {
                _superFlashTicksRemaining--;
                InputBuf.FreezeAge(1);
                return;
            }

            // Hitstop freeze: holds state without ticking frame countdown
            if (_hitstopTicksRemaining > 0)
            {
                _hitstopTicksRemaining--;
                InputBuf.FreezeAge(1);
                return;
            }

            // Recovery protection ticks down
            if (_recoveryProtectionTicks > 0)
            {
                _recoveryProtectionTicks--;
            }

            // Delayed Throw Tech Resolution: waits for 12-tick tech window before applying damage
            if (_isBeingThrown)
            {
                UpdateThrowBreakState(currentTick);
                return;
            }

            // If dead, apply gravity/decel only; no input processing or state transitions
            if (State == FighterState.Dead)
            {
                ApplyMovementPhysics();
                return;
            }

            // Latch facing direction strictly on combat tick during neutral states
            if (opponent != null && (State == FighterState.Idle || State == FighterState.WalkCreep || State == FighterState.Crouch))
            {
                UpdateFacingDirection();
            }

            // Record directional history for motions
            CmdParser.RecordDirection(_moveInput, currentTick, FacingDirection);

            // Process input buffer
            ProcessBufferedInputs(currentTick);

            // State machine update
            UpdateStateMachine(currentTick);

            // Apply displacement physics
            ApplyMovementPhysics();
        }

        private void UpdateThrowBreakState(int currentTick)
        {
            // If attacker is interrupted or dead, release defender safely
            if (_throwAttacker == null || _throwAttacker.State == FighterState.Hitstun || _throwAttacker.State == FighterState.Dead)
            {
                _isBeingThrown = false;
                _pendingThrowAttack = null;
                _throwAttacker = null;
                SetState(FighterState.Idle);
                return;
            }

            // Tech check: Valid if GrabBreak [P+M] or Throw chord/button was buffered ON OR AFTER the grab tick
            bool teched = false;
            if (InputBuf.HasCommand(ThreeButtonCommand.GrabBreak, currentTick, out var cmd3Break, 12) && cmd3Break.TickTimestamp >= _throwGrabTick)
            {
                teched = true;
                InputBuf.Consume(ThreeButtonCommand.GrabBreak);
            }
            else if (InputBuf.HasCommand(ThreeButtonCommand.Throw, currentTick, out var cmd3Throw, 12) && cmd3Throw.TickTimestamp >= _throwGrabTick)
            {
                teched = true;
                InputBuf.Consume(ThreeButtonCommand.Throw);
            }
            else if (InputBuf.HasCommand(InputButton.Burst, currentTick, out var cmdBurst, 12) && cmdBurst.TickTimestamp >= _throwGrabTick)
            {
                teched = true;
                InputBuf.Consume(InputButton.Burst);
            }
            else if (InputBuf.HasCommand(InputButton.Throw, currentTick, out var cmdThrow, 12) && cmdThrow.TickTimestamp >= _throwGrabTick)
            {
                teched = true;
                InputBuf.Consume(InputButton.Throw);
            }

            if (teched)
            {
                // Throw teched! Zero damage, returns both to neutral after 14 ticks of explicit protection
                _isBeingThrown = false;
                _recoveryProtectionTicks = 14;
                SetState(FighterState.Idle, 14);

                if (_throwAttacker != null)
                {
                    _throwAttacker._recoveryProtectionTicks = 14;
                    _throwAttacker.SetState(FighterState.Idle, 14);
                    PushbackSolver.ApplyPushback(_throwAttacker, this, 1.5f, _throwAttacker.FacingDirection);
                    _throwAttacker.TriggerAnimation("StandBlock");
                }
                TriggerAnimation("StandBlock");
                MobileHaptics.Trigger(HapticImpact.Medium);
                OnHitProcessed?.Invoke(HitResult.Teched, _pendingThrowAttack, transform.position);
                _pendingThrowAttack = null;
                _throwAttacker = null;
                return;
            }

            // Tech window ticks down
            _throwBreakTicksRemaining--;
            if (_throwBreakTicksRemaining <= 0)
            {
                // Tech window expired without tech: deal damage and knockdown
                _isBeingThrown = false;
                AttackData attack = _pendingThrowAttack;
                FighterController attacker = _throwAttacker;
                _pendingThrowAttack = null;
                _throwAttacker = null;

                if (attack != null)
                {
                    TakeDamage(attack.damage);
                    _hitstopTicksRemaining = attack.hitstopDefender;
                    if (attacker != null)
                    {
                        attacker.AddSuperMeter(6f);
                        attacker.ConfirmHit();
                    }
                    AddSuperMeter(4f);

                    int throwDir = attacker != null ? attacker.FacingDirection : FacingDirection;
                    _velocity.x = throwDir * (attack.launchVelocity.x > 0 ? attack.launchVelocity.x : 2.5f);
                    _velocity.y = attack.launchVelocity.y > 0 ? attack.launchVelocity.y : 3.0f;
                    EnterKnockdown(attack.knockdownDurationTicks > 0 ? attack.knockdownDurationTicks : 40);
                    MobileHaptics.Trigger(HapticImpact.Heavy);
                    OnHitProcessed?.Invoke(HitResult.Hit, attack, transform.position);
                }
                else
                {
                    SetState(FighterState.Idle);
                }
            }
        }

        private void UpdateStateMachine(int currentTick)
        {
            if (_stateTicksRemaining > 0)
            {
                _stateTicksRemaining--;
            }

            switch (State)
            {
                case FighterState.AttackStartup:
                    _currentAttackTick++;
                    if (_stateTicksRemaining <= 0)
                    {
                        EnterAttackActive();
                    }
                    break;

                case FighterState.AttackActive:
                    _currentAttackTick++;
                    if (_stateTicksRemaining <= 0)
                    {
                        EnterAttackRecovery();
                    }
                    break;

                case FighterState.AttackRecovery:
                    _currentAttackTick++;
                    if (_stateTicksRemaining <= 0)
                    {
                        SetState(FighterState.Idle);
                    }
                    break;

                case FighterState.ForwardDash:
                case FighterState.Backdash:
                    if (State == FighterState.Backdash && _stateTicksRemaining <= (backdashDurationTicks - backdashInvincibleTicks))
                    {
                        _isInvincible = false;
                    }

                    // Korean Backdash Cancel (KBD)
                    if (State == FighterState.Backdash && _stateTicksRemaining < (backdashDurationTicks - 4))
                    {
                        if (CmdParser.CheckKBDCancel(currentTick, FacingDirection, _lastBackdashStartTick))
                        {
                            ExecuteBackdash(isKbdCancel: true);
                            return;
                        }
                    }

                    if (_stateTicksRemaining <= 0)
                    {
                        SetState(FighterState.Idle);
                    }
                    break;

                case FighterState.ParryActive:
                    if (_stateTicksRemaining <= 0)
                    {
                        SetState(FighterState.ParryRecovery, parryRecoveryTicks);
                    }
                    break;

                case FighterState.ParryRecovery:
                case FighterState.ParrySuccess:
                case FighterState.Blockstun:
                case FighterState.Hitstun:
                case FighterState.Sidestep:
                    if (_stateTicksRemaining <= 0)
                    {
                        SetState(FighterState.Idle);
                    }
                    break;

                case FighterState.AirborneJuggle:
                case FighterState.BoundTornado:
                    if (_isGrounded && _velocity.y <= 0)
                    {
                        Juggle.OnLandedOnGround();
                        EnterKnockdown(36);
                    }
                    break;

                case FighterState.WallSplat:
                    if (_stateTicksRemaining <= 0 || (_isGrounded && _velocity.y <= 0))
                    {
                        EnterKnockdown(36);
                    }
                    break;

                case FighterState.Knockdown:
                    WakeupHandler.TickGrounded(currentTick, out bool mustForceStand);
                    if (_stateTicksRemaining <= 0 || mustForceStand)
                    {
                        // Neutral get-up with 14 ticks explicit recovery protection
                        SetState(FighterState.Wakeup, 14);
                        _recoveryProtectionTicks = 14;
                    }
                    break;

                case FighterState.Wakeup:
                    if (_stateTicksRemaining <= 0)
                    {
                        SetState(FighterState.Idle);
                    }
                    break;

                case FighterState.Idle:
                case FighterState.WalkCreep:
                case FighterState.Crouch:
                    if (_isGrounded)
                    {
                        if (_moveInput.y > 0.40f)
                        {
                            // Jump initiation (8.0m/s vertical launch, 3.2m/s horizontal)
                            float jumpX = 0f;
                            if (_moveInput.x * FacingDirection > 0.20f) jumpX = FacingDirection * jumpHorizontalSpeed;
                            else if (_moveInput.x * FacingDirection < -0.20f) jumpX = -FacingDirection * jumpHorizontalSpeed;

                            _velocity.x = jumpX;
                            _velocity.y = jumpVerticalLaunch;
                            _hasAttackedInJump = false;
                            SetState(FighterState.Idle);
                            TriggerAnimation("Jump");
                        }
                        else if (_moveInput.y < -0.20f)
                        {
                            if (State != FighterState.Crouch) SetState(FighterState.Crouch);
                        }
                        else if (Mathf.Abs(_moveInput.x) > 0.15f)
                        {
                            if (State != FighterState.WalkCreep) SetState(FighterState.WalkCreep);
                        }
                        else
                        {
                            if (State != FighterState.Idle) SetState(FighterState.Idle);
                        }
                    }
                    break;
            }

            // Landing check for airborne jump attacks: landing overrides with 6 vulnerable recovery ticks
            if (_isGrounded && _velocity.y <= 0 && State != FighterState.Knockdown && State != FighterState.Wakeup)
            {
                if (_hasAttackedInJump && (State == FighterState.AttackActive || State == FighterState.AttackRecovery))
                {
                    if (activeHitbox) activeHitbox.Deactivate();
                    SetState(FighterState.Idle, 6);
                    _hasAttackedInJump = false;
                }
            }
        }

        public void EnterKnockdown(int duration = 36)
        {
            SetState(FighterState.Knockdown, duration);
            _isInvincible = false;
            WakeupHandler.EnterGroundedState();
            TriggerAnimation("Knockdown");
        }

        #endregion

        #region Input Execution, Cancels & Supers

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        private void ProcessBufferedInputs(int currentTick)
        {
            if (State == FighterState.Dead) return;

            // 1. Okizeme Wakeup Inputs when Grounded
            if (State == FighterState.Knockdown)
            {
                if (_moveInput.x * FacingDirection < -0.35f)
                {
                    if (WakeupHandler.TryExecuteWakeupAction(WakeupAction.StandUpBack, out int invTicks))
                    {
                        SetState(FighterState.Wakeup, invTicks);
                        _recoveryProtectionTicks = invTicks;
                        _velocity.x = -FacingDirection * (backdashSpeed * 0.5f);
                        TriggerAnimation("StandUpBack");
                        return;
                    }
                }
                return;
            }

            // 2. Ultra / Super Reversal Chord (P+M+S or dedicated macro) - 100 Meter, frame-1 invincible interrupt!
            if (InputBuf.HasCommand(ThreeButtonCommand.Ultra, currentTick, out _) || InputBuf.HasCommand(ThreeButtonCommand.Super, currentTick, out _) || InputBuf.HasCommand(InputButton.DomainSuper, currentTick, out _))
            {
                if (CanExecuteSuper())
                {
                    InputBuf.Consume(ThreeButtonCommand.Ultra);
                    InputBuf.Consume(ThreeButtonCommand.Super);
                    InputBuf.Consume(InputButton.DomainSuper);
                    ExecuteSuper();
                    return;
                }
            }

            // 3. Defensive Burst / Grab Break Chord (P+M) - 50 Meter, defensive break during hitstun!
            if (InputBuf.HasCommand(ThreeButtonCommand.GrabBreak, currentTick, out _) || InputBuf.HasCommand(ThreeButtonCommand.Burst, currentTick, out _) || InputBuf.HasCommand(InputButton.Burst, currentTick, out _))
            {
                if (CanExecuteBurst())
                {
                    InputBuf.Consume(ThreeButtonCommand.GrabBreak);
                    InputBuf.Consume(ThreeButtonCommand.Burst);
                    InputBuf.Consume(InputButton.Burst);
                    ExecuteDefensiveBurst();
                    return;
                }
            }

            // 4. Close-Range Command Grab (66 or dedicated grab) - unblockable throw
            if (InputBuf.HasCommand(ThreeButtonCommand.CommandGrab, currentTick, out _) || InputBuf.HasCommand(ThreeButtonCommand.Throw, currentTick, out _) || InputBuf.HasCommand(InputButton.Throw, currentTick, out _))
            {
                if (IsGrounded && (IsActionable || CanAdvanceMeleeCombo()))
                {
                    InputBuf.Consume(ThreeButtonCommand.CommandGrab);
                    InputBuf.Consume(ThreeButtonCommand.Throw);
                    InputBuf.Consume(InputButton.Throw);
                    AttackData throwAtk = moveSet != null && moveSet.tacticalThrow != null ? moveSet.tacticalThrow : throwAttackData;
                    if (throwAtk != null)
                    {
                        StartAttack(throwAtk);
                        return;
                    }
                }
            }

            // In hitstun/blockstun, single attacks cannot be executed (only Ultra Reversal or Burst can interrupt)
            if (State == FighterState.Hitstun || State == FighterState.Blockstun || State == FighterState.AirborneJuggle)
            {
                return;
            }

            // 5. Special Attack Button [S] (Signature Skill / Node Teleport, cancelable from Melee combo on hit or block)
            if (InputBuf.HasCommand(ThreeButtonCommand.Special, currentTick, out var specCmd))
            {
                MoveSet ms = moveSet != null ? moveSet : MoveSet.CreateSamplePreset(fighterName);
                if (IsGrounded && (IsActionable || CanAdvanceMeleeCombo()))
                {
                    InputBuf.Consume(ThreeButtonCommand.Special);
                    InputBuf.Consume(InputButton.DriveSkill);
                    _meleeComboStep = 0; // Combo cancelled into special!

                    // Node-based spatial teleport skill
                    if (NodeTeleportSkill.IsTeleportFighter(fighterName))
                    {
                        NodeTeleportSkill.ExecuteTeleport(this);
                        return;
                    }

                    AttackData specAttack = ms.neutralS ?? ms.driveSkill;
                    if (specAttack != null)
                    {
                        StartAttack(specAttack);
                        return;
                    }
                }
            }

            // 6. Melee Attack Button [M] (3-Hit Target Combo: M1 -> M2 -> M3)
            if (InputBuf.HasCommand(ThreeButtonCommand.Melee, currentTick, out var meleeCmd))
            {
                MoveSet ms = moveSet != null ? moveSet : MoveSet.CreateSamplePreset(fighterName);
                if (IsGrounded)
                {
                    if (IsActionable && _meleeComboStep == 0)
                    {
                        InputBuf.Consume(ThreeButtonCommand.Melee);
                        InputBuf.Consume(InputButton.Heavy);
                        _meleeComboStep = 1;
                        AttackData m1 = ms.standingH ?? ms.heavyAttack;
                        StartAttack(m1);
                        return;
                    }
                    else if (_meleeComboStep == 1 && CanAdvanceMeleeCombo())
                    {
                        InputBuf.Consume(ThreeButtonCommand.Melee);
                        InputBuf.Consume(InputButton.Heavy);
                        _meleeComboStep = 2;
                        AttackData m2 = ms.forwardH ?? ms.mediumAttack ?? ms.standingH;
                        StartAttack(m2);
                        return;
                    }
                    else if (_meleeComboStep == 2 && CanAdvanceMeleeCombo())
                    {
                        InputBuf.Consume(ThreeButtonCommand.Melee);
                        InputBuf.Consume(InputButton.Heavy);
                        _meleeComboStep = 3;
                        AttackData m3 = ms.crouchingH ?? ms.lowSweepAttack ?? ms.heavyAttack;
                        StartAttack(m3);
                        return;
                    }
                }
                else if (!IsGrounded && !_hasAttackedInJump && (State == FighterState.Idle || State == FighterState.WalkCreep))
                {
                    InputBuf.Consume(ThreeButtonCommand.Melee);
                    InputBuf.Consume(InputButton.Heavy);
                    _hasAttackedInJump = true;
                    AttackData airAtk = ms.airH ?? ms.heavyAttack;
                    StartAttack(airAtk);
                    return;
                }
            }

            // 7. Projectile Button [P] (Consumes Projectile Meter, recharges in 1.8s)
            if (InputBuf.HasCommand(ThreeButtonCommand.Projectile, currentTick, out var projCmd))
            {
                if (projectileMeter >= 0.95f && IsGrounded && (IsActionable || CanAdvanceMeleeCombo()))
                {
                    InputBuf.Consume(ThreeButtonCommand.Projectile);
                    InputBuf.Consume(InputButton.Light);
                    projectileMeter = 0f; // Exhaust meter!

                    MoveSet ms = moveSet != null ? moveSet : MoveSet.CreateSamplePreset(fighterName);
                    AttackData projAttack = ms.standingL ?? ms.lightAttack;
                    StartAttack(projAttack);

                    // Fire unique character projectile
                    ProjectileFactory.SpawnProjectileForCharacter(fighterName, this, transform.position + new Vector3(FacingDirection * 0.8f, 1.0f, 0f), FacingDirection);
                    return;
                }
                else if (projectileMeter < 0.95f && IsGrounded && IsActionable)
                {
                    // Meter exhausted: click whiff feedback
                    InputBuf.Consume(ThreeButtonCommand.Projectile);
                    if (CombatAudioDirector.Instance != null) CombatAudioDirector.Instance.PlayBlock();
                    return;
                }
            }

            // 8. Legacy / Convenience Inputs fallback
            ProcessLegacyInputs(currentTick);
        }

        private bool CanAdvanceMeleeCombo()
        {
            if (CurrentAttack == null) return false;
            if (State != FighterState.AttackActive && State != FighterState.AttackRecovery) return false;
            // Advanceable on contact: hit confirmed OR blocked by defender!
            return _hitConfirmed || _isBlockedByDefender;
        }

        private bool CanExecuteResolvedAttack(AttackData attack)
        {
            if (attack == null) return false;
            if (IsGrounded && IsActionable) return true;
            if (!IsGrounded && !_hasAttackedInJump && (State == FighterState.Idle || State == FighterState.WalkCreep)) return true;
            if (IsInLegalHitCancelWindow(attack.attackType)) return true;
            return false;
        }

        private bool IsInLegalHitCancelWindow(AttackType nextAttackType)
        {
            if (!_hitConfirmed) return false;
            if (CurrentAttack == null) return false;
            if (State != FighterState.AttackActive && State != FighterState.AttackRecovery) return false;
            // Hit-cancel window: contact+1 through contact+8 advancing action ticks
            if (_currentAttackTick < _hitContactTick + 1 || _currentAttackTick > _hitContactTick + 8) return false;

            // Manual route: L -> L2 -> H -> neutral S -> Super
            if (CurrentAttack.attackType == AttackType.Light)
            {
                if (nextAttackType == AttackType.Light && _hitCancelCount == 0) return true;
                if (nextAttackType == AttackType.Heavy || nextAttackType == AttackType.DomainSuper) return true;
            }
            else if (CurrentAttack.attackType == AttackType.Heavy)
            {
                if (nextAttackType == AttackType.DriveSkill || nextAttackType == AttackType.DomainSuper) return true;
            }
            else if (CurrentAttack.attackType == AttackType.DriveSkill)
            {
                if (nextAttackType == AttackType.DomainSuper) return true;
            }

            return false;
        }

        public bool CanExecuteBurst()
        {
            return currentSuperMeter >= 50f && !_burstUsedThisRound && State == FighterState.Hitstun && !_isBeingThrown;
        }

        public void ExecuteDefensiveBurst()
        {
            SpendSuperMeter(50f);
            _burstUsedThisRound = true;
            AttackData burstAtk = (moveSet != null && moveSet.defensiveBurst != null) ? moveSet.defensiveBurst : MoveSet.CreateSamplePreset(fighterName).defensiveBurst;
            StartAttack(burstAtk);
            InvulnMask = InvulnerabilityMask.Strike;
            TriggerAnimation(burstAtk.animationTrigger);
        }

        public bool CanExecuteSuper()
        {
            if (currentSuperMeter < 100f) return false;
            if (State == FighterState.Dead || _isBeingThrown) return false;
            // Frame 1 invincible interrupt / reversal: interrupts ANY incoming attack or state!
            return true;
        }

        public void ExecuteSuper()
        {
            SpendSuperMeter(100f);
            _superFlashTicksRemaining = 18;
            _isInvincible = true;
            InvulnMask = InvulnerabilityMask.All;
            _hitstopTicksRemaining = 0;
            _stateTicksRemaining = 0;
            _meleeComboStep = 0;
            AttackData superAtk = (moveSet != null && moveSet.domainSuper != null) ? moveSet.domainSuper : (domainSuperData != null ? domainSuperData : MoveSet.CreateSamplePreset(fighterName).domainSuper);
            StartAttack(superAtk);
            if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerUltimateEntry(18 / 60f);
            OnDomainTriggered?.Invoke();
            TriggerAnimation("DomainSuper");
        }

        private void ProcessLegacyInputs(int currentTick)
        {
            // Backdash Button
            if (InputBuf.HasCommand(InputButton.Backdash, currentTick, out _))
            {
                if (CanDash())
                {
                    InputBuf.Consume(InputButton.Backdash);
                    ExecuteBackdash();
                    return;
                }
            }

            // Forward Dash
            if (InputBuf.HasCommand(InputButton.ForwardDash, currentTick, out _) || CmdParser.CheckForwardDash(currentTick, FacingDirection))
            {
                if (CanDash())
                {
                    InputBuf.Consume(InputButton.ForwardDash);
                    ExecuteForwardDash();
                    return;
                }
            }

            // Legacy Light/Medium/Heavy/DriveSkill
            if (InputBuf.HasCommand(InputButton.DriveSkill, currentTick, out _))
            {
                MoveSet ms = moveSet != null ? moveSet : MoveSet.CreateSamplePreset(fighterName);
                AttackData atk = ms.ResolveAttack(ThreeButtonCommand.Skill, IsGrounded, _moveInput, FacingDirection);
                if (atk != null && CanExecuteResolvedAttack(atk))
                {
                    InputBuf.Consume(InputButton.DriveSkill);
                    StartAttack(atk);
                    return;
                }
            }

            if (InputBuf.HasCommand(InputButton.Heavy, currentTick, out _))
            {
                MoveSet ms = moveSet != null ? moveSet : MoveSet.CreateSamplePreset(fighterName);
                AttackData atk = ms.ResolveAttack(ThreeButtonCommand.Heavy, IsGrounded, _moveInput, FacingDirection);
                if (atk != null && CanExecuteResolvedAttack(atk))
                {
                    InputBuf.Consume(InputButton.Heavy);
                    StartAttack(atk);
                    return;
                }
            }

            if (InputBuf.HasCommand(InputButton.Light, currentTick, out _))
            {
                MoveSet ms = moveSet != null ? moveSet : MoveSet.CreateSamplePreset(fighterName);
                AttackData atk = ms.ResolveAttack(ThreeButtonCommand.Light, IsGrounded, _moveInput, FacingDirection);
                if (atk != null && CanExecuteResolvedAttack(atk))
                {
                    InputBuf.Consume(InputButton.Light);
                    StartAttack(atk);
                    return;
                }
            }
        }

        private bool CanDash()
        {
            return State == FighterState.Idle || State == FighterState.WalkCreep;
        }

        public void ExecuteForwardDash()
        {
            if (!CanDash()) return;
            SetState(FighterState.ForwardDash, forwardDashDurationTicks);
            _velocity.x = FacingDirection * forwardDashSpeed;
            TriggerAnimation("ForwardDash");
        }

        public void ExecuteBackdash(bool isKbdCancel = false)
        {
            if (!isKbdCancel && !CanDash()) return;
            _lastBackdashStartTick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : 0;
            SetState(FighterState.Backdash, backdashDurationTicks);
            _isInvincible = true;
            _velocity.x = -FacingDirection * backdashSpeed;
            TriggerAnimation("Backdash");
        }

        public bool ExecuteParry()
        {
            if (!IsActionable || !IsGrounded) return false;
            SetState(FighterState.ParryActive, parryActiveTicks);
            TriggerAnimation("Parry");
            if (CombatAudioDirector.Instance != null) CombatAudioDirector.Instance.PlayBlock();
            return true;
        }

        public bool ExecuteSidestep(float directionZ = 1f)
        {
            if (!IsActionable || !IsGrounded) return false;
            SetState(FighterState.Sidestep, sidestepDurationTicks);
            TriggerAnimation("Sidestep");
            return true;
        }

        private void StartAttack(AttackData attack)
        {
            CurrentAttack = attack;
            _currentAttackTick = 0;
            _hitConfirmed = false;
            _hitContactTick = -999;
            SetState(FighterState.AttackStartup, attack.startupFrames);

            if (activeHitbox) activeHitbox.Deactivate();

            if (attack.isAirborneStrikeProtected)
            {
                InvulnMask |= InvulnerabilityMask.AirborneStrikeOnly;
            }

            if (attack.attackType == AttackType.JumpAttack)
            {
                _velocity.x = FacingDirection * jumpAttackVelocityX;
                _velocity.y = jumpAttackVelocityY;
            }

            TriggerAnimation(attack.animationTrigger);
        }

        private void EnterAttackActive()
        {
            _isInvincible = false;
            SetState(FighterState.AttackActive, CurrentAttack.activeFrames);
            if (activeHitbox && CurrentAttack)
            {
                activeHitbox.Activate(CurrentAttack);
            }
        }

        private void EnterAttackRecovery()
        {
            if (CurrentAttack != null && CurrentAttack.isAirborneStrikeProtected)
            {
                InvulnMask &= ~InvulnerabilityMask.AirborneStrikeOnly;
            }
            if (InvulnMask == InvulnerabilityMask.Strike && CurrentAttack == (moveSet != null ? moveSet.defensiveBurst : null))
            {
                InvulnMask = InvulnerabilityMask.None;
            }

            int recTicks = CurrentAttack.recoveryFrames;
            if (!IsGrounded && CurrentAttack.attackType == AttackType.JumpAttack)
            {
                recTicks = 12; // Air recovery is 12 ticks if still airborne
            }
            if (_isBlockedByDefender)
            {
                recTicks += _blockRecoveryBonusTicks;
            }

            SetState(FighterState.AttackRecovery, recTicks);
            if (activeHitbox)
            {
                activeHitbox.Deactivate();
            }
        }

        public void ForceCancelToNeutral()
        {
            if (activeHitbox) activeHitbox.Deactivate();
            CurrentAttack = null;
            _hitConfirmed = false;
            _hitContactTick = -999;
            _velocity = Vector3.zero;
            SetState(FighterState.Idle);
            OnRapidCanceled?.Invoke();
            TriggerAnimation("RapidCancel");
        }

        public void ExecuteDomainCinematic(AttackData domainData, int freezeTicks)
        {
            CurrentAttack = domainData;
            _currentAttackTick = 0;
            _isInvincible = true;
            _hitstopTicksRemaining = 0;
            SetState(FighterState.AttackStartup, domainData.startupFrames);
            if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerUltimateEntry(freezeTicks / 60f);
            OnDomainTriggered?.Invoke();
            TriggerAnimation("DomainSuper");
        }

        #endregion

        #region Hit & Damage Resolution (Directional Guard + Juggle Budget + 0-100 Meter)

        public void OnHitboxLanded(FighterController defender, AttackData attack, Hurtbox hurtbox, Vector3 hitPoint)
        {
            HitResult result = defender.ReceiveIncomingStrike(this, attack, hurtbox);

            if (result == HitResult.Whiff || result == HitResult.Crushed) return;

            // Delayed Throw grab contact: do not award hitstop, meter, or combo credit until tech window expires
            if (result == HitResult.Grabbed)
            {
                OnHitProcessed?.Invoke(result, attack, hitPoint);
                return;
            }

            _hitConfirmed = true;
            _hitContactTick = _currentAttackTick;

            if (result == HitResult.Hit || result == HitResult.CounterHit)
            {
                _hitstopTicksRemaining = attack.hitstopAttacker;
                if (result == HitResult.CounterHit) _hitstopTicksRemaining += attack.counterHitStopBonus;

                if (attack.attackType == AttackType.DomainSuper)
                {
                    if (DemonIdol.Cutscenes.CutsceneManager.Instance != null && DemonIdol.Cutscenes.VideoPathResolver.Resolve("ultimate_astral_kai", out _, out _, out _))
                    {
                        DemonIdol.Cutscenes.CutsceneManager.Instance.PlayCutsceneById("ultimate_astral_kai",
                            onComplete: () =>
                            {
                                if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerUltimateExit();
                            },
                            caption: "ASTRAL FINISH: VOID RIFT EXPANSION");
                    }
                }
            }
            else if (result == HitResult.Blocked)
            {
                _hitstopTicksRemaining = attack.hitstopOnBlock > 0 ? attack.hitstopOnBlock : 4;
            }
            else if (result == HitResult.Parried)
            {
                SetState(FighterState.ParryRecovery, defender.parryStunTicksOnAttacker);
                _hitstopTicksRemaining = defender.parryStunTicksOnAttacker;
            }

            OnHitProcessed?.Invoke(result, attack, hitPoint);
        }

        public HitResult ReceiveIncomingStrike(FighterController attacker, AttackData attack, Hurtbox hurtbox)
        {
            if (_isInvincible || State == FighterState.Dead)
            {
                return HitResult.Whiff;
            }

            // Explicit recovery protection (after throw tech, wake-up, etc.)
            if (_recoveryProtectionTicks > 0)
            {
                return HitResult.Whiff;
            }

            // Invulnerability Mask checks
            if ((InvulnMask & InvulnerabilityMask.Strike) != 0 && attack.hitLevel != HitLevel.Throw)
            {
                return HitResult.Whiff;
            }
            if ((InvulnMask & InvulnerabilityMask.Throw) != 0 && attack.hitLevel == HitLevel.Throw)
            {
                return HitResult.Whiff;
            }
            if ((InvulnMask & InvulnerabilityMask.AirborneStrikeOnly) != 0 && attacker != null && !attacker.IsGrounded)
            {
                return HitResult.Whiff;
            }

            // Active Parry Window (Frame-perfect defensive deflection)
            if (State == FighterState.ParryActive && attack.hitLevel != HitLevel.Throw)
            {
                SetState(FighterState.ParrySuccess, 14);
                _hitstopTicksRemaining = 6;
                AddSuperMeter(8f);
                if (attacker != null)
                {
                    attacker.SetState(FighterState.ParryRecovery, parryStunTicksOnAttacker);
                    attacker.SetHitstop(parryStunTicksOnAttacker);
                }
                Vector3 parryPos = hurtbox != null ? hurtbox.transform.position : transform.position;
                if (HitEffectCoordinator.Instance != null)
                {
                    HitEffectCoordinator.Instance.TriggerParryEffect(parryPos);
                }
                else if (CombatAudioDirector.Instance != null)
                {
                    CombatAudioDirector.Instance.PlayParry();
                }
                OnHitProcessed?.Invoke(HitResult.Parried, attack, parryPos);
                return HitResult.Parried;
            }

            // Sidestep Evasive Window (Dodges linear strikes during active evasion frames)
            if (State == FighterState.Sidestep && attack.hitLevel != HitLevel.Throw && _stateTicksRemaining >= (sidestepDurationTicks / 2))
            {
                return HitResult.Whiff;
            }

            // Down+S anti-air launcher protection window against airborne strikes
            if (CurrentAttack != null && CurrentAttack.isAirborneStrikeProtected &&
                _currentAttackTick >= CurrentAttack.protectionStartTick && _currentAttackTick <= CurrentAttack.protectionEndTick)
            {
                if (attacker != null && !attacker.IsGrounded)
                {
                    return HitResult.Whiff;
                }
            }

            // Duckable High whiffs crouching opponents
            if (attack.isDuckableHigh && (State == FighterState.Crouch || _moveInput.y < -0.20f))
            {
                return HitResult.Whiff;
            }

            // Ground Okizeme: Grounded bodies only take Low strikes (OTG)
            if (State == FighterState.Knockdown && attack.guardRequirement != GuardRequirement.Low && attack.hitLevel != HitLevel.Low)
            {
                return HitResult.Whiff;
            }

            // Throw resolution: unblockable by guard, requires both grounded, defender throwable, pushbox edge gap <= 0.35m
            if (attack.hitLevel == HitLevel.Throw || attack.guardRequirement == GuardRequirement.Throw)
            {
                if (!IsThrowable || attacker == null || !attacker.IsGrounded)
                {
                    return HitResult.Whiff;
                }

                float dx = Mathf.Abs(attacker.transform.position.x - transform.position.x);
                if (dx > 1.05f)
                {
                    return HitResult.Whiff;
                }

                int curTick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : 0;
                _isBeingThrown = true;
                _throwBreakTicksRemaining = attack.throwTechWindowTicks > 0 ? attack.throwTechWindowTicks : 12;
                _throwGrabTick = curTick;
                _throwAttacker = attacker;
                _pendingThrowAttack = attack;

                _velocity = Vector3.zero;
                SetState(FighterState.Hitstun, _throwBreakTicksRemaining + 4);
                return HitResult.Grabbed;
            }

            // Directional Guard Evaluation (SF-style directional defense)
            // Back guards standing; Down-Back guards crouching. No neutral auto-guard or air guard!
            bool isGuarding = false;
            bool isStandGuarding = false;
            bool isCrouchGuarding = false;

            if (IsGuardable && IsGrounded)
            {
                float toAttackerX = attacker != null ? (attacker.transform.position.x - transform.position.x) : FacingDirection;
                bool holdingBack = (toAttackerX >= 0 && _moveInput.x < -0.20f) || (toAttackerX < 0 && _moveInput.x > 0.20f);

                if (holdingBack)
                {
                    isStandGuarding = _moveInput.y >= -0.20f;
                    isCrouchGuarding = _moveInput.y < -0.20f;
                    isGuarding = isStandGuarding || isCrouchGuarding;
                }
            }

            bool isBlocked = false;
            if (isGuarding)
            {
                if (attack.guardRequirement == GuardRequirement.Normal)
                {
                    isBlocked = isStandGuarding || isCrouchGuarding;
                }
                else if (attack.guardRequirement == GuardRequirement.Low)
                {
                    isBlocked = isCrouchGuarding;
                }
                else if (attack.guardRequirement == GuardRequirement.Overhead)
                {
                    isBlocked = isStandGuarding;
                }
            }

            if (isBlocked)
            {
                // Meter on block: +2 attacker, +2 defender
                attacker?.AddSuperMeter(2f);
                AddSuperMeter(2f);

                if (attacker != null)
                {
                    attacker._isBlockedByDefender = true;
                    // Standing block frame advantage (+12f advantage for defender to execute standing melee punish)
                    if (isStandGuarding)
                    {
                        attacker.SetBlockRecoveryBonusTicks(12);
                    }
                }

                int blockstun = isStandGuarding ? 10 : (attack.blockstunTicks > 0 ? attack.blockstunTicks : 12);
                SetState(FighterState.Blockstun, blockstun);
                _hitstopTicksRemaining = attack.hitstopOnBlock > 0 ? attack.hitstopOnBlock : 4;

                float pushback = attack.pushbackOnBlock;
                bool isBarrier = Barrier != null && Barrier.IsBarrierActive;
                if (isBarrier)
                {
                    pushback *= BarrierBlockSystem.BARRIER_PUSHBACK_MULTIPLIER;
                }

                PushbackSolver.ApplyPushback(attacker, this, pushback, attacker != null ? attacker.FacingDirection : FacingDirection);
                TriggerAnimation(isCrouchGuarding ? "CrouchBlock" : "StandBlock");
                Vector3 blockPos = hurtbox != null ? hurtbox.transform.position : transform.position;
                if (HitEffectCoordinator.Instance != null)
                {
                    HitEffectCoordinator.Instance.TriggerBlockEffect(blockPos);
                }
                return isBarrier ? HitResult.BarrierBlocked : HitResult.Blocked;
            }

            // Juggle & Aerial Hit Check
            if (State == FighterState.AirborneJuggle || State == FighterState.BoundTornado)
            {
                if (Juggle.TryRegisterJuggleHit(attack, out Vector2 jugVel, out bool isBound))
                {
                    float scaledDamage = Mathf.Floor(attack.damage * JuggleEngine.GetDamageScale(Juggle.ComboHitCount, attack.attackType == AttackType.DomainSuper));
                    TakeDamage(scaledDamage);
                    _hitstopTicksRemaining = attack.hitstopDefender;
                    _velocity.x = (attacker != null ? attacker.FacingDirection : FacingDirection) * jugVel.x;
                    _velocity.y = jugVel.y;

                    if (isBound)
                    {
                        EnterKnockdown(attack.knockdownDurationTicks > 0 ? attack.knockdownDurationTicks : 36);
                    }
                    return HitResult.Hit;
                }
            }

            // Ground Clean Hit or Counter-Hit
            bool isCounterHit = IsCounterHitVulnerable;
            bool isGroundedOTG = (State == FighterState.Knockdown);

            float counterScale = isCounterHit ? 1.10f : 1.0f;
            float otgScale = isGroundedOTG ? 0.50f : 1.0f;
            int comboIndex = Juggle != null ? Juggle.ComboHitCount + 1 : 1;
            float comboScale = JuggleEngine.GetDamageScale(comboIndex, attack.attackType == AttackType.DomainSuper);

            float finalDamage = Mathf.Floor(attack.damage * counterScale * otgScale * comboScale);
            string txId = attack.attackType == AttackType.DomainSuper ? DomainExpansion.ActiveTransactionId : null;
            TakeDamage(finalDamage, txId);

            if (attack.attackType == AttackType.DomainSuper)
            {
                if (CutsceneManager.Instance != null)
                {
                    CutsceneManager.Instance.PlaySuperOverlay(attacker != null ? attacker.fighterName : fighterName, true);
                }
            }

            if (Juggle != null) Juggle.RegisterHit();

            // Hit Meter: +6 attacker (unless super/burst), +4 defender
            if (attack.attackType != AttackType.DomainSuper && attack != (moveSet != null ? moveSet.defensiveBurst : null))
            {
                attacker?.AddSuperMeter(6f);
            }
            AddSuperMeter(4f);

            if (isGroundedOTG)
            {
                _hitstopTicksRemaining = attack.hitstopDefender;
                TriggerAnimation("HitLow");
                return HitResult.Hit;
            }

            int hitstun = attack.hitstunTicks + (isCounterHit ? 4 : 0);
            _hitstopTicksRemaining = attack.hitstopDefender + (isCounterHit ? attack.counterHitStopBonus : 0);

            // Launcher / Wall splat / Knockdown checks
            bool isNearWall = PushbackSolver.CheckWallCollision(transform.position, out int wallDir);

            if (attack.causesWallBounce && isNearWall && Juggle.TryRegisterWallInteraction())
            {
                SetState(FighterState.AirborneJuggle);
                Juggle.RegisterLaunch(new Vector2(2.5f, 6.8f));
                _velocity.x = -wallDir * 4.2f;
                _velocity.y = 6.8f;
                TriggerAnimation("BoundTornado");
                if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerWallAccent(0.35f);
            }
            else if (attack.causesWallSplat && isNearWall && Juggle.TryRegisterWallInteraction())
            {
                SetState(FighterState.WallSplat, 24);
                _velocity = Vector3.zero;
                TriggerAnimation("WallSplat");
                if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerWallAccent(0.40f);
            }
            else if (attack.isLauncher)
            {
                SetState(FighterState.AirborneJuggle);
                Juggle.RegisterLaunch(attack.launchVelocity);
                _velocity.x = (attacker != null ? attacker.FacingDirection : FacingDirection) * (attack.launchVelocity.x > 0 ? attack.launchVelocity.x : 1.5f);
                _velocity.y = attack.launchVelocity.y > 0 ? attack.launchVelocity.y : 9.0f;
                TriggerAnimation("Launch");
                if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerLaunchTrack(0.55f);
            }
            else if (attack.causesKnockdown)
            {
                EnterKnockdown(attack.knockdownDurationTicks > 0 ? attack.knockdownDurationTicks : 36);
                if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerHitAccent(0.12f);
            }
            else
            {
                SetState(FighterState.Hitstun, Mathf.Max(10, hitstun));
                PushbackSolver.ApplyPushback(attacker, this, attack.pushbackOnHit, attacker != null ? attacker.FacingDirection : FacingDirection);
                TriggerAnimation(attack.hitLevel == HitLevel.Low ? "HitLow" : "HitMid");
                if (FighterCamera2D.Instance != null)
                {
                    if (isCounterHit) FighterCamera2D.Instance.TriggerCounterHitImpactZoom(0.20f, 0.45f);
                    else FighterCamera2D.Instance.TriggerHitAccent(0.10f);
                }
            }

            return isCounterHit ? HitResult.CounterHit : HitResult.Hit;
        }

        public void ApplyHorizontalDisplacement(float amount)
        {
            _velocity.x += amount;
        }

        public void SetHitstop(int ticks)
        {
            _hitstopTicksRemaining = Mathf.Max(_hitstopTicksRemaining, ticks);
        }

        public void TakeDamage(float amount, string transactionId = null)
        {
            if (amount <= 0) return;

            if (!string.IsNullOrEmpty(transactionId))
            {
                if (_consumedDamageTransactions.Contains(transactionId))
                {
                    Debug.LogWarning($"[FighterController] Duplicate damage transaction '{transactionId}' ignored.");
                    return;
                }
                _consumedDamageTransactions.Add(transactionId);
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            if (Burst != null) Burst.OnTakeDamage(amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                SetState(FighterState.Dead);
                _isBeingThrown = false;
                _pendingThrowAttack = null;
                _throwAttacker = null;
                if (activeHitbox != null) activeHitbox.Deactivate();
                TriggerAnimation("Defeat");
                if (opponent != null)
                    opponent.TriggerAnimation("Victory");

                // Death Dissolve Sequence
                DemonIdol.VFX.DeathDissolveController.TriggerDissolve(this);

                // Bounded super overlay authoring rule: A KO during the overlay ends it once
                if (CutsceneManager.Instance != null && CutsceneManager.Instance.IsPlaying)
                {
                    CutsceneManager.Instance.CancelActiveCutscene();
                }
            }
        }

        public void AddSuperMeter(float amount)
        {
            currentSuperMeter = Mathf.Clamp(currentSuperMeter + amount, 0f, maxSuperMeter);
            OnSuperMeterChanged?.Invoke(currentSuperMeter, maxSuperMeter);
        }

        public void SpendSuperMeter(float amount)
        {
            currentSuperMeter = Mathf.Clamp(currentSuperMeter - amount, 0f, maxSuperMeter);
            OnSuperMeterChanged?.Invoke(currentSuperMeter, maxSuperMeter);
        }

        public void ResetToIdle()
        {
            currentHealth = maxHealth;
            currentSuperMeter = 0f;
            SetState(FighterState.Idle);
            _stateTicksRemaining = 0;
            _velocity = Vector3.zero;
            Vector3 clampedPos = transform.position;
            clampedPos.x = PushbackSolver.ClampToArena(clampedPos.x);
            clampedPos.z = 0f;
            transform.position = clampedPos;

            _isBeingThrown = false;
            _throwBreakTicksRemaining = 0;
            _throwGrabTick = 0;
            _throwAttacker = null;
            _pendingThrowAttack = null;
            _consumedDamageTransactions.Clear();
            _isInvincible = false;
            _recoveryProtectionTicks = 0;
            _burstUsedThisRound = false;
            InvulnMask = InvulnerabilityMask.None;
            CurrentAttack = null;
            _currentAttackTick = 0;
            _hitConfirmed = false;
            _hitContactTick = -999;
            _hitCancelCount = 0;
            _hitstopTicksRemaining = 0;
            _superFlashTicksRemaining = 0;
            _hasAttackedInJump = false;
            _lastBackdashStartTick = -999;
            _moveInput = Vector2.zero;

            if (InputBuf != null) InputBuf.Clear();
            if (Juggle != null) Juggle.ForceResetCombo();
            if (WakeupHandler != null) WakeupHandler.Reset();
            if (activeHitbox != null) activeHitbox.Deactivate();
            if (Burst != null) Burst.ResetMeter();
            if (Barrier != null) Barrier.DeactivateBarrier();
            TriggerAnimation("Idle");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnSuperMeterChanged?.Invoke(currentSuperMeter, maxSuperMeter);
        }

        #endregion

        #region Displacement & Physics

        private void ApplyMovementPhysics()
        {
            if (_isGrounded)
            {
                if (State == FighterState.WalkCreep)
                {
                    float walkDir = _moveInput.x * FacingDirection;
                    float targetSpeed = walkDir >= 0 ? forwardWalkSpeed : backwardWalkSpeed;
                    _velocity.x = _moveInput.x * targetSpeed;
                }
                else if (State != FighterState.ForwardDash && State != FighterState.Backdash && State != FighterState.Hitstun)
                {
                    _velocity.x = Mathf.MoveTowards(_velocity.x, 0, 18f * CombatTickEngine.TICK_DELTA_TIME);
                }

                if (_velocity.y < 0) _velocity.y = -2f;
            }
            else
            {
                float grav = State == FighterState.AirborneJuggle ? Juggle.GetCurrentGravity() : gravity;
                _velocity.y -= grav * CombatTickEngine.TICK_DELTA_TIME;
            }

            _velocity.z = 0f;
            _charController.Move(_velocity * CombatTickEngine.TICK_DELTA_TIME);

            // Strict Z=0.0m planar clamp and Y>=0.0m floor grounding clamp
            Vector3 p = transform.position;
            bool posDirty = false;
            if (p.z != 0f)
            {
                p.z = 0f;
                posDirty = true;
            }
            if (p.y < 0f || float.IsNaN(p.y))
            {
                p.y = 0f;
                if (_velocity.y < 0f) _velocity.y = 0f;
                posDirty = true;
            }
            if (posDirty)
            {
                transform.position = p;
            }

            if (animator != null)
            {
                if (_animatorParams.Contains("MoveX")) animator.SetFloat("MoveX", _moveInput.x * FacingDirection);
                if (_animatorParams.Contains("Speed")) animator.SetFloat("Speed", Mathf.Abs(_velocity.x));
                if (_animatorParams.Contains("IsGrounded")) animator.SetBool("IsGrounded", _isGrounded);
                if (_animatorParams.Contains("IsCrouch")) animator.SetBool("IsCrouch", State == FighterState.Crouch);
                // Block pose holds for the blockstun the combat code already chose. Frame data is unchanged.
                if (_animatorParams.Contains("IsBlocking")) animator.SetBool("IsBlocking", State == FighterState.Blockstun);
            }
        }

        public void SetState(FighterState newState, int durationTicks = 0)
        {
            State = newState;
            _stateTicksRemaining = durationTicks;

            if (newState == FighterState.Idle || newState == FighterState.WalkCreep || newState == FighterState.Crouch || newState == FighterState.Hitstun || newState == FighterState.Dead)
            {
                _meleeComboStep = 0;
                _isBlockedByDefender = false;
                _blockRecoveryBonusTicks = 0;
            }

            OnStateChanged?.Invoke(newState);
        }

        public void ApplyCharacterDefinition(CharacterDefinition charDef)
        {
            if (charDef == null) return;
            fighterName = charDef.displayName;

            float speedFactor = Mathf.Clamp(charDef.speed / 50f, 0.7f, 1.4f);
            creepSpeed = 1.2f * speedFactor;
            forwardDashSpeed = 6.5f * speedFactor;
            backdashSpeed = 7.0f * speedFactor;

            if (charDef.moveSet != null)
            {
                ApplyMoveSet(charDef.moveSet);
            }
            else
            {
                ApplyMoveSet(MoveSet.CreateSamplePreset(charDef.id));
            }
        }

        public void ApplyMoveSet(MoveSet ms)
        {
            if (ms == null) return;
            moveSet = ms;
            ms.SyncLegacyBindings();

            if (ms.standingL != null) lightAttackData = ms.standingL;
            else if (ms.lightAttack != null) lightAttackData = ms.lightAttack;

            if (ms.crouchingL != null) lowAttackData = ms.crouchingL;
            else if (ms.lowSweepAttack != null) lowAttackData = ms.lowSweepAttack;

            if (ms.standingH != null) heavyAttackData = ms.standingH;
            else if (ms.heavyAttack != null) heavyAttackData = ms.heavyAttack;

            if (ms.neutralS != null) driveSkillData = ms.neutralS;
            else if (ms.driveSkill != null) driveSkillData = ms.driveSkill;

            if (ms.domainSuper != null) domainSuperData = ms.domainSuper;
            if (ms.tacticalThrow != null) throwAttackData = ms.tacticalThrow;
            if (ms.airL != null) jumpAttackData = ms.airL;
            else if (ms.overheadAttack != null) jumpAttackData = ms.overheadAttack;
        }

        #endregion
    }
}
