using UnityEngine;

namespace DemonIdol.BlazBlue
{
    public static class BBSignatureClipFallback
    {
        public static bool ClipNameMatches(string clipName, string token)
        {
            if (string.IsNullOrEmpty(clipName) || string.IsNullOrEmpty(token)) return false;
            string name = clipName.ToLowerInvariant();
            string key = token.ToLowerInvariant();
            if (name.Contains(key.Replace(" ", ""))) return true;
            if (key.Contains("1h") && (name.Contains("1h") || name.Contains("attack1h"))) return true;
            if (key.Contains("2h") && (name.Contains("2h") || name.Contains("attack2h"))) return true;
            if (key.Contains("spell") && (name.Contains("magic") || name.Contains("cast"))) return true;
            if (key.Contains("spearman") && name.Contains("spearman")) return true;
            if (key.Contains("hammer") && name.Contains("hammer")) return true;
            if (key.Contains("ninja") && name.Contains("ninja")) return true;
            if (key.Contains("knight") && name.Contains("knight")) return true;
            if (key.Contains("crossbow") && name.Contains("crossbow")) return true;
            if (key.Contains("brute") && name.Contains("brute")) return true;
            if (key.Contains("swordsman") && name.Contains("swordsman")) return true;
            if (key.Contains("karate") && name.Contains("karate")) return true;
            if (key.Contains("archer") && name.Contains("archer")) return true;
            if (key.Contains("mage") && name.Contains("mage")) return true;
            if (key.Contains("sorceress") && name.Contains("sorceress")) return true;
            if (key.Contains("cmu 79") && name.Contains("mdi_bb_attack_1")) return true;
            if (key.Contains("cmu 80") && name.Contains("mdi_bb_attack_2")) return true;
            if (key.Contains("cmu 83") && name.Contains("mdi_bb_attack_4")) return true;
            if (key.Contains("cmu 86") && name.Contains("mdi_bb_attack_5")) return true;
            if (key.Contains("guard") && name.Contains("guard")) return true;
            if (key.Contains("heavy") && name.Contains("heavy")) return true;
            return false;
        }

        public static AnimationClip ClipFor(BBCharacterProfile profile, string characterId, string moveId)
        {
            if (profile != null && profile.moveSet is BBMoveSet ms && ms.moves != null)
            {
                for (int i = 0; i < ms.moves.Length; i++)
                {
                    if (ms.moves[i] != null && ms.moves[i].moveId == moveId)
                    {
                        var c = ms.moves[i].clip != null ? ms.moves[i].clip : ms.moves[i].animationClip;
                        if (c != null) return c;
                    }
                }
            }

            if (!BBSignatureTable.TryBase(characterId, out var signature)) return null;
            BBCharacterPresentation card = BBPresentationLibrary.Find(characterId);
            if (moveId == "5D" || moveId == "2D" || moveId == "6D" || moveId == "j.D")
                return First(profile != null ? profile.signatureDriveClip : null, card != null ? card.driveClip : null);
            if (moveId == signature.specialId)
                return First(profile != null ? profile.signatureSpecialClip : null, card != null ? card.specialClip : null);
            if (moveId == signature.superId)
                return First(profile != null ? profile.signatureSuperClip : null, card != null ? card.superClip : null);
            return null;
        }

        static AnimationClip First(AnimationClip a, AnimationClip b)
        {
            return a != null ? a : b;
        }
    }
}
