using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Launch;

namespace DemonIdol.Combat
{
    [Serializable]
    public struct ReplayTickInput
    {
        public int tick;
        public int p1Buttons;
        public float p1MoveX;
        public float p1MoveY;
        public int p2Buttons;
        public float p2MoveX;
        public float p2MoveY;

        public ReplayTickInput(int t, int p1B, Vector2 p1M, int p2B, Vector2 p2M)
        {
            tick = t;
            p1Buttons = p1B;
            p1MoveX = p1M.x;
            p1MoveY = p1M.y;
            p2Buttons = p2B;
            p2MoveX = p2M.x;
            p2MoveY = p2M.y;
        }
    }

    [Serializable]
    public class MatchReplayData
    {
        public string replayId;
        public string timestamp;
        public int randomSeed;
        public string player1CharacterId = "C01";
        public string player2CharacterId = "C03";
        public string stageId = "ST01";
        public int winnerSlot; // 1 = P1, 2 = P2
        public int totalTicks;
        public List<ReplayTickInput> tickInputs = new List<ReplayTickInput>();
    }

    /// <summary>
    /// Frame-accurate Deterministic Match Replay Session.
    /// Records discrete inputs per 60Hz tick without floating precision drift.
    /// Supports saving to disk via AtomicFileSave and frame-by-step playback.
    /// </summary>
    public class MatchReplaySession : MonoBehaviour
    {
        public static MatchReplaySession Instance { get; private set; }

        public bool IsRecording { get; private set; } = false;
        public bool IsPlayingBack { get; private set; } = false;

        private MatchReplayData _currentData;
        private int _playbackIndex = 0;

        [SerializeField] private FighterController _fighter1;
        [SerializeField] private FighterController _fighter2;

        public event Action<MatchReplayData> OnReplaySaved;
        public event Action OnPlaybackFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick += HandleCombatTick;
            }
        }

        private void OnDestroy()
        {
            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.OnTick -= HandleCombatTick;
            }
        }

        public void BindFighters(FighterController p1, FighterController p2)
        {
            _fighter1 = p1;
            _fighter2 = p2;
        }

        public void StartRecording(string p1Id, string p2Id, string stageId, int seed)
        {
            _currentData = new MatchReplayData
            {
                replayId = "RPL_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                timestamp = DateTime.UtcNow.ToString("o"),
                randomSeed = seed,
                player1CharacterId = p1Id,
                player2CharacterId = p2Id,
                stageId = stageId,
                tickInputs = new List<ReplayTickInput>(3600)
            };

            IsRecording = true;
            IsPlayingBack = false;
        }

        public MatchReplayData StopRecording(int winnerSlot)
        {
            if (!IsRecording || _currentData == null) return null;

            IsRecording = false;
            _currentData.winnerSlot = winnerSlot;
            _currentData.totalTicks = CombatTickEngine.Instance != null ? CombatTickEngine.Instance.CurrentTick : _currentData.tickInputs.Count;

            SaveReplayToDisk(_currentData);
            OnReplaySaved?.Invoke(_currentData);
            return _currentData;
        }

        public void StartPlayback(MatchReplayData data)
        {
            if (data == null) return;
            _currentData = data;
            _playbackIndex = 0;
            IsPlayingBack = true;
            IsRecording = false;

            if (_fighter1 != null) _fighter1.isPlayerControlled = false;
            if (_fighter2 != null) _fighter2.isPlayerControlled = false;

            if (CombatTickEngine.Instance != null)
            {
                CombatTickEngine.Instance.ResetTicks();
            }
        }

        public void StopPlayback()
        {
            IsPlayingBack = false;
            _playbackIndex = 0;
            OnPlaybackFinished?.Invoke();
        }

        private void HandleCombatTick(int currentTick)
        {
            if (IsRecording)
            {
                RecordCurrentTick(currentTick);
            }
            else if (IsPlayingBack)
            {
                PlaybackCurrentTick(currentTick);
            }
        }

        private void RecordCurrentTick(int tick)
        {
            if (_fighter1 == null || _fighter2 == null || _currentData == null) return;

            int p1Buttons = 0;
            if (_fighter1.InputBuf != null && _fighter1.InputBuf.PeekLatest(out var p1Cmd))
            {
                p1Buttons = (int)p1Cmd.Button;
            }

            int p2Buttons = 0;
            if (_fighter2.InputBuf != null && _fighter2.InputBuf.PeekLatest(out var p2Cmd))
            {
                p2Buttons = (int)p2Cmd.Button;
            }

            var entry = new ReplayTickInput(tick, p1Buttons, _fighter1.MoveInput, p2Buttons, _fighter2.MoveInput);
            _currentData.tickInputs.Add(entry);
        }

        private void PlaybackCurrentTick(int tick)
        {
            if (_currentData == null || _playbackIndex >= _currentData.tickInputs.Count)
            {
                StopPlayback();
                return;
            }

            var entry = _currentData.tickInputs[_playbackIndex];
            _playbackIndex++;

            if (_fighter1 != null)
            {
                _fighter1.SetMoveInput(new Vector2(entry.p1MoveX, entry.p1MoveY));
                if (entry.p1Buttons != 0 && _fighter1.InputBuf != null)
                {
                    _fighter1.InputBuf.Enqueue((InputButton)entry.p1Buttons, tick, new Vector2(entry.p1MoveX, entry.p1MoveY));
                }
            }

            if (_fighter2 != null)
            {
                _fighter2.SetMoveInput(new Vector2(entry.p2MoveX, entry.p2MoveY));
                if (entry.p2Buttons != 0 && _fighter2.InputBuf != null)
                {
                    _fighter2.InputBuf.Enqueue((InputButton)entry.p2Buttons, tick, new Vector2(entry.p2MoveX, entry.p2MoveY));
                }
            }
        }

        private void SaveReplayToDisk(MatchReplayData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string dir = System.IO.Path.Combine(Application.persistentDataPath, "Replays");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                string path = System.IO.Path.Combine(dir, $"{data.replayId}.json");
                AtomicFileSave.Write(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchReplaySession] Failed to persist replay: {ex.Message}");
            }
        }
    }
}
