using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Content Contract: StageDefinition ScriptableObject.
    /// Defines an arena stage: bounds, wall thresholds, camera limits, 
    /// music, spawn positions, and scene/prefab references.
    /// Authored completely without modifying code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStageDefinition", menuName = "DemonIdol/Combat/StageDefinition")]
    public class StageDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "flooded_grounds";
        public string displayName = "Flooded Grounds - Stone Bridge";
        [TextArea(2, 4)]
        public string description = "Ancient overgrown stone bridge suspended over mystic waters. Features wall splats and infinite vertical air space.";

        [Header("Scene / Prefab Reference")]
        public string sceneName = "UniversalFighter_Demo";
        public GameObject stagePrefab;
        public Sprite thumbnailSprite;
        public Sprite backgroundSprite;
        public string backgroundResourcePath;

        [Header("Package-Native Universal Fighter Mapping (UFE)")]
        public string ufeStageName = "Training Room (Prefab)";
        public bool signatureArenaAuthored = false;

        public string GetEffectiveUFEStageName()
        {
            return !string.IsNullOrEmpty(ufeStageName) ? ufeStageName : "Training Room (Prefab)";
        }

        [Header("Spatial Dimensions & Arena Bounds")]
        [Tooltip("Total playable horizontal width in meters (e.g. 21.0m for ±10.5m arena limits)")]
        public float playableWidth = 21.0f;
        [Tooltip("X threshold in meters where wall bounce / wall splats activate (e.g. 9.8m)")]
        public float wallThreshold = 9.8f;
        public float groundSurfaceY = 0.0f;

        [Header("Camera Framing Limits")]
        public float cameraMinZ = -4.8f;
        public float cameraMaxZ = -9.2f;
        public float cameraMinY = 1.0f;
        public float cameraMaxY = 6.5f;

        [Header("Audio & Music")]
        public AudioClip bgmAudioClip;
        public string bgmTrackName = "Ancient Torii Drums";

        [Header("Fighter Spawn Coordinates")]
        public Vector3 p1SpawnPosition = new Vector3(-2.5f, 0f, 0f);
        public Vector3 p2SpawnPosition = new Vector3(2.5f, 0f, 0f);

        public float ArenaLimitX => playableWidth * 0.5f;
    }
}
