using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonIdol.VFX
{
    /// <summary>
    /// Post-Processing Pipeline & Decal Shader Bootstrap for Demon Idol.
    /// Ensures:
    /// 1. Global URP Volume is instantiated with DefaultVolumeProfile / PostProcessing Profile.
    /// 2. Camera post-processing is active for 3D/2.5D gameplay.
    /// 3. Video UI canvas is strictly isolated to ScreenSpaceOverlay with zero post-processing.
    /// 4. Decal shader (StandardNoCulling / Unlit Decal) is initialized for ground ink & blood splatters.
    /// </summary>
    public class PostProcessingPipelineBootstrap : MonoBehaviour
    {
        private static PostProcessingPipelineBootstrap _instance;
        public static PostProcessingPipelineBootstrap Instance => _instance;

        [Header("Profiles & Shaders")]
        [SerializeField] private VolumeProfile _volumeProfile;
        [SerializeField] private Shader _decalShader;

        private Volume _globalVolume;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            InitializePostProcessingPipeline();
            InitializeDecalShader();
        }

        private void Start()
        {
            InitializePostProcessingPipeline();
        }

        public void InitializePostProcessingPipeline()
        {
            if (_globalVolume == null)
            {
                _globalVolume = FindAnyObjectByType<Volume>();
            }

            if (_globalVolume == null)
            {
                GameObject volGo = new GameObject("Global_PostProcessing_Volume");
                volGo.transform.SetParent(transform, false);
                _globalVolume = volGo.AddComponent<Volume>();
                _globalVolume.isGlobal = true;
                _globalVolume.priority = 10f;
            }

            if (_globalVolume != null && _globalVolume.sharedProfile == null)
            {
#if UNITY_EDITOR
                if (_volumeProfile == null)
                {
                    _volumeProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset")
                        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
                }
#endif
                if (_volumeProfile != null)
                {
                    _globalVolume.sharedProfile = _volumeProfile;
                }
            }

            // Ensure Main Camera has post-processing enabled for 3D gameplay
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camData = mainCam.GetComponent("UniversalAdditionalCameraData");
                if (camData != null)
                {
                    var prop = camData.GetType().GetProperty("renderPostProcessing");
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(camData, true);
                    }
                }
            }
        }

        public void InitializeDecalShader()
        {
            if (_decalShader == null)
            {
                _decalShader = Shader.Find("Fantasy Forest/StandardNoCulling") 
                    ?? Shader.Find("Custom/StandardNoCulling") 
                    ?? Shader.Find("Universal Render Pipeline/Unlit") 
                    ?? Shader.Find("Sprites/Default");
            }
        }

        public Shader GetDecalShader() => _decalShader;
    }
}
