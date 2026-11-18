using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Professional Box Editor Window for BlazBlue 4-Button combat.
    /// Provides 60 Hz tick timeline scrubbing, 24 fps frame mapping,
    /// Scene view box handles on Z=0, bone-follow bake helpers,
    /// opponent onion-skinning, frame advantage calculation, and non-destructive saving.
    /// </summary>
    public class BBBoxEditorWindow : EditorWindow
    {
        [MenuItem("Demon Idol/BlazBlue/Box Editor")]
        public static void OpenWindow()
        {
            var win = GetWindow<BBBoxEditorWindow>("BB Box Editor");
            win.minSize = new Vector2(480, 600);
            win.Show();
        }

        [Header("Target Move")]
        public BBAttackData targetAttack;
        public GameObject previewAvatar;

        [Header("Timeline Scrubber")]
        [Range(0, 120)]
        public int currentTick = 0;
        public HumanBodyBones followBone = HumanBodyBones.RightHand;
        public Vector3 boneOffset = new Vector3(0.12f, 0f, 0f);
        public Vector2 boxSize = new Vector2(0.28f, 0.22f);
        public BBBoxKind editKind = BBBoxKind.Hit;
        public BBAttribute editAttribute = BBAttribute.Body;

        [Header("Onion Skinning")]
        public bool showOpponentOnionSkin = true;
        public float opponentDistance = 1.2f;
        public bool opponentCrouching = false;

        private Vector2 _scrollPos;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("BlazBlue Box & Frame Authoring", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 1. Move Selection
            EditorGUI.BeginChangeCheck();
            targetAttack = (BBAttackData)EditorGUILayout.ObjectField("Target Attack Data", targetAttack, typeof(BBAttackData), false);
            previewAvatar = (GameObject)EditorGUILayout.ObjectField("Preview Avatar", previewAvatar, typeof(GameObject), true);

            if (targetAttack == null)
            {
                EditorGUILayout.HelpBox("Select a BBAttackData asset to edit boxes and frame timing.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            int totalTicks = targetAttack.TotalTicks;
            if (totalTicks <= 0) totalTicks = 1;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Timeline & Playback", EditorStyles.boldLabel);

            // 2. Timeline Scrubber (60 Hz tick + 24 fps mocap frame)
            currentTick = EditorGUILayout.IntSlider("Combat Tick (60 Hz)", currentTick, 0, totalTicks - 1);
            float cmuFrame = currentTick / 2.5f;
            EditorGUILayout.LabelField($"Source CMU Frame (24 fps): {cmuFrame:F1} / {totalTicks / 2.5f:F1}");

            // Phase indicator
            string phaseName = "Startup";
            Color phaseCol = Color.green;
            if (currentTick >= targetAttack.startupFrames + targetAttack.activeFrames)
            {
                phaseName = "Recovery";
                phaseCol = Color.cyan;
            }
            else if (currentTick >= targetAttack.startupFrames)
            {
                phaseName = "Active";
                phaseCol = Color.red;
            }

            var origCol = GUI.color;
            GUI.color = phaseCol;
            EditorGUILayout.LabelField($"Phase: {phaseName} (Tick {currentTick})", EditorStyles.boldLabel);
            GUI.color = origCol;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Frame Advantage & Metrics", EditorStyles.boldLabel);

            int calcHitAdv = targetAttack.hitstunTicks - ((targetAttack.activeFrames - 1) + targetAttack.recoveryFrames);
            int calcBlockAdv = targetAttack.blockstunTicks - ((targetAttack.activeFrames - 1) + targetAttack.recoveryFrames);

            EditorGUILayout.LabelField($"Advantage on Hit: {(calcHitAdv >= 0 ? "+" : "")}{calcHitAdv} ticks");
            EditorGUILayout.LabelField($"Advantage on Block: {(calcBlockAdv >= 0 ? "+" : "")}{calcBlockAdv} ticks");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bone-Follow Bake Helper", EditorStyles.boldLabel);
            followBone = (HumanBodyBones)EditorGUILayout.EnumPopup("Follow Bone", followBone);
            boneOffset = EditorGUILayout.Vector3Field("Offset (Meters)", boneOffset);
            boxSize = EditorGUILayout.Vector2Field("Box Half-Extents", boxSize);
            editKind = (BBBoxKind)EditorGUILayout.EnumPopup("Box Kind", editKind);
            editAttribute = (BBAttribute)EditorGUILayout.EnumPopup("Attribute", editAttribute);

            if (GUILayout.Button("Bake Box To Active Ticks"))
            {
                BakeBoneBoxToTrack();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Onion-Skin Opponent", EditorStyles.boldLabel);
            showOpponentOnionSkin = EditorGUILayout.Toggle("Show Opponent", showOpponentOnionSkin);
            opponentDistance = EditorGUILayout.Slider("Distance (m)", opponentDistance, 0.5f, 3.0f);
            opponentCrouching = EditorGUILayout.Toggle("Opponent Crouching", opponentCrouching);

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Attack Data & Box Track", GUILayout.Height(30)))
            {
                ValidateCurrentMove();
            }

            if (GUILayout.Button("Save Attack Data", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(targetAttack);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=#00e676>[BBBoxEditor] Successfully saved {targetAttack.name}.</color>");
            }

            EditorGUILayout.EndScrollView();
        }

        private void BakeBoneBoxToTrack()
        {
            if (targetAttack == null) return;

            int totalTicks = targetAttack.TotalTicks;
            if (totalTicks <= 0) totalTicks = 1;

            if (targetAttack.boxes == null)
            {
                targetAttack.boxes = ScriptableObject.CreateInstance<BBBoxTrack>();
                targetAttack.boxes.name = $"{targetAttack.name}_Boxes";
                AssetDatabase.AddObjectToAsset(targetAttack.boxes, targetAttack);
            }

            var track = targetAttack.boxes;
            track.tickCount = totalTicks;
            track.frameStart = new int[totalTicks + 1];

            var boxList = new List<BBBox>();
            int currentBoxOffset = 0;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentBoxOffset;

                // Only add hitbox during active phase
                if (t >= targetAttack.startupFrames && t < targetAttack.startupFrames + targetAttack.activeFrames)
                {
                    // Calculate bone position if preview avatar exists
                    Vector3 localPos = boneOffset;
                    if (previewAvatar != null)
                    {
                        var anim = previewAvatar.GetComponent<Animator>();
                        if (anim != null && anim.isHuman)
                        {
                            Transform boneT = anim.GetBoneTransform(followBone);
                            if (boneT != null)
                            {
                                Vector3 rel = previewAvatar.transform.InverseTransformPoint(boneT.position);
                                localPos += rel;
                            }
                        }
                    }

                    var newBox = new BBBox(
                        localPos.x,
                        localPos.y,
                        boxSize.x,
                        boxSize.y,
                        editKind,
                        0,
                        editAttribute
                    );

                    boxList.Add(newBox);
                    currentBoxOffset++;
                }
            }

            track.frameStart[totalTicks] = currentBoxOffset;
            track.boxes = boxList.ToArray();
            track.keyTicks = new int[] { targetAttack.startupFrames };

            EditorUtility.SetDirty(track);
            EditorUtility.SetDirty(targetAttack);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00e676>[BBBoxEditor] Baked {boxList.Count} boxes for active ticks {targetAttack.startupFrames}..{targetAttack.startupFrames + targetAttack.activeFrames - 1}.</color>");
        }

        private void ValidateCurrentMove()
        {
            if (targetAttack == null) return;

            int total = targetAttack.TotalTicks;
            if (targetAttack.boxes != null && targetAttack.boxes.tickCount != total)
            {
                Debug.LogWarning($"[BBBoxEditor] Track tickCount ({targetAttack.boxes.tickCount}) does not match TotalTicks ({total})!");
            }
            else
            {
                Debug.Log($"<color=#00e676>[BBBoxEditor] {targetAttack.name} validated successfully. Total Ticks: {total}</color>");
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (targetAttack == null) return;

            Handles.matrix = Matrix4x4.identity;

            // Draw fighter origin
            Handles.color = Color.white;
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, 0.05f);

            // Draw boxes for current tick
            if (targetAttack.boxes != null)
            {
                var tempBoxes = new BBWorldBox[16];
                int count = targetAttack.boxes.GetBoxes(currentTick, tempBoxes, 0f, 0f, 1);

                for (int i = 0; i < count; i++)
                {
                    ref BBWorldBox b = ref tempBoxes[i];
                    Handles.color = b.kind == BBBoxKind.Hit ? Color.red : Color.green;

                    Vector3 p1 = new Vector3(b.minX, b.minY, 0f);
                    Vector3 p2 = new Vector3(b.maxX, b.minY, 0f);
                    Vector3 p3 = new Vector3(b.maxX, b.maxY, 0f);
                    Vector3 p4 = new Vector3(b.minX, b.maxY, 0f);

                    Handles.DrawLine(p1, p2);
                    Handles.DrawLine(p2, p3);
                    Handles.DrawLine(p3, p4);
                    Handles.DrawLine(p4, p1);
                }
            }

            // Draw opponent onion-skin
            if (showOpponentOnionSkin)
            {
                Handles.color = new Color(1f, 1f, 0f, 0.4f);
                float oppX = opponentDistance;
                float height = opponentCrouching ? 1.1f : 1.75f;
                Vector3 c = new Vector3(oppX, height * 0.5f, 0f);
                Vector3 s = new Vector3(0.5f, height, 0.05f);
                Handles.DrawWireCube(c, s);
            }
        }
    }
}
