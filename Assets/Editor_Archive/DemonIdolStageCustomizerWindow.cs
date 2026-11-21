#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Editor;

namespace DemonIdol.EditorTools
{
    /// <summary>
    /// Master Stage & Arena Customization Studio.
    /// Provides an interactive studio to precisely customize every battle stage in Demon Idol:
    /// - 1. Stage Identity & Lore (Display Name, ID, Lore Description)
    /// - 2. 3D Stage Prefab & Visual Diorama (Prefab binding, Mesh Renderers, Materials, Toon Shaders)
    /// - 3. Spatial Physics & Arena Bounds (Playable Width, Wall Splat Threshold, Spawn Positions)
    /// - 4. SF6 Dynamic Camera Calibration (Min/Max Zoom Z, Eye-level Height Y, 40% Framing)
    /// - 5. Audio & Atmosphere (BGM Track, Ambient Key Light Color, Sun Intensity)
    /// - 6. 1-Click Deployment to ArcadeArena.unity & Play Testing
    /// </summary>
    public class DemonIdolStageCustomizerWindow : EditorWindow
    {
        private const string STAGES_DATA_DIR = "Assets/CombatData/Stages";
        private const string STAGES_PREFAB_DIR = "Assets/Prefabs/Stages";

        private static readonly string[] StageIds = new string[]
        {
            "ST01", "ST02", "ST03", "ST04", "ST05", "ST06",
            "ST07", "ST08", "ST09", "ST10", "ST11", "ST12"
        };

        private static readonly string[] StageLabels = new string[]
        {
            "ST01 - Low Poly Desert (Mesa Spires & Amber Dunes)",
            "ST02 - RTS Sci-Fi Hangar (Industrial Gantry & Cyan Neon)",
            "ST03 - Space Alien Worlds (Extraterrestrial Crystals & Nebulae)",
            "ST04 - Cartoon City (Downtown Street Corner & Sunlit Asphalt)",
            "ST05 - Metro Subway (Gritty Retro Transit Platform & Tracks)",
            "ST06 - Modern Office Tower (Rooftop Penthouse Helipad Dusk)",
            "ST07 - College Raimon (Japanese School & Campus Courtyard)",
            "ST08 - Demon Lord Sanctum (Throne Room)",
            "ST09 - Volumetric Mist Turbine (Sub-level Vault)",
            "ST10 - Subway Underpass Dais (Concourse)",
            "ST11 - Neon Spotlight Runway (Outdoor Stage)",
            "ST12 - Abyssal Gargoyle Altar (Wasteland Gateway)"
        };

        private int _selectedStageIndex = 0; // Default ST01
        private int _selectedTab = 0;
        private readonly string[] _tabNames = new string[]
        {
            "🏰 Identity & Lore",
            "🎨 3D Stage & Prefab",
            "📐 Physics & Bounds",
            "🎥 SF6 Camera Tuning",
            "🎵 Audio & Lighting",
            "📁 File Links"
        };

        private Vector2 _scrollPos;
        private StageDefinition _currentStageDef;
        private GameObject _currentStagePrefab;

        private Color _customKeyLightColor = new Color(1.0f, 0.96f, 0.90f, 1f);
        private float _customLightIntensity = 1.25f;

        [MenuItem("Demon Idol/⚔️ Combat & Stages/Stage & Arena Customization Studio", priority = 39)]
        public static void OpenWindow()
        {
            var window = GetWindow<DemonIdolStageCustomizerWindow>("Stage Studio");
            window.minSize = new Vector2(650, 680);
            window.Show();
        }

        private void OnEnable()
        {
            LoadCurrentStage();
        }

        private void LoadCurrentStage()
        {
            if (_selectedStageIndex < 0 || _selectedStageIndex >= StageIds.Length) return;
            string id = StageIds[_selectedStageIndex];

            // 1. Load StageDefinition from Assets/CombatData/Stages
            string[] defFiles = Directory.Exists(STAGES_DATA_DIR)
                ? Directory.GetFiles(STAGES_DATA_DIR, $"{id}*.asset")
                : Array.Empty<string>();

            if (defFiles != null && defFiles.Length > 0)
            {
                _currentStageDef = AssetDatabase.LoadAssetAtPath<StageDefinition>(defFiles[0]);
            }
            else
            {
                // Fallback to StageRegistry in memory
                _currentStageDef = StageRegistry.GetStageById(id);
            }

            // 2. Load 3D Stage Prefab
            if (_currentStageDef != null && _currentStageDef.stagePrefab != null)
            {
                _currentStagePrefab = _currentStageDef.stagePrefab;
            }
            else
            {
                string[] prefabs = Directory.Exists(STAGES_PREFAB_DIR)
                    ? Directory.GetFiles(STAGES_PREFAB_DIR, $"{id}*.prefab")
                    : Array.Empty<string>();

                if (prefabs != null && prefabs.Length > 0)
                {
                    _currentStagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[0]);
                }
                else
                {
                    _currentStagePrefab = null;
                }
            }

            // Auto-configure lighting preset by stage
            switch (id)
            {
                case "ST01": // Desert
                    _customKeyLightColor = new Color(1.0f, 0.92f, 0.80f, 1f);
                    _customLightIntensity = 1.35f;
                    break;
                case "ST02": // Cyber Hangar
                    _customKeyLightColor = new Color(0.80f, 0.95f, 1.0f, 1f);
                    _customLightIntensity = 1.20f;
                    break;
                case "ST03": // Alien Worlds
                    _customKeyLightColor = new Color(0.95f, 0.80f, 1.0f, 1f);
                    _customLightIntensity = 1.15f;
                    break;
                case "ST04": // Cartoon City
                    _customKeyLightColor = new Color(1.0f, 0.98f, 0.92f, 1f);
                    _customLightIntensity = 1.40f;
                    break;
                case "ST05": // Metro Subway
                    _customKeyLightColor = new Color(0.85f, 0.85f, 0.75f, 1f);
                    _customLightIntensity = 1.05f;
                    break;
                case "ST06": // Rooftop Dusk
                    _customKeyLightColor = new Color(1.0f, 0.85f, 0.70f, 1f);
                    _customLightIntensity = 1.10f;
                    break;
                default:
                    _customKeyLightColor = new Color(1.0f, 0.96f, 0.90f, 1f);
                    _customLightIntensity = 1.25f;
                    break;
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(6);
            DrawStageSelector();

            EditorGUILayout.Space(8);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(30));

            EditorGUILayout.Space(8);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0:
                    DrawIdentityAndLoreTab();
                    break;
                case 1:
                    DrawStagePrefabTab();
                    break;
                case 2:
                    DrawPhysicsAndBoundsTab();
                    break;
                case 3:
                    DrawCameraTuningTab();
                    break;
                case 4:
                    DrawAudioAndLightingTab();
                    break;
                case 5:
                    DrawFileLinksTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            DrawFooterActions();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("DEMON IDOL — STAGE & ARENA CUSTOMIZATION STUDIO", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Directly inspect, customize, and deploy all 3D stage dioramas, camera framing, lighting & audio.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawStageSelector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Select Battle Stage:", EditorStyles.boldLabel, GUILayout.Width(140));

            int newIndex = EditorGUILayout.Popup(_selectedStageIndex, StageLabels, GUILayout.Height(22));
            if (newIndex != _selectedStageIndex)
            {
                _selectedStageIndex = newIndex;
                LoadCurrentStage();
            }

            if (GUILayout.Button("Ping Stage Asset", GUILayout.Width(120), GUILayout.Height(22)))
            {
                if (_currentStageDef != null)
                {
                    EditorGUIUtility.PingObject(_currentStageDef);
                    Selection.activeObject = _currentStageDef;
                }
                else if (_currentStagePrefab != null)
                {
                    EditorGUIUtility.PingObject(_currentStagePrefab);
                    Selection.activeObject = _currentStagePrefab;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================================
        // TAB 0: IDENTITY & LORE
        // =========================================================================
        private void DrawIdentityAndLoreTab()
        {
            if (_currentStageDef == null)
            {
                EditorGUILayout.HelpBox($"StageDefinition for {StageIds[_selectedStageIndex]} not found. Creating a local instance.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Stage Identity & Narrative Lore", EditorStyles.boldLabel);
            _currentStageDef.displayName = EditorGUILayout.TextField("Display Name", _currentStageDef.displayName);
            _currentStageDef.id = EditorGUILayout.TextField("Stage ID", _currentStageDef.id);
            _currentStageDef.ufeStageName = EditorGUILayout.TextField("UFE Mapping Alias", _currentStageDef.ufeStageName);

            EditorGUILayout.LabelField("Atmosphere & Environmental Lore:");
            _currentStageDef.description = EditorGUILayout.TextArea(_currentStageDef.description, GUILayout.Height(60));
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentStageDef);
            }
        }

        // =========================================================================
        // TAB 1: 3D STAGE & PREFAB
        // =========================================================================
        private void DrawStagePrefabTab()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("3D Stage Diorama Prefab & 2D Art", EditorStyles.boldLabel);

            if (_currentStageDef != null)
            {
                _currentStageDef.stagePrefab = (GameObject)EditorGUILayout.ObjectField("Stage 3D Prefab", _currentStageDef.stagePrefab, typeof(GameObject), false);
                _currentStageDef.thumbnailSprite = (Sprite)EditorGUILayout.ObjectField("Thumbnail Sprite", _currentStageDef.thumbnailSprite, typeof(Sprite), false);
                _currentStageDef.backgroundSprite = (Sprite)EditorGUILayout.ObjectField("Background Sprite", _currentStageDef.backgroundSprite, typeof(Sprite), false);
                _currentStagePrefab = _currentStageDef.stagePrefab;
            }
            else
            {
                _currentStagePrefab = (GameObject)EditorGUILayout.ObjectField("Stage 3D Prefab", _currentStagePrefab, typeof(GameObject), false);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            if (_currentStagePrefab != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label($"Prefab Diorama Inspector: {_currentStagePrefab.name}", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Stage Prefab in Scene", GUILayout.Height(26)))
                {
                    AssetDatabase.OpenAsset(_currentStagePrefab);
                }
                if (GUILayout.Button("Select in Project", GUILayout.Height(26)))
                {
                    Selection.activeObject = _currentStagePrefab;
                    EditorGUIUtility.PingObject(_currentStagePrefab);
                }
                EditorGUILayout.EndHorizontal();

                var renderers = _currentStagePrefab.GetComponentsInChildren<Renderer>(true);
                if (renderers != null && renderers.Length > 0)
                {
                    EditorGUILayout.Space(6);
                    GUILayout.Label($"Environment Meshes & Materials ({renderers.Length} Renderers):", EditorStyles.miniBoldLabel);
                    int count = 0;
                    foreach (var r in renderers)
                    {
                        if (count++ > 15)
                        {
                            EditorGUILayout.LabelField($"... and {renderers.Length - 15} more meshes", EditorStyles.miniLabel);
                            break;
                        }
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(r.gameObject.name, GUILayout.Width(200));
                        if (r.sharedMaterial != null)
                        {
                            EditorGUILayout.ObjectField(r.sharedMaterial, typeof(Material), false);
                            if (GUILayout.Button("Ping", GUILayout.Width(50)))
                            {
                                EditorGUIUtility.PingObject(r.sharedMaterial);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("No Material", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck() && _currentStageDef != null)
            {
                EditorUtility.SetDirty(_currentStageDef);
            }
        }

        // =========================================================================
        // TAB 2: PHYSICS & BOUNDS
        // =========================================================================
        private void DrawPhysicsAndBoundsTab()
        {
            if (_currentStageDef == null)
            {
                EditorGUILayout.HelpBox("StageDefinition not loaded.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Spatial Dimensions & 2.5D Arena Boundaries", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Controls wall-splats, corner traps, pushback limits, and ground level.", EditorStyles.miniLabel);

            _currentStageDef.playableWidth = EditorGUILayout.Slider("Playable Width (meters)", _currentStageDef.playableWidth, 16.0f, 32.0f);
            _currentStageDef.wallThreshold = EditorGUILayout.Slider("Wall Splat Threshold (meters)", _currentStageDef.wallThreshold, 8.0f, 15.0f);
            _currentStageDef.groundSurfaceY = EditorGUILayout.FloatField("Ground Surface Y", _currentStageDef.groundSurfaceY);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox($"Active Bounds: Left Wall at -{_currentStageDef.playableWidth * 0.5f:F1}m, Right Wall at +{_currentStageDef.playableWidth * 0.5f:F1}m. Wall interaction begins at ±{_currentStageDef.wallThreshold:F1}m.", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Fighter Spawn Coordinates", EditorStyles.boldLabel);
            _currentStageDef.p1SpawnPosition = EditorGUILayout.Vector3Field("P1 Starting Position", _currentStageDef.p1SpawnPosition);
            _currentStageDef.p2SpawnPosition = EditorGUILayout.Vector3Field("P2 Starting Position", _currentStageDef.p2SpawnPosition);
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentStageDef);
            }
        }

        // =========================================================================
        // TAB 3: SF6 CAMERA TUNING
        // =========================================================================
        private void DrawCameraTuningTab()
        {
            if (_currentStageDef == null)
            {
                EditorGUILayout.HelpBox("StageDefinition not loaded.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Street Fighter 6 Dynamic 40% Framing Calibration", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Ensures characters take precisely 40% of screen height with dynamic physical zoom.", EditorStyles.miniLabel);

            _currentStageDef.cameraMinZ = EditorGUILayout.Slider("Close / Throw Zoom (Min Z)", _currentStageDef.cameraMinZ, -7.0f, -3.0f);
            _currentStageDef.cameraMaxZ = EditorGUILayout.Slider("Corner Spacing Zoom (Max Z)", _currentStageDef.cameraMaxZ, -14.0f, -7.0f);

            EditorGUILayout.Space(4);
            _currentStageDef.cameraMinY = EditorGUILayout.Slider("Grounded Eye-Level (Min Y)", _currentStageDef.cameraMinY, 0.5f, 2.5f);
            _currentStageDef.cameraMaxY = EditorGUILayout.Slider("High Jump Ceiling (Max Y)", _currentStageDef.cameraMaxY, 4.0f, 10.0f);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Physical Specs: Full Frame Super 35 (36mm x 20.25mm), 50mm portrait prime lens, heroic lens shift (0, 0.035).", MessageType.None);
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentStageDef);
            }
        }

        // =========================================================================
        // TAB 4: AUDIO & LIGHTING
        // =========================================================================
        private void DrawAudioAndLightingTab()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Stage Music & Background Soundscape", EditorStyles.boldLabel);

            if (_currentStageDef != null)
            {
                _currentStageDef.bgmAudioClip = (AudioClip)EditorGUILayout.ObjectField("BGM Audio Clip", _currentStageDef.bgmAudioClip, typeof(AudioClip), false);
                _currentStageDef.bgmTrackName = EditorGUILayout.TextField("BGM Track Title", _currentStageDef.bgmTrackName);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Atmospheric Lighting Calibration", EditorStyles.boldLabel);
            _customKeyLightColor = EditorGUILayout.ColorField("Key Sun Light Color", _customKeyLightColor);
            _customLightIntensity = EditorGUILayout.Slider("Sun Light Intensity", _customLightIntensity, 0.5f, 2.5f);
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() && _currentStageDef != null)
            {
                EditorUtility.SetDirty(_currentStageDef);
            }
        }

        // =========================================================================
        // TAB 5: FILE LINKS
        // =========================================================================
        private void DrawFileLinksTab()
        {
            string id = StageIds[_selectedStageIndex];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"File Locations for {id}", EditorStyles.boldLabel);

            DrawPathRow("Stage Definition Asset", _currentStageDef != null ? AssetDatabase.GetAssetPath(_currentStageDef) : "In-Memory / Auto-Generated", _currentStageDef);
            DrawPathRow("Stage 3D Prefab", _currentStagePrefab != null ? AssetDatabase.GetAssetPath(_currentStagePrefab) : "Not Found", _currentStagePrefab);
            DrawPathRow("Active Battle Scene", "Assets/Scenes/ArcadeArena.unity", AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/ArcadeArena.unity"));
            DrawPathRow("Stage Director Script", "Assets/Scripts/Editor/DemonIdolStageDirector.cs", AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/DemonIdolStageDirector.cs"));
            EditorGUILayout.EndVertical();
        }

        private void DrawPathRow(string label, string path, UnityEngine.Object assetObj)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.TextField(path);
            if (assetObj != null)
            {
                if (GUILayout.Button("Focus", GUILayout.Width(60)))
                {
                    Selection.activeObject = assetObj;
                    EditorGUIUtility.PingObject(assetObj);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================================
        // FOOTER ACTIONS
        // =========================================================================
        private void DrawFooterActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f, 1f);
            if (GUILayout.Button("⚡ Deploy This Stage to Arcade Arena Scene", GUILayout.Height(36)))
            {
                DeployStageToArcadeArena();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("🎮 Test Stage in Play Mode", GUILayout.Height(36), GUILayout.Width(180)))
            {
                DeployStageToArcadeArena();
                DemonIdolMainMenuBuilder.PlayMatchInEditor();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💾 Save Stage Asset Changes", GUILayout.Height(24)))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=#00e676>[Stage Studio] Saved changes for {StageIds[_selectedStageIndex]} successfully!</color>");
            }
            if (GUILayout.Button("Open ArcadeArena Scene File", GUILayout.Height(24)))
            {
                DemonIdolMainMenuBuilder.OpenArcadeArenaScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DeployStageToArcadeArena()
        {
            if (_currentStagePrefab != null)
            {
                DemonIdolArcadeArenaBuilder.ActiveStagePrefabPath = AssetDatabase.GetAssetPath(_currentStagePrefab);
            }
            else
            {
                string id = StageIds[_selectedStageIndex];
                string[] prefabs = Directory.GetFiles(STAGES_PREFAB_DIR, $"{id}*.prefab");
                if (prefabs.Length > 0)
                {
                    DemonIdolArcadeArenaBuilder.ActiveStagePrefabPath = prefabs[0];
                }
            }

            DemonIdolArcadeArenaBuilder.ActiveKeyLightColor = _customKeyLightColor;
            DemonIdolArcadeArenaBuilder.ActiveLightIntensity = _customLightIntensity;

            DemonIdolArcadeArenaBuilder.BuildArcadeArenaScene();
            Debug.Log($"<color=#00e676>[Stage Studio] Successfully deployed {StageIds[_selectedStageIndex]} to ArcadeArena.unity!</color>");
        }
    }
}
#endif

#endif
