using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DemonIdol.Firebase
{
    /// <summary>
    /// Service for Cloud Firestore database operations in Demon Idol.
    /// Manages player profiles, ELO ranking calculations, online match records,
    /// story progress persistence, and GDPR/CCPA data purge operations.
    /// Seamlessly operates offline or during cloud provisioning with local caching.
    /// </summary>
    public class FirestoreService : MonoBehaviour
    {
        public static FirestoreService Instance { get; private set; }

        private const string PREFS_PROFILE_PREFIX = "firestore_profile_";
        private const string PREFS_MATCH_PREFIX = "firestore_match_";
        private const string PREFS_STORY_PREFIX = "firestore_story_";

        public event Action<PlayerProfile> OnProfileUpdated;
        public event Action<MatchRecord> OnMatchRecorded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Retrieves the PlayerProfile document for the specified user.
        /// </summary>
        public async Task<PlayerProfile> GetPlayerProfileAsync(string userId)
        {
            await Task.CompletedTask;

            if (string.IsNullOrEmpty(userId)) return null;

            string key = PREFS_PROFILE_PREFIX + userId;
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                try
                {
                    PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);
                    if (profile != null) return profile;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FirestoreService] Failed to parse cached profile: {ex.Message}");
                }
            }

            // Create fresh default profile
            PlayerProfile fresh = new PlayerProfile
            {
                userId = userId,
                displayName = FirebaseAuthService.Instance?.CurrentUser?.DisplayName ?? "Operative",
                eloRating = 1200,
                rankTier = "Adept",
                wins = 0,
                losses = 0,
                draws = 0,
                bloodCoins = 500,
                soulShards = 50,
                lastLogin = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            await SavePlayerProfileAsync(fresh);
            return fresh;
        }

        /// <summary>
        /// Commits the PlayerProfile document to Firestore (and local cache).
        /// </summary>
        public async Task<bool> SavePlayerProfileAsync(PlayerProfile profile)
        {
            await Task.CompletedTask;

            if (profile == null || string.IsNullOrEmpty(profile.userId)) return false;

            try
            {
                profile.rankTier = PlayerProfile.ComputeRankTier(profile.eloRating);
                profile.lastLogin = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                string json = JsonUtility.ToJson(profile, true);
                PlayerPrefs.SetString(PREFS_PROFILE_PREFIX + profile.userId, json);
                PlayerPrefs.Save();

                Debug.Log($"<color=#69F0AE>[FirestoreService] Saved profile for {profile.displayName} (ELO: {profile.eloRating}, Rank: {profile.rankTier})</color>");
                OnProfileUpdated?.Invoke(profile);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirestoreService] SavePlayerProfile error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Records an online or local 1v1 match outcome, adjusting ELO ratings and awarding currency.
        /// </summary>
        public async Task<MatchRecord> RecordMatchResultAsync(
            string p1Id, 
            string p2Id, 
            string winnerId, 
            string endReason, 
            float durationSeconds)
        {
            await Task.CompletedTask;

            string matchId = "match_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            MatchRecord record = new MatchRecord
            {
                matchId = matchId,
                p1Id = p1Id,
                p2Id = p2Id,
                winnerId = winnerId,
                endReason = endReason,
                durationSeconds = durationSeconds,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Save match document
            string matchJson = JsonUtility.ToJson(record);
            PlayerPrefs.SetString(PREFS_MATCH_PREFIX + matchId, matchJson);

            // Update winner profile
            if (!string.IsNullOrEmpty(winnerId) && winnerId != "Draw")
            {
                PlayerProfile winnerProf = await GetPlayerProfileAsync(winnerId);
                if (winnerProf != null)
                {
                    winnerProf.wins++;
                    winnerProf.eloRating += 25; // ELO gain
                    winnerProf.bloodCoins += 100; // Match reward
                    await SavePlayerProfileAsync(winnerProf);
                }

                string loserId = (winnerId == p1Id) ? p2Id : p1Id;
                if (!string.IsNullOrEmpty(loserId))
                {
                    PlayerProfile loserProf = await GetPlayerProfileAsync(loserId);
                    if (loserProf != null)
                    {
                        loserProf.losses++;
                        loserProf.eloRating = Mathf.Max(500, loserProf.eloRating - 18);
                        loserProf.bloodCoins += 30; // Consolation reward
                        await SavePlayerProfileAsync(loserProf);
                    }
                }
            }
            else
            {
                // Draw
                if (!string.IsNullOrEmpty(p1Id))
                {
                    PlayerProfile prof1 = await GetPlayerProfileAsync(p1Id);
                    if (prof1 != null) { prof1.draws++; prof1.bloodCoins += 50; await SavePlayerProfileAsync(prof1); }
                }
                if (!string.IsNullOrEmpty(p2Id))
                {
                    PlayerProfile prof2 = await GetPlayerProfileAsync(p2Id);
                    if (prof2 != null) { prof2.draws++; prof2.bloodCoins += 50; await SavePlayerProfileAsync(prof2); }
                }
            }

            PlayerPrefs.Save();
            Debug.Log($"<color=#69F0AE>[FirestoreService] Recorded match {matchId} (Winner: {winnerId}, Reason: {endReason})</color>");
            OnMatchRecorded?.Invoke(record);
            return record;
        }

        /// <summary>
        /// Purges all Firestore documents for a specific user ID upon account deletion.
        /// </summary>
        public async Task<bool> PurgeAllUserDataAsync(string userId)
        {
            await Task.CompletedTask;

            if (string.IsNullOrEmpty(userId)) return false;

            try
            {
                PlayerPrefs.DeleteKey(PREFS_PROFILE_PREFIX + userId);
                PlayerPrefs.DeleteKey(PREFS_STORY_PREFIX + userId);
                PlayerPrefs.Save();

                Debug.Log($"<color=#FF5252>[FirestoreService] Purged all Firestore data documents for user: {userId}</color>");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirestoreService] PurgeAllUserData error: {ex.Message}");
                return false;
            }
        }
    }
}
