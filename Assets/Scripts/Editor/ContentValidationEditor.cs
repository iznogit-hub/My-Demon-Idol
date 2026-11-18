using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DemonIdol.Combat;
using DemonIdol.Cutscenes;

namespace DemonIdol.EditorTools
{
    public class ContentValidationEditor : EditorWindow
    {
        public struct ValidationEntry
        {
            public enum Severity { Info, Warning, Error }
            public Severity severity;
            public string category;
            public string message;
            public UnityEngine.Object targetObject;
        }

        private List<ValidationEntry> _results = new List<ValidationEntry>();
        private Vector2 _scrollPos;
        private int _errorCount = 0;
        private int _warningCount = 0;
        private int _infoCount = 0;

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Validate Content Definitions", priority = 50)]
        public static void OpenWindow()
        {
            var window = GetWindow<ContentValidationEditor>("Content Validator");
            window.minSize = new Vector2(550, 450);
            window.RunValidation();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("MY DEMON IDOL: CONTENT DEFINITION VALIDATOR", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Audits ScriptableObjects for character stats, moveset frames, stage bounds, and video bindings.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("RUN FULL VALIDATION AUDIT", GUILayout.Height(32)))
            {
                RunValidation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Summary bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Errors: {_errorCount}", EditorStyles.boldLabel);
            GUILayout.Space(12);
            GUILayout.Label($"Warnings: {_warningCount}", EditorStyles.boldLabel);
            GUILayout.Space(12);
            GUILayout.Label($"Passed / Info: {_infoCount}");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Results list
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            if (_results == null || _results.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Run Full Validation Audit' to validate all character and stage ScriptableObjects.", MessageType.Info);
            }
            else
            {
                foreach (var entry in _results)
                {
                    MessageType msgType = MessageType.Info;
                    if (entry.severity == ValidationEntry.Severity.Error) msgType = MessageType.Error;
                    else if (entry.severity == ValidationEntry.Severity.Warning) msgType = MessageType.Warning;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox($"[{entry.category}] {entry.message}", msgType);
                    if (entry.targetObject != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Select Asset", GUILayout.Width(100)))
                        {
                            EditorGUIUtility.PingObject(entry.targetObject);
                            Selection.activeObject = entry.targetObject;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        public static void RunValidationBatch()
        {
            ValidateAll(out var entries);
            int errors = 0;
            int warnings = 0;
            foreach (var e in entries)
            {
                if (e.severity == ValidationEntry.Severity.Error) errors++;
                else if (e.severity == ValidationEntry.Severity.Warning) warnings++;
            }
            Debug.Log($"[ContentValidation] Batch audit complete. {errors} Errors, {warnings} Warnings.");
        }

        public void RunValidation()
        {
            _results.Clear();
            _errorCount = 0;
            _warningCount = 0;
            _infoCount = 0;

            ValidateAll(out var entries);
            _results = entries;

            foreach (var e in _results)
            {
                if (e.severity == ValidationEntry.Severity.Error) _errorCount++;
                else if (e.severity == ValidationEntry.Severity.Warning) _warningCount++;
                else _infoCount++;
            }

            Debug.Log($"[ContentValidation] Audit complete. {_errorCount} Errors, {_warningCount} Warnings, {_infoCount} Info items.");
        }

        public static void ValidateAll(out List<ValidationEntry> log)
        {
            log = new List<ValidationEntry>();

            ValidateCharacters(log);
            ValidateStages(log);
        }

        private static void ValidateCharacters(List<ValidationEntry> log)
        {
            string[] charGuids = AssetDatabase.FindAssets("t:CharacterDefinition");
            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (charGuids.Length == 0)
            {
                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Warning,
                    category = "Characters",
                    message = "No CharacterDefinition assets found in project. CharacterRegistry runtime fallbacks will be used."
                });
            }

            foreach (string guid in charGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterDefinition charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (charDef == null) continue;

                // ID Check
                if (string.IsNullOrWhiteSpace(charDef.id))
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Character ID",
                        message = $"Character '{charDef.name}' has empty or whitespace ID at '{path}'.",
                        targetObject = charDef
                    });
                }
                else if (seenIds.Contains(charDef.id))
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Character ID",
                        message = $"Duplicate character ID '{charDef.id}' found in '{path}'.",
                        targetObject = charDef
                    });
                }
                else
                {
                    seenIds.Add(charDef.id);
                }

                // Stats Check
                if (charDef.speed < 0f || charDef.speed > 100f)
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Warning,
                        category = "Character Stats",
                        message = $"Character '{charDef.id}' speed {charDef.speed} is outside standard [0, 100] range.",
                        targetObject = charDef
                    });
                }

                // MoveSet Check
                if (charDef.moveSet == null)
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "MoveSet",
                        message = $"Character '{charDef.id}' has NULL MoveSet reference.",
                        targetObject = charDef
                    });
                }
                else
                {
                    ValidateMoveSet(charDef.id, charDef.moveSet, log);
                }

                // VideoBindings Check
                if (charDef.videoBindings != null)
                {
                    ValidateVideoBindings(charDef.id, charDef.videoBindings, log);
                }
                else
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Info,
                        category = "VideoBindings",
                        message = $"Character '{charDef.id}' has no custom VideoBindings; will use universal fallback clips.",
                        targetObject = charDef
                    });
                }

                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Info,
                    category = "Character OK",
                    message = $"Character '{charDef.displayName}' ({charDef.id}) validated successfully.",
                    targetObject = charDef
                });
            }
        }

        private static void ValidateMoveSet(string charId, MoveSet ms, List<ValidationEntry> log)
        {
            ValidateAttack(charId, "LightAttack", ms.lightAttack, true, log);
            ValidateAttack(charId, "MediumAttack", ms.mediumAttack, true, log);
            ValidateAttack(charId, "HeavyAttack", ms.heavyAttack, true, log);
            ValidateAttack(charId, "DriveSkill", ms.driveSkill, true, log);
            ValidateAttack(charId, "DomainSuper", ms.domainSuper, true, log);
            ValidateAttack(charId, "TacticalThrow", ms.tacticalThrow, true, log);

            ValidateAttack(charId, "OverheadAttack", ms.overheadAttack, false, log);
            ValidateAttack(charId, "LowSweepAttack", ms.lowSweepAttack, false, log);
            ValidateAttack(charId, "AntiAirAttack", ms.antiairAttack, false, log);

            if (ms.additionalMoves != null)
            {
                for (int i = 0; i < ms.additionalMoves.Count; i++)
                {
                    ValidateAttack(charId, $"AdditionalMove[{i}]", ms.additionalMoves[i], false, log);
                }
            }
        }

        private static void ValidateAttack(string charId, string slotName, AttackData atk, bool required, List<ValidationEntry> log)
        {
            if (atk == null)
            {
                if (required)
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "MoveSet Frame",
                        message = $"Character '{charId}' MoveSet slot '{slotName}' is missing (NULL)."
                    });
                }
                return;
            }

            if (atk.startupFrames <= 0)
            {
                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Error,
                    category = "Frame Data",
                    message = $"Attack '{atk.attackName}' ({slotName}) in '{charId}' has non-positive startupFrames ({atk.startupFrames}). Must be >= 1.",
                    targetObject = atk
                });
            }

            if (atk.activeFrames <= 0)
            {
                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Error,
                    category = "Frame Data",
                    message = $"Attack '{atk.attackName}' ({slotName}) in '{charId}' has non-positive activeFrames ({atk.activeFrames}). Must be >= 1.",
                    targetObject = atk
                });
            }

            if (atk.recoveryFrames <= 0)
            {
                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Warning,
                    category = "Frame Data",
                    message = $"Attack '{atk.attackName}' ({slotName}) in '{charId}' has recoveryFrames <= 0 ({atk.recoveryFrames}).",
                    targetObject = atk
                });
            }

            if (atk.damage < 0)
            {
                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Error,
                    category = "Damage",
                    message = $"Attack '{atk.attackName}' ({slotName}) in '{charId}' has negative damage ({atk.damage}).",
                    targetObject = atk
                });
            }
        }

        private static void ValidateVideoBindings(string charId, VideoBindings vb, List<ValidationEntry> log)
        {
            if (vb.bindings == null || vb.bindings.Count == 0)
            {
                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Info,
                    category = "VideoBindings",
                    message = $"VideoBindings for '{charId}' has no explicit entries.",
                    targetObject = vb
                });
                return;
            }

            for (int i = 0; i < vb.bindings.Count; i++)
            {
                var entry = vb.bindings[i];
                if (string.IsNullOrWhiteSpace(entry.moveOrSuperId))
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Warning,
                        category = "VideoBindings",
                        message = $"VideoBindings entry [{i}] for '{charId}' has empty moveOrSuperId.",
                        targetObject = vb
                    });
                }

                if (entry.directClip == null && string.IsNullOrWhiteSpace(entry.relativePath))
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Warning,
                        category = "VideoBindings",
                        message = $"VideoBindings entry '{entry.moveOrSuperId}' in '{charId}' has neither VideoClip nor streaming path.",
                        targetObject = vb
                    });
                }
            }
        }

        private static void ValidateStages(List<ValidationEntry> log)
        {
            string[] stageGuids = AssetDatabase.FindAssets("t:StageDefinition");
            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (stageGuids.Length == 0)
            {
                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Warning,
                    category = "Stages",
                    message = "No StageDefinition assets found in project. StageRegistry runtime fallbacks will be used."
                });
            }

            foreach (string guid in stageGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageDefinition stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(path);
                if (stage == null) continue;

                if (string.IsNullOrWhiteSpace(stage.id))
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Stage ID",
                        message = $"Stage '{stage.name}' has empty ID at '{path}'.",
                        targetObject = stage
                    });
                }
                else if (seenIds.Contains(stage.id))
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Stage ID",
                        message = $"Duplicate stage ID '{stage.id}' found in '{path}'.",
                        targetObject = stage
                    });
                }
                else
                {
                    seenIds.Add(stage.id);
                }

                if (stage.playableWidth <= 0f)
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Stage Bounds",
                        message = $"Stage '{stage.id}' has non-positive playableWidth ({stage.playableWidth}). Must be > 0 (e.g. 28.0m).",
                        targetObject = stage
                    });
                }

                float halfWidth = stage.playableWidth * 0.5f;
                if (stage.p1SpawnPosition.x >= stage.p2SpawnPosition.x)
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Stage Spawns",
                        message = $"Stage '{stage.id}' P1 spawn ({stage.p1SpawnPosition.x}) is not to the left of P2 ({stage.p2SpawnPosition.x}).",
                        targetObject = stage
                    });
                }

                if (Mathf.Abs(stage.p1SpawnPosition.x) > halfWidth || Mathf.Abs(stage.p2SpawnPosition.x) > halfWidth)
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Stage Spawns",
                        message = $"Stage '{stage.id}' spawn points exceed half-width ({halfWidth}m).",
                        targetObject = stage
                    });
                }

                if (stage.wallThreshold > halfWidth)
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Warning,
                        category = "Stage Splat",
                        message = $"Stage '{stage.id}' wallThreshold ({stage.wallThreshold}) exceeds halfWidth ({halfWidth}).",
                        targetObject = stage
                    });
                }

                if (Mathf.Abs(stage.cameraMinZ) > Mathf.Abs(stage.cameraMaxZ))
                {
                    log.Add(new ValidationEntry
                    {
                        severity = ValidationEntry.Severity.Error,
                        category = "Stage Camera",
                        message = $"Stage '{stage.id}' camera distance range invalid: |cameraMinZ| ({Mathf.Abs(stage.cameraMinZ)}) > |cameraMaxZ| ({Mathf.Abs(stage.cameraMaxZ)}).",
                        targetObject = stage
                    });
                }

                log.Add(new ValidationEntry
                {
                    severity = ValidationEntry.Severity.Info,
                    category = "Stage OK",
                    message = $"Stage '{stage.displayName}' ({stage.id}) validated successfully (Width: {stage.playableWidth:F1}m, Wall: {stage.wallThreshold:F1}m, CamZ: [{stage.cameraMinZ:F1}, {stage.cameraMaxZ:F1}]).",
                    targetObject = stage
                });
            }
        }
    }
}
