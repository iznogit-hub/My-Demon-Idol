using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using DemonIdol.Combat;

namespace DemonIdol.CameraControl
{
    [System.Serializable]
    public class StageCameraEntry
    {
        public string stageId = "ST01";
        public string stageName = "Central Courtyard";
        public Camera camera;
        public FighterCamera2D fighterCamera;
        public Vector3 basePosition = new Vector3(-0.05f, 1.93f, -6.5f);
        public Vector3 baseEulerAngles = new Vector3(2.8f, 0f, 0f);
        public float focalLength = 29.405f;
        public float fieldOfView = 38.0f;
        public Vector2 sensorSize = new Vector2(36.0f, 20.25f);
        public Vector2 lensShift = new Vector2(0f, 0.035f);
        [Range(0.30f, 0.50f)]
        public float characterScreenFraction = 0.40f;
        [TextArea(1, 3)]
        public string description;
    }

    /// <summary>
    /// Master Stage Camera Director for College Raimon.
    /// Manages the 7 distinct stage cameras framed across different areas of the campus:
    /// - ST01: Camera_ST01_Courtyard (Academic wing & clocktower plaza)
    /// - ST02: Camera_ST02_MainGate (School gates & sakura blossom avenue)
    /// - ST03: Camera_ST03_Rooftop (Rooftop observatory & urban skyline)
    /// - ST04: Camera_ST04_Dojo (Traditional wooden dojo pavilion & lanterns)
    /// - ST05: Camera_ST05_Stadium (Athletic track & stadium floodlight towers)
    /// - ST06: Camera_ST06_Promenade (Campus transit edge & bicycle hub)
    /// - ST07: Camera_ST07_MidnightCourtyard (Midnight rain courtyard, user-calibrated baseline)
    /// </summary>
    [ExecuteAlways]
    public class StageCameraManager : MonoBehaviour
    {
        public static StageCameraManager Instance { get; private set; }

        [Header("Stage Camera Roster (7 Campus Sectors)")]
        [SerializeField] private List<StageCameraEntry> _stageCameras = new List<StageCameraEntry>();

        [Header("Active Stage Camera")]
        [SerializeField] private string _activeStageId = "ST07";
        [SerializeField] private Camera _activeCamera;

        public string ActiveStageId => _activeStageId;
        public Camera ActiveCamera => _activeCamera;
        public IReadOnlyList<StageCameraEntry> StageCameras => _stageCameras;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
                Instance = this;
            }
            else
            {
                Instance = this;
            }

            EnsureCameraRosterConfigured();
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                SwitchToStageCamera(_activeStageId);
            }
        }

        public void EnsureCameraRosterConfigured()
        {
            if (_stageCameras == null) _stageCameras = new List<StageCameraEntry>();
            if (_stageCameras.Count == 0)
            {
                PopulateDefaultCameraEntries();
            }
        }

        public void PopulateDefaultCameraEntries()
        {
            _stageCameras.Clear();

            _stageCameras.Add(new StageCameraEntry
            {
                stageId = "ST01",
                stageName = "Central Courtyard & Clocktower Plaza",
                basePosition = new Vector3(-0.05f, 1.15f, -4.6f),
                baseEulerAngles = new Vector3(2.8f, 0f, 0f),
                focalLength = 32.0f,
                fieldOfView = 35.0f,
                sensorSize = new Vector2(36.0f, 20.25f),
                lensShift = new Vector2(0f, 0.035f),
                characterScreenFraction = 0.58f,
                description = "Crisp daylight framing of the iconic academic clock tower facade."
            });

            _stageCameras.Add(new StageCameraEntry
            {
                stageId = "ST02",
                stageName = "Academy Main Gate & Sakura Avenue",
                basePosition = new Vector3(0.0f, 1.15f, -4.8f),
                baseEulerAngles = new Vector3(2.2f, 0f, 0f),
                focalLength = 32.0f,
                fieldOfView = 35.0f,
                sensorSize = new Vector2(36.0f, 20.25f),
                lensShift = new Vector2(0f, 0.030f),
                characterScreenFraction = 0.58f,
                description = "Lower framing capturing the classical school gates and sakura cherry blossoms."
            });

            _stageCameras.Add(new StageCameraEntry
            {
                stageId = "ST03",
                stageName = "Rooftop Observatory & Skyline Deck",
                basePosition = new Vector3(0.0f, 1.25f, -4.6f),
                baseEulerAngles = new Vector3(3.5f, 0f, 0f),
                focalLength = 32.0f,
                fieldOfView = 35.0f,
                sensorSize = new Vector2(36.0f, 20.25f),
                lensShift = new Vector2(0f, 0.040f),
                characterScreenFraction = 0.58f,
                description = "Steeper downward pitch emphasizing rooftop drop-off and sprawling city skyline."
            });

            _stageCameras.Add(new StageCameraEntry
            {
                stageId = "ST04",
                stageName = "Martial Arts Dojo & Traditional Pavilion",
                basePosition = new Vector3(0.0f, 1.15f, -4.6f),
                baseEulerAngles = new Vector3(2.5f, 0f, 0f),
                focalLength = 32.0f,
                fieldOfView = 35.0f,
                sensorSize = new Vector2(36.0f, 20.25f),
                lensShift = new Vector2(0f, 0.035f),
                characterScreenFraction = 0.58f,
                description = "Intimate cinematic framing of traditional Japanese wooden columns and warm paper lanterns."
            });

            _stageCameras.Add(new StageCameraEntry
            {
                stageId = "ST05",
                stageName = "Athletic Stadium & Sports Track",
                basePosition = new Vector3(0.0f, 1.20f, -4.8f),
                baseEulerAngles = new Vector3(2.8f, 0f, 0f),
                focalLength = 30.0f,
                fieldOfView = 37.0f,
                sensorSize = new Vector2(36.0f, 20.25f),
                lensShift = new Vector2(0f, 0.025f),
                characterScreenFraction = 0.56f,
                description = "Sports framing showing the running track and floodlight bleachers."
            });

            _stageCameras.Add(new StageCameraEntry
            {
                stageId = "ST06",
                stageName = "Transit Promenade & Campus Edge",
                basePosition = new Vector3(0.0f, 1.15f, -4.6f),
                baseEulerAngles = new Vector3(2.0f, 0f, 0f),
                focalLength = 32.0f,
                fieldOfView = 35.0f,
                sensorSize = new Vector2(36.0f, 20.25f),
                lensShift = new Vector2(0f, 0.030f),
                characterScreenFraction = 0.58f,
                description = "Street-level modern perspective framing campus walkways and illuminated kiosks."
            });

            _stageCameras.Add(new StageCameraEntry
            {
                stageId = "ST07",
                stageName = "College Raimon (Midnight Rain Courtyard)",
                basePosition = new Vector3(-0.05f, 1.05f, -3.55f),
                baseEulerAngles = new Vector3(2.8f, 0f, 0f),
                focalLength = 32.0f,
                fieldOfView = 35.0f,
                sensorSize = new Vector2(36.0f, 20.25f),
                lensShift = new Vector2(0f, 0.035f),
                characterScreenFraction = 0.59f,
                description = "Calibrated 59% character screen height framing the midnight thunderstorm courtyard."
            });
        }

        public StageCameraEntry GetEntry(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) stageId = "ST07";
            string canonical = StageRegistry.ResolveCanonicalStageId(stageId);

            for (int i = 0; i < _stageCameras.Count; i++)
            {
                if (string.Equals(_stageCameras[i].stageId, stageId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_stageCameras[i].stageId, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    return _stageCameras[i];
                }
            }
            return _stageCameras.Count > 0 ? _stageCameras[0] : null;
        }

        public void SwitchToStageCamera(string stageId, Transform fighter1 = null, Transform fighter2 = null)
        {
            EnsureCameraRosterConfigured();
            StageCameraEntry targetEntry = GetEntry(stageId);
            if (targetEntry == null) return;

            _activeStageId = targetEntry.stageId;

            // 1. If dedicated Camera GameObjects exist, activate the matching camera
            Camera targetCam = targetEntry.camera;
            if (targetCam == null)
            {
                // Fallback to searching by standard naming
                string camName = $"Camera_{targetEntry.stageId}";
                var found = GameObject.Find(camName);
                if (found != null) targetCam = found.GetComponent<Camera>();
            }

            if (targetCam != null)
            {
                // Deactivate all other registered stage cameras
                foreach (var entry in _stageCameras)
                {
                    if (entry.camera != null && entry.camera != targetCam)
                    {
                        var audioList = entry.camera.GetComponent<AudioListener>();
                        if (audioList != null) audioList.enabled = false;
                        entry.camera.tag = "Untagged";
                        entry.camera.gameObject.SetActive(false);
                    }
                }

                targetCam.gameObject.SetActive(true);
                targetCam.tag = "MainCamera";
                EnsureSingleActiveAudioListener(targetCam);

                _activeCamera = targetCam;

                // Configure Physical Camera optics
                targetCam.usePhysicalProperties = true;
                targetCam.sensorSize = targetEntry.sensorSize;
                targetCam.focalLength = targetEntry.focalLength;
                targetCam.fieldOfView = targetEntry.fieldOfView;
                targetCam.lensShift = targetEntry.lensShift;

                // Ensure FighterCamera2D targets
                FighterCamera2D fCam = targetEntry.fighterCamera ?? targetCam.GetComponent<FighterCamera2D>();
                if (fCam != null)
                {
                    if (fighter1 != null && fighter2 != null)
                    {
                        fCam.SetTargets(fighter1, fighter2);
                    }
                    fCam.focalLength = targetEntry.focalLength;
                    fCam.lensShift = targetEntry.lensShift;
                    fCam.targetCharacterScreenFraction = targetEntry.characterScreenFraction;
                    fCam.offset = new Vector3(targetEntry.basePosition.x, targetEntry.basePosition.y, targetEntry.basePosition.z);
                    fCam.pitchAngle = targetEntry.baseEulerAngles.x;
                    fCam.minDistanceZ = -3.2f;
                    fCam.maxDistanceZ = -7.0f;
                    fCam.ConfigurePhysicalCamera();
                }
            }
            else
            {
                // If using a single shared Main Camera, adapt it dynamically to this stage's optics
                Camera mainCam = Camera.main ?? FindAnyObjectByType<Camera>();
                if (mainCam != null)
                {
                    _activeCamera = mainCam;
                    EnsureSingleActiveAudioListener(mainCam);
                    mainCam.transform.position = targetEntry.basePosition;
                    mainCam.transform.rotation = Quaternion.Euler(targetEntry.baseEulerAngles);

                    mainCam.usePhysicalProperties = true;
                    mainCam.sensorSize = targetEntry.sensorSize;
                    mainCam.focalLength = targetEntry.focalLength;
                    mainCam.fieldOfView = targetEntry.fieldOfView;
                    mainCam.lensShift = targetEntry.lensShift;

                    FighterCamera2D fCam = mainCam.GetComponent<FighterCamera2D>();
                    if (fCam != null)
                    {
                        if (fighter1 != null && fighter2 != null)
                        {
                            fCam.SetTargets(fighter1, fighter2);
                        }
                        fCam.focalLength = targetEntry.focalLength;
                        fCam.lensShift = targetEntry.lensShift;
                        fCam.targetCharacterScreenFraction = targetEntry.characterScreenFraction;
                        fCam.offset = new Vector3(targetEntry.basePosition.x, targetEntry.basePosition.y, targetEntry.basePosition.z);
                        fCam.pitchAngle = targetEntry.baseEulerAngles.x;
                        fCam.minDistanceZ = -3.2f;
                        fCam.maxDistanceZ = -7.0f;
                        fCam.ConfigurePhysicalCamera();
                    }
                }
            }

            // 2. Synchronize CampusZoneAnchor environment placement and lighting
            CampusZoneAnchor.Instance?.ApplyZone(targetEntry.stageId);

            Debug.Log($"<color=#00e676>[StageCameraManager] Active Stage Camera: {targetEntry.stageId} - {targetEntry.stageName} (FL={targetEntry.focalLength:F1}mm, FOV={targetEntry.fieldOfView:F1}°)</color>");
        }

        public static void EnsureSingleActiveAudioListener(Camera activeCam)
        {
            if (activeCam == null) return;
            var activeListener = activeCam.GetComponent<AudioListener>();
            if (activeListener == null)
            {
                activeListener = activeCam.gameObject.AddComponent<AudioListener>();
            }
            activeListener.enabled = true;

            var allListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 0; i < allListeners.Length; i++)
            {
                if (allListeners[i] != null && allListeners[i] != activeListener)
                {
                    allListeners[i].enabled = false;
                }
            }
        }

        #region Context Menu Previews for Unity Editor

        [ContextMenu("Preview ST01 - Central Courtyard")]
        public void PreviewST01() => SwitchToStageCamera("ST01");

        [ContextMenu("Preview ST02 - Sakura Main Gate")]
        public void PreviewST02() => SwitchToStageCamera("ST02");

        [ContextMenu("Preview ST03 - Rooftop Observatory")]
        public void PreviewST03() => SwitchToStageCamera("ST03");

        [ContextMenu("Preview ST04 - Martial Arts Dojo")]
        public void PreviewST04() => SwitchToStageCamera("ST04");

        [ContextMenu("Preview ST05 - Athletic Stadium")]
        public void PreviewST05() => SwitchToStageCamera("ST05");

        [ContextMenu("Preview ST06 - Transit Promenade")]
        public void PreviewST06() => SwitchToStageCamera("ST06");

        [ContextMenu("Preview ST07 - Midnight Courtyard")]
        public void PreviewST07() => SwitchToStageCamera("ST07");

        #endregion
    }
}
