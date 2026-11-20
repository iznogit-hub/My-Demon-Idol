using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Arcade Ladder Game Mode Director for BlazBlue 4-Button (Spec §11.3, M12).
    /// Orchestrates an 8-stage single-player tournament ladder against the roster girls.
    /// Tracks total score, stage transitions, time bonuses, combo bonuses, and Astral Finish callouts.
    /// </summary>
    public class BBArcadeDirector : MonoBehaviour
    {
        private static BBArcadeDirector _instance;
        public static BBArcadeDirector Instance => _instance;

        public struct ArcadeStage
        {
            public int stageNumber;
            public string opponentId;
            public string opponentName;
            public string stageId;
            public BBAIDifficulty aiDifficulty;
        }

        public const int TOTAL_STAGES = 8;

        [Header("Arcade State")]
        [SerializeField] private int _currentStageIndex = 0;
        [SerializeField] private string _playerFighterId = "C01";
        [SerializeField] private int _totalScore = 0;
        [SerializeField] private int _continuesUsed = 0;
        [SerializeField] private bool _arcadeCompleted = false;

        public int CurrentStageIndex => _currentStageIndex;
        public string PlayerFighterId => _playerFighterId;
        public int TotalScore => _totalScore;
        public int ContinuesUsed => _continuesUsed;
        public bool IsArcadeCompleted => _arcadeCompleted;

        private static readonly ArcadeStage[] DefaultLadder = new ArcadeStage[]
        {
            new ArcadeStage { stageNumber = 1, opponentId = "C04", opponentName = "UnityChan", stageId = "ST01", aiDifficulty = BBAIDifficulty.Easy },
            new ArcadeStage { stageNumber = 2, opponentId = "C02", opponentName = "Aika", stageId = "ST02", aiDifficulty = BBAIDifficulty.Medium },
            new ArcadeStage { stageNumber = 3, opponentId = "C09", opponentName = "PicoChan", stageId = "ST03", aiDifficulty = BBAIDifficulty.Medium },
            new ArcadeStage { stageNumber = 4, opponentId = "C12", opponentName = "Fina", stageId = "ST04", aiDifficulty = BBAIDifficulty.Hard },
            new ArcadeStage { stageNumber = 5, opponentId = "C08", opponentName = "SanpakuChan", stageId = "ST05", aiDifficulty = BBAIDifficulty.Hard },
            new ArcadeStage { stageNumber = 6, opponentId = "C06", opponentName = "Cultist", stageId = "ST06", aiDifficulty = BBAIDifficulty.Hard },
            new ArcadeStage { stageNumber = 7, opponentId = "C10", opponentName = "Reika", stageId = "ST01", aiDifficulty = BBAIDifficulty.Hard },
            new ArcadeStage { stageNumber = 8, opponentId = "C07", opponentName = "StellarWitch", stageId = "ST07", aiDifficulty = BBAIDifficulty.Boss }
        };

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void StartArcade(string playerId)
        {
            _playerFighterId = playerId;
            _currentStageIndex = 0;
            _totalScore = 0;
            _continuesUsed = 0;
            _arcadeCompleted = false;
        }

        public ArcadeStage GetCurrentStage()
        {
            if (_currentStageIndex < 0 || _currentStageIndex >= TOTAL_STAGES)
            {
                return DefaultLadder[0];
            }

            var stage = DefaultLadder[_currentStageIndex];

            // If player chose same character as default opponent, swap opponent to Akio (C01)
            if (stage.opponentId == _playerFighterId)
            {
                stage.opponentId = "C01";
                stage.opponentName = "Akio";
            }

            return stage;
        }

        public void ReportStageResult(bool playerWon, bool isPerfect, bool isAstralFinish, int remainingTimeSeconds, int maxCombo)
        {
            if (playerWon)
            {
                int stagePoints = 100000;
                if (isPerfect) stagePoints += 50000;
                if (isAstralFinish) stagePoints += 200000;
                stagePoints += remainingTimeSeconds * 1000;
                stagePoints += maxCombo * 500;

                _totalScore += stagePoints;

                _currentStageIndex++;
                if (_currentStageIndex >= TOTAL_STAGES)
                {
                    _arcadeCompleted = true;
                }
            }
            else
            {
                _continuesUsed++;
            }
        }

        public void AdvanceStage()
        {
            if (_currentStageIndex < TOTAL_STAGES - 1)
            {
                _currentStageIndex++;
            }
            else
            {
                _arcadeCompleted = true;
            }
        }
    }
}
