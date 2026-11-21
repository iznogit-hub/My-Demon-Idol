using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    [System.Serializable]
    public class CampusZoneData
    {
        public string stageId;
        public string zoneName;
        public string zoneSubtitle;
        public Vector3 campusOffset;
        public Vector3 campusEulerAngles;
        public Color keyLightColor = new Color(1.0f, 0.95f, 0.90f, 1f);
        public float keyLightIntensity = 1.25f;
        public Color accentLightColor = new Color(1.0f, 0.85f, 0.90f, 1f);
        public float accentLightIntensity = 1.35f;
        public Vector2 cameraMinMaxZ = new Vector2(-6.5f, -13.5f);
        public float cameraPitch = 2.8f;
        public string bgmTrackName = "Academy Courtyard BGM";
        [TextArea(2, 4)]
        public string description;
        public string hazardText;
    }

    /// <summary>
    /// Master Campus Zone Anchor for College Raimon.
    /// Drives the 7 combat arena stages derived from the College Raimon mega-model:
    /// ST01: Central Courtyard & Clocktower Plaza
    /// ST02: Academy Main Gate & Sakura Avenue
    /// ST03: Rooftop Observatory & Skyline Deck
    /// ST04: Martial Arts Dojo & Traditional Pavilion
    /// ST05: Athletic Stadium & Sports Track
    /// ST06: Transit Promenade & Bike Hub
    /// ST07: Midnight Rain Courtyard (User-calibrated anchor)
    /// </summary>
    public class CampusZoneAnchor : MonoBehaviour
    {
        public static CampusZoneAnchor Instance { get; private set; }

        [Header("Target College Raimon Transform")]
        [SerializeField] private Transform _campusTransform;
        [SerializeField] private Light _keyLight;
        [SerializeField] private Light _accentLight;

        [Header("Stage Scale")]
        [Tooltip("Scale slider for College Raimon root backdrop. Baseline = 0.65 for optimal camera framing.")]
        [Range(0.2f, 2.0f)]
        public float stageScale = 0.65f;

        [Header("Current Active Zone")]
        [SerializeField] private string _activeStageId = "ST07";

        [Header("Campus Zones (ST01 - ST07)")]
        [SerializeField] private CampusZoneData[] _zones = new CampusZoneData[]
        {
            new CampusZoneData
            {
                stageId = "ST01",
                zoneName = "Central Courtyard",
                zoneSubtitle = "ACADEMY COURTYARD // CLOCKTOWER PLAZA",
                campusOffset = new Vector3(0.71f, 0f, 1.41f),
                campusEulerAngles = new Vector3(0f, 0f, 0f),
                keyLightColor = new Color(1.0f, 0.97f, 0.92f, 1f),
                keyLightIntensity = 1.4f,
                accentLightColor = new Color(1.0f, 0.85f, 0.90f, 1f),
                accentLightIntensity = 1.2f,
                cameraMinMaxZ = new Vector2(-6.5f, -13.5f),
                cameraPitch = 2.8f,
                bgmTrackName = "Courtyard High Noon BGM",
                description = "Iconic central courtyard flanked by the clocktower facade and main academic wing.",
                hazardText = "[ CLOCKTOWER COURTYARD • PLANAR 2.5D ARENA ]"
            },
            new CampusZoneData
            {
                stageId = "ST02",
                zoneName = "Sakura Main Gate",
                zoneSubtitle = "ACADEMY MAIN GATE // SAKURA AVENUE",
                campusOffset = new Vector3(14.8f, 0f, -28.4f),
                campusEulerAngles = new Vector3(0f, 180f, 0f),
                keyLightColor = new Color(1.0f, 0.90f, 0.82f, 1f),
                keyLightIntensity = 1.3f,
                accentLightColor = new Color(1.0f, 0.75f, 0.85f, 1f),
                accentLightIntensity = 1.5f,
                cameraMinMaxZ = new Vector2(-6.5f, -13.5f),
                cameraPitch = 2.8f,
                bgmTrackName = "Sakura Dawn BGM",
                description = "Traditional school entrance pillars and wrought iron gates framed by blooming sakura cherry blossoms.",
                hazardText = "[ SAKURA AVENUE • CHERRY BLOSSOM SWARM ]"
            },
            new CampusZoneData
            {
                stageId = "ST03",
                zoneName = "Rooftop Observatory",
                zoneSubtitle = "CAMPUS ROOFTOP // METROPOLIS SKYLINE",
                campusOffset = new Vector3(-12.0f, -18.3f, 18.2f),
                campusEulerAngles = new Vector3(0f, 0f, 0f),
                keyLightColor = new Color(1.0f, 0.70f, 0.50f, 1f),
                keyLightIntensity = 1.5f,
                accentLightColor = new Color(0.85f, 0.55f, 0.95f, 1f),
                accentLightIntensity = 1.3f,
                cameraMinMaxZ = new Vector2(-6.5f, -13.5f),
                cameraPitch = 2.8f,
                bgmTrackName = "Skyline Sunset BGM",
                description = "High rooftop deck enclosed by chain-link fences, overlooking the sprawling urban metropolis at twilight.",
                hazardText = "[ ROOFTOP SKYLINE • HIGH ALTITUDE SHIMMER ]"
            },
            new CampusZoneData
            {
                stageId = "ST04",
                zoneName = "Martial Arts Dojo",
                zoneSubtitle = "CAMPUS DOJO // TRADITIONAL PAVILION",
                campusOffset = new Vector3(32.4f, 0f, 8.5f),
                campusEulerAngles = new Vector3(0f, 90f, 0f),
                keyLightColor = new Color(1.0f, 0.85f, 0.65f, 1f),
                keyLightIntensity = 1.35f,
                accentLightColor = new Color(1.0f, 0.65f, 0.35f, 1f),
                accentLightIntensity = 1.4f,
                cameraMinMaxZ = new Vector2(-6.5f, -13.5f),
                cameraPitch = 2.8f,
                bgmTrackName = "Dojo Discipline BGM",
                description = "Traditional Japanese wooden dojo pavilion with lattice screens, polished decking, and hanging lanterns.",
                hazardText = "[ TRADITIONAL DOJO • WOODEN PILLARS & LANTERNS ]"
            },
            new CampusZoneData
            {
                stageId = "ST05",
                zoneName = "Athletic Stadium",
                zoneSubtitle = "SPORTS COMPLEX // ATHLETIC TRACK",
                campusOffset = new Vector3(-45.0f, 0f, -12.0f),
                campusEulerAngles = new Vector3(0f, 270f, 0f),
                keyLightColor = new Color(0.88f, 0.92f, 1.0f, 1f),
                keyLightIntensity = 1.45f,
                accentLightColor = new Color(0.70f, 0.85f, 1.0f, 1f),
                accentLightIntensity = 1.2f,
                cameraMinMaxZ = new Vector2(-6.5f, -13.5f),
                cameraPitch = 2.8f,
                bgmTrackName = "Stadium Overcast BGM",
                description = "Campus athletic field bordered by running tracks, bleacher stands, and high-intensity floodlight towers.",
                hazardText = "[ ATHLETIC STADIUM • FLOODLIGHT CONTRAST ]"
            },
            new CampusZoneData
            {
                stageId = "ST06",
                zoneName = "Transit Promenade",
                zoneSubtitle = "CAMPUS PROMENADE // TRANSIT EDGE",
                campusOffset = new Vector3(22.0f, 0f, -42.0f),
                campusEulerAngles = new Vector3(0f, 180f, 0f),
                keyLightColor = new Color(0.95f, 0.85f, 0.75f, 1f),
                keyLightIntensity = 1.25f,
                accentLightColor = new Color(0.35f, 0.90f, 1.0f, 1f),
                accentLightIntensity = 1.5f,
                cameraMinMaxZ = new Vector2(-6.5f, -13.5f),
                cameraPitch = 2.8f,
                bgmTrackName = "Promenade Dusk BGM",
                description = "Modern transit promenade connecting campus walkways, bicycle shelters, and vending kiosks.",
                hazardText = "[ TRANSIT PROMENADE • NEON REFLECTIONS ]"
            },
            new CampusZoneData
            {
                stageId = "ST07",
                zoneName = "College Raimon (Midnight Rain)",
                zoneSubtitle = "ACADEMY COURTYARD // THUNDERSTORM MIDNIGHT",
                campusOffset = new Vector3(0.71f, 0f, 1.41f),
                campusEulerAngles = new Vector3(0f, 0f, 0f),
                keyLightColor = new Color(0.38f, 0.45f, 0.70f, 1f),
                keyLightIntensity = 0.95f,
                accentLightColor = new Color(0.28f, 0.42f, 0.68f, 1f),
                accentLightIntensity = 0.85f,
                cameraMinMaxZ = new Vector2(-6.5f, -13.5f),
                cameraPitch = 2.8f,
                bgmTrackName = "Academy Courtyard BGM",
                description = "Sprawling Japanese high school & college campus courtyard under a midnight thunderstorm with sakura blossoms.",
                hazardText = "[ MIDNIGHT COURTYARD • RAIN & WET REFLECTIONS ]"
            }
        };

        private void Awake()
        {
            Instance = this;
            if (_campusTransform == null)
            {
                var courtyard = transform.Find("Environment_BackdropDiorama/CollegeRaimon_Courtyard");
                if (courtyard != null) _campusTransform = courtyard;
                else
                {
                    var diorama = GameObject.Find("Backdrop_Diorama");
                    if (diorama != null) _campusTransform = diorama.transform;
                }
            }
        }

        public CampusZoneData GetZoneData(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return _zones[0];
            string canonical = StageRegistry.ResolveCanonicalStageId(stageId);
            for (int i = 0; i < _zones.Length; i++)
            {
                if (string.Equals(_zones[i].stageId, stageId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(_zones[i].stageId, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    return _zones[i];
                }
            }
            return _zones[0];
        }

        public void ApplyZone(string stageId)
        {
            CampusZoneData zone = GetZoneData(stageId);
            if (zone == null) return;
            _activeStageId = zone.stageId;

            if (_campusTransform != null)
            {
                float s = stageScale;
                _campusTransform.localScale = new Vector3(s, s, s);
                if (zone.stageId == "ST07")
                {
                    _campusTransform.localPosition = new Vector3(-10.2f * s, -100.35f * s, 2.2f);
                    _campusTransform.localRotation = Quaternion.identity;
                }
                else
                {
                    _campusTransform.localPosition = zone.campusOffset;
                    _campusTransform.localRotation = Quaternion.Euler(zone.campusEulerAngles);
                }
            }

            if (_keyLight != null)
            {
                _keyLight.color = zone.keyLightColor;
                _keyLight.intensity = zone.keyLightIntensity;
            }

            if (_accentLight != null)
            {
                _accentLight.color = zone.accentLightColor;
                _accentLight.intensity = zone.accentLightIntensity;
            }

            if (DemonIdol.CameraControl.StageCameraManager.Instance != null && DemonIdol.CameraControl.StageCameraManager.Instance.ActiveStageId != zone.stageId)
            {
                DemonIdol.CameraControl.StageCameraManager.Instance.SwitchToStageCamera(zone.stageId);
            }
        }
    }
}
