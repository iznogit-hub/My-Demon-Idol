using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Self-contained authoring configuration for Stage Prefabs.
    /// Encapsulates combat geometry, spawn points, corner walls, camera framing limits,
    /// lighting, fog, URP volume post-processing, and audio loops in a single inspector.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class BBStagePrefabConfig : MonoBehaviour
    {
        public static BBStagePrefabConfig ActiveConfig { get; private set; }

        private void OnEnable()
        {
            ActiveConfig = this;
            ApplyEnvironmentSettings();
        }

        private void OnDisable()
        {
            if (ActiveConfig == this)
            {
                ActiveConfig = null;
            }
        }

        [Header("Stage Identity")]
        public string stageId = "ST07";
        public string stageDisplayName = "College Raimon — Rainy Courtyard";
        public Sprite stageThumbnail;

        [Header("Combat Geometry & Bounds")]
        public float p1SpawnX = -2.5f;
        public float p2SpawnX = 2.5f;
        public float floorY = 0f;
        public float leftWallX = -10.2f;
        public float rightWallX = 10.2f;

        [Header("Camera Framing Limits")]
        public float cameraMinX = -8.0f;
        public float cameraMaxX = 8.0f;
        public float cameraMinY = 1.0f;
        public float cameraMaxY = 6.5f;
        public float cameraDefaultZ = -4.8f;

        [Header("Atmosphere, Lighting & Fog")]
        public Material skyboxMaterial;
        public Color ambientColor = new Color(0.2f, 0.22f, 0.28f, 1f);
        public Color directionalLightColor = new Color(0.7f, 0.75f, 0.9f, 1f);
        public float directionalLightIntensity = 0.85f;
        public bool fogEnabled = true;
        public Color fogColor = new Color(0.08f, 0.1f, 0.15f, 1f);
        public float fogDensity = 0.025f;

        [Header("Post-FX & Audio")]
        public VolumeProfile postProcessProfile;
        public AudioClip stageBGM;
        public AudioClip ambientLoopSFX;

        [Header("Props Root & Hierarchy")]
        public GameObject stagePropsRoot;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 1. Green floor plane line and guide plane
            Gizmos.color = Color.green;
            Vector3 floorStart = new Vector3(leftWallX - 2.5f, floorY, 0f);
            Vector3 floorEnd = new Vector3(rightWallX + 2.5f, floorY, 0f);
            Gizmos.DrawLine(floorStart, floorEnd);
            Gizmos.DrawLine(floorStart + new Vector3(0, 0, -1.5f), floorEnd + new Vector3(0, 0, -1.5f));
            Gizmos.DrawLine(floorStart + new Vector3(0, 0, 1.5f), floorEnd + new Vector3(0, 0, 1.5f));

            // 2. Red left/right corner walls
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(leftWallX, floorY - 0.5f, 0f), new Vector3(leftWallX, floorY + 7.0f, 0f));
            Gizmos.DrawLine(new Vector3(rightWallX, floorY - 0.5f, 0f), new Vector3(rightWallX, floorY + 7.0f, 0f));
            Gizmos.DrawWireCube(new Vector3(leftWallX, floorY + 3.5f, 0f), new Vector3(0.2f, 7.0f, 3.0f));
            Gizmos.DrawWireCube(new Vector3(rightWallX, floorY + 3.5f, 0f), new Vector3(0.2f, 7.0f, 3.0f));

            // 3. Cyan P1/P2 spawn arrows
            Gizmos.color = Color.cyan;
            Vector3 p1Pos = new Vector3(p1SpawnX, floorY, 0f);
            Vector3 p2Pos = new Vector3(p2SpawnX, floorY, 0f);
            Gizmos.DrawWireSphere(p1Pos + Vector3.up * 0.15f, 0.25f);
            Gizmos.DrawWireSphere(p2Pos + Vector3.up * 0.15f, 0.25f);

            // P1 arrow pointing right (+X)
            Vector3 p1Arrow = p1Pos + Vector3.up * 0.6f;
            Gizmos.DrawLine(p1Arrow, p1Arrow + Vector3.right * 0.9f);
            Gizmos.DrawLine(p1Arrow + Vector3.right * 0.9f, p1Arrow + new Vector3(0.6f, 0.2f, 0));
            Gizmos.DrawLine(p1Arrow + Vector3.right * 0.9f, p1Arrow + new Vector3(0.6f, -0.2f, 0));

            // P2 arrow pointing left (-X)
            Vector3 p2Arrow = p2Pos + Vector3.up * 0.6f;
            Gizmos.DrawLine(p2Arrow, p2Arrow + Vector3.left * 0.9f);
            Gizmos.DrawLine(p2Arrow + Vector3.left * 0.9f, p2Arrow + new Vector3(-0.6f, 0.2f, 0));
            Gizmos.DrawLine(p2Arrow + Vector3.left * 0.9f, p2Arrow + new Vector3(-0.6f, -0.2f, 0));

            // 4. Yellow camera bounds rectangle
            Gizmos.color = Color.yellow;
            float camWidth = Mathf.Abs(cameraMaxX - cameraMinX);
            float camHeight = Mathf.Abs(cameraMaxY - cameraMinY);
            Vector3 camCenter = new Vector3((cameraMinX + cameraMaxX) * 0.5f, (cameraMinY + cameraMaxY) * 0.5f, 0f);
            Gizmos.DrawWireCube(camCenter, new Vector3(camWidth, camHeight, 0.1f));
        }
#endif

        /// <summary>
        /// Applies lighting, fog, and skybox settings to the active environment.
        /// Safe to call in both Edit Mode and Play Mode.
        /// </summary>
        public void ApplyEnvironmentSettings()
        {
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
            }
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;

            // Apply directional light
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    lights[i].color = directionalLightColor;
                    lights[i].intensity = directionalLightIntensity;
                    break;
                }
            }

            // Apply URP Volume profile
            if (postProcessProfile != null)
            {
                var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
                for (int i = 0; i < volumes.Length; i++)
                {
                    if (volumes[i].isGlobal)
                    {
                        volumes[i].profile = postProcessProfile;
                        break;
                    }
                }
            }
        }
    }
}
