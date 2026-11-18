using System;
using System.Collections;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Audio;

namespace DemonIdol.VFX
{
    /// <summary>
    /// Executes the cinematic Domain Expansion AOE around the target:
    /// Traps the opponent in a runic magic circle, swirls dark souls/ink around them,
    /// performs multi-hit domain slashes, and culminates in a massive domain shatter explosion.
    /// </summary>
    public class DomainExpansionAOE : MonoBehaviour
    {
        private static DomainExpansionAOE _instance;
        public static DomainExpansionAOE Instance
        {
            get
            {
                if (_instance == null) return null;
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Domain AOE Prefabs (Cartoon FX Remaster)")]
        [SerializeField] private GameObject _runicAuraCirclePrefab;   // CFXR3 Magic Aura A (Runic)
        [SerializeField] private GameObject _soulsEscapePrefab;       // CFXR2 Souls Escape
        [SerializeField] private GameObject _domainShatterBurstPrefab;// CFXR2 WW Enemy Explosion or Fire Explosion
        [SerializeField] private GameObject _slashFlurryPrefab;       // CFXR Hit A (Red) or Sword Hit Cross

        [Header("Domain Timing")]
        [SerializeField] private float _trappingDuration = 1.6f;
        [SerializeField] private int _flurrySlashCount = 6;
        [SerializeField] private float _flurryInterval = 0.12f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Casts the Domain Expansion AOE around the opponent.
        /// </summary>
        public void CastDomainAOE(FighterController caster, FighterController target, Action onDomainCompleted = null)
        {
            CastDomainAOE(caster != null ? caster.transform : null, target != null ? target.transform : null, onDomainCompleted);
        }

        /// <summary>
        /// Casts the Domain Expansion AOE around the target Transform (UFE & generic 3D combat compatible).
        /// </summary>
        public void CastDomainAOE(Transform caster, Transform target, Action onDomainCompleted = null)
        {
            if (target == null)
            {
                onDomainCompleted?.Invoke();
                return;
            }

            StartCoroutine(DomainSequenceRoutine(caster, target, onDomainCompleted));
        }

        private IEnumerator DomainSequenceRoutine(Transform caster, Transform target, Action onComplete)
        {
            Vector3 enemyBasePos = target.transform.position;
            Vector3 enemyChestPos = enemyBasePos + new Vector3(0, 1.1f, 0);

            // 1. Audio & Camera Slow-Mo Shock
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayDomainActivation();
            }
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.AddTrauma(0.5f);
            }
            MobileHaptics.Trigger(HapticImpact.Heavy);

            // 2. Spawn Runic Domain Circle on the floor at opponent's feet
            GameObject runicCircle = null;
            if (_runicAuraCirclePrefab != null)
            {
                runicCircle = Instantiate(_runicAuraCirclePrefab, new Vector3(enemyBasePos.x, 0.05f, 0f), Quaternion.Euler(0, 0, 0));
            }

            // 3. Spawn Swirling Souls / Dark Ink vortex around the enemy
            GameObject soulsVortex = null;
            if (_soulsEscapePrefab != null)
            {
                soulsVortex = Instantiate(_soulsEscapePrefab, enemyChestPos, Quaternion.identity);
            }

            // 4. Multi-hit slash flurry inside the domain barrier
            for (int i = 0; i < _flurrySlashCount; i++)
            {
                yield return new WaitForSeconds(_flurryInterval);

                Vector3 jitterPoint = enemyChestPos + new Vector3(
                    UnityEngine.Random.Range(-0.35f, 0.35f),
                    UnityEngine.Random.Range(-0.4f, 0.4f),
                    0f
                );

                if (_slashFlurryPrefab != null)
                {
                    Instantiate(_slashFlurryPrefab, jitterPoint, Quaternion.identity);
                }

                if (CombatAudioDirector.Instance != null)
                {
                    CombatAudioDirector.Instance.PlaySwingLight();
                    CombatAudioDirector.Instance.PlayHitMedium();
                }

                if (FighterCamera2D.Instance != null)
                {
                    FighterCamera2D.Instance.AddTrauma(0.12f);
                }
                MobileHaptics.Trigger(HapticImpact.Medium);
            }

            yield return new WaitForSeconds(0.3f);

            // 5. Final Domain Shatter Explosion!
            if (_domainShatterBurstPrefab != null)
            {
                Instantiate(_domainShatterBurstPrefab, enemyChestPos, Quaternion.identity);
            }

            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayHitHeavy();
            }

            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.AddTrauma(0.65f);
            }
            MobileHaptics.Trigger(HapticImpact.CounterHit);

            // Clean up persistent domain aura objects
            if (runicCircle != null) Destroy(runicCircle, _trappingDuration);
            if (soulsVortex != null) Destroy(soulsVortex, _trappingDuration);

            onComplete?.Invoke();
        }
    }
}
