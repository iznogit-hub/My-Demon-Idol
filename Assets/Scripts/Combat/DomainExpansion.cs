using System;
using UnityEngine;
using DemonIdol.Cutscenes;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Implements 100% Super Meter Domain Expansion (BlazBlue Astral Heat / Distortion Drive).
    /// Features:
    /// - Unique combat_transaction_id guaranteeing exactly-once meter spend and damage resolution.
    /// - 20-tick authored simulation superfreeze window.
    /// - Connecting MP4 ultimate cinematic presentation with zero-stutter fallback to live camera + ink VFX.
    /// - Simulation authority contract: game outcomes are NEVER driven by video duration or frame events.
    /// </summary>
    public class DomainExpansion : MonoBehaviour
    {
        public const float DOMAIN_COST = 100.0f;
        public const int CINEMATIC_FREEZE_TICKS = 20;

        public static string ActiveTransactionId { get; private set; } = string.Empty;

        // Cinematic Events
        public event Action<string> OnDomainInitiatedWithToken;
        public event Action OnDomainInitiated;
        public event Action OnDomainResolved;

        public static bool CanExecute(FighterController fighter)
        {
            if (fighter == null) return false;

            bool canAct = fighter.State == FighterState.Idle ||
                          fighter.State == FighterState.WalkCreep ||
                          fighter.State == FighterState.AttackRecovery; // Can cancel recovery of heavy or drive

            return canAct && fighter.currentSuperMeter >= DOMAIN_COST;
        }

        public static bool TriggerDomain(FighterController fighter, AttackData domainData)
        {
            if (!CanExecute(fighter) || domainData == null) return false;

            // Generate unique combat transaction ID tying meter, damage, and cinematic (§4.6)
            string transactionId = "TX_DOMAIN_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            ActiveTransactionId = transactionId;

            // Read authored lock ticks from video definition, fallback to constant 20
            VideoAssetDef def = VideoPathResolver.GetDef("ultimate_astral_kai");
            int freezeTicks = (def != null && def.authoredLockTicks > 0) ? def.authoredLockTicks : CINEMATIC_FREEZE_TICKS;

            fighter.SpendSuperMeter(DOMAIN_COST);
            fighter.ExecuteDomainCinematic(domainData, freezeTicks);

            var domainComp = fighter.GetComponent<DomainExpansion>();
            domainComp?.OnDomainInitiatedWithToken?.Invoke(transactionId);
            domainComp?.OnDomainInitiated?.Invoke();

            // Trigger global simulation superfreeze across all fighters
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.TriggerSuperfreeze(freezeTicks);
            }

            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.TriggerUltimateEntry(freezeTicks / 60f);
            }

            // Atmospheric aura VFX activates during freeze startup;
            // High-impact MP4 cinematic presentation is deferred until strike connection in FighterController.OnHitboxLanded
            if (DemonIdol.VFX.DomainExpansionAOE.Instance != null && fighter.opponent != null)
            {
                DemonIdol.VFX.DomainExpansionAOE.Instance.CastDomainAOE(fighter, fighter.opponent, () =>
                {
                    domainComp?.OnDomainResolved?.Invoke();
                });
            }
            else
            {
                domainComp?.OnDomainResolved?.Invoke();
            }

            return true;
        }

        private static void TriggerLiveFallbackVFX(FighterController fighter, DomainExpansion domainComp)
        {
            if (DemonIdol.VFX.DomainExpansionAOE.Instance != null && fighter.opponent != null)
            {
                DemonIdol.VFX.DomainExpansionAOE.Instance.CastDomainAOE(fighter, fighter.opponent, () =>
                {
                    if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerUltimateExit();
                    domainComp?.OnDomainResolved?.Invoke();
                });
            }
            else
            {
                if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.TriggerUltimateExit();
                domainComp?.OnDomainResolved?.Invoke();
            }
        }
    }
}
