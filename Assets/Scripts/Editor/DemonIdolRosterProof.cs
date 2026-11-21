#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    public static class DemonIdolRosterProof
    {
        public struct FighterClips
        {
            public string id;
            public string name;
            public string archetype;
            public string driveClip;
            public string specialClip;
            public string superClip;
            public string collisionNote;
        }

        [MenuItem("Demon Idol/BlazBlue/Print Roster Proof Table")]
        public static List<FighterClips> GenerateProofTable()
        {
            var results = new List<FighterClips>();
            var tripletMap = new Dictionary<string, string>(); // (drive|special|super) -> first fighter id

            for (int i = 0; i < BBMatchFlow.Roster.Length; i++)
            {
                string id = BBMatchFlow.Roster[i];
                var def = CharacterRegistry.GetCharacterById(id);
                string charName = def != null ? def.displayName : id;
                var info = BBRosterArchetype.Get(id);
                string archetype = info.warrior.ToString();

                var profile = FindProfile(id);
                string driveName = profile != null && profile.signatureDriveClip != null ? profile.signatureDriveClip.name : "None";
                string specialName = profile != null && profile.signatureSpecialClip != null ? profile.signatureSpecialClip.name : "None";
                string superName = profile != null && profile.signatureSuperClip != null ? profile.signatureSuperClip.name : "None";

                string key = $"{driveName}|{specialName}|{superName}";
                string flag = "";
                if (tripletMap.TryGetValue(key, out string existingId))
                {
                    flag = $"DUPLICATE of {existingId}";
                }
                else
                {
                    tripletMap[key] = id;
                }

                var fc = new FighterClips
                {
                    id = id,
                    name = charName,
                    archetype = archetype,
                    driveClip = driveName,
                    specialClip = specialName,
                    superClip = superName,
                    collisionNote = flag
                };
                results.Add(fc);

                Debug.Log($"[RosterProof] {id, -5} | {charName, -14} | {archetype, -10} | Drive: {driveName, -20} | Special: {specialName, -20} | Super: {superName, -20} | {flag}");
            }

            return results;
        }

        static BBCharacterProfile FindProfile(string id)
        {
            string[] guids = AssetDatabase.FindAssets("BBProfile_" + id + " t:BBCharacterProfile");
            if (guids == null || guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("BBProfile_ t:BBCharacterProfile");
            }
            for (int i = 0; i < guids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (p.Contains(id))
                {
                    var prof = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(p);
                    if (prof != null) return prof;
                }
            }
            return null;
        }
    }
}
#endif
