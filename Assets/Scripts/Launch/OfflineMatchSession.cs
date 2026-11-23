using System;

namespace DemonIdol.Launch
{
    public enum PlayModeKind
    {
        Story = 0,
        Arcade = 1,
        LocalVersus = 2,
        Training = 3,
        OnlineUnavailable = 4
    }

    public enum MatchFlowStep
    {
        Boot = 0,
        Menu = 1,
        CharacterSelect = 2,
        StageSelect = 3,
        InMatch = 4,
        Results = 5
    }

    /// <summary>
    /// Project-owned offline match session. Online is a visible rejection, not a simulated lobby.
    /// </summary>
    public sealed class OfflineMatchSession
    {
        // Launch Roster Arcade ladder ending in Torgrim (SK08) boss fight
        static readonly string[] ArcadeOpponents = { "C02", "C05", "C06", "C08", "C09", "C11", "C12", "C14", "EX01", "EX04", "SK08" };

        public PlayModeKind Mode { get; private set; } = PlayModeKind.Training;
        public MatchFlowStep Step { get; private set; } = MatchFlowStep.Boot;
        public string MatchId { get; private set; }
        public string Player1Id { get; private set; } = "C01";
        public string Player2Id { get; private set; } = "C02";
        public string StageId { get; private set; } = "ST01";
        public bool ResultCommitted { get; private set; }
        public int ArcadeIndex { get; private set; }
        public int LastWinnerPlayer { get; private set; }

        public bool P2IsCpu => Mode != PlayModeKind.LocalVersus;

        public bool TrySelectMode(string modeName, out string rejection)
        {
            if (IsOnlineName(modeName))
            {
                Mode = PlayModeKind.OnlineUnavailable;
                Step = MatchFlowStep.Menu;
                rejection = "Online matchmaking is unavailable. No backend is configured. Local Versus is the two-player match.";
                return false;
            }

            Mode = ParseMode(modeName);
            Step = MatchFlowStep.CharacterSelect;
            if (Mode == PlayModeKind.Arcade)
            {
                ArcadeIndex = 0;
                Player2Id = ArcadeOpponents[0];
            }
            rejection = null;
            return true;
        }

        public void MarkStageSelect()
        {
            Step = MatchFlowStep.StageSelect;
        }

        public void MarkMenu()
        {
            Step = MatchFlowStep.Menu;
            MatchId = null;
            ResultCommitted = false;
        }

        public string BeginMatch(string player1Id, string player2Id, string stageId)
        {
            Player1Id = string.IsNullOrEmpty(player1Id) ? "C01" : player1Id;
            // An explicit menu pick always wins. The arcade ladder is only the
            // fallback when the caller did not name an opponent (profiler / empty slot).
            if (Mode == PlayModeKind.Arcade && string.IsNullOrEmpty(player2Id))
            {
                int arcadeSlot = ArcadeIndex;
                if (arcadeSlot < 0)
                    arcadeSlot = 0;
                if (arcadeSlot >= ArcadeOpponents.Length)
                    arcadeSlot = ArcadeOpponents.Length - 1;
                Player2Id = ArcadeOpponents[arcadeSlot];
            }
            else
            {
                Player2Id = string.IsNullOrEmpty(player2Id) ? "C02" : player2Id;
            }
            StageId = string.IsNullOrEmpty(stageId) ? "ST01" : stageId;
            MatchId = Guid.NewGuid().ToString("N");
            ResultCommitted = false;
            LastWinnerPlayer = 0;
            Step = MatchFlowStep.InMatch;
            return MatchId;
        }

        public bool CommitResult(int winningPlayer)
        {
            if (ResultCommitted || string.IsNullOrEmpty(MatchId))
                return false;
            ResultCommitted = true;
            LastWinnerPlayer = winningPlayer;
            Step = MatchFlowStep.Results;
            return true;
        }

        public string BeginRematch()
        {
            return BeginMatch(Player1Id, Player2Id, StageId);
        }

        public bool TryAdvanceArcade(out string nextOpponentId)
        {
            nextOpponentId = null;
            if (Mode != PlayModeKind.Arcade)
                return false;
            int next = ArcadeIndex + 1;
            if (next >= ArcadeOpponents.Length)
                return false;
            ArcadeIndex = next;
            nextOpponentId = ArcadeOpponents[next];
            Player2Id = nextOpponentId;
            return true;
        }

        public static bool IsOnlineName(string modeName)
        {
            if (string.IsNullOrEmpty(modeName))
                return false;
            return modeName.Equals("PvP", StringComparison.OrdinalIgnoreCase)
                || modeName.Equals("Online", StringComparison.OrdinalIgnoreCase)
                || modeName.Equals("OnlineVersus", StringComparison.OrdinalIgnoreCase);
        }

        public static PlayModeKind ParseMode(string modeName)
        {
            if (string.IsNullOrEmpty(modeName))
                return PlayModeKind.Training;
            if (modeName.Equals("Story", StringComparison.OrdinalIgnoreCase))
                return PlayModeKind.Story;
            if (modeName.Equals("Arcade", StringComparison.OrdinalIgnoreCase))
                return PlayModeKind.Arcade;
            if (modeName.Equals("LocalVersus", StringComparison.OrdinalIgnoreCase)
                || modeName.Equals("Versus", StringComparison.OrdinalIgnoreCase))
                return PlayModeKind.LocalVersus;
            if (modeName.Equals("Tutorial", StringComparison.OrdinalIgnoreCase)
                || modeName.Equals("Curriculum", StringComparison.OrdinalIgnoreCase))
                return PlayModeKind.Training;
            return PlayModeKind.Training;
        }
    }
}
