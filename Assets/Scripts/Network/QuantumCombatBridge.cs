using System;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Audio;
using DemonIdol.VFX;

namespace DemonIdol.Network
{
    /// <summary>
    /// Quantum Simulation-to-View Bridge.
    /// Bridges Photon Quantum's deterministic rollback simulation with Unity's visual presentation
    /// (Animators, Sumi-e Bloodsplats, Audio Conductor, and Camera Trauma).
    /// </summary>
    public class QuantumCombatBridge : MonoBehaviour
    {
        private static bool _isQuitting = false;
        private static QuantumCombatBridge _instance;
        public static QuantumCombatBridge Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<QuantumCombatBridge>();
                }
                return _instance;
            }
            set => _instance = value;
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        [Header("Local View References")]
        [SerializeField] private FighterController _fighterViewP1;
        [SerializeField] private FighterController _fighterViewP2;

        public FighterController FighterViewP1 => _fighterViewP1;
        public FighterController FighterViewP2 => _fighterViewP2;

        public bool IsQuantumActive { get; private set; } = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            FindFightersIfMissing();
            SubscribeSim();
        }

        private void OnEnable()
        {
            SubscribeSim();
        }

        private void OnDisable()
        {
            UnsubscribeSim();
        }

        private void OnDestroy()
        {
            UnsubscribeSim();
            if (_instance == this) _instance = null;
        }

        private void SubscribeSim()
        {
            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                DemonIdol.BlazBlue.BBMatchSim.Instance.OnSimTickCompleted -= HandleSimTickCompleted;
                DemonIdol.BlazBlue.BBMatchSim.Instance.OnSimTickCompleted += HandleSimTickCompleted;
            }
        }

        private void UnsubscribeSim()
        {
            if (DemonIdol.BlazBlue.BBMatchSim.Instance != null)
            {
                DemonIdol.BlazBlue.BBMatchSim.Instance.OnSimTickCompleted -= HandleSimTickCompleted;
            }
        }

        private void HandleSimTickCompleted(DemonIdol.BlazBlue.BBFighterSnapshot p1Snap, DemonIdol.BlazBlue.BBFighterSnapshot p2Snap)
        {
            if (IsQuantumActive)
            {
                ResyncPresentationFromSnapshot(p1Snap, p2Snap);
            }
        }

        /// <summary>
        /// Snaps visual presentation (position, facing, and locomotion animation) directly from deterministic snapshots.
        /// Zero reliance on PhysX, CharacterController velocity, or non-deterministic frame intervals.
        /// </summary>
        public void ResyncPresentationFromSnapshot(DemonIdol.BlazBlue.BBFighterSnapshot p1Snap, DemonIdol.BlazBlue.BBFighterSnapshot p2Snap)
        {
            FindFightersIfMissing();
            ResyncFighterView(_fighterViewP1, p1Snap);
            ResyncFighterView(_fighterViewP2, p2Snap);
        }

        private void ResyncFighterView(FighterController fc, DemonIdol.BlazBlue.BBFighterSnapshot snap)
        {
            if (fc == null) return;

            var cc = fc.GetComponent<CharacterController>();
            if (cc != null && cc.enabled)
            {
                cc.enabled = false;
                fc.transform.position = snap.position;
                cc.enabled = true;
            }
            else
            {
                fc.transform.position = snap.position;
            }

            fc.SetFacing(snap.facingDirection);

            var brain = fc.brain != null ? fc.brain : fc.GetComponent<DemonIdol.BlazBlue.BBFighterBrain>();
            if (brain != null && brain.animDriver != null)
            {
                sbyte facingNumpad = snap.facingDirection > 0 ? (sbyte)6 : (sbyte)4;
                brain.animDriver.UpdateLocomotionState(snap.state, facingNumpad, snap.isGrounded);
            }
        }

        public void BindFighters(FighterController p1, FighterController p2)
        {
            if (Instance == null) Instance = this;
            _fighterViewP1 = p1;
            _fighterViewP2 = p2;
        }

        public void FindFightersIfMissing()
        {
            if (_fighterViewP1 == null || _fighterViewP2 == null)
            {
                var fighters = FindObjectsByType<FighterController>();
                if (fighters != null && fighters.Length >= 2)
                {
                    Array.Sort(fighters, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
                    if (_fighterViewP1 == null) _fighterViewP1 = fighters[0];
                    if (_fighterViewP2 == null) _fighterViewP2 = fighters[1];
                }
                else if (fighters != null && fighters.Length == 1)
                {
                    if (_fighterViewP1 == null) _fighterViewP1 = fighters[0];
                }
            }
        }

        /// <summary>
        /// Called when a Quantum online match begins.
        /// Transfers simulation authority to Quantum's deterministic rollback runner.
        /// </summary>
        public void InitializeQuantumMatch()
        {
            FindFightersIfMissing();
            IsQuantumActive = true;

            int localSlot = QuantumPvPManager.Instance != null ? QuantumPvPManager.Instance.LocalPlayerSlot : 0;
            if (_fighterViewP1 != null)
            {
                _fighterViewP1.isPlayerControlled = (localSlot == 0);
            }
            if (_fighterViewP2 != null)
            {
                _fighterViewP2.isPlayerControlled = (localSlot == 1);
            }

            // Ensure Quantum view is bound to brains
            var view = FindAnyObjectByType<Quantum.BBMatchQuantumView>();
            if (view != null)
            {
                var brain1 = _fighterViewP1 != null ? _fighterViewP1.brain : null;
                var brain2 = _fighterViewP2 != null ? _fighterViewP2.brain : null;
                view.BindBrains(brain1, brain2);
            }

            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.PauseSimulation(false);
            }

            Debug.Log($"<color=#C73E2E><b>[Quantum Combat]</b></color> Photon Quantum Rollback Simulation Initialized (Local Slot: P{localSlot + 1}).");
        }

        public void ResetQuantumMatch()
        {
            IsQuantumActive = false;
            if (_fighterViewP1 != null)
            {
                _fighterViewP1.isPlayerControlled = true;
                if (_fighterViewP1.brain != null) _fighterViewP1.brain.customInputProvider = null;
            }
            if (_fighterViewP2 != null)
            {
                _fighterViewP2.isPlayerControlled = false;
                if (_fighterViewP2.brain != null) _fighterViewP2.brain.customInputProvider = null;
            }
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.PauseSimulation(false);
            }
        }

        /// <summary>
        /// Injects remote opponent input into the target fighter's InputBuffer and movement controller.
        /// </summary>
        public void InjectRemoteInput(int slot, QuantumFighterInput input)
        {
            FighterController target = (slot == 0) ? _fighterViewP1 : _fighterViewP2;
            if (target == null) return;

            int currentTick = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : (DemonIdol.BlazBlue.BBMatchSim.Instance != null ? DemonIdol.BlazBlue.BBMatchSim.Instance.currentTick : 0);
            Vector2 moveDir = input.GetDirection();
            target.SetMoveInput(moveDir);

            // Feed directly into BBInputSampler if present on target
            var sampler = target.GetComponent<DemonIdol.BlazBlue.BBInputSampler>();
            if (sampler != null)
            {
                sampler.virtualStickInput = moveDir;
                byte held = 0;
                if (input.HasButton(InputButton.Light)) held |= (byte)DemonIdol.BlazBlue.BBButton.A;
                if (input.HasButton(InputButton.Medium)) held |= (byte)DemonIdol.BlazBlue.BBButton.B;
                if (input.HasButton(InputButton.Heavy)) held |= (byte)DemonIdol.BlazBlue.BBButton.C;
                if (input.HasButton(InputButton.DriveSkill)) held |= (byte)DemonIdol.BlazBlue.BBButton.D;
                sampler.virtualHeldMask = held;
            }

            foreach (InputButton btn in (InputButton[])Enum.GetValues(typeof(InputButton)))
            {
                if (btn != InputButton.None && input.HasButton(btn))
                {
                    target.InputBuf.Enqueue(btn, currentTick, moveDir);
                }
            }
        }

        #region Quantum Event Dispatchers (View Presentation)

        /// <summary>
        /// Triggered by Quantum Simulation Signal: OnHitLanded
        /// </summary>
        public void OnQuantumHitEvent(int attackerIndex, HitResult result, Vector3 hitPosition, float damage, bool isHeavy)
        {
            // 1. Camera Trauma Shake
            if (FighterCamera2D.Instance != null)
            {
                float trauma = isHeavy ? 0.35f : 0.15f;
                if (result == HitResult.CounterHit) trauma *= 1.6f;
                if (result == HitResult.Parried) trauma = 0.35f;
                FighterCamera2D.Instance.AddTrauma(trauma);
            }

            // 2. Ink & Blood Splatters
            if (InkBloodSplatterManager.Instance != null)
            {
                Color splatColor = (result == HitResult.CounterHit) ? 
                    InkBloodSplatterManager.Instance.toriiVermilionColor : 
                    InkBloodSplatterManager.Instance.sumieInkColor;

                if (result == HitResult.Parried)
                {
                    InkBloodSplatterManager.Instance.SpawnParryFlash(hitPosition);
                }
                else if (result == HitResult.Hit || result == HitResult.CounterHit)
                {
                    InkBloodSplatterManager.Instance.SpawnAirborneBurst(hitPosition, splatColor, isHeavy ? 1.5f : 1.0f);
                    InkBloodSplatterManager.Instance.SpawnGroundDecal(new Vector3(hitPosition.x, 0.015f, 0f), splatColor, isHeavy ? 1.5f : 1.0f);
                }
            }

            // 3. Audio Triggers
            if (CombatAudioDirector.Instance != null)
            {
                if (result == HitResult.Parried) CombatAudioDirector.Instance.PlayParry();
                else if (result == HitResult.Blocked) CombatAudioDirector.Instance.PlayBlock();
                else if (result == HitResult.CounterHit || isHeavy) CombatAudioDirector.Instance.PlayHitHeavy();
                else CombatAudioDirector.Instance.PlayHitLight();
            }

            // 4. Native iPhone Taptic Haptics
            if (result == HitResult.Parried) MobileHaptics.Trigger(HapticImpact.Parry);
            else if (result == HitResult.CounterHit) MobileHaptics.Trigger(HapticImpact.CounterHit);
            else if (isHeavy) MobileHaptics.Trigger(HapticImpact.Heavy);
            else MobileHaptics.Trigger(HapticImpact.Light);
        }

        /// <summary>
        /// Triggered by Quantum Simulation Signal: OnRapidCancel
        /// </summary>
        public void OnQuantumRapidCancel(int playerIndex)
        {
            if (CombatAudioDirector.Instance != null) CombatAudioDirector.Instance.PlayRapidCancel();
            if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.AddTrauma(0.25f);
            MobileHaptics.Trigger(HapticImpact.Medium);
        }

        /// <summary>
        /// Triggered by Quantum Simulation Signal: OnDomainExpansion
        /// </summary>
        public void OnQuantumDomainExpansion(int playerIndex)
        {
            if (CombatAudioDirector.Instance != null) CombatAudioDirector.Instance.PlayDomainActivation();
            if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.AddTrauma(0.6f);
            MobileHaptics.Trigger(HapticImpact.Heavy);
        }

        #endregion
    }
}
