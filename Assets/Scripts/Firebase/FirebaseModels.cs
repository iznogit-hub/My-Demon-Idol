using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Firebase
{
    [Serializable]
    public class FirebaseUser
    {
        public string UserId = string.Empty;
        public string DisplayName = "Operative";
        public string Email = string.Empty;
        public bool IsAnonymous = true;
        public string IdToken = string.Empty;

        public FirebaseUser() { }

        public FirebaseUser(string userId, string displayName, string email, bool isAnonymous, string idToken)
        {
            UserId = userId;
            DisplayName = displayName;
            Email = email;
            IsAnonymous = isAnonymous;
            IdToken = idToken;
        }
    }

    [Serializable]
    public class PlayerProfile
    {
        public string userId = string.Empty;
        public string displayName = "Operative";
        public int eloRating = 1200;
        public string rankTier = "Adept"; // Initiate, Adept, Master, Grandmaster, Demon Lord
        public int wins = 0;
        public int losses = 0;
        public int draws = 0;
        public int bloodCoins = 500;
        public int soulShards = 50;
        public List<string> unlockedFighters = new List<string> { "Kai", "Mira", "BigBoy" };
        public string lastLogin = string.Empty;

        public float WinRate => (wins + losses) > 0 ? (float)wins / (wins + losses) * 100f : 0f;

        public static string ComputeRankTier(int elo)
        {
            if (elo >= 2200) return "Demon Lord";
            if (elo >= 1800) return "Grandmaster";
            if (elo >= 1500) return "Master";
            if (elo >= 1200) return "Adept";
            return "Initiate";
        }
    }

    [Serializable]
    public class MatchRecord
    {
        public string matchId = string.Empty;
        public string p1Id = string.Empty;
        public string p2Id = string.Empty;
        public string winnerId = string.Empty;
        public string endReason = string.Empty;
        public float durationSeconds = 0f;
        public string timestamp = string.Empty;
    }

    [Serializable]
    public class StoryProgressData
    {
        public string userId = string.Empty;
        public int currentCheckpoint = 0;
        public int starsCollected = 0;
        public List<string> completedChapters = new List<string>();
        public string lastSaved = string.Empty;
    }

    [Serializable]
    public class FirebaseNotificationPayload
    {
        public string title = string.Empty;
        public string body = string.Empty;
        public string channelId = "demon_idol_general";
        public string dataJson = string.Empty;
        public string timestamp = string.Empty;
    }
}
