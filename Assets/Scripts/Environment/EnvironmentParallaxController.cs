using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Multi-Layer Environment and Character Parallax Controller.
    /// Provides production-grade 2.5D background depth scaling, camera tracking isolation,
    /// high-frequency screen-shake filtering, and procedural Japanese Torii / Shrine backdrop integration.
    /// </summary>
    public class EnvironmentParallaxController : MonoBehaviour
    {
        private static EnvironmentParallaxController _instance;
        public static EnvironmentParallaxController Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        [System.Serializable]
        public class ParallaxLayer
        {
            public string layerName = "Layer";
            public Transform layerTransform;
            [Range(-1.5f, 1.5f)] public float parallaxFactorX = 0.2f;
            [Range(0f, 0.5f)] public float parallaxFactorY = 0.05f;
            public bool clampHorizontal = true;
            public float minX = -8f;
            public float maxX = 8f;
            public float autoDriftSpeedX = 0f;

            [HideInInspector] public Vector3 initialLocalPos;
            [HideInInspector] public float currentDrift = 0f;
        }

        [Header("Camera & Tracking Sources")]
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Transform _fighter1;
        [SerializeField] private Transform _fighter2;
        [SerializeField] private Vector3 _arenaOrigin = Vector3.zero;

        [Header("Smoothing & Shake Isolation")]
        [Range(1f, 30f)] public float parallaxSmoothSpeed = 12f;
        [Tooltip("If true, filters out rapid camera trauma so backgrounds do not jitter during hitstop")]
        public bool filterScreenShake = true;

        [Header("Parallax Layers (Far to Near)")]
        [SerializeField] private List<ParallaxLayer> _layers = new List<ParallaxLayer>();
        public List<ParallaxLayer> Layers => _layers;

        [Header("Atmospheric Drift")]
        public bool enableAtmosphericDrift = true;

        private Vector3 _smoothedCamPos;
        private Vector3 _camVelocity;
        private Vector3 _referenceInitialCamPos;
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            ResolveCameraAndFighters();
            InitializeLayerPositions();
        }

        private void Start()
        {
            ResolveCameraAndFighters();
            if (!_isInitialized)
            {
                InitializeLayerPositions();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void BindCamera(Camera cam)
        {
            _targetCamera = cam;
            if (_targetCamera != null)
            {
                _referenceInitialCamPos = _targetCamera.transform.position;
                _smoothedCamPos = _referenceInitialCamPos;
            }
        }

        public void BindFighters(Transform f1, Transform f2)
        {
            _fighter1 = f1;
            _fighter2 = f2;
        }

        public void AddLayer(Transform layerTransform, float factorX, float factorY, string name = "CustomLayer", float autoDriftSpeedX = 0f)
        {
            if (layerTransform == null) return;

            var layer = new ParallaxLayer
            {
                layerName = name,
                layerTransform = layerTransform,
                parallaxFactorX = factorX,
                parallaxFactorY = factorY,
                autoDriftSpeedX = autoDriftSpeedX,
                initialLocalPos = layerTransform.localPosition
            };
            _layers.Add(layer);
        }

        public void InitializeLayerPositions()
        {
            foreach (var layer in _layers)
            {
                if (layer.layerTransform != null)
                {
                    layer.initialLocalPos = layer.layerTransform.localPosition;
                    layer.currentDrift = 0f;
                }
            }

            if (_targetCamera != null)
            {
                _referenceInitialCamPos = _targetCamera.transform.position;
                _smoothedCamPos = _referenceInitialCamPos;
            }
            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (_targetCamera == null)
            {
                ResolveCameraAndFighters();
                if (_targetCamera == null) return;
            }

            Vector3 currentRawCamPos = _targetCamera.transform.position;

            // Optional: calculate mid-fighter tracking fallback if camera is decoupled
            if (_fighter1 != null && _fighter2 != null)
            {
                Vector3 fightersMid = (_fighter1.position + _fighter2.position) * 0.5f;
                currentRawCamPos.x = Mathf.Lerp(currentRawCamPos.x, fightersMid.x, 0.4f);
            }

            // Low-pass smooth damping to filter out high-frequency shake/trauma
            if (filterScreenShake && Application.isPlaying)
            {
                _smoothedCamPos = Vector3.SmoothDamp(_smoothedCamPos, currentRawCamPos, ref _camVelocity, 1f / parallaxSmoothSpeed, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            else
            {
                _smoothedCamPos = currentRawCamPos;
            }

            Vector3 camDelta = _smoothedCamPos - _referenceInitialCamPos;

            // Update each parallax layer
            float dt = Application.isPlaying ? Time.unscaledDeltaTime : 0.01666f;
            for (int i = 0; i < _layers.Count; i++)
            {
                ParallaxLayer layer = _layers[i];
                if (layer.layerTransform == null) continue;

                if (enableAtmosphericDrift && Mathf.Abs(layer.autoDriftSpeedX) > 0.0001f)
                {
                    layer.currentDrift += layer.autoDriftSpeedX * dt;
                }

                float targetOffsetX = (camDelta.x * layer.parallaxFactorX) + layer.currentDrift;
                float targetOffsetY = camDelta.y * layer.parallaxFactorY;

                if (layer.clampHorizontal)
                {
                    targetOffsetX = Mathf.Clamp(targetOffsetX, layer.minX, layer.maxX);
                }

                Vector3 targetLocalPos = layer.initialLocalPos + new Vector3(targetOffsetX, targetOffsetY, 0f);
                if (Application.isPlaying)
                {
                    layer.layerTransform.localPosition = Vector3.Lerp(layer.layerTransform.localPosition, targetLocalPos, 14f * dt);
                }
                else
                {
                    layer.layerTransform.localPosition = targetLocalPos;
                }
            }
        }

        private void ResolveCameraAndFighters()
        {
            if (_targetCamera == null)
            {
                if (FighterCamera2D.Instance != null && FighterCamera2D.Instance.GetComponent<Camera>() != null)
                {
                    _targetCamera = FighterCamera2D.Instance.GetComponent<Camera>();
                }
                else
                {
                    _targetCamera = Camera.main;
                }

                if (_targetCamera != null)
                {
                    _referenceInitialCamPos = _targetCamera.transform.position;
                    _smoothedCamPos = _referenceInitialCamPos;
                }
            }

            if (_fighter1 == null || _fighter2 == null)
            {
                var fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
                if (fighters != null && fighters.Length >= 2)
                {
                    _fighter1 = fighters[0].transform;
                    _fighter2 = fighters[1].transform;
                }
            }
        }

        /// <summary>
        /// Procedural Generator for Japanese Torii Shrine / Celestial Blood Moon Parallax Backdrop.
        /// Instantiates 4 structured atmospheric depth layers with Universal Render Pipeline (URP) materials.
        /// </summary>
        public static GameObject CreateProceduralSanctumLayers(Transform parent = null)
        {
            GameObject root = new GameObject("Environment_ParallaxSanctum");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            var controller = root.AddComponent<EnvironmentParallaxController>();

            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit == null) urpUnlit = Shader.Find("Unlit/Color");

            // --- LAYER 1: Deep Sky & Celestial Blood Moon (Z = 45m) ---
            GameObject skyMoonLayer = new GameObject("Layer_01_FarCelestialSky");
            skyMoonLayer.transform.SetParent(root.transform, false);
            skyMoonLayer.transform.localPosition = new Vector3(0, 10f, 45f);

            GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "BloodMoon_Orb";
            moon.transform.SetParent(skyMoonLayer.transform, false);
            moon.transform.localPosition = new Vector3(8f, 6f, 0f);
            moon.transform.localScale = new Vector3(9f, 9f, 0.2f);
            DestroyImmediate(moon.GetComponent<Collider>());

            var moonMat = new Material(urpUnlit);
            moonMat.SetColor("_BaseColor", new Color(0.95f, 0.22f, 0.20f, 0.95f)); // Torii Crimson Blood Moon
            moon.GetComponent<Renderer>().sharedMaterial = moonMat;

            // Deep background gradient silhouette plane
            GameObject skyPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyPlane.name = "FarSky_GradientPlane";
            skyPlane.transform.SetParent(skyMoonLayer.transform, false);
            skyPlane.transform.localPosition = new Vector3(0, 2f, 2f);
            skyPlane.transform.localScale = new Vector3(120f, 60f, 1f);
            DestroyImmediate(skyPlane.GetComponent<Collider>());

            var skyMat = new Material(urpUnlit);
            skyMat.SetColor("_BaseColor", new Color(0.04f, 0.03f, 0.06f, 1f)); // Midnight Obsidian
            skyPlane.GetComponent<Renderer>().sharedMaterial = skyMat;

            controller.AddLayer(skyMoonLayer.transform, 0.05f, 0.02f, "FarCelestialSky", autoDriftSpeedX: 0.02f);

            // --- LAYER 2: Distant Bamboo Mountain Silhouettes (Z = 28m) ---
            GameObject mountainLayer = new GameObject("Layer_02_MidMountainSilhouettes");
            mountainLayer.transform.SetParent(root.transform, false);
            mountainLayer.transform.localPosition = new Vector3(0, 4f, 28f);

            var mountainMat = new Material(urpUnlit);
            mountainMat.SetColor("_BaseColor", new Color(0.08f, 0.06f, 0.12f, 0.9f)); // Sumi-e Ink Mist Violet

            for (int i = -3; i <= 3; i++)
            {
                GameObject ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ridge.name = $"MountainRidge_{i}";
                ridge.transform.SetParent(mountainLayer.transform, false);
                ridge.transform.localPosition = new Vector3(i * 12f, Mathf.Sin(i) * 2f, 0);
                ridge.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 15f : -15f));
                ridge.transform.localScale = new Vector3(16f, 14f, 1f);
                DestroyImmediate(ridge.GetComponent<Collider>());
                ridge.GetComponent<Renderer>().sharedMaterial = mountainMat;
            }

            controller.AddLayer(mountainLayer.transform, 0.18f, 0.05f, "MidMountainSilhouettes", autoDriftSpeedX: -0.01f);

            // --- LAYER 3: Ceremonial Torii Shrine & Pagoda Pillars (Z = 15m) ---
            GameObject shrineLayer = new GameObject("Layer_03_ToriiShrineMidground");
            shrineLayer.transform.SetParent(root.transform, false);
            shrineLayer.transform.localPosition = new Vector3(0, 3f, 15f);

            var toriiMat = new Material(urpUnlit);
            toriiMat.SetColor("_BaseColor", new Color(0.72f, 0.14f, 0.12f, 1f)); // Vermilion Lacquer

            // Left Grand Torii Gate Post
            CreateToriiArch(shrineLayer.transform, new Vector3(-12f, 0f, 0f), toriiMat);
            // Right Grand Torii Gate Post
            CreateToriiArch(shrineLayer.transform, new Vector3(12f, 0f, 0f), toriiMat);

            controller.AddLayer(shrineLayer.transform, 0.38f, 0.08f, "ToriiShrineMidground");

            // --- LAYER 4: Near Stone Lanterns & Foreground Accents (Z = -2.5m) ---
            GameObject foregroundLayer = new GameObject("Layer_04_NearLanternAccents");
            foregroundLayer.transform.SetParent(root.transform, false);
            foregroundLayer.transform.localPosition = new Vector3(0, 0.8f, -2.5f);

            var stoneMat = new Material(urpUnlit);
            stoneMat.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.20f, 1f)); // Weathered Granite

            var lanternGlowMat = new Material(urpUnlit);
            lanternGlowMat.SetColor("_BaseColor", new Color(1.0f, 0.75f, 0.25f, 1f)); // Amber Fire

            CreateStoneLantern(foregroundLayer.transform, new Vector3(-15f, 0, 0), stoneMat, lanternGlowMat);
            CreateStoneLantern(foregroundLayer.transform, new Vector3(15f, 0, 0), stoneMat, lanternGlowMat);

            controller.AddLayer(foregroundLayer.transform, -0.22f, 0.08f, "NearLanternAccents");

            controller.InitializeLayerPositions();
            return root;
        }

        private static void CreateToriiArch(Transform parent, Vector3 localPos, Material mat)
        {
            GameObject torii = new GameObject("ToriiArch");
            torii.transform.SetParent(parent, false);
            torii.transform.localPosition = localPos;

            // Pillar Left
            GameObject pL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pL.transform.SetParent(torii.transform, false);
            pL.transform.localPosition = new Vector3(-3f, 3.5f, 0);
            pL.transform.localScale = new Vector3(0.7f, 3.5f, 0.7f);
            DestroyImmediate(pL.GetComponent<Collider>());
            pL.GetComponent<Renderer>().sharedMaterial = mat;

            // Pillar Right
            GameObject pR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pR.transform.SetParent(torii.transform, false);
            pR.transform.localPosition = new Vector3(3f, 3.5f, 0);
            pR.transform.localScale = new Vector3(0.7f, 3.5f, 0.7f);
            DestroyImmediate(pR.GetComponent<Collider>());
            pR.GetComponent<Renderer>().sharedMaterial = mat;

            // Main Crossbar (Kasagi)
            GameObject bar1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar1.transform.SetParent(torii.transform, false);
            bar1.transform.localPosition = new Vector3(0, 6.8f, 0);
            bar1.transform.localScale = new Vector3(8.5f, 0.7f, 0.9f);
            DestroyImmediate(bar1.GetComponent<Collider>());
            bar1.GetComponent<Renderer>().sharedMaterial = mat;

            // Secondary Tie Beam (Nuki)
            GameObject bar2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar2.transform.SetParent(torii.transform, false);
            bar2.transform.localPosition = new Vector3(0, 5.6f, 0);
            bar2.transform.localScale = new Vector3(7.2f, 0.45f, 0.7f);
            DestroyImmediate(bar2.GetComponent<Collider>());
            bar2.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateStoneLantern(Transform parent, Vector3 localPos, Material stoneMat, Material glowMat)
        {
            GameObject lantern = new GameObject("StoneLantern");
            lantern.transform.SetParent(parent, false);
            lantern.transform.localPosition = localPos;

            // Base
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.transform.SetParent(lantern.transform, false);
            b.transform.localPosition = new Vector3(0, 0.6f, 0);
            b.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            DestroyImmediate(b.GetComponent<Collider>());
            b.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Fire Box (Glow)
            GameObject fire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fire.transform.SetParent(lantern.transform, false);
            fire.transform.localPosition = new Vector3(0, 1.4f, 0);
            fire.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            DestroyImmediate(fire.GetComponent<Collider>());
            fire.GetComponent<Renderer>().sharedMaterial = glowMat;

            // Roof (Kasa)
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(lantern.transform, false);
            roof.transform.localPosition = new Vector3(0, 1.8f, 0);
            roof.transform.localScale = new Vector3(1.1f, 0.25f, 1.1f);
            DestroyImmediate(roof.GetComponent<Collider>());
            roof.GetComponent<Renderer>().sharedMaterial = stoneMat;
        }
    }
}
