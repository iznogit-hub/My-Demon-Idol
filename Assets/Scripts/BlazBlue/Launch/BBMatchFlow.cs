using System;

namespace DemonIdol.BlazBlue
{
    public enum BBFlowScreen
    {
        Title,
        ModeSelect,
        CharacterSelect,
        Versus,
        RoundIntro,
        Fight,
        RoundEnd,
        MatchEnd,
        Pause,
        ArcadeNext,
        Ending,
        Online
    }

    /// <summary>
    /// Launch loop state machine. Art, video, and cutscene slots are hooks only.
    /// </summary>
    public class BBMatchFlow
    {
        public static readonly string[] Roster =
        {
            "C01", "C02", "C04", "C05", "C06", "C07", "C08", "C09", "C10", "C11", "C12", "C13", "C14",
            "SK02", "SK03", "SK05", "SK06", "SK08",
            "EX01", "EX02", "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        public BBFlowScreen Screen = BBFlowScreen.Title;
        public BBFlowScreen ScreenBeforePause;
        public string Mode = "Arcade";
        public string P1Id = "C01";
        public string P2Id = "C02";
        public string StageId = "ST07";
        public int Round = 1;
        public int P1Wins;
        public int P2Wins;
        public int WinsNeeded = 2;
        public bool LastRoundPerfect;
        public bool LastRoundTime;
        public int ArcadeIndex;
        public string WinnerQuoteHook;
        public string PortraitHook;
        public string VideoHook;

        public event Action<string> OnEnding;

        public void GoToModeSelect() => Screen = BBFlowScreen.ModeSelect;

        public void SelectMode(string mode)
        {
            Mode = string.IsNullOrEmpty(mode) ? "Arcade" : mode;
            Screen = Mode == "Online" ? BBFlowScreen.Online : BBFlowScreen.CharacterSelect;
        }

        public void ConfirmCharacters(string p1, string p2)
        {
            if (!string.IsNullOrEmpty(p1)) P1Id = p1;
            if (!string.IsNullOrEmpty(p2)) P2Id = p2;
            PortraitHook = "vs";
            VideoHook = "vs";
            Screen = BBFlowScreen.Versus;
        }

        public void BeginRound()
        {
            Screen = BBFlowScreen.RoundIntro;
        }

        public void BeginFight()
        {
            Screen = BBFlowScreen.Fight;
        }

        public void Pause()
        {
            if (Screen == BBFlowScreen.Pause) return;
            ScreenBeforePause = Screen;
            Screen = BBFlowScreen.Pause;
        }

        public void Resume()
        {
            if (Screen == BBFlowScreen.Pause) Screen = ScreenBeforePause;
        }

        public void ReportRound(int winnerPlayer, bool perfect, bool timeUp)
        {
            LastRoundPerfect = perfect;
            LastRoundTime = timeUp;
            if (winnerPlayer == 1) P1Wins++;
            else if (winnerPlayer == 2) P2Wins++;
            Screen = BBFlowScreen.RoundEnd;
            if (P1Wins >= WinsNeeded || P2Wins >= WinsNeeded)
            {
                Screen = BBFlowScreen.MatchEnd;
                WinnerQuoteHook = winnerPlayer == 1 ? P1Id : P2Id;
                PortraitHook = "winner";
                VideoHook = "victory";
            }
        }

        public void ContinueAfterRound()
        {
            if (Screen != BBFlowScreen.MatchEnd && P1Wins < WinsNeeded && P2Wins < WinsNeeded)
            {
                Round++;
                Screen = BBFlowScreen.RoundIntro;
                return;
            }
            if (Mode == "Arcade" && P1Wins >= WinsNeeded)
            {
                ArcadeIndex++;
                if (ArcadeIndex >= Roster.Length - 1)
                {
                    Screen = BBFlowScreen.Ending;
                    OnEnding?.Invoke(P1Id);
                    return;
                }
                P2Id = NextArcadeOpponent();
                P1Wins = 0;
                P2Wins = 0;
                Round = 1;
                Screen = BBFlowScreen.ArcadeNext;
                return;
            }
            Screen = BBFlowScreen.MatchEnd;
        }

        public string NextArcadeOpponent()
        {
            for (int i = 0; i < Roster.Length; i++)
            {
                int index = (ArcadeIndex + i) % Roster.Length;
                if (Roster[index] != P1Id) return Roster[index];
            }
            return "C02";
        }

        public void Rematch()
        {
            P1Wins = 0;
            P2Wins = 0;
            Round = 1;
            Screen = BBFlowScreen.Versus;
        }

        public void ReturnToCharacters() => Screen = BBFlowScreen.CharacterSelect;

        public void ReturnToMenu() => Screen = BBFlowScreen.Title;
    }
}
