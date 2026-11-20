using UnityEngine;

namespace DemonIdol.BlazBlue
{
    public static class BBPresentationLibrary
    {
        static BBCharacterPresentation[] _cache;

        public static BBCharacterPresentation Find(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            if (_cache == null)
                _cache = Resources.LoadAll<BBCharacterPresentation>("BBPresentation");
            for (int i = 0; i < _cache.Length; i++)
            {
                if (_cache[i] != null && _cache[i].characterId == characterId)
                    return _cache[i];
            }
            return null;
        }

        public static void ClearCache()
        {
            _cache = null;
        }
    }
}
