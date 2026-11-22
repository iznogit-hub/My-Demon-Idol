#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBImpactFrameMeasurer
    {
        public const string AssetPath = "Assets/CombatData/BlazBlue/ClipImpact.asset";

        // Invoked via DemonIdolMenu
        public static void MeasureAllAttackClips()
        {
            Debug.Log("<color=#00e676>[BBImpactFrameMeasurer] Starting Impact Frame Measurement for all attack clips...</color>");

            // 1. Gather all unique animation clips used in BBAttackData assets
            var clipMap = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            var attackGuids = AssetDatabase.FindAssets("t:BBAttackData", new[] { "Assets/CombatData/BlazBlue" });
            foreach (var guid in attackGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
                if (attack != null && attack.clip != null && !clipMap.ContainsKey(attack.clip.name))
                {
                    clipMap[attack.clip.name] = attack.clip;
                }
            }

            // Also search approved animation packs for any additional attack clips
            var animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Kevin Iglesias", "Assets/ExplosiveLLC" });
            foreach (var guid in animGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Exclude Huge Mocap or Stylized3DMonster
                if (path.IndexOf("Huge Mocap", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Stylized3DMonster", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null && !clipMap.ContainsKey(clip.name))
                {
                    string n = clip.name.ToLowerInvariant();
                    if (n.Contains("attack") || n.Contains("cast") || n.Contains("magic") || n.Contains("punch") || n.Contains("kick"))
                    {
                        clipMap[clip.name] = clip;
                    }
                }
            }

            Debug.Log($"[BBImpactFrameMeasurer] Found {clipMap.Count} candidate attack clips to measure.");

            // 2. Instantiate temporary humanoid rig for bone tracking
            GameObject dummyGo = null;
            Animator dummyAnimator = null;
            Transform rightHand = null, leftHand = null, rightFoot = null, leftFoot = null;

            // Try loading standard humanoid prefab
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/C01_Akio/Model_C01.prefab");
            if (modelPrefab != null)
            {
                dummyGo = UnityEngine.Object.Instantiate(modelPrefab);
                dummyGo.hideFlags = HideFlags.HideAndDontSave;
                dummyAnimator = dummyGo.GetComponentInChildren<Animator>();
            }

            if (dummyAnimator != null && dummyAnimator.isHuman)
            {
                rightHand = dummyAnimator.GetBoneTransform(HumanBodyBones.RightHand);
                leftHand = dummyAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
                rightFoot = dummyAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
                leftFoot = dummyAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            }

            // 3. Load or create ClipImpact.asset
            var impactAsset = AssetDatabase.LoadAssetAtPath<BBClipImpactAsset>(AssetPath);
            if (impactAsset == null)
            {
                impactAsset = ScriptableObject.CreateInstance<BBClipImpactAsset>();
                string dir = Path.GetDirectoryName(AssetPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                AssetDatabase.CreateAsset(impactAsset, AssetPath);
            }

            impactAsset.entries.Clear();

            Transform[] strikingBones = new Transform[] { rightHand, leftHand, rightFoot, leftFoot };

            foreach (var kvp in clipMap)
            {
                AnimationClip clip = kvp.Value;
                if (clip == null) continue;

                float clipLength = clip.length;
                float sampleRate = 60f;
                int totalFrames = Mathf.Max(1, Mathf.RoundToInt(clipLength * sampleRate));

                int bestFrame = 0;
                float bestTime = 0f;
                float maxForwardVel = -1f;
                float maxForwardExt = -999f;
                int maxExtFrame = 0;

                if (dummyGo != null && dummyAnimator != null)
                {
                    Vector3[] prevPositions = new Vector3[strikingBones.Length];
                    // Sample frame 0
                    clip.SampleAnimation(dummyGo, 0f);
                    for (int b = 0; b < strikingBones.Length; b++)
                    {
                        if (strikingBones[b] != null)
                            prevPositions[b] = dummyGo.transform.InverseTransformPoint(strikingBones[b].position);
                    }

                    for (int f = 1; f <= totalFrames; f++)
                    {
                        float t = (float)f / sampleRate;
                        if (t > clipLength) t = clipLength;
                        clip.SampleAnimation(dummyGo, t);

                        for (int b = 0; b < strikingBones.Length; b++)
                        {
                            if (strikingBones[b] == null) continue;
                            Vector3 localPos = dummyGo.transform.InverseTransformPoint(strikingBones[b].position);
                            float forwardVel = (localPos.z - prevPositions[b].z) * sampleRate;
                            if (forwardVel > maxForwardVel)
                            {
                                maxForwardVel = forwardVel;
                                bestFrame = f;
                                bestTime = t;
                            }
                            if (localPos.z > maxForwardExt)
                            {
                                maxForwardExt = localPos.z;
                                maxExtFrame = f;
                            }
                            prevPositions[b] = localPos;
                        }
                    }
                }

                // If velocity didn't detect a forward strike (e.g. downward or spinning), use max forward extension or ~30% into the clip
                if (bestFrame <= 0 || maxForwardVel <= 0.05f)
                {
                    if (maxExtFrame > 0 && maxExtFrame < totalFrames)
                    {
                        bestFrame = maxExtFrame;
                        bestTime = (float)bestFrame / sampleRate;
                    }
                    else
                    {
                        bestFrame = Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.32f), 4, totalFrames - 2);
                        bestTime = (float)bestFrame / sampleRate;
                    }
                }

                impactAsset.entries.Add(new BBClipImpactAsset.ImpactEntry
                {
                    clipName = clip.name,
                    impactFrame60 = bestFrame,
                    impactTime = bestTime,
                    maxVelocity = Mathf.Max(0f, maxForwardVel)
                });
            }

            if (dummyGo != null)
            {
                UnityEngine.Object.DestroyImmediate(dummyGo);
            }

            EditorUtility.SetDirty(impactAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00e676>[BBImpactFrameMeasurer] Measured {impactAsset.entries.Count} clips and saved to {AssetPath}</color>");
        }
    }
}
#endif
