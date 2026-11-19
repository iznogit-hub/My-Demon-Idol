using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    public enum BBVersusState
    {
        Intro = 0,
        RoundRunning = 1,
        RoundEnded = 2,
        MatchFinished = 3
    }

    /// <summary>
    /// Local Versus Mode Director for BlazBlue 4-Button (Spec §11.3, M12).
    /// Orchestrates round progression, round timers (99s default), win pips,
    /// KO detection, time-outs, and rematch flow.
    /// </summary>
    public class BBVersusDirector : MonoBehaviour
    {
        private static BBVersusDirector _instance;
        public static BBVersusDirector Instance => _instance;

        public const int ROUNDS_TO_WIN = 2;
        public const int DEFAULT_ROUND_SECONDS = 99;

        [Header("Versus State")]
        public BBVersusState state = BBVersusState.Intro;
        public int currentRound = 1;
        public int p1RoundsWon = 0;
        public int p2RoundsWon = 0;
        public float roundTimeRemaining = DEFAULT_ROUND_SECONDS;
        public int winnerPlayerIndex = -1; // 0 for P1, 1 for P2, -1 for Draw

        [Header("Fighters")]
        public BBFighterBrain p1Brain;
        public BBFighterBrain p2Brain;

        private void Awake()
        {
            _instance = this;
        }

        public void StartVersusMatch(BBFighterBrain p1, BBFighterBrain p2)
        {
            p1Brain = p1;
            p2Brain = p2;
            currentRound = 1;
            p1RoundsWon = 0;
            p2RoundsWon = 0;
            winnerPlayerIndex = -1;
            StartRound(currentRound);
        }

        public void StartRound(int roundNumber)
        {
            currentRound = roundNumber;
            roundTimeRemaining = DEFAULT_ROUND_SECONDS;
            state = BBVersusState.RoundRunning;

            if (p1Brain != null)
            {
                p1Brain.currentHealth = 11000f;
                p1Brain.heatMeter = 0f;
                p1Brain.barrierMeter = 1000f;
                p1Brain.inDanger = false;
            }

            if (p2Brain != null)
            {
                p2Brain.currentHealth = 11000f;
                p2Brain.heatMeter = 0f;
                p2Brain.barrierMeter = 1000f;
                p2Brain.inDanger = false;
            }

            if (BBBattleHUDController.Instance != null)
            {
                BBBattleHUDController.Instance.ShowAnnouncement($"ROUND {roundNumber}", 90);
                BBBattleHUDController.Instance.UpdateRoundTimer(Mathf.CeilToInt(roundTimeRemaining));
            }
        }

        public void TickRound(float deltaTime)
        {
            if (state != BBVersusState.RoundRunning) return;

            roundTimeRemaining -= deltaTime;
            if (roundTimeRemaining <= 0f)
            {
                roundTimeRemaining = 0f;
                EvaluateTimeOver();
                return;
            }

            if (BBBattleHUDController.Instance != null)
            {
                BBBattleHUDController.Instance.UpdateRoundTimer(Mathf.CeilToInt(roundTimeRemaining));
            }

            // Check K.O.
            if (p1Brain != null && p1Brain.currentHealth <= 0f)
            {
                EndRound(1, false); // P2 wins
            }
            else if (p2Brain != null && p2Brain.currentHealth <= 0f)
            {
                EndRound(0, false); // P1 wins
            }
        }

        private void EvaluateTimeOver()
        {
            float hp1 = p1Brain != null ? p1Brain.currentHealth : 0f;
            float hp2 = p2Brain != null ? p2Brain.currentHealth : 0f;

            if (hp1 > hp2) EndRound(0, true);
            else if (hp2 > hp1) EndRound(1, true);
            else EndRound(-1, true); // Double K.O. / Draw
        }

        public void EndRound(int roundWinner, bool isTimeOver)
        {
            state = BBVersusState.RoundEnded;

            if (roundWinner == 0) p1RoundsWon++;
            else if (roundWinner == 1) p2RoundsWon++;

            string msg = isTimeOver ? "TIME UP!" : "K.O.!";
            if (BBBattleHUDController.Instance != null)
            {
                BBBattleHUDController.Instance.ShowAnnouncement(msg, 120);
            }

            // Check Match Winner
            if (p1RoundsWon >= ROUNDS_TO_WIN)
            {
                winnerPlayerIndex = 0;
                state = BBVersusState.MatchFinished;
            }
            else if (p2RoundsWon >= ROUNDS_TO_WIN)
            {
                winnerPlayerIndex = 1;
                state = BBVersusState.MatchFinished;
            }
        }
    }
}
