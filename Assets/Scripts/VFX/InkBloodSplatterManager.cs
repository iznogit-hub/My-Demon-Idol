using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.VFX
{
    /// <summary>
    /// Master Sumi-e ink & blood splatter manager.
    /// Manages object-pooled airborne particle bursts, ground paver ink/blood decals,
    /// and Torii Vermilion counter-hit explosions.
    /// Includes runtime procedural fallback textures so it runs 100% out of the box.
    /// </summary>
    public class InkBloodSplatterManager : MonoBehaviour
    {
        private static InkBloodSplatterManager _instance;
        public static InkBloodSplatterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<InkBloodSplatterManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("InkBloodSplatterManager");
                        _instance = go.AddComponent<InkBloodSplatterManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Palette Tokens")]
        public Color sumieInkColor = new Color(0.72f, 0.12f, 0.12f, 0.95f);       // Deep Crimson Blood
        public Color toriiVermilionColor = new Color(0.92f, 0.22f, 0.18f, 0.98f); // Blazing Vermilion Counter Blood
        public Color parryGlowColor = new Color(1.0f, 0.95f, 0.75f, 0.95f);       // Golden Talisman White

        [Header("Prefabs (Optional - Will use procedural fallback if null)")]
        [SerializeField] private GameObject _inkBurstParticlePrefab;
        [SerializeField] private GameObject _groundDecalPrefab;
        [SerializeField] private GameObject _parryFlashPrefab;

        [Header("Pool Capacities")]
        [SerializeField] private int _burstPoolSize = 16;
        [SerializeField] private int _decalPoolSize = 32;

        private readonly Queue<GameObject> _burstPool = new Queue<GameObject>();
        private readonly Queue<GameObject> _decalPool = new Queue<GameObject>();

        private Sprite _proceduralInkDecalSprite;
        private Material _proceduralVfxMaterial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            FulfillPrefabsIfMissing();
            GenerateProceduralFallbacks();
            InitializePools();
        }

        private void FulfillPrefabsIfMissing()
        {
#if UNITY_EDITOR
            if (_inkBurstParticlePrefab == null)
                _inkBurstParticlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR2 Blood Shape Splash.prefab");
            if (_groundDecalPrefab == null)
                _groundDecalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Liquids/CFXR2 Blood (Directional).prefab");
            if (_parryFlashPrefab == null)
                _parryFlashPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Flash.prefab");
#endif
        }

        public void BindFighters(FighterController f1, FighterController f2)
        {
            if (f1 != null)
            {
                f1.OnHitProcessed -= HandleFighterHit;
                f1.OnHitProcessed += HandleFighterHit;
            }
            if (f2 != null)
            {
                f2.OnHitProcessed -= HandleFighterHit;
                f2.OnHitProcessed += HandleFighterHit;
            }
        }

        private float _lastFighterCheck = -10f;

        private void Start()
        {
            AutoWireFighters();
        }

        private void Update()
        {
            if (Time.unscaledTime - _lastFighterCheck > 2.0f)
            {
                _lastFighterCheck = Time.unscaledTime;
                AutoWireFighters();
            }
        }

        public void AutoWireFighters()
        {
            FighterController[] fighters = FindObjectsByType<FighterController>();
            foreach (var f in fighters)
            {
                if (f != null)
                {
                    f.OnHitProcessed -= HandleFighterHit;
                    f.OnHitProcessed += HandleFighterHit;
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            FighterController[] fighters = FindObjectsByType<FighterController>();
            foreach (var f in fighters)
            {
                if (f != null) f.OnHitProcessed -= HandleFighterHit;
            }
        }

        private void HandleFighterHit(HitResult result, AttackData attack, Vector3 hitPoint)
        {
            if (result == HitResult.Whiff || result == HitResult.Crushed) return;

            bool isCounterHit = (result == HitResult.CounterHit);
            bool isHeavy = (attack != null && attack.attackType == AttackType.Heavy);

            if (result == HitResult.Parried)
            {
                SpawnParryFlash(hitPoint);
                return;
            }

            if (result == HitResult.Hit || isCounterHit)
            {
                // 1. Airborne Ink/Blood Burst
                Color burstColor = isCounterHit ? toriiVermilionColor : sumieInkColor;
                float scale = isHeavy ? 1.6f : (isCounterHit ? 1.3f : 1.0f);
                SpawnAirborneBurst(hitPoint, burstColor, scale);

                // 2. Ground Paver Splat Decal
                Vector3 groundPos = new Vector3(hitPoint.x + Random.Range(-0.3f, 0.3f), 0.015f, 0f);
                SpawnGroundDecal(groundPos, burstColor, scale);
            }
        }

        #region Spawning & Pooling

        public void SpawnAirborneBurst(Vector3 position, Color color, float scaleMultiplier = 1.0f)
        {
            GameObject obj = GetFromPool(_burstPool, _inkBurstParticlePrefab, "InkBurst_Instance");
            obj.transform.position = position;
            obj.transform.localScale = Vector3.one * scaleMultiplier;

            ParticleSystem ps = obj.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
                ps.Clear();
                ps.Play();
            }

            obj.SetActive(true);
        }

        public void SpawnGroundDecal(Vector3 groundPosition, Color color, float scaleMultiplier = 1.0f)
        {
            GameObject obj = GetFromPool(_decalPool, _groundDecalPrefab, "GroundDecal_Instance");
            obj.transform.position = groundPosition;

            GroundDecalFader fader = obj.GetComponent<GroundDecalFader>();
            if (fader != null)
            {
                fader.Initialize(color, scaleMultiplier);
            }

            obj.SetActive(true);
        }

        public void SpawnParryFlash(Vector3 position)
        {
            SpawnAirborneBurst(position, parryGlowColor, 1.8f);
        }

        private GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab, string defaultName)
        {
            while (pool != null && pool.Count > 0)
            {
                GameObject pooled = pool.Dequeue();
                if (pooled != null)
                {
                    pool.Enqueue(pooled); // Circular reuse
                    return pooled;
                }
            }

            // Fallback if pool is empty or objects were destroyed
            GameObject newObj = prefab != null ? 
                Instantiate(prefab, transform) : 
                (defaultName.Contains("Burst") ? CreateProceduralBurstParticle(defaultName) : CreateProceduralDecalObject(defaultName));
            
            if (newObj != null && pool != null)
            {
                pool.Enqueue(newObj);
            }
            return newObj;
        }

        #endregion

        #region Procedural Fallbacks (Guarantees zero missing asset errors)

        private void InitializePools()
        {
            // Burst Pool
            for (int i = 0; i < _burstPoolSize; i++)
            {
                GameObject obj = _inkBurstParticlePrefab != null ? 
                    Instantiate(_inkBurstParticlePrefab, transform) : 
                    CreateProceduralBurstParticle("InkBurst_Pooled");

                obj.SetActive(false);
                _burstPool.Enqueue(obj);
            }

            // Decal Pool
            for (int i = 0; i < _decalPoolSize; i++)
            {
                GameObject obj = _groundDecalPrefab != null ? 
                    Instantiate(_groundDecalPrefab, transform) : 
                    CreateProceduralDecalObject("GroundDecal_Pooled");

                obj.SetActive(false);
                _decalPool.Enqueue(obj);
            }
        }

        private void GenerateProceduralFallbacks()
        {
            // 64x64 procedural circular ink splatter texture with organic brush edges
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(32, 32);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 8f;
                    float radius = 22f + noise;

                    if (dist < radius)
                    {
                        float alpha = Mathf.SmoothStep(1f, 0f, (dist - 14f) / (radius - 14f));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();

            _proceduralInkDecalSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);

            Shader defaultShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (defaultShader == null) defaultShader = Shader.Find("Sprites/Default");
            _proceduralVfxMaterial = new Material(defaultShader);
        }

        private GameObject CreateProceduralDecalObject(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _proceduralInkDecalSprite;
            sr.material = _proceduralVfxMaterial;
            sr.sortingOrder = 1;

            go.AddComponent<GroundDecalFader>();
            return go;
        }

        private GameObject CreateProceduralBurstParticle(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 8f;
            main.startSize = 0.45f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Disable;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _proceduralVfxMaterial;

            return go;
        }

        #endregion
    }
}
