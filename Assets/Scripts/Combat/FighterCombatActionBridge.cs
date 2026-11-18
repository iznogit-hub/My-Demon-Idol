using System;
using UnityEngine;
using UnityEngine.Events;

namespace DemonIdol.Combat
{
    [Serializable]
    public class FighterHitActionEvent : UnityEvent<HitResult, AttackData, Vector3> { }

    [Serializable]
    public class FighterStateActionEvent : UnityEvent<FighterState> { }

    [Serializable]
    public class FighterFloatActionEvent : UnityEvent<float, float> { }

    /// <summary>
    /// Unity Actions & Events bridge component for Demon Idol.
    /// Exposes high-level UnityEvents in the Inspector for artists, level designers,
    /// and cinematographers to trigger cameras, particles, lighting, screenshake,
    /// and audio directly from in-engine fighting game events without writing C#.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class FighterCombatActionBridge : MonoBehaviour
    {
        [Header("Target Fighter")]
        [SerializeField] private FighterController _fighter;

        [Header("Unity Action Callbacks")]
        public FighterHitActionEvent onHitConfirmed = new FighterHitActionEvent();
        public FighterHitActionEvent onCounterHit = new FighterHitActionEvent();
        public FighterHitActionEvent onParrySuccessful = new FighterHitActionEvent();
        public FighterStateActionEvent onStateChanged = new FighterStateActionEvent();
        public FighterFloatActionEvent onHealthChanged = new FighterFloatActionEvent();
        public FighterFloatActionEvent onSuperMeterChanged = new FighterFloatActionEvent();
        public UnityEvent onRapidCanceled = new UnityEvent();
        public UnityEvent onDomainSuperTriggered = new UnityEvent();
        public UnityEvent onDefeated = new UnityEvent();

        private void Awake()
        {
            if (_fighter == null) _fighter = GetComponent<FighterController>();
        }

        private void OnEnable()
        {
            if (_fighter == null) return;
            _fighter.OnHitProcessed += HandleHitProcessed;
            _fighter.OnStateChanged += HandleStateChanged;
            _fighter.OnHealthChanged += HandleHealthChanged;
            _fighter.OnSuperMeterChanged += HandleSuperMeterChanged;
            _fighter.OnRapidCanceled += HandleRapidCanceled;
            _fighter.OnDomainTriggered += HandleDomainTriggered;
        }

        private void OnDisable()
        {
            if (_fighter == null) return;
            _fighter.OnHitProcessed -= HandleHitProcessed;
            _fighter.OnStateChanged -= HandleStateChanged;
            _fighter.OnHealthChanged -= HandleHealthChanged;
            _fighter.OnSuperMeterChanged -= HandleSuperMeterChanged;
            _fighter.OnRapidCanceled -= HandleRapidCanceled;
            _fighter.OnDomainTriggered -= HandleDomainTriggered;
        }

        private void HandleHitProcessed(HitResult result, AttackData attack, Vector3 hitPoint)
        {
            onHitConfirmed?.Invoke(result, attack, hitPoint);
            if (result == HitResult.CounterHit) onCounterHit?.Invoke(result, attack, hitPoint);
            else if (result == HitResult.Parried) onParrySuccessful?.Invoke(result, attack, hitPoint);
        }

        private void HandleStateChanged(FighterState state)
        {
            onStateChanged?.Invoke(state);
            if (state == FighterState.Dead) onDefeated?.Invoke();
        }

        private void HandleHealthChanged(float cur, float max) => onHealthChanged?.Invoke(cur, max);
        private void HandleSuperMeterChanged(float cur, float max) => onSuperMeterChanged?.Invoke(cur, max);
        private void HandleRapidCanceled() => onRapidCanceled?.Invoke();
        private void HandleDomainTriggered() => onDomainSuperTriggered?.Invoke();
    }
}
