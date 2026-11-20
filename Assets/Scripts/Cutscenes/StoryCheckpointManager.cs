using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.Cutscenes
{
    [Serializable]
    public class StorySaveData
    {
        public int version = 1;
        public string lastSaveTime = string.Empty;
        public List<string> committedTransactionKeys = new List<string>();
        public List<string> unlockedGallerySlots = new List<string>();
        public List<CharacterStoryProgress> characterProgress = new List<CharacterStoryProgress>();
        // Preserve any unknown data for forward compatibility
        public List<string> preservedUnknownIds = new List<string>();
    }

    [Serializable]
    public class CharacterStoryProgress
    {
        public string characterId;
        public bool act1Clear;
        public bool act2Clear;
        public bool storyClear;
        public bool storyToggleEnabled = true;
        public int clearCount = 0;
    }

    [Serializable]
    public class StoryCheckpoint
    {
        public string checkpointId = "ACT1_CLEAR"; // ACT1_CLEAR, ACT2_CLEAR, STORY_CLEAR
        public string chapterTitle = "Chapter 1: The Blood Shrine";
        public string opponentName = "C02";
        public string stageId = "ST01";
        public VideoClip introCutscene;
        public VideoClip victoryCutscene;
        public string introCutsceneSlotId = "S02_ACT1_TRANSITION";
        public string victoryCutsceneSlotId = "S03_ACT2_TRANSITION";
        public int rewardCoins = 500;
    }

    /// <summary>
    /// Master Story Checkpoint & Progression Manager for Demon Idol.
    /// Features:
    /// - Strict transactional checkpoint persistence (ACT1_CLEAR, ACT2_CLEAR, STORY_CLEAR).
    /// - Commits from SIM RESULT before video playback.
    /// - Deduplication key: characterId + checkpointId + runId.
    /// - Atomic persistence: temp file + atomic rename.
    /// - Stable string ID indexing (C01..C06), preserving unknown IDs without crashing.
    /// - SF6-style Story On/Off toggle: hidden on first run, enabled after first clear.
    /// - Gallery / Theater unlocks verification.
    /// </summary>
    public class StoryCheckpointManager : MonoBehaviour
    {
        public const string CHECKPOINT_ACT1_CLEAR = "ACT1_CLEAR";
        public const string CHECKPOINT_ACT2_CLEAR = "ACT2_CLEAR";
        public const string CHECKPOINT_STORY_CLEAR = "STORY_CLEAR";

        private static StoryCheckpointManager _instance;
        public static StoryCheckpointManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<StoryCheckpointManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("StoryCheckpointManager");
                        _instance = go.AddComponent<StoryCheckpointManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private const string SAVE_FILENAME = "demonidol_story_save.json";
        private static string _customSavePath = null;

        public static string SaveFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customSavePath)) return _customSavePath;
                string dir = Application.persistentDataPath;
                if (string.IsNullOrEmpty(dir)) dir = Directory.GetCurrentDirectory();
                return Path.Combine(dir, SAVE_FILENAME);
            }
            set => _customSavePath = value;
        }

        private StorySaveData _saveData = new StorySaveData();

        [Header("Active Story Run State")]
        public string CurrentCharacterId { get; private set; } = "C01";
        public string CurrentRunId { get; private set; } = Guid.NewGuid().ToString("N");
        public string LastSavedTimeString => _saveData.lastSaveTime;
        public int CurrentCheckpointIndex { get; set; } = 0;

        public event Action<string, string> OnCheckpointCommitted; // characterId, checkpointId
        public event Action<StoryCheckpoint> OnCheckpointLoaded;
        public event Action<StoryCheckpoint> OnCheckpointCompleted;

        private static readonly List<StoryCheckpoint> _defaultCheckpoints = new List<StoryCheckpoint>
        {
            new StoryCheckpoint { checkpointId = CHECKPOINT_ACT1_CLEAR, chapterTitle = "ACT 1: The Empty Stage", opponentName = "C02", introCutsceneSlotId = "S02_ACT1_TRANSITION", victoryCutsceneSlotId = "S03_ACT2_TRANSITION" },
            new StoryCheckpoint { checkpointId = CHECKPOINT_ACT2_CLEAR, chapterTitle = "ACT 2: Ink and Applause", opponentName = "C04", introCutsceneSlotId = "S03_ACT2_TRANSITION", victoryCutsceneSlotId = "S04_ACT3_TRANSITION" },
            new StoryCheckpoint { checkpointId = CHECKPOINT_STORY_CLEAR, chapterTitle = "ACT 3: Curtain of Iron", opponentName = "C06", introCutsceneSlotId = "S05_FINAL_BOSS_INTRO", victoryCutsceneSlotId = "S06_FINAL_BOSS_DEFEAT" }
        };

        public StoryCheckpoint GetCurrentCheckpoint()
        {
            int idx = Mathf.Clamp(CurrentCheckpointIndex, 0, _defaultCheckpoints.Count - 1);
            return _defaultCheckpoints[idx];
        }

        public void StartCurrentCheckpoint(Action onBattleReady)
        {
            var cp = GetCurrentCheckpoint();
            OnCheckpointLoaded?.Invoke(cp);

            if (CutsceneManager.Instance != null && DemonLordNarratorManager.Instance != null)
            {
                // Sequence high-res 2K narrative still + thematic act transition video with smooth fade-in/fade-out from black
                Sprite still = CurrentCheckpointIndex switch
                {
                    0 => DemonLordNarratorManager.Instance.GetGhostCompanionSprite(),
                    1 => DemonLordNarratorManager.Instance.GetShadowDemonAltSprite(),
                    _ => DemonLordNarratorManager.Instance.GetShadowDemonSprite()
                };

                string stillCaption = CurrentCheckpointIndex switch
                {
                    0 => "PROLOGUE // THE IDOL'S COVENANT",
                    1 => "CHAPTER 2 // SOUL DRAIN OF THE SHIKIGAMI",
                    _ => "FINAL CHAPTER // THE DEMON LORD ASCENDS"
                };

                string videoSlot = cp?.introCutsceneSlotId ?? "S02_ACT1_TRANSITION";

                CutsceneManager.Instance.PlayStoryStill(still, stillCaption, 2.2f, () =>
                {
                    CutsceneManager.Instance.PlayManifestSlot(videoSlot, () =>
                    {
                        onBattleReady?.Invoke();
                    }, () =>
                    {
                        onBattleReady?.Invoke();
                    });
                });
            }
            else
            {
                onBattleReady?.Invoke();
            }
        }

        public void CompleteCurrentCheckpoint(Action onNextScreen)
        {
            var cp = GetCurrentCheckpoint();
            string cpId = cp != null ? cp.checkpointId : CHECKPOINT_ACT1_CLEAR;
            CommitSimCheckpoint(CurrentCharacterId, cpId);
            CurrentCheckpointIndex = Mathf.Min(CurrentCheckpointIndex + 1, _defaultCheckpoints.Count);
            OnCheckpointCompleted?.Invoke(cp);
            onNextScreen?.Invoke();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            LoadSaveData();
        }

        public void StartNewRun(string characterId, string runId = null)
        {
            CurrentCharacterId = CharacterRegistry.ResolveCanonicalId(characterId);
            CurrentRunId = !string.IsNullOrEmpty(runId) ? runId : Guid.NewGuid().ToString("N");

            // Unlock character STORY_OPEN slot upon beginning story mode
            UnlockGallerySlot($"{CurrentCharacterId}_STORY_OPEN");
            UnlockGallerySlot("S01_GAME_OPENING");
            SaveDataAtomically();
        }

        public bool CommitSimCheckpoint(string characterId, string checkpointId, string runId = null)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(checkpointId)) return false;
            characterId = CharacterRegistry.ResolveCanonicalId(characterId);
            string effectiveRunId = !string.IsNullOrEmpty(runId) ? runId : CurrentRunId;

            // Transaction Dedupe Key: characterId + ":" + checkpointId + ":" + runId
            string txKey = $"{characterId}:{checkpointId}:{effectiveRunId}";
            if (_saveData.committedTransactionKeys.Contains(txKey))
            {
                // Already committed for this run — ignore duplicate
                return false;
            }

            var progress = GetOrCreateProgress(characterId);

            if (string.Equals(checkpointId, CHECKPOINT_ACT1_CLEAR, StringComparison.OrdinalIgnoreCase))
            {
                progress.act1Clear = true;
                UnlockGallerySlot("S02_ACT1_TRANSITION");
                UnlockGallerySlot("S03_ACT2_TRANSITION");
            }
            else if (string.Equals(checkpointId, CHECKPOINT_ACT2_CLEAR, StringComparison.OrdinalIgnoreCase))
            {
                progress.act2Clear = true;
                UnlockGallerySlot("S04_ACT3_TRANSITION");
                UnlockGallerySlot("S05_FINAL_BOSS_INTRO");
            }
            else if (string.Equals(checkpointId, CHECKPOINT_STORY_CLEAR, StringComparison.OrdinalIgnoreCase))
            {
                progress.storyClear = true;
                progress.clearCount++;
                UnlockGallerySlot("S06_FINAL_BOSS_DEFEAT");
                UnlockGallerySlot("S07_FINALE_EPILOGUE");
                UnlockGallerySlot("S08_CREDITS");
                UnlockGallerySlot($"{characterId}_STORY_END");
            }

            _saveData.committedTransactionKeys.Add(txKey);
            _saveData.lastSaveTime = DateTime.UtcNow.ToString("o");

            SaveDataAtomically();
            OnCheckpointCommitted?.Invoke(characterId, checkpointId);
            return true;
        }

        public bool HasClearedStory(string characterId)
        {
            characterId = CharacterRegistry.ResolveCanonicalId(characterId);
            var p = GetProgress(characterId);
            return p != null && p.storyClear;
        }

        public bool HasCheckpoint(string characterId, string checkpointId)
        {
            characterId = CharacterRegistry.ResolveCanonicalId(characterId);
            var p = GetProgress(characterId);
            if (p == null) return false;

            if (string.Equals(checkpointId, CHECKPOINT_ACT1_CLEAR, StringComparison.OrdinalIgnoreCase)) return p.act1Clear;
            if (string.Equals(checkpointId, CHECKPOINT_ACT2_CLEAR, StringComparison.OrdinalIgnoreCase)) return p.act2Clear;
            if (string.Equals(checkpointId, CHECKPOINT_STORY_CLEAR, StringComparison.OrdinalIgnoreCase)) return p.storyClear;
            return false;
        }

        public bool ShouldShowStoryToggle(string characterId)
        {
            // SF6 rule: Hidden on first run; appears once cleared
            return HasClearedStory(characterId);
        }

        public bool GetStoryToggle(string characterId)
        {
            characterId = CharacterRegistry.ResolveCanonicalId(characterId);
            var p = GetProgress(characterId);
            if (p == null || !p.storyClear)
            {
                // First run: always show story
                return true;
            }
            return p.storyToggleEnabled;
        }

        public void SetStoryToggle(string characterId, bool enabled)
        {
            characterId = CharacterRegistry.ResolveCanonicalId(characterId);
            var p = GetOrCreateProgress(characterId);
            p.storyToggleEnabled = enabled;
            SaveDataAtomically();
        }

        public bool IsSlotUnlocked(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return false;
            if (_saveData.unlockedGallerySlots.Contains(slotId)) return true;

            int underscoreIdx = slotId.IndexOf('_');
            if (underscoreIdx > 0)
            {
                string prefix = slotId.Substring(0, underscoreIdx);
                string suffix = slotId.Substring(underscoreIdx);
                string canon = CharacterRegistry.ResolveCanonicalId(prefix);
                if (canon != prefix && _saveData.unlockedGallerySlots.Contains(canon + suffix))
                {
                    return true;
                }
                string legacy = CharacterRegistry.MapAuthoritativeIdToLegacy(prefix);
                if (!string.IsNullOrEmpty(legacy) && _saveData.unlockedGallerySlots.Contains(legacy + suffix))
                {
                    return true;
                }
            }
            return false;
        }

        public void UnlockGallerySlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return;
            if (!_saveData.unlockedGallerySlots.Contains(slotId))
            {
                _saveData.unlockedGallerySlots.Add(slotId);
            }

            int underscoreIdx = slotId.IndexOf('_');
            if (underscoreIdx > 0)
            {
                string prefix = slotId.Substring(0, underscoreIdx);
                string suffix = slotId.Substring(underscoreIdx);
                string canon = CharacterRegistry.ResolveCanonicalId(prefix);
                if (canon != prefix && !_saveData.unlockedGallerySlots.Contains(canon + suffix))
                {
                    _saveData.unlockedGallerySlots.Add(canon + suffix);
                }
                string legacy = CharacterRegistry.MapAuthoritativeIdToLegacy(prefix);
                if (!string.IsNullOrEmpty(legacy) && !_saveData.unlockedGallerySlots.Contains(legacy + suffix))
                {
                    _saveData.unlockedGallerySlots.Add(legacy + suffix);
                }
            }
        }

        private CharacterStoryProgress GetProgress(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            string canon = CharacterRegistry.ResolveCanonicalId(characterId);
            string legacy = CharacterRegistry.MapAuthoritativeIdToLegacy(canon);

            for (int i = 0; i < _saveData.characterProgress.Count; i++)
            {
                string storedId = _saveData.characterProgress[i].characterId;
                if (string.Equals(storedId, characterId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(storedId, canon, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(legacy) && string.Equals(storedId, legacy, StringComparison.OrdinalIgnoreCase)))
                {
                    return _saveData.characterProgress[i];
                }
            }
            return null;
        }

        private CharacterStoryProgress GetOrCreateProgress(string characterId)
        {
            characterId = CharacterRegistry.ResolveCanonicalId(characterId);
            var p = GetProgress(characterId);
            if (p != null) return p;

            p = new CharacterStoryProgress { characterId = characterId };
            _saveData.characterProgress.Add(p);
            return p;
        }

        #region Atomic Persistence

        private static void SafeLogError(string message)
        {
            try
            {
                Debug.LogError(message);
            }
            catch
            {
                Console.WriteLine("[ERROR] " + message);
            }
        }

        private string SerializeStorySave(StorySaveData data)
        {
            try
            {
                return JsonUtility.ToJson(data, true);
            }
            catch
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"version\": {data.version},");
                sb.AppendLine($"  \"lastSaveTime\": \"{data.lastSaveTime}\",");
                sb.AppendLine("  \"committedTransactionKeys\": [");
                for (int i = 0; i < data.committedTransactionKeys.Count; i++)
                {
                    sb.AppendLine($"    \"{data.committedTransactionKeys[i]}\"{(i < data.committedTransactionKeys.Count - 1 ? "," : "")}");
                }
                sb.AppendLine("  ],");
                sb.AppendLine("  \"unlockedGallerySlots\": [");
                for (int i = 0; i < data.unlockedGallerySlots.Count; i++)
                {
                    sb.AppendLine($"    \"{data.unlockedGallerySlots[i]}\"{(i < data.unlockedGallerySlots.Count - 1 ? "," : "")}");
                }
                sb.AppendLine("  ],");
                sb.AppendLine("  \"characterProgress\": [");
                for (int i = 0; i < data.characterProgress.Count; i++)
                {
                    var cp = data.characterProgress[i];
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"characterId\": \"{cp.characterId}\",");
                    sb.AppendLine($"      \"act1Clear\": {cp.act1Clear.ToString().ToLower()},");
                    sb.AppendLine($"      \"act2Clear\": {cp.act2Clear.ToString().ToLower()},");
                    sb.AppendLine($"      \"storyClear\": {cp.storyClear.ToString().ToLower()},");
                    sb.AppendLine($"      \"storyToggleEnabled\": {cp.storyToggleEnabled.ToString().ToLower()},");
                    sb.AppendLine($"      \"clearCount\": {cp.clearCount}");
                    sb.Append("    }");
                    if (i < data.characterProgress.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
                return sb.ToString();
            }
        }

        private StorySaveData DeserializeStorySave(string json)
        {
            try
            {
                var loaded = JsonUtility.FromJson<StorySaveData>(json);
                if (loaded != null) return loaded;
            }
            catch { }

            var result = new StorySaveData();
            if (string.IsNullOrEmpty(json)) return result;

            using (var reader = new StringReader(json))
            {
                string line;
                CharacterStoryProgress currentProg = null;
                bool inTxKeys = false;
                bool inGallery = false;

                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("\"committedTransactionKeys\"")) { inTxKeys = true; inGallery = false; continue; }
                    if (trimmed.StartsWith("\"unlockedGallerySlots\"")) { inGallery = true; inTxKeys = false; continue; }
                    if (trimmed.StartsWith("]")) { inTxKeys = false; inGallery = false; continue; }

                    if (inTxKeys)
                    {
                        var parts = trimmed.Trim('[', ']', ',').Split(',');
                        foreach (var p in parts)
                        {
                            string key = p.Trim(' ', '\"');
                            if (!string.IsNullOrEmpty(key) && !result.committedTransactionKeys.Contains(key))
                                result.committedTransactionKeys.Add(key);
                        }
                    }
                    else if (inGallery)
                    {
                        var parts = trimmed.Trim('[', ']', ',').Split(',');
                        foreach (var p in parts)
                        {
                            string key = p.Trim(' ', '\"');
                            if (!string.IsNullOrEmpty(key) && !result.unlockedGallerySlots.Contains(key))
                                result.unlockedGallerySlots.Add(key);
                        }
                    }

                    if (trimmed.StartsWith("{") || trimmed.Contains("\"characterId\""))
                    {
                        if (currentProg == null && (trimmed.Contains("\"characterId\"") || line.Contains("{")))
                        {
                            currentProg = new CharacterStoryProgress();
                            result.characterProgress.Add(currentProg);
                        }
                    }
                    if (currentProg != null)
                    {
                        if (trimmed.StartsWith("\"characterId\""))
                        {
                            int colon = trimmed.IndexOf(':');
                            if (colon >= 0) currentProg.characterId = trimmed.Substring(colon + 1).Trim(' ', '\"', ',');
                        }
                        else if (trimmed.StartsWith("\"act1Clear\""))
                        {
                            currentProg.act1Clear = trimmed.Contains("true");
                        }
                        else if (trimmed.StartsWith("\"act2Clear\""))
                        {
                            currentProg.act2Clear = trimmed.Contains("true");
                        }
                        else if (trimmed.StartsWith("\"storyClear\""))
                        {
                            currentProg.storyClear = trimmed.Contains("true");
                        }
                        else if (trimmed.StartsWith("\"storyToggleEnabled\""))
                        {
                            currentProg.storyToggleEnabled = trimmed.Contains("true");
                        }
                        else if (trimmed.StartsWith("\"clearCount\""))
                        {
                            int colon = trimmed.IndexOf(':');
                            if (colon >= 0 && int.TryParse(trimmed.Substring(colon + 1).Trim(' ', ','), out int cc))
                                currentProg.clearCount = cc;
                        }
                    }
                    if (trimmed.StartsWith("}") && currentProg != null)
                    {
                        currentProg = null;
                    }
                }
            }
            return result;
        }

        public void SaveDataAtomically()
        {
            try
            {
                AtomicFileSave.Write(SaveFilePath, SerializeStorySave(_saveData));
            }
            catch (Exception ex)
            {
                SafeLogError($"[StoryCheckpointManager] Failed to atomically save story progress: {ex.Message}");
            }
        }

        public void LoadSaveData()
        {
            if (!AtomicFileSave.TryRead(SaveFilePath, out string json, out bool recovered))
            {
                _saveData = new StorySaveData();
                return;
            }

            try
            {
                _saveData = DeserializeStorySave(json);
                if (recovered)
                    SaveDataAtomically();
            }
            catch (Exception ex)
            {
                SafeLogError($"[StoryCheckpointManager] Failed to load story save data: {ex.Message}");
                _saveData = new StorySaveData();
            }
        }

        public void ResetStoryProgress()
        {
            _saveData = new StorySaveData();
            string path = SaveFilePath;
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
            string tmp = path + ".tmp";
            if (File.Exists(tmp))
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        #endregion
    }
}
