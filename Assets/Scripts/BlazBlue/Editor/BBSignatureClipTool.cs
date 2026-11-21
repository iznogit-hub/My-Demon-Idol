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
            List<AnimationClip> clips = null;
            int assigned = 0;
            for (int i = 0; i < BBMatchFlow.Roster.Length; i++)
            {
                string id = BBMatchFlow.Roster[i];
                if (!BBSignatureTable.TryBase(id, out var signature)) continue;
                var profile = FindProfile(id);
                var card = FindCard(id);
                var (driveToken, specialToken, superToken) = GetFighterTokens(id, signature.clipToken);
                var drive = BBSignatureTable.ResolveClip(driveToken);
                if (drive == null)
                {
                    if (clips == null) clips = LoadClips();
                    drive = FindClip(clips, driveToken);
                }

                var special = BBSignatureTable.ResolveClip(specialToken);
                if (special == null)
                {
                    if (clips == null) clips = LoadClips();
                    special = FindClip(clips, specialToken);
                }

                var superClip = BBSignatureTable.ResolveClip(superToken);
                if (superClip == null)
                {
                    if (clips == null) clips = LoadClips();
                    superClip = FindClip(clips, superToken);
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

        static (string drive, string special, string superClip) GetFighterTokens(string id, string fallback)
        {
            switch (id)
            {
                case "C01": return ("KI 1H Attack", "Swordsman Attack1", "CMU 79 disc");
                case "C02": return ("KI spellcast", "Karate Attack1", "KI Magic Omni");
                case "C04": return ("CMU 79 disc", "Archer Attack1", "KI spellcast bubble");
                case "C05": return ("Spearman Attack1", "KI Polearm", "CMU 86 cleave");
                case "C06": return ("KI Magic Omni", "Mage Attack1", "KI Magic Call");
                case "C07": return ("CMU 80 stars", "Sorceress Attack1", "KI spellcast bubble");
                case "C08": return ("Ninja Attack1", "KI 1H Left", "CMU 81 slam");
                case "C09": return ("Hammer Attack1", "Heavy Punch", "CMU 83 slam");
                case "C10": return ("Knight shield", "Knight Attack1", "CMU 86 cleave");
                case "C11": return ("Crossbow Attack1", "KI Magic Direct 1H L", "CMU 79 disc");
                case "C12": return ("KI 2H", "2Handed Attack1", "CMU 86 cleave");
                case "C13": return ("KI spellcast bubble", "Karate Attack1", "CMU 80 stars");
                case "C14": return ("CMU 86 cleave", "KI 2H", "CMU 83 slam");
                case "SK02": return ("Knight Attack1", "KI 1H Attack", "KI Magic Call");
                case "SK03": return ("Brute Attack1", "Heavy Punch", "CMU 81 slam");
                case "SK05": return ("Mage Attack1", "KI spellcast", "KI Magic Omni");
                case "SK06": return ("2Handed Attack1", "KI 2H", "CMU 86 cleave");
                case "SK08": return ("CMU 81 slam", "Hammer Attack1", "CMU 83 slam");
                case "EX01": return ("Heavy Punch", "Brute Attack1", "CMU 81 slam");
                case "EX02": return ("CMU 83 slam", "Hammer Attack1", "CMU 81 slam");
                case "EX03": return ("Swordsman Attack1", "KI 1H Attack", "CMU 79 disc");
                case "EX04": return ("Karate Attack1", "Heavy Punch", "CMU 80 stars");
                case "EX05": return ("Archer Attack1", "KI Magic Call", "CMU 80 stars");
                case "EX06": return ("Guard Cast", "Guard Attack", "Guard Cast");
                case "EX07": return ("Sorceress Attack1", "KI spellcast bubble", "CMU 80 stars");
                default: return (fallback, fallback, fallback);
            }
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
            string[] roots = { "Assets/Kevin Iglesias", "Assets/ExplosiveLLC", "Assets/Martial Art Animations Sample" };
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
