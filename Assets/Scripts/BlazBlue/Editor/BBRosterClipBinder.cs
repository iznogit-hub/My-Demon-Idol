#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Editor
{
    /// <summary>
    /// Run from the Unity editor after this purge. Reimports archetype clips,
    /// appends CMU clip ranges without editing existing takes, fills any override
    /// slot that is still empty, and rebuilds Casual1 / Sapphi humanoid avatars.
    /// </summary>
    public static class BBRosterClipBinder
    {
        const string KiController = "Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller";

        [MenuItem("Demon Idol/BlazBlue/Bind Roster Clips And Avatars")]
        public static void Bind()
        {
            int clips = AppendApprovedPackRanges();
            int filled = FillEmptyOverrideSlots();
            int baked = BakeLocomotionClips();
            bool casual = RebuildHumanoid("Assets/AnimeGirls/Casual1/SourceFiles/Mesh/AnimeGirl.fbx");
            bool sapphi = RebuildHumanoid("Assets/SapphiArt/SapphiArtchan/OBJ/SapphiArtchan.fbx");
            AssetDatabase.SaveAssets();
            Debug.Log($"[BBRoster] Approved pack ranges added {clips}. Empty override slots filled {filled}. Locomotion baked {baked}. Casual1 humanoid {casual}. Sapphi humanoid {sapphi}.");
            if (!sapphi)
            {
                Debug.LogWarning("[BBRoster] SapphiArtchan stayed generic. Open the FBX Rig tab, set Humanoid, and map Hips if the skeleton has no standard humanoid bones.");
            }
        }

        static int AppendApprovedPackRanges()
        {
            string[] files =
            {
                "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Animations/Karate@Attack1.FBX",
                "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/Knight@Attack1.FBX",
                "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Animations/Swordsman@Attack1.FBX",
                "Assets/Martial Art Animations Sample/Animations/Atk_P_1.fbx",
                "Assets/Martial Art Animations Sample/Animations/Atk_K_1.fbx"
            };
            int added = 0;
            for (int i = 0; i < files.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(files[i]) as ModelImporter;
                if (importer == null) continue;
                var existing = importer.clipAnimations;
                if (existing == null || existing.Length == 0) existing = importer.defaultClipAnimations;
                var list = new List<ModelImporterClipAnimation>();
                if (existing != null)
                {
                    for (int c = 0; c < existing.Length; c++) list.Add(existing[c]);
                }
                string take = list.Count > 0 ? list[0].takeName : Path.GetFileNameWithoutExtension(files[i]);
                string clipName = "MDI_BB_Attack_" + (i + 1);
                bool already = false;
                for (int c = 0; c < list.Count; c++)
                {
                    if (list[c].name == clipName) already = true;
                }
                if (already) continue;
                float last = list.Count > 0 ? list[0].lastFrame : 40f;
                var extra = new ModelImporterClipAnimation
                {
                    name = clipName,
                    takeName = take,
                    firstFrame = 8f,
                    lastFrame = Mathf.Min(last, 32f),
                    loopTime = false,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = false,
                    lockRootRotation = true,
                    lockRootHeightY = true,
                    lockRootPositionXZ = true
                };
                list.Add(extra);
                importer.clipAnimations = list.ToArray();
                importer.SaveAndReimport();
                added++;
            }
            return added;
        }

        static int FillEmptyOverrideSlots()
        {
            var ki = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KiController);
            var fallback = FindClip("Assets/ExplosiveLLC", "Attack1");
            if (fallback == null || ki == null) return 0;
            int filled = 0;
            string[] guids = AssetDatabase.FindAssets("t:AnimatorOverrideController", new[] { "Assets/MDI_Characters/Runtime/BlazBlue" });
            for (int i = 0; i < guids.Length; i++)
            {
                var over = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (over == null) continue;
                if (over.runtimeAnimatorController != ki)
                {
                    over.runtimeAnimatorController = ki;
                }
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>(over.overridesCount);
                over.GetOverrides(pairs);
                bool changed = false;
                for (int p = 0; p < pairs.Count; p++)
                {
                    if (pairs[p].Value != null) continue;
                    pairs[p] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[p].Key, fallback);
                    changed = true;
                    filled++;
                }
                if (changed) over.ApplyOverrides(pairs);
            }
            return filled;
        }

        static AnimationClip FindClip(string root, string token)
        {
            string[] guids = AssetDatabase.FindAssets(token + " t:AnimationClip", new[] { root });
            for (int i = 0; i < guids.Length; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (clip != null) return clip;
            }
            return null;
        }

        static int BakeLocomotionClips()
        {
            string[] warriors = { "Karate", "Swordsman", "Ninja", "Knight", "2Handed", "Hammer", "Spearman", "Mage", "Sorceress", "Archer", "Crossbow", "Brute" };
            string[] types = { "Idle", "Walk", "Jump", "Fall", "Run" };
            int modified = 0;

            foreach (var w in warriors)
            {
                string[] guids = AssetDatabase.FindAssets(w + "@ t:Model", new[] { "Assets/ExplosiveLLC" });
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    bool match = false;
                    foreach (var t in types)
                    {
                        if (path.Contains("@" + t) || (t == "Idle" && path.Contains("@Idle1"))) { match = true; break; }
                    }
                    if (!match) continue;
                    var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (imp == null) continue;
                    var clips = imp.clipAnimations;
                    if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
                    if (clips != null && clips.Length > 0)
                    {
                        bool changed = false;
                        for (int i = 0; i < clips.Length; i++)
                        {
                            if (!clips[i].lockRootPositionXZ)
                            {
                                clips[i].lockRootPositionXZ = true;
                                changed = true;
                            }
                        }
                        if (changed)
                        {
                            imp.clipAnimations = clips;
                            imp.SaveAndReimport();
                            modified++;
                        }
                    }
                }
            }
            return modified;
        }

        static bool RebuildHumanoid(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return false;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            return avatar != null && avatar.isHuman;
        }
    }
}
#endif
