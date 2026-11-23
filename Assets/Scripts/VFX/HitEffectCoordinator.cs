using System;
using UnityEngine;
using DemonIdol.Audio;
using DemonIdol.VFX;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Coordinates audio-visual impact feel:
    /// Cartoon FX hit sparks, blood shapes, parry flashes, camera trauma shake,
    /// and audio impact SFX via CombatAudioDirector.
    /// Features dynamic fighter auto-discovery and auto-binding.
    /// </summary>
    public class HitEffectCoordinator : MonoBehaviour
    {
        private static HitEffectCoordinator _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        public static HitEffectCoordinator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<HitEffectCoordinator>();
                    if (_instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("HitEffectCoordinator");
                        _instance = go.AddComponent<HitEffectCoordinator>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Fighter References")]
        [SerializeField] private FighterController _fighter1;
        [SerializeField] private FighterController _fighter2;

        [Header("Cartoon FX Prefabs (JMO Assets Auto-Hooked)")]
        [SerializeField] private GameObject _hitSparkPrefab;        // CFXR Hit A (Red)
        [SerializeField] private GameObject _counterHitSparkPrefab; // CFXR _SLASH_ or Sword Hit Cross
        [SerializeField] private GameObject _blockSparkPrefab;      // CFXR Impact Glowing HDR (Blue)
        [SerializeField] private GameObject _parrySparkPrefab;      // CFXR Hit D 3D (Yellow)
        [SerializeField] private GameObject _groundHitPrefab;       // CFXR2 Ground Hit (Knockdown/Bound)
        [SerializeField] private GameObject _bloodSplashPrefab;     // CFXR2 Blood Shape Splash
        [SerializeField] private GameObject _slashSparkPrefab;      // CFXR4 Sword Hit PLAIN (Cross)

        [Header("3D Shockwave Prefabs (Kevin Iglesias)")]
        [SerializeField] private GameObject _spellShockwaveExplosionPrefab;
        [SerializeField] private GameObject _spellShockwaveGroundPrefab;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            FulfillPrefabsIfMissing();
        }

        private float _lastSearchTime = -10f;

        private void Start()
        {
            FindFightersIfMissing();
        }

        private void Update()
        {
            // Only search periodically if neither fighter is bound (prevents per-frame GC spikes)
            if (_fighter1 == null && _fighter2 == null && Time.unscaledTime - _lastSearchTime > 2.0f)
            {
                _lastSearchTime = Time.unscaledTime;
                FindFightersIfMissing();
            }
        }

        public void BindFighters(FighterController f1, FighterController f2)
        {
            UnsubscribeFighters();

            _fighter1 = f1;
            _fighter2 = f2;

            SubscribeFighters();
        }

        #region Public UFE / Direct VFX Triggers

        public void TriggerHitEffect(Vector3 hitPosition, float damage, bool isCounterHit, bool isHeavy)
        {
            // 1. Camera Trauma Shake (Subtle, non-disorienting impact feel)
            if (FighterCamera2D.Instance != null)
            {
                float shake = isHeavy ? 0.12f : 0.05f;
                if (isCounterHit) shake *= 1.25f;
                FighterCamera2D.Instance.AddTrauma(shake);
            }

            // 2. Mobile Haptics
            if (isCounterHit) MobileHaptics.Trigger(HapticImpact.CounterHit);
            else if (isHeavy) MobileHaptics.Trigger(HapticImpact.Heavy);
            else MobileHaptics.Trigger(HapticImpact.Light);

            // 3. Audio SFX
            if (CombatAudioDirector.Instance != null)
            {
                if (isCounterHit || isHeavy) CombatAudioDirector.Instance.PlayHitHeavy();
                else CombatAudioDirector.Instance.PlayHitLight();
            }

            // 4. Cartoon FX Spark & Blood
            GameObject sparkPrefab = isCounterHit ? (_counterHitSparkPrefab != null ? _counterHitSparkPrefab : _hitSparkPrefab) : _hitSparkPrefab;
            if (sparkPrefab != null)
            {
                GameObject spark = Instantiate(sparkPrefab, hitPosition, Quaternion.identity);
                Destroy(spark, 1.5f);
            }

            if (InkBloodSplatterManager.Instance != null)
            {
                Color splatColor = isCounterHit ? InkBloodSplatterManager.Instance.toriiVermilionColor : InkBloodSplatterManager.Instance.sumieInkColor;
                InkBloodSplatterManager.Instance.SpawnAirborneBurst(hitPosition, splatColor, isHeavy ? 1.5f : 1.0f);

                // Ground ink / blood decal with smooth GroundDecalFader
                Vector3 groundPos = new Vector3(hitPosition.x + UnityEngine.Random.Range(-0.25f, 0.25f), 0.015f, 0f);
                InkBloodSplatterManager.Instance.SpawnGroundDecal(groundPos, splatColor, isHeavy ? 1.5f : 1.0f);
            }
        }

        public void SpawnHitBurst(Vector3 hitPosition, bool isCounter = false, bool isHeavy = true)
        {
            TriggerHitEffect(hitPosition, isHeavy ? 60f : 30f, isCounter, isHeavy);
        }

        public void TriggerBlockEffect(Vector3 blockPosition)
        {
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.AddTrauma(0.08f);
            }

            MobileHaptics.Trigger(HapticImpact.Light);

            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayBlock();
            }

            if (_blockSparkPrefab != null)
            {
                GameObject spark = Instantiate(_blockSparkPrefab, blockPosition, Quaternion.identity);
                Destroy(spark, 1.2f);
            }
        }

        public void TriggerParryEffect(Vector3 parryPosition)
        {
            if (FighterCamera2D.Instance != null)
            {
                FighterCamera2D.Instance.AddTrauma(0.35f);
            }

            MobileHaptics.Trigger(HapticImpact.Parry);

            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayParry();
            }

            if (_parrySparkPrefab != null)
            {
                GameObject spark = Instantiate(_parrySparkPrefab, parryPosition, Quaternion.identity);
                Destroy(spark, 1.5f);
            }

            if (InkBloodSplatterManager.Instance != null)
            {
                InkBloodSplatterManager.Instance.SpawnParryFlash(parryPosition);
            }
        }

        #endregion

        private void FindFightersIfMissing()
        {
            if (_fighter1 != null && _fighter2 != null) return;

            var fighters = FindObjectsByType<FighterController>();
            if (fighters == null || fighters.Length == 0) return;

            if (fighters.Length >= 2)
            {
                FighterController f1 = fighters[0];
                FighterController f2 = fighters[1];
                if (!f1.isPlayerControlled && f2.isPlayerControlled)
                {
                    f1 = fighters[1];
                    f2 = fighters[0];
                }
                BindFighters(f1, f2);
            }
            else if (fighters.Length == 1 && _fighter1 == null)
            {
                BindFighters(fighters[0], null);
            }
        }

        private void SubscribeFighters()
        {
            if (_fighter1 != null)
            {
                _fighter1.OnHitProcessed -= HandleHitEvent;
                _fighter1.OnRapidCanceled -= HandleRapidCancel;
                _fighter1.OnDomainTriggered -= HandleDomainTriggered;

                _fighter1.OnHitProcessed += HandleHitEvent;
                _fighter1.OnRapidCanceled += HandleRapidCancel;
                _fighter1.OnDomainTriggered += HandleDomainTriggered;
            }

            if (_fighter2 != null)
            {
                _fighter2.OnHitProcessed -= HandleHitEvent;
                _fighter2.OnRapidCanceled -= HandleRapidCancel;
                _fighter2.OnDomainTriggered -= HandleDomainTriggered;

                _fighter2.OnHitProcessed += HandleHitEvent;
                _fighter2.OnRapidCanceled += HandleRapidCancel;
                _fighter2.OnDomainTriggered += HandleDomainTriggered;
            }
        }

        private void UnsubscribeFighters()
        {
            if (_fighter1 != null)
            {
                _fighter1.OnHitProcessed -= HandleHitEvent;
                _fighter1.OnRapidCanceled -= HandleRapidCancel;
                _fighter1.OnDomainTriggered -= HandleDomainTriggered;
            }

            if (_fighter2 != null)
            {
                _fighter2.OnHitProcessed -= HandleHitEvent;
                _fighter2.OnRapidCanceled -= HandleRapidCancel;
                _fighter2.OnDomainTriggered -= HandleDomainTriggered;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFighters();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void FulfillPrefabsIfMissing()
        {
#if UNITY_EDITOR
            if (_hitSparkPrefab == null)
                _hitSparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab");
            if (_counterHitSparkPrefab == null)
                _counterHitSparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab");
            if (_blockSparkPrefab == null)
                _blockSparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab");
            if (_parrySparkPrefab == null)
                _parrySparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Flash.prefab");
            if (_bloodSplashPrefab == null)
                _bloodSplashPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR2 Blood Shape Splash.prefab");
            if (_groundHitPrefab == null)
                _groundHitPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR2 Blood (Directional).prefab");
            if (_slashSparkPrefab == null)
                _slashSparkPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab");
            if (_spellShockwaveExplosionPrefab == null)
                _spellShockwaveExplosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Spellcasting Animations/Prefabs/Spells/Human_Spell_Shockwave_Explosion.prefab");
            if (_spellShockwaveGroundPrefab == null)
                _spellShockwaveGroundPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Spellcasting Animations/Prefabs/Spells/Human_Spell_Shockwave_Ground.prefab");
#endif
        }

        private void HandleHitEvent(HitResult result, AttackData attack, Vector3 hitPoint)
        {
            if (result == HitResult.Whiff || result == HitResult.Crushed) return;

            // 0. Grab Break Tech Event Handling
            if (result == HitResult.Teched)
            {
                if (FighterCamera2D.Instance != null) FighterCamera2D.Instance.AddTrauma(0.18f);
                MobileHaptics.Trigger(HapticImpact.Medium);
                if (CombatAudioDirector.Instance != null) CombatAudioDirector.Instance.PlayParry();

                // Clash Sparks & Shockwave
                if (_parrySparkPrefab != null) Instantiate(_parrySparkPrefab, hitPoint, Quaternion.identity);
                else if (_blockSparkPrefab != null) Instantiate(_blockSparkPrefab, hitPoint, Quaternion.identity);
                ShockwaveDistortionEmitter.EmitShockwave(hitPoint, 1.8f, 0.22f);
                return;
            }

            // 1. Camera Trauma Shake (Subtle, non-disorienting impact feel)
            if (FighterCamera2D.Instance != null && attack != null)
            {
                float shake = attack.screenShakeTrauma * 0.35f;
                if (result == HitResult.CounterHit) shake *= 1.25f;
                if (result == HitResult.Parried) shake = 0.08f;
                FighterCamera2D.Instance.AddTrauma(shake);
            }

            // 2. Mobile Haptics (iPhone CoreHaptics / Native)
            switch (result)
            {
                case HitResult.Parried:
                    MobileHaptics.Trigger(HapticImpact.Parry);
                    break;
                case HitResult.Blocked:
                case HitResult.BarrierBlocked:
                    MobileHaptics.Trigger(HapticImpact.Medium);
                    break;
                case HitResult.CounterHit:
                    MobileHaptics.Trigger(HapticImpact.CounterHit);
                    break;
                case HitResult.Hit:
                    if (attack != null && attack.attackType == AttackType.Heavy)
                        MobileHaptics.Trigger(HapticImpact.Heavy);
                    else if (attack != null && attack.attackType == AttackType.Medium)
                        MobileHaptics.Trigger(HapticImpact.Medium);
                    else
                        MobileHaptics.Trigger(HapticImpact.Light);
                    break;
            }

            // 3. Audio SFX playback via CombatAudioDirector
            if (CombatAudioDirector.Instance != null)
            {
                switch (result)
                {
                    case HitResult.Parried:
                        CombatAudioDirector.Instance.PlayParry();
                        break;
                    case HitResult.Blocked:
                    case HitResult.BarrierBlocked:
                        CombatAudioDirector.Instance.PlayBlock();
                        break;
                    case HitResult.CounterHit:
                        CombatAudioDirector.Instance.PlayHitHeavy();
                        break;
                    case HitResult.Hit:
                        if (attack != null && attack.attackType == AttackType.Heavy)
                            CombatAudioDirector.Instance.PlayHitHeavy();
                        else if (attack != null && attack.attackType == AttackType.Medium)
                            CombatAudioDirector.Instance.PlayHitMedium();
                        else
                            CombatAudioDirector.Instance.PlayHitLight();
                        break;
                }
            }

            // 4. Spawn Cartoon FX Particles & Blood Shapes
            GameObject sparkPrefab = null;
            switch (result)
            {
                case HitResult.Parried:
                    sparkPrefab = _parrySparkPrefab;
                    break;
                case HitResult.Blocked:
                case HitResult.BarrierBlocked:
                    sparkPrefab = _blockSparkPrefab;
                    break;
                case HitResult.CounterHit:
                    sparkPrefab = _counterHitSparkPrefab != null ? _counterHitSparkPrefab : _hitSparkPrefab;
                    break;
                case HitResult.Hit:
                    sparkPrefab = _hitSparkPrefab;
                    break;
            }

            if (sparkPrefab != null)
            {
                Instantiate(sparkPrefab, hitPoint, Quaternion.identity);
            }

            // Determine attack facing direction for directional bursts
            float facingDir = 1f;
            if (_fighter1 != null && (_fighter1.State == FighterState.AttackActive || _fighter1.State == FighterState.AttackStartup))
                facingDir = _fighter1.FacingDirection;
            else if (_fighter2 != null && (_fighter2.State == FighterState.AttackActive || _fighter2.State == FighterState.AttackStartup))
                facingDir = _fighter2.FacingDirection;

            bool isHeavyOrSuper = attack != null && (attack.attackType == AttackType.Heavy || attack.attackType == AttackType.DomainSuper);

            // 5. Shockwave Effects: Screen-space distortion ring + 3D particle spell shockwave
            if (result == HitResult.Hit || result == HitResult.CounterHit)
            {
                // Screen distortion ring
                ShockwaveDistortionEmitter.EmitShockwave(hitPoint, isHeavyOrSuper ? 2.8f : 1.8f, isHeavyOrSuper ? 0.32f : 0.22f);

                // 3D spell shockwave blast on heavy/counter/super impacts
                if ((isHeavyOrSuper || result == HitResult.CounterHit) && _spellShockwaveExplosionPrefab != null)
                {
                    GameObject swBlast = Instantiate(_spellShockwaveExplosionPrefab, hitPoint, Quaternion.identity);
                    Destroy(swBlast, 2.5f);
                }
            }

            // 6. Directional Blood Splatter & Ground Decals
            if (result == HitResult.Hit || result == HitResult.CounterHit)
            {
                // Directional Cartoon FX blood splash oriented along strike direction
                if (_bloodSplashPrefab != null)
                {
                    Quaternion bloodRot = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), facingDir > 0 ? 0f : 180f, UnityEngine.Random.Range(-25f, 25f));
                    GameObject bloodGo = Instantiate(_bloodSplashPrefab, hitPoint, bloodRot);
                    float bScale = isHeavyOrSuper ? 1.5f : 1.0f;
                    if (result == HitResult.CounterHit) bScale *= 1.3f;
                    bloodGo.transform.localScale = Vector3.one * bScale;
                    Destroy(bloodGo, 2.0f);
                }

                // Sumi-e stylized ink / blood airborne burst & persistent ground decals
                if (InkBloodSplatterManager.Instance != null)
                {
                    Color splatColor = (result == HitResult.CounterHit) ? InkBloodSplatterManager.Instance.toriiVermilionColor : InkBloodSplatterManager.Instance.sumieInkColor;
                    InkBloodSplatterManager.Instance.SpawnAirborneBurst(hitPoint, splatColor, isHeavyOrSuper ? 1.5f : 1.0f);

                    Vector3 groundPos = new Vector3(hitPoint.x + UnityEngine.Random.Range(-0.25f, 0.25f), 0.015f, 0f);
                    InkBloodSplatterManager.Instance.SpawnGroundDecal(groundPos, splatColor, isHeavyOrSuper ? 1.5f : 1.0f);
                }
            }

            // 7. Ground shockwave / impact dust on knockdowns & bounds
            if (attack != null && attack.causesKnockdown)
            {
                Vector3 groundImpactPos = new Vector3(hitPoint.x, 0.05f, 0f);
                if (_spellShockwaveGroundPrefab != null)
                {
                    GameObject groundSw = Instantiate(_spellShockwaveGroundPrefab, groundImpactPos, Quaternion.identity);
                    Destroy(groundSw, 3.0f);
                }
                else if (_groundHitPrefab != null)
                {
                    Instantiate(_groundHitPrefab, groundImpactPos, Quaternion.identity);
                }
            }
        }

        private void HandleRapidCancel()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayRapidCancel();
            }
        }

        private void HandleDomainTriggered()
        {
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayDomainActivation();
            }
        }
    }
}
