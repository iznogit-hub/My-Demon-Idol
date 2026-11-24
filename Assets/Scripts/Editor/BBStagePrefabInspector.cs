#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using DemonIdol.BlazBlue;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Custom Inspector for BBStagePrefabConfig.
    /// Provides one-click lighting/fog preview in Scene View, automated bounds validation,
    /// and clean foldouts for stage geometry, camera limits, and atmosphere.
    /// </summary>
    [CustomEditor(typeof(BBStagePrefabConfig))]
    public class BBStagePrefabInspector : UnityEditor.Editor
    {
        private BBStagePrefabConfig _config;

        private static bool _foldoutIdentity = true;
        private static bool _foldoutBounds = true;
        private static bool _foldoutAtmosphere = true;
        private static bool _foldoutAudioProps = true;

        private string _validationStatus = null;
        private MessageType _validationMessageType = MessageType.None;

        private GUIStyle _headerStyle;
        private GUIStyle _boxCardStyle;

        private void OnEnable()
        {
            _config = target as BBStagePrefabConfig;
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };

            _boxCardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            if (_config == null) return;

            serializedObject.Update();

            // Header Banner
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"STAGE PREFAB: {_config.stageId} — {_config.stageDisplayName}", _headerStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("All-in-One Arena Customizer: Geometry, Walls, Camera, Lighting, Fog & Audio", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 1. Stage Identity
            _foldoutIdentity = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutIdentity, "1. Stage Identity");
            if (_foldoutIdentity)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stageId"), new GUIContent("Stage ID (e.g. ST07)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stageDisplayName"), new GUIContent("Display Name"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stageThumbnail"), new GUIContent("Thumbnail Sprite (Select UI)"));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 2. Combat Geometry & Bounds
            _foldoutBounds = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutBounds, "2. Combat Geometry & Bounds (Live Gizmos in Prefab Mode)");
            if (_foldoutBounds)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                EditorGUILayout.LabelField("Spawn Positions & Floor", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("p1SpawnX"), new GUIContent("P1 Spawn X (Meters)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("p2SpawnX"), new GUIContent("P2 Spawn X (Meters)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("floorY"), new GUIContent("Floor Surface Y"));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Corner Wall Boundaries", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("leftWallX"), new GUIContent("Left Wall X (Corner)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rightWallX"), new GUIContent("Right Wall X (Corner)"));

                float arenaWidth = Mathf.Abs(_config.rightWallX - _config.leftWallX);
                EditorGUILayout.LabelField($"Total Playable Arena Width: {arenaWidth:F2}m", EditorStyles.miniLabel);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Camera Framing Limits", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMinX"), new GUIContent("Camera Min X"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMaxX"), new GUIContent("Camera Max X"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMinY"), new GUIContent("Camera Min Y"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMaxY"), new GUIContent("Camera Max Y"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraDefaultZ"), new GUIContent("Camera Default Z (Depth)"));

                EditorGUILayout.Space(6);

                // Validate Bounds Button
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
                if (GUILayout.Button("🔍 Validate Stage Bounds & Floor Alignment", GUILayout.Height(26)))
                {
                    ValidateStageBounds();
                }
                GUI.backgroundColor = Color.white;

                if (!string.IsNullOrEmpty(_validationStatus))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(_validationStatus, _validationMessageType);
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 3. Atmosphere, Lighting & Fog
            _foldoutAtmosphere = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutAtmosphere, "3. Atmosphere, Lighting & Fog");
            if (_foldoutAtmosphere)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("skyboxMaterial"), new GUIContent("Skybox Material"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientColor"), new GUIContent("Ambient Light Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("directionalLightColor"), new GUIContent("Directional Light Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("directionalLightIntensity"), new GUIContent("Directional Intensity"));

                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fogEnabled"), new GUIContent("Enable Fog"));
                if (_config.fogEnabled)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogColor"), new GUIContent("Fog Color"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fogDensity"), new GUIContent("Fog Density"));
                }

                EditorGUILayout.Space(6);

                // Preview Lighting Button
                GUI.backgroundColor = new Color(1f, 0.75f, 0.2f);
                if (GUILayout.Button("☀️ Preview Stage Lighting & Fog in Editor", GUILayout.Height(26)))
                {
                    _config.ApplyEnvironmentSettings();
                    SceneView.RepaintAll();
                    Debug.Log($"<color=#00e676>[BBStagePrefabInspector] Applied lighting, fog ({_config.fogDensity:F3}), and atmosphere to Scene View for {_config.stageDisplayName}</color>");
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(6);

            // 4. Post-FX, Audio & Hierarchy
            _foldoutAudioProps = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutAudioProps, "4. Post-FX, Audio & Hierarchy");
            if (_foldoutAudioProps)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("postProcessProfile"), new GUIContent("Post Process Profile (URP)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stageBGM"), new GUIContent("Stage BGM (Fight Music)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientLoopSFX"), new GUIContent("Ambient Loop SFX (Rain/Wind)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stagePropsRoot"), new GUIContent("Stage Props Root (Visual Container)"));

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(12);

            // Save Button
            GUI.backgroundColor = new Color(0.15f, 0.85f, 0.45f);
            if (GUILayout.Button("💾 Save Stage Prefab & Linked Assets", GUILayout.Height(34)))
            {
                SaveStagePrefab();
            }
            GUI.backgroundColor = Color.white;

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateStageBounds()
        {
            var issues = new System.Text.StringBuilder();
            bool valid = true;

            // Check LeftWall < P1 < P2 < RightWall
            if (!(_config.leftWallX < _config.p1SpawnX))
            {
                issues.AppendLine($"• Left Wall ({_config.leftWallX}m) must be to the left of P1 Spawn ({_config.p1SpawnX}m).");
                valid = false;
            }
            if (!(_config.p1SpawnX < _config.p2SpawnX))
            {
                issues.AppendLine($"• P1 Spawn ({_config.p1SpawnX}m) must be to the left of P2 Spawn ({_config.p2SpawnX}m).");
                valid = false;
            }
            if (!(_config.p2SpawnX < _config.rightWallX))
            {
                issues.AppendLine($"• P2 Spawn ({_config.p2SpawnX}m) must be to the left of Right Wall ({_config.rightWallX}m).");
                valid = false;
            }

            // Check Camera bounds
            if (!(_config.cameraMinX < _config.cameraMaxX))
            {
                issues.AppendLine($"• Camera Min X ({_config.cameraMinX}) must be less than Camera Max X ({_config.cameraMaxX}).");
                valid = false;
            }
            if (!(_config.cameraMinY < _config.cameraMaxY))
            {
                issues.AppendLine($"• Camera Min Y ({_config.cameraMinY}) must be less than Camera Max Y ({_config.cameraMaxY}).");
                valid = false;
            }

            // Check Floor Collider / Visual Alignment
            var colliders = _config.GetComponentsInChildren<Collider>(true);
            bool floorFound = false;
            float closestFloorTop = float.MinValue;

            foreach (var col in colliders)
            {
                if (col.gameObject.name.IndexOf("ground", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    col.gameObject.name.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    col.gameObject.name.IndexOf("combatplatform", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    floorFound = true;
                    float topY = col.bounds.max.y;
                    if (Mathf.Abs(topY - _config.floorY) < 0.15f)
                    {
                        closestFloorTop = topY;
                        break;
                    }
                    closestFloorTop = topY;
                }
            }

            if (floorFound)
            {
                if (Mathf.Abs(closestFloorTop - _config.floorY) > 0.15f)
                {
                    issues.AppendLine($"• Floor collider top surface is at Y={closestFloorTop:F2}m, but floorY is configured as {_config.floorY:F2}m. Misaligned by {Mathf.Abs(closestFloorTop - _config.floorY):F2}m.");
                    valid = false;
                }
            }
            else
            {
                issues.AppendLine("• Note: No child collider named 'Ground', 'Floor', or 'CombatPlatform' detected under stage root.");
            }

            if (valid)
            {
                _validationStatus = "✅ VALID: Stage Bounds & Geometry are perfectly calibrated!\n" +
                                    $"• Spacing: LeftWall ({_config.leftWallX:F1}m) < P1 ({_config.p1SpawnX:F1}m) < P2 ({_config.p2SpawnX:F1}m) < RightWall ({_config.rightWallX:F1}m)\n" +
                                    $"• Floor Surface aligns to Y={_config.floorY:F2}m\n" +
                                    $"• Camera framing: [{_config.cameraMinX:F1}m .. {_config.cameraMaxX:F1}m, {_config.cameraMinY:F1}m .. {_config.cameraMaxY:F1}m, Z={_config.cameraDefaultZ:F1}m]";
                _validationMessageType = MessageType.Info;
                Debug.Log($"<color=#00e676>[BBStagePrefabInspector] {_validationStatus}</color>");
            }
            else
            {
                _validationStatus = "❌ BOUNDS VALIDATION ISSUES DETECTED:\n" + issues.ToString();
                _validationMessageType = MessageType.Error;
                Debug.LogWarning($"[BBStagePrefabInspector] {_validationStatus}");
            }
        }

        private void SaveStagePrefab()
        {
            if (_config == null) return;

            EditorUtility.SetDirty(_config);
            EditorUtility.SetDirty(_config.gameObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBStagePrefabInspector] Successfully saved stage prefab {_config.stageId} ({_config.stageDisplayName}) to disk!</color>");
        }
    }
}
#endif
