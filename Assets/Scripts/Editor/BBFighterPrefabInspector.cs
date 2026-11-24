#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Prefab-First All-in-One Customizer for Fighters.
    /// Allows designers to swap 3D models with auto-grounding & auto-rigging,
    /// slice FBX animations, add/remove/reorder moves, auto-sync hitboxes to peak frames,
    /// customize locomotion & weapon sockets, look-dev outfits, generate portraits,
    /// and test directly in BattleArena from one Inspector.
    /// </summary>
    [CustomEditor(typeof(BBFighterPrefabConfig))]
    public class BBFighterPrefabInspector : UnityEditor.Editor
    {
        private BBFighterPrefabConfig _config;

        // Foldout States
        private static bool _foldoutModelSwap = true;
        private static bool _foldoutAttacks = true;
        private static bool _foldoutLocomotion = true;
        private static bool _foldoutWeapons = true;
        private static bool _foldoutIdentity = true;
        private static bool _foldoutLookDev = true;

        // Feature 1: Model Swapper State
        private GameObject _newModelSource = null;
        private float _targetHeight = 1.70f;

        // Feature 2: Moves List & Management
        private static readonly string[] SEVEN_MOVE_IDS = new string[]
        {
            "5A", "5B", "5C", "2C", "j.B", "5D", "236236C"
        };
        private List<BBAttackData> _cachedMoves = new List<BBAttackData>();

        // Preview & Scrubbing State
        private string _activePreviewMoveId = null;
        private int _scrubTick = 0;
        private bool _weaponLivePreview = false;

        // Move Deletion & Addition State
        private int _confirmRemoveIndex = -1;
        private bool _showAddMovePanel = false;
        private static readonly string[] PRESET_MOVE_IDS = new string[]
        {
            "5A", "5B", "5C", "2A", "2B", "2C", "6A", "6B", "6C",
            "j.A", "j.B", "j.C", "236A", "236B", "236C",
            "214A", "214B", "214C", "5D", "236236C", "Custom"
        };
        private int _selectedPresetIndex = 0;
        private string _customMoveIdInput = "214A";
        private string _newMoveNameInput = "";
        private AnimationClip _newMoveClipInput = null;

        // Sub-Clip Slicer State
        private string _activeSlicerMoveId = null;
        private int _sliceStartFrame = 0;
        private int _sliceEndFrame = 30;
        private string _sliceSubClipName = "";

        // Section 2: Locomotion Slots
        private struct LocomotionSlot
        {
            public string displayName;
            public string[] matchOriginalKeywords;
            public AnimationClip currentOverrideClip;
            public AnimationClip originalClipKey;
        }

        private List<LocomotionSlot> _locomotionSlots = new List<LocomotionSlot>();

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxCardStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _successStyle;

        private void OnEnable()
        {
            _config = target as BBFighterPrefabConfig;
            if (_config != null)
            {
                _config.ResolveLinkedAssets();
                if (_config.characterDefinition != null && _config.characterDefinition.heightMeters >= 1.2f)
                {
                    _targetHeight = _config.characterDefinition.heightMeters;
                }
                RefreshMovesList();
                RefreshLocomotionSlots();
            }
        }

        private void OnDisable()
        {
            StopAttackPreview();
            if (_weaponLivePreview && _config != null && _config.weaponRig != null)
            {
                BBWeaponRig.DestroyOrphanedWeaponChildren(_config.weaponRig.PrimaryBoneTransform);
                BBWeaponRig.DestroyOrphanedWeaponChildren(_config.weaponRig.SecondaryBoneTransform);
            }
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.2f, 0.8f, 1f) }
            };

            _boxCardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _warningStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11
            };

            _successStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11
            };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            if (_config == null) return;

            serializedObject.Update();

            // Header Banner
            DrawHeaderBanner();

            EditorGUILayout.Space(6);

            // Feature 1: 3D Model & Rig Swapper
            DrawSection0_ModelSwapper();

            EditorGUILayout.Space(6);

            // Feature 2: Attack Clips, Frame Data, Add/Slice & Auto-Sync
            DrawSection1_MovesAndAnimations();

            EditorGUILayout.Space(6);

            // Section 2: Layer 0 Locomotion & Reaction Clips
            DrawSection2_Locomotion();

            EditorGUILayout.Space(6);

            // Section 3: Weapon Rig, Scale & Socket Preview
            DrawSection3_WeaponRig();

            EditorGUILayout.Space(6);

            // Section 4: Identity, Portrait & Material Quick-Links
            DrawSection4_Identity();

            EditorGUILayout.Space(6);

            // Feature 3: Look-Dev, Outfits, Portrait & Live Arena Test Strip
            DrawSection5_LookDevAndTestStrip();

            EditorGUILayout.Space(12);

            // Save All Button
            DrawSaveAllButton();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderBanner()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"FIGHTER PREFAB: {_config.characterId} — {_config.characterDisplayName}", _headerStyle);
            if (GUILayout.Button("Auto-Resolve", GUILayout.Width(95), GUILayout.Height(22)))
            {
                _config.ResolveLinkedAssets();
                RefreshMovesList();
                RefreshLocomotionSlots();
                EditorUtility.SetDirty(_config);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Profile: {(_config.profile != null ? _config.profile.name : "<None>")}  |  Definition: {(_config.characterDefinition != null ? _config.characterDefinition.name : "<None>")}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        #region Feature 1: 1-Click 3D Model Swapper

        private void DrawSection0_ModelSwapper()
        {
            _foldoutModelSwap = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutModelSwap, "3D Model & Rig Swap (1-Click Drop-In)");
            if (_foldoutModelSwap)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                EditorGUILayout.LabelField("Swap Fighter 3D Model without Breaking Prefab Wiring", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Instantiates the model cleanly as child Model_<ID>, normalizes uniform scale to Target Height, grounds soles at footY ≈ 0, configures Animator, attaches AnimationEventReceiver, and re-binds BBWeaponRig, BBAnimationDriver, and Hurtboxes.", MessageType.None);

                EditorGUILayout.Space(4);

                _newModelSource = (GameObject)EditorGUILayout.ObjectField("New 3D Model (.fbx/prefab)", _newModelSource, typeof(GameObject), false);
                _targetHeight = EditorGUILayout.FloatField("Target Height (m)", _targetHeight);

                // Validation of selected model
                if (_newModelSource != null)
                {
                    string fbxPath = AssetDatabase.GetAssetPath(_newModelSource);
                    ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                    bool isHumanoid = false;

                    if (importer != null)
                    {
                        isHumanoid = (importer.animationType == ModelImporterAnimationType.Human);
                    }
                    else
                    {
                        var anim = _newModelSource.GetComponentInChildren<Animator>(true);
                        isHumanoid = (anim != null && anim.isHuman);
                    }

                    if (!isHumanoid && importer != null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.HelpBox("⚠️ Selected model is not configured as Humanoid! BlazBlue combat animations require a Humanoid Avatar.", MessageType.Warning);
                        if (GUILayout.Button("🛠 Convert Model FBX to Humanoid & Apply", GUILayout.Height(26)))
                        {
                            importer.animationType = ModelImporterAnimationType.Human;
                            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                            importer.SaveAndReimport();
                            AssetDatabase.Refresh();
                            Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Converted {fbxPath} to Humanoid Avatar successfully!</color>");
                        }
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.Space(4);

                // Swap Button
                GUI.backgroundColor = (_newModelSource != null) ? new Color(0.2f, 0.85f, 0.4f) : Color.gray;
                EditorGUI.BeginDisabledGroup(_newModelSource == null);
                if (GUILayout.Button("🔄 1-Click Swap 3D Model (Normalize Scale, Ground & Auto-Rig)", GUILayout.Height(30)))
                {
                    ExecuteModelSwap(_newModelSource, _targetHeight);
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public void ExecuteModelSwap(GameObject modelSource, float targetHeight)
        {
            if (modelSource == null || _config == null) return;

            Undo.SetCurrentGroupName($"Swap 3D Model on {_config.gameObject.name}");
            int undoGroup = Undo.GetCurrentGroup();

            Transform root = _config.transform;
            string targetChildName = $"Model_{_config.characterId}";

            // 1. Locate and remove old model child
            Transform oldModelTransform = root.Find(targetChildName);
            if (oldModelTransform == null)
            {
                // Fallback: search for any child starting with Model_ or containing existing Animator
                for (int i = 0; i < root.childCount; i++)
                {
                    var child = root.GetChild(i);
                    if (child.name.StartsWith("Model", StringComparison.OrdinalIgnoreCase) ||
                        (_config.animator != null && child == _config.animator.transform))
                    {
                        oldModelTransform = child;
                        break;
                    }
                }
            }

            // Fallback for flat prefabs: check for Armature or Body
            if (oldModelTransform == null)
            {
                var oldArmature = root.Find("Armature");
                if (oldArmature != null) Undo.DestroyObjectImmediate(oldArmature.gameObject);
                var oldBody = root.Find("Body");
                if (oldBody != null) Undo.DestroyObjectImmediate(oldBody.gameObject);
            }
            else
            {
                Undo.DestroyObjectImmediate(oldModelTransform.gameObject);
            }

            // 2. Instantiate new model as child
            GameObject newInstance = PrefabUtility.InstantiatePrefab(modelSource, root) as GameObject;
            if (newInstance == null)
            {
                newInstance = Instantiate(modelSource, root);
            }
            newInstance.name = targetChildName;
            Undo.RegisterCreatedObjectUndo(newInstance, "Instantiate Swapped Model");

            newInstance.transform.localPosition = Vector3.zero;
            newInstance.transform.localRotation = Quaternion.identity;
            newInstance.transform.localScale = Vector3.one;

            // 3. Configure Animator on new model
            Animator anim = newInstance.GetComponent<Animator>();
            if (anim == null) anim = Undo.AddComponent<Animator>(newInstance);
            Undo.RecordObject(anim, "Configure Swapped Animator");
            if (_config.overrideController != null)
            {
                anim.runtimeAnimatorController = _config.overrideController;
            }
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(anim);

            // 4. Attach AnimationEventReceiver to silence FootL/FootR errors
            if (newInstance.GetComponent<AnimationEventReceiver>() == null)
            {
                Undo.AddComponent<AnimationEventReceiver>(newInstance);
            }

            // 5. Normalize Scale and Grounding (footY ≈ 0)
            foreach (var smr in newInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
            }

            Bounds b = new Bounds();
            bool hasBounds = false;
            foreach (var r in newInstance.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsBodyRenderer(r)) continue;
                if (!hasBounds)
                {
                    b = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }

            if (hasBounds && b.size.y > 0.05f)
            {
                float scaleFactor = targetHeight / b.size.y;
                newInstance.transform.localScale = Vector3.one * scaleFactor;

                anim.Rebind();
                anim.Update(0.033f);

                float lowestWorldY = float.MaxValue;
                foreach (var r in newInstance.GetComponentsInChildren<Renderer>(true))
                {
                    if (!IsBodyRenderer(r)) continue;
                    if (r.bounds.min.y < lowestWorldY)
                    {
                        lowestWorldY = r.bounds.min.y;
                    }
                }

                if (lowestWorldY != float.MaxValue)
                {
                    float footDelta = lowestWorldY - root.position.y;
                    Vector3 lp = newInstance.transform.localPosition;
                    lp.y -= footDelta;
                    newInstance.transform.localPosition = lp;
                }
            }

            // 6. Re-bind components
            Undo.RecordObject(_config, "Update Config Animator");
            _config.animator = anim;
            EditorUtility.SetDirty(_config);

            var animDriver = _config.GetComponent<BBAnimationDriver>();
            if (animDriver != null)
            {
                Undo.RecordObject(animDriver, "Rebind AnimationDriver");
                animDriver.animator = anim;
                EditorUtility.SetDirty(animDriver);
            }

            var fc = _config.GetComponent<FighterController>();
            if (fc != null)
            {
                Undo.RecordObject(fc, "Rebind FighterController");
                fc.animator = anim;
                EditorUtility.SetDirty(fc);
            }

            var shadow = _config.GetComponentInChildren<FighterBlobShadow>(true);
            if (shadow != null)
            {
                Undo.RecordObject(shadow, "Rebind FighterBlobShadow");
                shadow.fighterTarget = root;
                EditorUtility.SetDirty(shadow);
            }

            var rig = _config.weaponRig != null ? _config.weaponRig : _config.GetComponent<BBWeaponRig>();
            if (rig != null)
            {
                Undo.RecordObject(rig, "Rebind WeaponRig");
                rig.LocateBoneTransforms();
                rig.AttachWeapons();
                EditorUtility.SetDirty(rig);
            }

            if (_config.characterDefinition != null)
            {
                Undo.RecordObject(_config.characterDefinition, "Update CharacterDefinition");
                _config.characterDefinition.modelPrefab = modelSource;
                _config.characterDefinition.heightMeters = targetHeight;
                if (anim.avatar != null) _config.characterDefinition.avatar = anim.avatar;
                EditorUtility.SetDirty(_config.characterDefinition);
            }

            // 7. Update BBBodyBoxes hurtboxes from Hip Height
            float hipH = 0.95f;
            if (anim.isHuman)
            {
                var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null) hipH = hips.position.y - root.position.y;
            }
            else
            {
                hipH = targetHeight * 0.52f;
            }

            var brain = _config.GetComponent<BBFighterBrain>();
            if (brain != null)
            {
                Undo.RecordObject(brain, "Update Hurtboxes");
                float weaponReach = (rig != null && rig.primaryTargetLength > 0.1f) ? rig.primaryTargetLength : 0.85f;
                brain.bodyBoxes = BBBodyBoxes.CreateFromHip(hipH, weaponReach);
                EditorUtility.SetDirty(brain);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(_config.gameObject);

            RefreshLocomotionSlots();
            RefreshMovesList();

            Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Successfully swapped 3D model on {_config.characterId} to '{modelSource.name}'! Scale: {newInstance.transform.localScale.x:F3}, Grounded footY: {newInstance.transform.localPosition.y:F3}m, Hip Height: {hipH:F2}m.</color>");
            SceneView.RepaintAll();
        }

        private static bool IsBodyRenderer(Renderer r)
        {
            if (r == null) return false;
            Transform t = r.transform;
            while (t != null)
            {
                string n = t.name;
                if (n.Contains("Weapon") || n.Contains("Prop") || n.Contains("Shadow") ||
                    n.Contains("Blob") || n.Contains("Hitbox") || n.Contains("Hurtbox") ||
                    n.Contains("Sword") || n.Contains("Wand") || n.Contains("Staff") ||
                    n.Contains("Blaster") || n.Contains("Gun") || n.Contains("Hammer") ||
                    n.Contains("Sheath") || n.Contains("Trail"))
                {
                    return false;
                }
                t = t.parent;
            }

            if (r is SkinnedMeshRenderer smr)
            {
                if (smr.rootBone == null) return false;
                string bn = smr.rootBone.name;
                if (bn.Contains("Weapon") || bn.Contains("Prop") || bn.Contains("Sword") ||
                    bn.Contains("Hammer") || bn.Contains("Wand") || bn.Contains("Staff") ||
                    bn.Contains("Gun") || bn.Contains("Blaster") || bn.Contains("Sheath"))
                {
                    return false;
                }
            }

            return (r is SkinnedMeshRenderer) || (r is MeshRenderer && !r.name.Contains("Visual") && !r.name.Contains("Shadow"));
        }

        #endregion

        #region Feature 2: Attack Moves, Frame Data, Add/Slice & Auto-Sync

        private void RefreshMovesList()
        {
            _cachedMoves.Clear();

            // 1. Primary source: BBMoveSet linked through profile
            if (_config.profile != null && _config.profile.moveSet is BBMoveSet moveSet && moveSet.moves != null)
            {
                for (int i = 0; i < moveSet.moves.Length; i++)
                {
                    if (moveSet.moves[i] != null && !_cachedMoves.Contains(moveSet.moves[i]))
                    {
                        _cachedMoves.Add(moveSet.moves[i]);
                    }
                }
            }

            // 2. Ensure all 7 core moves are represented
            string charFolder = !string.IsNullOrEmpty(_config.characterDisplayName) ? _config.characterDisplayName : _config.characterId;
            for (int i = 0; i < SEVEN_MOVE_IDS.Length; i++)
            {
                string moveId = SEVEN_MOVE_IDS[i];
                bool found = false;
                for (int m = 0; m < _cachedMoves.Count; m++)
                {
                    if (_cachedMoves[m] != null && _cachedMoves[m].moveId == moveId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    string[] guids = AssetDatabase.FindAssets($"BB_{charFolder}_{moveId} t:BBAttackData", new[] { "Assets/CombatData/BlazBlue" });
                    if (guids.Length == 0)
                    {
                        guids = AssetDatabase.FindAssets($"BB_{_config.characterId}_{moveId} t:BBAttackData", new[] { "Assets/CombatData/BlazBlue" });
                    }
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var atk = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
                        if (atk != null && !_cachedMoves.Contains(atk))
                        {
                            _cachedMoves.Add(atk);
                        }
                    }
                }
            }
        }

        private void DrawSection1_MovesAndAnimations()
        {
            _foldoutAttacks = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutAttacks, $"Section 1: Attack Moves & Frame Data ({_cachedMoves.Count} Moves)");
            if (_foldoutAttacks)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                for (int i = 0; i < _cachedMoves.Count; i++)
                {
                    var move = _cachedMoves[i];
                    DrawSingleMoveRow(move, i);

                    if (i < _cachedMoves.Count - 1)
                    {
                        EditorGUILayout.Space(4);
                        Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                        Handles.DrawLine(new Vector2(10, GUILayoutUtility.GetLastRect().yMax + 2), new Vector2(EditorGUIUtility.currentViewWidth - 20, GUILayoutUtility.GetLastRect().yMax + 2));
                        EditorGUILayout.Space(4);
                    }
                }

                EditorGUILayout.Space(8);

                // Add Move / Animation Slot Panel Toggle
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = _showAddMovePanel ? new Color(1f, 0.4f, 0.4f) : new Color(0.2f, 0.8f, 1f);
                if (GUILayout.Button(_showAddMovePanel ? "✖ Close Add Move Panel" : "➕ Add Move / Animation Slot", GUILayout.Height(28)))
                {
                    _showAddMovePanel = !_showAddMovePanel;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                if (_showAddMovePanel)
                {
                    DrawAddMovePanel();
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawSingleMoveRow(BBAttackData move, int slotIndex)
        {
            if (move == null) return;

            string moveId = move.moveId;
            bool isPreviewing = (_activePreviewMoveId == moveId);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Move Header with Badges and Controls
            EditorGUILayout.BeginHorizontal();
            GUI.color = isPreviewing ? new Color(1f, 0.4f, 0.4f) : Color.white;
            EditorGUILayout.LabelField($"[{move.moveId}] {move.attackName}", EditorStyles.boldLabel, GUILayout.Width(170));
            GUI.color = Color.white;

            GUILayout.Label($"Kind: {move.kind}", EditorStyles.miniLabel, GUILayout.Width(85));
            GUILayout.Label($"Btn: {move.button}", EditorStyles.miniLabel, GUILayout.Width(50));
            GUILayout.FlexibleSpace();

            // Reorder Buttons (Up / Down)
            EditorGUI.BeginDisabledGroup(slotIndex == 0);
            if (GUILayout.Button("▲", GUILayout.Width(22), GUILayout.Height(20)))
            {
                MoveMoveOrder(slotIndex, slotIndex - 1);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(slotIndex == _cachedMoves.Count - 1);
            if (GUILayout.Button("▼", GUILayout.Width(22), GUILayout.Height(20)))
            {
                MoveMoveOrder(slotIndex, slotIndex + 1);
            }
            EditorGUI.EndDisabledGroup();

            // Preview Scrub Button
            GUI.backgroundColor = isPreviewing ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.8f, 1f);
            if (GUILayout.Button(isPreviewing ? "⏹ Stop Preview" : "▶ Preview Scrub", GUILayout.Width(105), GUILayout.Height(20)))
            {
                if (isPreviewing) StopAttackPreview();
                else StartAttackPreview(moveId);
            }
            GUI.backgroundColor = Color.white;

            // Remove Button (×)
            GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
            if (GUILayout.Button("×", GUILayout.Width(22), GUILayout.Height(20)))
            {
                _confirmRemoveIndex = (_confirmRemoveIndex == slotIndex) ? -1 : slotIndex;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Inline Remove Confirmation Bar
            if (_confirmRemoveIndex == slotIndex)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Remove move [{move.moveId}] '{move.attackName}'?", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlink from Set Only", GUILayout.Height(22)))
                {
                    RemoveMoveFromSet(slotIndex, false);
                    _confirmRemoveIndex = -1;
                }
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("Delete .asset File", GUILayout.Height(22)))
                {
                    RemoveMoveFromSet(slotIndex, true);
                    _confirmRemoveIndex = -1;
                }
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Cancel", GUILayout.Width(70), GUILayout.Height(22)))
                {
                    _confirmRemoveIndex = -1;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            // Clip Slot & Auto-validation
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var newClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", move.clip, typeof(AnimationClip), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(move, "Change Attack Clip");
                move.clip = newClip;
                move.animationClip = newClip;
                move.clipName = newClip != null ? newClip.name : "";
                EditorUtility.SetDirty(move);
            }

            // FBX Slicer Toggle Button
            string clipPath = (move.clip != null) ? AssetDatabase.GetAssetPath(move.clip) : "";
            bool isFbx = !string.IsNullOrEmpty(clipPath) && (clipPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || clipPath.EndsWith(".FBX", StringComparison.OrdinalIgnoreCase));
            if (isFbx)
            {
                bool isSlicerActive = (_activeSlicerMoveId == move.moveId);
                GUI.backgroundColor = isSlicerActive ? new Color(1f, 0.6f, 0.2f) : new Color(0.8f, 0.8f, 0.9f);
                if (GUILayout.Button(isSlicerActive ? "Hide Slicer" : "✂️ Slice FBX", GUILayout.Width(85), GUILayout.Height(19)))
                {
                    _activeSlicerMoveId = isSlicerActive ? null : move.moveId;
                    if (_activeSlicerMoveId != null)
                    {
                        _sliceStartFrame = move.clipFirstFrame;
                        _sliceEndFrame = move.clipLastFrame > 0 ? move.clipLastFrame : 30;
                        _sliceSubClipName = string.IsNullOrEmpty(move.clipName) ? $"{_config.characterDisplayName}_{move.moveId}" : move.clipName;
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // Clip settings check
            if (move.clip != null)
            {
                if (!move.clip.isHumanMotion)
                {
                    EditorGUILayout.HelpBox($"Warning: Clip '{move.clip.name}' is not marked as Humanoid motion!", MessageType.Warning);
                }

                var clipSettings = AnimationUtility.GetAnimationClipSettings(move.clip);
                if (clipSettings.loopTime)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("⚠️ Loop Time is checked on this attack clip! Attack clips should not loop.", MessageType.Warning);
                    if (GUILayout.Button("Disable Loop Time", GUILayout.Width(130), GUILayout.Height(26)))
                    {
                        Undo.RecordObject(move.clip, "Disable Loop Time");
                        clipSettings.loopTime = false;
                        AnimationUtility.SetAnimationClipSettings(move.clip, clipSettings);
                        EditorUtility.SetDirty(move.clip);
                        Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Disabled Loop Time on {move.clip.name}</color>");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            // Inline FBX Sub-Clip Slicer Panel
            if (_activeSlicerMoveId == move.moveId && isFbx)
            {
                DrawFBXSlicerPanel(move, clipPath);
            }

            // Inline Frame Data & Stats: Startup, Active, Recovery, Damage, Hitstop, Reach
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("S", GUILayout.Width(14));
            int newStartup = EditorGUILayout.IntField(move.startupFrames, GUILayout.Width(34));

            EditorGUILayout.LabelField("A", GUILayout.Width(14));
            int newActive = EditorGUILayout.IntField(move.activeFrames, GUILayout.Width(34));

            EditorGUILayout.LabelField("R", GUILayout.Width(14));
            int newRecovery = EditorGUILayout.IntField(move.recoveryFrames, GUILayout.Width(34));

            int total = newStartup + newActive + newRecovery;
            EditorGUILayout.LabelField($"Tot: {total}f", EditorStyles.miniLabel, GUILayout.Width(50));

            EditorGUILayout.LabelField("Dmg", GUILayout.Width(30));
            float newDamage = EditorGUILayout.FloatField(move.damage, GUILayout.Width(45));

            EditorGUILayout.LabelField("Stop", GUILayout.Width(28));
            int newHitstop = EditorGUILayout.IntField(move.hitstopAttacker, GUILayout.Width(32));

            EditorGUILayout.LabelField("Reach", GUILayout.Width(38));
            float newReach = EditorGUILayout.FloatField(move.hitboxOffset.x, GUILayout.Width(40));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(move, "Edit Attack Frame Data");
                move.startupFrames = Mathf.Max(1, newStartup);
                move.activeFrames = Mathf.Max(1, newActive);
                move.recoveryFrames = Mathf.Max(1, newRecovery);
                move.damage = Mathf.Max(0f, newDamage);
                move.hitstopAttacker = Mathf.Max(0, newHitstop);
                move.hitstopDefender = Mathf.Max(0, newHitstop);
                move.hitboxOffset = new Vector3(newReach, move.hitboxOffset.y, move.hitboxOffset.z);

                string charFolder = !string.IsNullOrEmpty(_config.characterDisplayName) ? _config.characterDisplayName : _config.characterId;
                RebuildBoxTrackForMove(move, charFolder, _config.characterId);
                EditorUtility.SetDirty(move);
            }

            EditorGUILayout.EndHorizontal();

            // 1-Click Auto-Sync Hitbox to Peak Frame Button
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.75f);
            if (GUILayout.Button("🎯 Auto-Sync Hitbox to Peak Frame (Delta = 0)", GUILayout.Height(22)))
            {
                AutoSyncHitboxToPeakFrame(move);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Active Frame Scrubbing Controls
            if (isPreviewing)
            {
                DrawScrubbingControls(move);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFBXSlicerPanel(BBAttackData move, string fbxPath)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"✂️ FBX Sub-Clip Slicer: {Path.GetFileName(fbxPath)}", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sub-Clip Name", GUILayout.Width(100));
            _sliceSubClipName = EditorGUILayout.TextField(_sliceSubClipName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Start Frame", GUILayout.Width(80));
            _sliceStartFrame = EditorGUILayout.IntField(_sliceStartFrame, GUILayout.Width(60));
            EditorGUILayout.LabelField("End Frame", GUILayout.Width(80));
            _sliceEndFrame = EditorGUILayout.IntField(_sliceEndFrame, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("Slice & Save FBX Clip (Bake Feet + No Root Motion)", GUILayout.Height(24)))
            {
                SliceFBXSubClip(fbxPath, _sliceSubClipName, _sliceStartFrame, _sliceEndFrame, move);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void SliceFBXSubClip(string fbxPath, string subClipName, int startFrame, int endFrame, BBAttackData targetMove)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            var clips = new List<ModelImporterClipAnimation>(importer.clipAnimations);
            if (clips.Count == 0)
            {
                clips.AddRange(importer.defaultClipAnimations);
            }

            ModelImporterClipAnimation target = null;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i].name == subClipName)
                {
                    target = clips[i];
                    break;
                }
            }

            if (target == null)
            {
                target = new ModelImporterClipAnimation();
                clips.Add(target);
            }

            target.name = subClipName;
            target.firstFrame = startFrame;
            target.lastFrame = endFrame;
            target.loopTime = false;
            target.lockRootRotation = true;
            target.lockRootHeightY = true;
            target.lockRootPositionXZ = true;
            target.keepOriginalOrientation = true;
            target.keepOriginalPositionY = false;
            target.keepOriginalPositionXZ = true;
            target.heightFromFeet = true;

            importer.clipAnimations = clips.ToArray();
            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            // Locate and assign sliced clip
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in subAssets)
            {
                if (a is AnimationClip c && c.name == subClipName)
                {
                    Undo.RecordObject(targetMove, "Assign Sliced Sub-Clip");
                    targetMove.clip = c;
                    targetMove.animationClip = c;
                    targetMove.clipName = c.name;
                    targetMove.clipFirstFrame = startFrame;
                    targetMove.clipLastFrame = endFrame;
                    EditorUtility.SetDirty(targetMove);
                    break;
                }
            }

            Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Successfully sliced and configured sub-clip '{subClipName}' [{startFrame}..{endFrame}] inside {fbxPath}!</color>");
        }

        private void AutoSyncHitboxToPeakFrame(BBAttackData move)
        {
            if (move == null || move.clip == null || _config == null || _config.animator == null)
            {
                Debug.LogWarning("[BBFighterPrefabInspector] Cannot auto-sync hitbox: Missing move, clip, or animator.");
                return;
            }

            Transform rootTransform = _config.animator.transform;
            AnimationClip clip = move.clip;

            List<Transform> bones = new List<Transform>();
            if (_config.animator.isHuman)
            {
                AddBoneIfValid(bones, _config.animator.GetBoneTransform(HumanBodyBones.RightHand));
                AddBoneIfValid(bones, _config.animator.GetBoneTransform(HumanBodyBones.LeftHand));
                AddBoneIfValid(bones, _config.animator.GetBoneTransform(HumanBodyBones.RightFoot));
                AddBoneIfValid(bones, _config.animator.GetBoneTransform(HumanBodyBones.LeftFoot));
                AddBoneIfValid(bones, _config.animator.GetBoneTransform(HumanBodyBones.RightToes));
                AddBoneIfValid(bones, _config.animator.GetBoneTransform(HumanBodyBones.LeftToes));
            }
            if (_config.weaponRig != null)
            {
                AddBoneIfValid(bones, _config.weaponRig.PrimaryBoneTransform);
                AddBoneIfValid(bones, _config.weaponRig.SecondaryBoneTransform);
            }

            if (bones.Count == 0)
            {
                Debug.LogWarning("[BBFighterPrefabInspector] No valid limb or weapon bones found to sample.");
                return;
            }

            int sampleTicks = Mathf.Max(10, Mathf.RoundToInt(clip.length * 60f));
            int bestTick = move.startupFrames;
            float maxReach = float.MinValue;
            Vector3 bestLimbLocalPos = Vector3.zero;

            for (int t = 0; t <= sampleTicks; t++)
            {
                float time = (float)t / 60f;
                clip.SampleAnimation(_config.animator.gameObject, time);

                for (int b = 0; b < bones.Count; b++)
                {
                    Vector3 localPos = rootTransform.InverseTransformPoint(bones[b].position);
                    float forwardExt = Mathf.Max(localPos.z, localPos.x);
                    if (forwardExt > maxReach)
                    {
                        maxReach = forwardExt;
                        bestTick = t;
                        bestLimbLocalPos = localPos;
                    }
                }
            }

            // Restore pose to best tick
            clip.SampleAnimation(_config.animator.gameObject, (float)bestTick / 60f);

            Undo.RecordObject(move, "Auto-Sync Hitbox to Peak Frame");

            move.startupFrames = Mathf.Max(1, bestTick);
            move.measuredImpactFrame = bestTick;

            float reachX = Mathf.Abs(bestLimbLocalPos.x) > Mathf.Abs(bestLimbLocalPos.z) ? bestLimbLocalPos.x : bestLimbLocalPos.z;
            float reachY = Mathf.Max(0.2f, bestLimbLocalPos.y);
            move.hitboxOffset = new Vector3(Mathf.Abs(reachX), reachY, 0f);

            if (move.hitboxSize.magnitude < 0.1f)
            {
                move.hitboxSize = new Vector3(0.7f, 0.6f, 0.6f);
            }

            // Dustloop Recovery Math: recovery = blockstun - (active - 1) - onBlock
            move.recoveryFrames = Mathf.Max(1, move.blockstunTicks - (move.activeFrames - 1) - move.advantageOnBlock);

            string charFolder = !string.IsNullOrEmpty(_config.characterDisplayName) ? _config.characterDisplayName : _config.characterId;
            RebuildBoxTrackForMove(move, charFolder, _config.characterId);

            EditorUtility.SetDirty(move);
            if (move.boxes != null) EditorUtility.SetDirty(move.boxes);

            Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Auto-synced hitbox for [{move.moveId}] to peak frame {bestTick} (Reach: {reachX:F2}m, Height: {reachY:F2}m, Delta: 0f)!</color>");
            SceneView.RepaintAll();
        }

        private static void AddBoneIfValid(List<Transform> list, Transform t)
        {
            if (t != null && !list.Contains(t)) list.Add(t);
        }

        private void DrawAddMovePanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("➕ Add New BlazBlue Move / Animation Slot", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Slot ID Preset", GUILayout.Width(100));
            _selectedPresetIndex = EditorGUILayout.Popup(_selectedPresetIndex, PRESET_MOVE_IDS);
            string chosenMoveId = PRESET_MOVE_IDS[_selectedPresetIndex];
            if (chosenMoveId == "Custom")
            {
                _customMoveIdInput = EditorGUILayout.TextField(_customMoveIdInput);
                chosenMoveId = _customMoveIdInput;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Move Name", GUILayout.Width(100));
            _newMoveNameInput = EditorGUILayout.TextField(_newMoveNameInput);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Animation Clip", GUILayout.Width(100));
            _newMoveClipInput = (AnimationClip)EditorGUILayout.ObjectField(_newMoveClipInput, typeof(AnimationClip), false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.45f);
            if (GUILayout.Button("Create Move Asset & Link to MoveSet", GUILayout.Height(26)))
            {
                CreateAndLinkNewMove(chosenMoveId, _newMoveNameInput, _newMoveClipInput);
                _showAddMovePanel = false;
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(26)))
            {
                _showAddMovePanel = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateAndLinkNewMove(string moveId, string attackName, AnimationClip clip)
        {
            if (_config == null) return;

            string charFolder = !string.IsNullOrEmpty(_config.characterDisplayName) ? _config.characterDisplayName : _config.characterId;
            string combatDir = $"Assets/CombatData/BlazBlue/{charFolder}";
            if (!Directory.Exists(combatDir)) Directory.CreateDirectory(combatDir);

            string assetPath = $"{combatDir}/BB_{_config.characterId}_{moveId}.asset";
            var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(assetPath);
            if (attack == null)
            {
                attack = ScriptableObject.CreateInstance<BBAttackData>();
                AssetDatabase.CreateAsset(attack, assetPath);
            }

            Undo.RecordObject(attack, "Create New Move");

            DeriveMoveButtonAndKind(moveId, out var btn, out var kind, out var motion, out var airOk, out var groundOk,
                out int defS, out int defA, out int defR, out int defDmg, out int defLvl, out var guard);

            attack.moveId = moveId;
            attack.attackName = string.IsNullOrEmpty(attackName) ? $"{_config.characterDisplayName} {moveId}" : attackName;
            attack.button = btn;
            attack.kind = kind;
            attack.motion = motion;
            attack.airOk = airOk;
            attack.groundOk = groundOk;
            attack.guard = guard;

            attack.startupFrames = defS;
            attack.activeFrames = defA;
            attack.recoveryFrames = defR;
            attack.damage = defDmg;
            attack.attackLevel = defLvl;

            // Dustloop Recovery Math:
            // recovery = blockstun - (active - 1) - onBlock
            int onBlock = (kind == BBMoveKind.DistortionDrive) ? -14 : (kind == BBMoveKind.Special ? -6 : -2);
            attack.advantageOnBlock = onBlock;
            attack.blockstunTicks = attack.recoveryFrames + (attack.activeFrames - 1) + onBlock;
            attack.hitstunTicks = (kind == BBMoveKind.DistortionDrive || moveId == "2C") ? 36 : (attack.blockstunTicks + 4);
            attack.advantageOnHit = attack.hitstunTicks - ((attack.activeFrames - 1) + attack.recoveryFrames);

            int hitstop = defLvl >= 5 ? 14 : (defLvl >= 3 ? 11 : (defLvl == 2 ? 10 : 9));
            attack.hitstopAttacker = hitstop;
            attack.hitstopDefender = hitstop;
            attack.hitstopOnBlock = hitstop;
            attack.counterHitStopBonus = 4;

            attack.clip = clip;
            attack.animationClip = clip;
            attack.clipName = clip != null ? clip.name : "";
            attack.clipFirstFrame = 0;
            attack.clipLastFrame = clip != null ? Mathf.RoundToInt(clip.length * 60f) : (defS + defA + defR);
            attack.measuredImpactFrame = defS;

            RebuildBoxTrackForMove(attack, charFolder, _config.characterId);
            EditorUtility.SetDirty(attack);

            // Link into BBMoveSet
            if (_config.profile != null && _config.profile.moveSet is BBMoveSet moveSet)
            {
                Undo.RecordObject(moveSet, "Link Move to MoveSet");
                var list = new List<BBAttackData>(moveSet.moves);
                bool alreadyLinked = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].moveId == moveId)
                    {
                        list[i] = attack;
                        alreadyLinked = true;
                        break;
                    }
                }
                if (!alreadyLinked)
                {
                    list.Add(attack);
                }
                moveSet.moves = list.ToArray();
                moveSet.RebuildLookupTables();
                EditorUtility.SetDirty(moveSet);
                EditorUtility.SetDirty(_config.profile);
            }

            AssetDatabase.SaveAssets();
            RefreshMovesList();

            Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Successfully created and linked move [{moveId}] '{attack.attackName}' to {_config.characterId}!</color>");
        }

        private static void DeriveMoveButtonAndKind(string moveId, out BBButton button, out BBMoveKind kind, out BBMotion motion,
            out bool airOk, out bool groundOk, out int defaultStartup, out int defaultActive, out int defaultRecovery,
            out int defaultDamage, out int defaultLevel, out BBGuard guard)
        {
            airOk = moveId.StartsWith("j.", StringComparison.OrdinalIgnoreCase) || moveId.StartsWith("j", StringComparison.OrdinalIgnoreCase);
            groundOk = !airOk;
            motion = BBMotion.None;
            kind = BBMoveKind.Normal;
            guard = airOk ? BBGuard.Overhead : BBGuard.Mid;

            if (moveId.Contains("236236"))
            {
                button = BBButton.C;
                kind = BBMoveKind.DistortionDrive;
                motion = BBMotion.M236236;
                defaultStartup = 9; defaultActive = 4; defaultRecovery = 26; defaultDamage = 2200; defaultLevel = 5;
            }
            else if (moveId.Contains("236"))
            {
                button = moveId.EndsWith("A") ? BBButton.A : (moveId.EndsWith("B") ? BBButton.B : BBButton.C);
                kind = BBMoveKind.Special;
                motion = BBMotion.M236;
                defaultStartup = 12; defaultActive = 4; defaultRecovery = 18; defaultDamage = 750; defaultLevel = 4;
            }
            else if (moveId.Contains("214"))
            {
                button = moveId.EndsWith("A") ? BBButton.A : (moveId.EndsWith("B") ? BBButton.B : BBButton.C);
                kind = BBMoveKind.Special;
                motion = BBMotion.M214;
                defaultStartup = 14; defaultActive = 4; defaultRecovery = 20; defaultDamage = 800; defaultLevel = 4;
            }
            else if (moveId.EndsWith("D"))
            {
                button = BBButton.D;
                kind = BBMoveKind.DriveMove;
                defaultStartup = 13; defaultActive = 4; defaultRecovery = 17; defaultDamage = 750; defaultLevel = 3;
            }
            else if (moveId.EndsWith("C"))
            {
                button = BBButton.C;
                kind = (moveId.StartsWith("2") || moveId.StartsWith("6")) ? BBMoveKind.CommandNormal : BBMoveKind.Normal;
                if (moveId.StartsWith("2")) guard = BBGuard.Low;
                defaultStartup = 11; defaultActive = 4; defaultRecovery = 18; defaultDamage = 820; defaultLevel = 3;
            }
            else if (moveId.EndsWith("B"))
            {
                button = BBButton.B;
                kind = (moveId.StartsWith("2") || moveId.StartsWith("6")) ? BBMoveKind.CommandNormal : BBMoveKind.Normal;
                if (moveId.StartsWith("6")) guard = BBGuard.Overhead;
                defaultStartup = 7; defaultActive = 4; defaultRecovery = 12; defaultDamage = 520; defaultLevel = 2;
            }
            else
            {
                button = BBButton.A;
                kind = BBMoveKind.Normal;
                defaultStartup = 5; defaultActive = 3; defaultRecovery = 8; defaultDamage = 280; defaultLevel = 1;
            }
        }

        private static void RebuildBoxTrackForMove(BBAttackData move, string charFolder, string charId)
        {
            if (move == null) return;

            string boxDir = $"Assets/CombatData/BlazBlue/{charFolder}/BoxTracks";
            if (!Directory.Exists(boxDir)) Directory.CreateDirectory(boxDir);

            string boxPath = $"{boxDir}/BoxTrack_{charId}_{move.moveId}.asset";
            var track = move.boxes;
            if (track == null)
            {
                track = AssetDatabase.LoadAssetAtPath<BBBoxTrack>(boxPath);
                if (track == null)
                {
                    track = ScriptableObject.CreateInstance<BBBoxTrack>();
                    AssetDatabase.CreateAsset(track, boxPath);
                }
                move.boxes = track;
            }

            int totalTicks = Mathf.Max(1, move.TotalTicks);
            track.tickCount = totalTicks;

            float cx = move.hitboxOffset.x > 0.05f ? move.hitboxOffset.x : 0.85f;
            float cy = move.hitboxOffset.y > 0.05f ? move.hitboxOffset.y : 1.05f;
            float hx = move.hitboxSize.x > 0.05f ? move.hitboxSize.x * 0.5f : 0.45f;
            float hy = move.hitboxSize.y > 0.05f ? move.hitboxSize.y * 0.5f : 0.35f;

            var activeBox = new BBBox(cx, cy, hx, hy, BBBoxKind.Hit, 0, move.attribute);

            track.frameStart = new int[totalTicks + 1];
            var boxList = new List<BBBox>();
            int currentOffset = 0;

            int startActive = move.startupFrames;
            int endActive = move.startupFrames + move.activeFrames;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentOffset;
                if (t >= startActive && t < endActive)
                {
                    boxList.Add(activeBox);
                    currentOffset++;
                }
            }
            track.frameStart[totalTicks] = currentOffset;
            track.boxes = boxList.ToArray();
            track.keyTicks = new int[] { startActive };
            track.bakeHash = $"Bake_{charId}_{move.moveId}_{move.startupFrames}_{move.activeFrames}_{move.recoveryFrames}";

            EditorUtility.SetDirty(track);
        }

        private void MoveMoveOrder(int fromIndex, int toIndex)
        {
            if (_config.profile == null || _config.profile.moveSet is not BBMoveSet moveSet) return;
            if (fromIndex < 0 || fromIndex >= moveSet.moves.Length || toIndex < 0 || toIndex >= moveSet.moves.Length) return;

            Undo.RecordObject(moveSet, "Reorder Moves");
            var temp = moveSet.moves[fromIndex];
            moveSet.moves[fromIndex] = moveSet.moves[toIndex];
            moveSet.moves[toIndex] = temp;
            moveSet.RebuildLookupTables();
            EditorUtility.SetDirty(moveSet);
            RefreshMovesList();
        }

        private void RemoveMoveFromSet(int index, bool deleteAsset)
        {
            if (_config.profile == null || _config.profile.moveSet is not BBMoveSet moveSet) return;
            if (index < 0 || index >= moveSet.moves.Length) return;

            var moveToRemove = moveSet.moves[index];
            Undo.RecordObject(moveSet, "Remove Move");

            var list = new List<BBAttackData>(moveSet.moves);
            list.RemoveAt(index);
            moveSet.moves = list.ToArray();
            moveSet.RebuildLookupTables();
            EditorUtility.SetDirty(moveSet);

            if (deleteAsset && moveToRemove != null)
            {
                string path = AssetDatabase.GetAssetPath(moveToRemove);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            RefreshMovesList();
        }

        private void DrawScrubbingControls(BBAttackData move)
        {
            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            int totalTicks = Mathf.Max(1, move.TotalTicks);
            bool isActiveTick = (_scrubTick >= move.startupFrames && _scrubTick < move.startupFrames + move.activeFrames);

            EditorGUILayout.BeginHorizontal();
            GUI.color = isActiveTick ? Color.red : Color.yellow;
            string phaseName = _scrubTick < move.startupFrames ? "STARTUP" : (isActiveTick ? "HITBOX ACTIVE (LETHAL)" : "RECOVERY");
            EditorGUILayout.LabelField($"Frame Scrub: {_scrubTick} / {totalTicks}  [{phaseName}]", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _scrubTick = EditorGUILayout.IntSlider(_scrubTick, 0, totalTicks);
            if (EditorGUI.EndChangeCheck())
            {
                SampleMoveClip(move, _scrubTick);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Prev Frame", GUILayout.Height(20)))
            {
                _scrubTick = Mathf.Max(0, _scrubTick - 1);
                SampleMoveClip(move, _scrubTick);
            }
            if (GUILayout.Button("First Active (S)", GUILayout.Height(20)))
            {
                _scrubTick = move.startupFrames;
                SampleMoveClip(move, _scrubTick);
            }
            if (GUILayout.Button("Next Frame ▶", GUILayout.Height(20)))
            {
                _scrubTick = Mathf.Min(totalTicks, _scrubTick + 1);
                SampleMoveClip(move, _scrubTick);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox($"Live red wireframe hitbox is projected onto the Scene View / Prefab Mode at Frame {_scrubTick}.", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void StartAttackPreview(string moveId)
        {
            _activePreviewMoveId = moveId;
            var move = GetMoveById(moveId);
            if (move != null)
            {
                _scrubTick = move.startupFrames;
                SampleMoveClip(move, _scrubTick);
            }
            SceneView.RepaintAll();
        }

        private void StopAttackPreview()
        {
            _activePreviewMoveId = null;
            if (_config != null && _config.animator != null && _config.overrideController != null)
            {
                var idleClip = GetLocomotionClip("Idle");
                if (idleClip != null)
                {
                    idleClip.SampleAnimation(_config.animator.gameObject, 0f);
                }
            }
            SceneView.RepaintAll();
        }

        private void SampleMoveClip(BBAttackData move, int tick)
        {
            if (_config == null || _config.animator == null || move == null || move.clip == null) return;

            float sampleTime = (float)tick / 60f;
            sampleTime = Mathf.Clamp(sampleTime, 0f, move.clip.length);
            move.clip.SampleAnimation(_config.animator.gameObject, sampleTime);
            SceneView.RepaintAll();
        }

        private BBAttackData GetMoveById(string moveId)
        {
            for (int i = 0; i < _cachedMoves.Count; i++)
            {
                if (_cachedMoves[i] != null && _cachedMoves[i].moveId == moveId) return _cachedMoves[i];
            }
            return null;
        }

        #endregion

        #region Section 2: Layer 0 Locomotion & Reaction Clips

        private void RefreshLocomotionSlots()
        {
            _locomotionSlots.Clear();

            var aoc = _config.overrideController;
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            if (aoc != null)
            {
                aoc.GetOverrides(overrides);
            }

            var slotKeywords = new (string slot, string[] keys)[]
            {
                ("Idle",      new[] { "combatidle01", "idle01", "idle" }),
                ("Walk",      new[] { "run01_forward", "walk", "locomotion" }),
                ("Run",       new[] { "straferun01_right", "run", "dash" }),
                ("Jump",      new[] { "castingenter01", "jump" }),
                ("Fall",      new[] { "fall", "jump01_loop" }),
                ("Land",      new[] { "land", "jump01_land" }),
                ("Guard",     new[] { "combatidle1h01", "guard", "block" }),
                ("Hit",       new[] { "combatdamage01", "castingdamage01", "damage", "hit" }),
                ("Knockdown", new[] { "death01", "knockdown" }),
                ("GetUp",     new[] { "castingexit01", "wakeup", "getup" })
            };

            foreach (var mapping in slotKeywords)
            {
                LocomotionSlot slot = new LocomotionSlot
                {
                    displayName = mapping.slot,
                    matchOriginalKeywords = mapping.keys,
                    currentOverrideClip = null,
                    originalClipKey = null
                };

                for (int i = 0; i < overrides.Count; i++)
                {
                    var orig = overrides[i].Key;
                    if (orig == null) continue;
                    string origName = orig.name.ToLowerInvariant();

                    bool matched = false;
                    foreach (var key in mapping.keys)
                    {
                        if (origName.Contains(key))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                    {
                        slot.originalClipKey = orig;
                        slot.currentOverrideClip = overrides[i].Value;
                        break;
                    }
                }

                _locomotionSlots.Add(slot);
            }
        }

        private void DrawSection2_Locomotion()
        {
            _foldoutLocomotion = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutLocomotion, "Section 2: Layer 0 Locomotion & Reaction Clips (Inline)");
            if (_foldoutLocomotion)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                var aoc = _config.overrideController;
                if (aoc == null)
                {
                    EditorGUILayout.HelpBox("AnimatorOverrideController not found on prefab or profile. Please link or auto-resolve.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField($"Override Controller: {aoc.name}", EditorStyles.miniBoldLabel);

                    for (int i = 0; i < _locomotionSlots.Count; i++)
                    {
                        var slot = _locomotionSlots[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(slot.displayName, GUILayout.Width(100));

                        EditorGUI.BeginChangeCheck();
                        var newClip = (AnimationClip)EditorGUILayout.ObjectField(slot.currentOverrideClip, typeof(AnimationClip), false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            ApplyLocomotionClip(slot, newClip);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.Space(4);

                // Enforce Grounded Import Settings Button
                GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
                if (GUILayout.Button("Enforce Grounded Import Settings (Root Motion = false)", GUILayout.Height(26)))
                {
                    EnforceGroundedSettings();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ApplyLocomotionClip(LocomotionSlot slot, AnimationClip newClip)
        {
            var aoc = _config.overrideController;
            if (aoc == null || slot.originalClipKey == null) return;

            Undo.RecordObject(aoc, $"Update Locomotion {slot.displayName}");
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; i++)
            {
                if (overrides[i].Key == slot.originalClipKey)
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(slot.originalClipKey, newClip);
                    break;
                }
            }

            aoc.ApplyOverrides(overrides);
            EditorUtility.SetDirty(aoc);
            RefreshLocomotionSlots();
        }

        private AnimationClip GetLocomotionClip(string slotName)
        {
            for (int i = 0; i < _locomotionSlots.Count; i++)
            {
                if (_locomotionSlots[i].displayName == slotName)
                    return _locomotionSlots[i].currentOverrideClip;
            }
            return null;
        }

        private void EnforceGroundedSettings()
        {
            if (_config == null) return;

            int corrected = 0;
            if (_config.animator != null && _config.animator.applyRootMotion)
            {
                Undo.RecordObject(_config.animator, "Enforce Grounded Settings");
                _config.animator.applyRootMotion = false;
                EditorUtility.SetDirty(_config.animator);
                corrected++;
            }

            var animators = _config.GetComponentsInChildren<Animator>(true);
            foreach (var a in animators)
            {
                if (a.applyRootMotion)
                {
                    Undo.RecordObject(a, "Enforce Grounded Settings");
                    a.applyRootMotion = false;
                    EditorUtility.SetDirty(a);
                    corrected++;
                }
            }

            Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Successfully enforced Grounded Import Settings on {_config.gameObject.name}! (Apply Root Motion = false on {corrected} animators)</color>");
        }

        #endregion

        #region Section 3: Weapon Rig, Scale & Socket Preview

        private void DrawSection3_WeaponRig()
        {
            _foldoutWeapons = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutWeapons, "Section 3: Weapon Rig, Scale & Socket Preview (BBWeaponRig Inline)");
            if (_foldoutWeapons)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                var rig = _config.weaponRig;
                if (rig == null)
                {
                    EditorGUILayout.HelpBox("BBWeaponRig not found on this fighter prefab. Would you like to add one?", MessageType.Info);
                    if (GUILayout.Button("Add BBWeaponRig Component", GUILayout.Height(24)))
                    {
                        Undo.AddComponent<BBWeaponRig>(_config.gameObject);
                        _config.weaponRig = _config.GetComponent<BBWeaponRig>();
                        EditorUtility.SetDirty(_config.gameObject);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    return;
                }

                EditorGUI.BeginChangeCheck();

                rig.warriorType = (BBWarriorType)EditorGUILayout.EnumPopup("Warrior Archetype", rig.warriorType);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Primary Weapon (Right Hand)", EditorStyles.boldLabel);
                rig.primaryWeaponPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", rig.primaryWeaponPrefab, typeof(GameObject), false);
                rig.primarySocket = (HumanBodyBones)EditorGUILayout.EnumPopup("Socket Bone", rig.primarySocket);
                rig.primaryTargetLength = EditorGUILayout.FloatField("Target Length (m)", rig.primaryTargetLength);
                rig.primaryPositionOffset = EditorGUILayout.Vector3Field("Pos Offset", rig.primaryPositionOffset);
                rig.primaryRotationOffset = EditorGUILayout.Vector3Field("Rot Offset", rig.primaryRotationOffset);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Secondary Weapon (Left Hand / Shield)", EditorStyles.boldLabel);
                rig.secondaryWeaponPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", rig.secondaryWeaponPrefab, typeof(GameObject), false);
                rig.secondarySocket = (HumanBodyBones)EditorGUILayout.EnumPopup("Socket Bone", rig.secondarySocket);
                rig.secondaryTargetLength = EditorGUILayout.FloatField("Target Length (m)", rig.secondaryTargetLength);
                rig.secondaryPositionOffset = EditorGUILayout.Vector3Field("Pos Offset", rig.secondaryPositionOffset);
                rig.secondaryRotationOffset = EditorGUILayout.Vector3Field("Rot Offset", rig.secondaryRotationOffset);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(rig, "Edit Weapon Rig");
                    EditorUtility.SetDirty(rig);

                    if (_weaponLivePreview)
                    {
                        UpdateLiveWeaponPreview(rig);
                    }
                }

                EditorGUILayout.Space(6);

                // Live Preview Button
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = _weaponLivePreview ? new Color(1f, 0.4f, 0.4f) : new Color(0.2f, 0.8f, 1f);
                if (GUILayout.Button(_weaponLivePreview ? "⏹ Stop Weapon Preview" : "🗡 Live Preview Weapon in Prefab Mode", GUILayout.Height(26)))
                {
                    _weaponLivePreview = !_weaponLivePreview;
                    if (_weaponLivePreview)
                    {
                        rig.LocateBoneTransforms();
                        rig.AttachWeapons();
                        MarkPreviewInstancesNoSave(rig);
                    }
                    else
                    {
                        BBWeaponRig.DestroyOrphanedWeaponChildren(rig.PrimaryBoneTransform);
                        BBWeaponRig.DestroyOrphanedWeaponChildren(rig.SecondaryBoneTransform);
                    }
                    SceneView.RepaintAll();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void UpdateLiveWeaponPreview(BBWeaponRig rig)
        {
            if (rig == null) return;
            rig.LocateBoneTransforms();
            rig.AttachWeapons();
            MarkPreviewInstancesNoSave(rig);
            SceneView.RepaintAll();
        }

        private void MarkPreviewInstancesNoSave(BBWeaponRig rig)
        {
            if (rig.PrimaryInstance != null)
            {
                rig.PrimaryInstance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                if (rig.PrimaryInstance.transform.parent != null)
                    rig.PrimaryInstance.transform.parent.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }
            if (rig.SecondaryInstance != null)
            {
                rig.SecondaryInstance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                if (rig.SecondaryInstance.transform.parent != null)
                    rig.SecondaryInstance.transform.parent.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            }
        }

        #endregion

        #region Section 4: Identity, Portrait & Material Quick-Links

        private void DrawSection4_Identity()
        {
            _foldoutIdentity = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutIdentity, "Section 4: Identity, Portrait & Material Quick-Links");
            if (_foldoutIdentity)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                EditorGUILayout.BeginHorizontal();

                // Left Column: Details
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField($"Fighter ID: {_config.characterId}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Display Name: {_config.characterDisplayName}");
                EditorGUILayout.LabelField($"Archetype: {(_config.weaponRig != null ? _config.weaponRig.warriorType.ToString() : "None")}");
                EditorGUILayout.LabelField($"Drive Name: {(_config.profile != null ? _config.profile.driveName : "None")}");

                float hipHeight = 0.95f;
                if (_config.animator != null && _config.animator.isHuman)
                {
                    var hips = _config.animator.GetBoneTransform(HumanBodyBones.Hips);
                    if (hips != null) hipHeight = hips.position.y - _config.transform.position.y;
                }
                else if (_config.characterDefinition != null)
                {
                    hipHeight = _config.characterDefinition.heightMeters * 0.52f;
                }
                EditorGUILayout.LabelField($"Hip Height: {hipHeight:F2}m");
                EditorGUILayout.EndVertical();

                // Right Column: Portrait Thumbnail
                if (_config.characterDefinition != null && _config.characterDefinition.portraitSprite != null)
                {
                    var tex = _config.characterDefinition.portraitSprite.texture;
                    if (tex != null)
                    {
                        Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
                        GUI.DrawTexture(previewRect, tex, ScaleMode.ScaleToFit);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Quick Links:", EditorStyles.miniBoldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping BBProfile", GUILayout.Height(22)))
                {
                    if (_config.profile != null)
                    {
                        EditorGUIUtility.PingObject(_config.profile);
                        Selection.activeObject = _config.profile;
                    }
                }

                if (GUILayout.Button("Ping CharacterDefinition", GUILayout.Height(22)))
                {
                    if (_config.characterDefinition != null)
                    {
                        EditorGUIUtility.PingObject(_config.characterDefinition);
                        Selection.activeObject = _config.characterDefinition;
                    }
                }

                if (GUILayout.Button("Ping Raimon Materials", GUILayout.Height(22)))
                {
                    PingRaimonMaterials();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void PingRaimonMaterials()
        {
            string id = _config.characterId;
            string matPath = $"Assets/Materials/RaimonUnified/{id}";
            var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(matPath);
            if (folder != null)
            {
                EditorGUIUtility.PingObject(folder);
                Selection.activeObject = folder;
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets($"{id} t:Material", new[] { "Assets/Materials/RaimonUnified" });
            if (matGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                EditorGUIUtility.PingObject(mat);
                Selection.activeObject = mat;
            }
            else
            {
                var r = _config.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null)
                {
                    EditorGUIUtility.PingObject(r.sharedMaterial);
                    Selection.activeObject = r.sharedMaterial;
                }
            }
        }

        #endregion

        #region Feature 3: Look-Dev, Outfits, Portrait & Live Arena Test Strip

        private void DrawSection5_LookDevAndTestStrip()
        {
            _foldoutLookDev = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutLookDev, "Section 5: Look-Dev, Outfits, Portrait & Live Arena Test");
            if (_foldoutLookDev)
            {
                EditorGUILayout.BeginVertical(_boxCardStyle);

                // Sub-section 1: Material & Outfit Slots
                EditorGUILayout.LabelField("Model Outfits & Materials (Drag/Swap RaimonUnified or Custom Materials)", EditorStyles.boldLabel);

                var renderers = _config.GetComponentsInChildren<Renderer>(true);
                int matSlotsCount = 0;
                foreach (var r in renderers)
                {
                    if (!IsBodyRenderer(r)) continue;

                    var sharedMats = r.sharedMaterials;
                    for (int m = 0; m < sharedMats.Length; m++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{r.name} [{m}]", GUILayout.Width(130));

                        EditorGUI.BeginChangeCheck();
                        var newMat = (Material)EditorGUILayout.ObjectField(sharedMats[m], typeof(Material), false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(r, "Change Fighter Material");
                            sharedMats[m] = newMat;
                            r.sharedMaterials = sharedMats;
                            EditorUtility.SetDirty(r);
                        }
                        EditorGUILayout.EndHorizontal();
                        matSlotsCount++;
                    }
                }

                if (matSlotsCount == 0)
                {
                    EditorGUILayout.HelpBox("No body mesh renderers found under child Model rig.", MessageType.Info);
                }

                EditorGUILayout.Space(8);

                // Sub-section 2: Quick Action Buttons (Portrait & Arena Test)
                EditorGUILayout.BeginHorizontal();

                // 1. Update Fighter Portrait Button
                GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
                if (GUILayout.Button("📸 Update Fighter Portrait", GUILayout.Height(30)))
                {
                    CaptureAndAssignPortrait();
                }

                // 2. Test in BattleArena Button
                GUI.backgroundColor = new Color(0.95f, 0.55f, 0.15f);
                if (GUILayout.Button("⚔️ Test This Fighter in BattleArena", GUILayout.Height(30)))
                {
                    TestFighterInBattleArena();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void CaptureAndAssignPortrait()
        {
            if (_config == null || _config.characterDefinition == null)
            {
                Debug.LogWarning("[BBFighterPrefabInspector] Cannot generate portrait: Missing CharacterDefinition.");
                return;
            }

            var sprite = DemonIdolPortraitGenerator.GenerateAndAssignPortraitForFighter(_config.characterDefinition, _config.gameObject);
            if (sprite != null)
            {
                Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Successfully rendered and assigned fresh bust portrait for {_config.characterId} ({_config.characterDisplayName})!</color>");
            }
        }

        private void TestFighterInBattleArena()
        {
            SaveAllLinkedAssets();

            string p1Id = !string.IsNullOrEmpty(_config.characterId) ? _config.characterId : "C01";
            string p2Id = (p1Id == "C01") ? "C02" : "C01";

            PlayerPrefs.SetString("MDI_P1_CharacterId", p1Id);
            PlayerPrefs.SetString("MDI_P2_CharacterId", p2Id);
            PlayerPrefs.SetString("MDI_StageId", "ST07");
            PlayerPrefs.SetString("MDI_GameMode", "Training");
            PlayerPrefs.Save();

            string arenaScenePath = "Assets/Scenes/BattleArena.unity";
            if (!File.Exists(arenaScenePath))
            {
                arenaScenePath = "Assets/Scenes/ArcadeArena.unity";
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(arenaScenePath);
                EditorApplication.isPlaying = true;
            }
        }

        #endregion

        #region Save All Linked Assets

        private void DrawSaveAllButton()
        {
            GUI.backgroundColor = new Color(0.15f, 0.85f, 0.45f);
            if (GUILayout.Button("💾 Save All Linked Assets (Profile, MoveSet, AOC & Prefab)", GUILayout.Height(34)))
            {
                SaveAllLinkedAssets();
            }
            GUI.backgroundColor = Color.white;
        }

        public void SaveAllLinkedAssets()
        {
            if (_config == null) return;

            EditorUtility.SetDirty(_config);
            EditorUtility.SetDirty(_config.gameObject);

            if (_config.profile != null)
            {
                EditorUtility.SetDirty(_config.profile);
                if (_config.profile.moveSet != null) EditorUtility.SetDirty(_config.profile.moveSet);
            }

            if (_config.characterDefinition != null)
            {
                EditorUtility.SetDirty(_config.characterDefinition);
            }

            if (_config.overrideController != null)
            {
                EditorUtility.SetDirty(_config.overrideController);
            }

            if (_config.weaponRig != null)
            {
                EditorUtility.SetDirty(_config.weaponRig);
            }

            for (int i = 0; i < _cachedMoves.Count; i++)
            {
                if (_cachedMoves[i] != null) EditorUtility.SetDirty(_cachedMoves[i]);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[BBFighterPrefabInspector] Saved all linked fighter assets for {_config.characterId} — {_config.characterDisplayName}!</color>");
        }

        #endregion

        #region Scene View Gizmo & Overlay

        private void OnSceneGUI()
        {
            if (_config == null || string.IsNullOrEmpty(_activePreviewMoveId)) return;

            var move = GetMoveById(_activePreviewMoveId);
            if (move == null) return;

            Transform targetTransform = _config.animator != null ? _config.animator.transform : _config.transform;

            Vector3 worldCenter = targetTransform.TransformPoint(move.hitboxOffset);
            Vector3 worldSize = Vector3.Scale(move.hitboxSize, targetTransform.lossyScale);

            bool isActiveTick = (_scrubTick >= move.startupFrames && _scrubTick < move.startupFrames + move.activeFrames);

            if (isActiveTick)
            {
                // Live red wireframe hitbox
                Handles.color = Color.red;
                Handles.DrawWireCube(worldCenter, worldSize);

                // Subtle transparent red volume
                Handles.color = new Color(1f, 0f, 0f, 0.25f);
                Handles.CubeHandleCap(0, worldCenter, targetTransform.rotation, Mathf.Max(worldSize.x, worldSize.y), EventType.Repaint);

                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.red }
                };
                Handles.Label(worldCenter + Vector3.up * (worldSize.y * 0.5f + 0.15f), $"HITBOX ACTIVE [{move.moveId}] Frame {_scrubTick}/{move.TotalTicks}", labelStyle);
            }
            else
            {
                // Ghost outline when inactive
                Handles.color = new Color(1f, 0.7f, 0.7f, 0.4f);
                Handles.DrawWireCube(worldCenter, worldSize);

                GUIStyle ghostStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0.8f, 0.7f) }
                };
                Handles.Label(worldCenter + Vector3.up * (worldSize.y * 0.5f + 0.1f), $"[{move.moveId}] Hitbox Inactive ({_scrubTick}/{move.TotalTicks}f)", ghostStyle);
            }
        }

        #endregion
    }
}
#endif
