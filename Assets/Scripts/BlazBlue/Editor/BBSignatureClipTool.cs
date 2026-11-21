#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBSignatureClipTool
    {
        [MenuItem("Demon Idol/BlazBlue/Apply Signature Clips")]
        public static void Apply()
        {
            var clips = LoadClips();
            int assigned = 0;
            for (int i = 0; i < BBMatchFlow.Roster.Length; i++)
            {
                string id = BBMatchFlow.Roster[i];
                if (!BBSignatureTable.TryBase(id, out var signature)) continue;
                var profile = FindProfile(id);
                var card = FindCard(id);
                var drive = BBSignatureTable.ResolveClip(signature.clipToken);
                if (drive == null) drive = FindClip(clips, signature.clipToken);
                var special = drive;
                var superClip = drive;

                if (id == "EX06")
                {
                    var cast = BBSignatureTable.ResolveClip("Guard Cast");
                    var atk = BBSignatureTable.ResolveClip("Guard Attack");
                    drive = cast != null ? cast : drive;
                    special = atk != null ? atk : drive;
                    superClip = cast != null ? cast : drive;
                }

                if (profile != null)
                {
                    profile.signatureDriveClip = drive;
                    profile.signatureSpecialClip = special;
                    profile.signatureSuperClip = superClip;
                    EditorUtility.SetDirty(profile);
                }
                if (card != null)
                {
                    card.driveClip = drive;
                    card.specialClip = special;
                    card.superClip = superClip;
                    EditorUtility.SetDirty(card);
                }
                if (profile != null && profile.blazBlueOverrideController != null && drive != null)
                {
                    AssignOverride(profile.blazBlueOverrideController, drive);
                    EditorUtility.SetDirty(profile.blazBlueOverrideController);
                }
                if (drive != null) assigned++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[BBSignature] Assigned signature clips for " + assigned + " fighters. Each write stayed on that fighter's profile and override.");
        }

        static void AssignOverride(AnimatorOverrideController over, AnimationClip clip)
        {
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            over.GetOverrides(pairs);
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].Key == null) continue;
                if (!BBSignatureClipFallback.ClipNameMatches(pairs[i].Key.name, "Attack")) continue;
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, clip);
                over.ApplyOverrides(pairs);
                return;
            }
        }

        static List<AnimationClip> LoadClips()
        {
            var list = new List<AnimationClip>();
            string[] roots = { "Assets/Kevin Iglesias", "Assets/ExplosiveLLC", "Assets/Huge Mocap Library" };
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", roots);
            for (int i = 0; i < guids.Length; i++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (clip != null) list.Add(clip);
            }
            return list;
        }

        static AnimationClip FindClip(List<AnimationClip> clips, string token)
        {
            for (int i = 0; i < clips.Count; i++)
            {
                if (BBSignatureClipFallback.ClipNameMatches(clips[i].name, token))
                    return clips[i];
            }
            return null;
        }

        static BBCharacterProfile FindProfile(string id)
        {
            string[] guids = AssetDatabase.FindAssets("BBProfile_" + id + " t:BBCharacterProfile");
            for (int i = 0; i < guids.Length; i++)
            {
                var profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (profile != null && profile.characterId != null && profile.characterId.StartsWith(id))
                    return profile;
            }
            return null;
        }

        static BBCharacterPresentation FindCard(string id)
        {
            string[] guids = AssetDatabase.FindAssets("BBPresentation_" + id + " t:BBCharacterPresentation");
            for (int i = 0; i < guids.Length; i++)
            {
                var card = AssetDatabase.LoadAssetAtPath<BBCharacterPresentation>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (card != null && card.characterId == id) return card;
            }
            return null;
        }
    }
}
#endif
