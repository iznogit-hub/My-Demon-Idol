using System;
using System.Collections.Generic;

namespace DemonIdol.Launch
{
    /// <summary>
    /// Cosmetic reward grant, once per match id. Training records the id and grants nothing.
    /// Nothing in this ledger changes life, damage, or move data.
    /// </summary>
    public sealed class MatchRewardLedger
    {
        readonly HashSet<string> _grantedMatchIds = new HashSet<string>(StringComparer.Ordinal);

        public int CosmeticXp { get; private set; }
        public int GrantCount => _grantedMatchIds.Count;

        public bool WasGranted(string matchId)
        {
            return !string.IsNullOrEmpty(matchId) && _grantedMatchIds.Contains(matchId);
        }

        public bool TryGrant(string matchId, PlayModeKind mode, out int grantedXp)
        {
            grantedXp = 0;
            if (!IsMatchId(matchId))
                return false;
            if (!_grantedMatchIds.Add(matchId))
                return false;

            if (mode == PlayModeKind.Training || mode == PlayModeKind.OnlineUnavailable)
                return true;

            grantedXp = 25;
            CosmeticXp += grantedXp;
            return true;
        }

        public bool UndoGrant(string matchId, int grantedXp)
        {
            if (!IsMatchId(matchId) || !_grantedMatchIds.Remove(matchId))
                return false;
            if (grantedXp > 0)
                CosmeticXp -= grantedXp;
            if (CosmeticXp < 0)
                CosmeticXp = 0;
            return true;
        }

        public string ToDocument()
        {
            var ids = new List<string>(_grantedMatchIds);
            ids.Sort(StringComparer.Ordinal);
            string quoted = ids.Count == 0 ? string.Empty : "\"" + string.Join("\",\"", ids) + "\"";
            return "{\"version\":1,\"cosmeticXp\":" + CosmeticXp + ",\"matchIds\":[" + quoted + "]}";
        }

        public bool TryLoadDocument(string document)
        {
            if (!TryParseDocument(document, out int xp, out List<string> ids))
                return false;

            _grantedMatchIds.Clear();
            for (int i = 0; i < ids.Count; i++)
                _grantedMatchIds.Add(ids[i]);
            CosmeticXp = xp;
            return true;
        }

        public static bool TryParseDocument(string document, out int cosmeticXp, out List<string> matchIds)
        {
            cosmeticXp = 0;
            matchIds = null;
            if (!JsonSyntax.TryObject(document, out Dictionary<string, JsonToken> fields, out _))
                return false;
            if (fields.Count != 3 || !fields.ContainsKey("version") || !fields.ContainsKey("cosmeticXp") || !fields.ContainsKey("matchIds"))
                return false;
            if (!int.TryParse(fields["version"].Raw, out int version) || version != 1)
                return false;
            if (!int.TryParse(fields["cosmeticXp"].Raw, out int xp) || xp < 0 || fields["cosmeticXp"].Raw.IndexOf('.') >= 0)
                return false;

            string rawIds = fields["matchIds"].Raw;
            if (rawIds.Length < 2 || rawIds[0] != '[' || rawIds[rawIds.Length - 1] != ']')
                return false;
            var ids = new List<string>();
            string body = rawIds.Substring(1, rawIds.Length - 2).Trim();
            if (body.Length == 0)
            {
                cosmeticXp = xp;
                matchIds = ids;
                return true;
            }

            int index = 0;
            while (index < body.Length)
            {
                while (index < body.Length && char.IsWhiteSpace(body[index]))
                    index++;
                if (index >= body.Length || body[index] != '"')
                    return false;
                int end = index + 1;
                while (end < body.Length && body[end] != '"')
                {
                    if (body[end] == '\\')
                        return false;
                    end++;
                }
                if (end >= body.Length)
                    return false;
                string id = body.Substring(index + 1, end - index - 1);
                if (!IsMatchId(id) || ids.Contains(id))
                    return false;
                ids.Add(id);
                index = end + 1;
                while (index < body.Length && char.IsWhiteSpace(body[index]))
                    index++;
                if (index >= body.Length)
                    break;
                if (body[index] != ',')
                    return false;
                index++;
                while (index < body.Length && char.IsWhiteSpace(body[index]))
                    index++;
                if (index >= body.Length)
                    return false;
            }

            cosmeticXp = xp;
            matchIds = ids;
            return true;
        }

        public static bool IsMatchId(string matchId)
        {
            if (string.IsNullOrEmpty(matchId))
                return false;
            for (int i = 0; i < matchId.Length; i++)
            {
                char c = matchId[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (!ok)
                    return false;
            }
            return true;
        }
    }
}
