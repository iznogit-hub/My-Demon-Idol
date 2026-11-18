using System.IO;
using UnityEngine;

namespace DemonIdol.Launch
{
    /// <summary>
    /// Composition root for the offline session, the once-only cosmetic ledger, and save files.
    /// </summary>
    public static class LaunchServices
    {
        public const string RewardsFileName = "demonidol-rewards.json";

        public static OfflineMatchSession Session { get; private set; } = new OfflineMatchSession();
        public static MatchRewardLedger Rewards { get; private set; } = new MatchRewardLedger();

        public static string RewardsPath => Path.Combine(Application.persistentDataPath, RewardsFileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadRewards()
        {
            if (!AtomicFileSave.TryRead(RewardsPath, out string document, out bool recovered) || string.IsNullOrEmpty(document))
                return;
            if (!Rewards.TryLoadDocument(document))
                return;
            if (recovered)
                AtomicFileSave.Write(RewardsPath, Rewards.ToDocument());
        }

        public static bool CommitAndGrant(int winningPlayer, out int grantedXp)
        {
            grantedXp = 0;
            if (Session.ResultCommitted || string.IsNullOrEmpty(Session.MatchId))
                return false;
            if (Rewards.WasGranted(Session.MatchId))
            {
                Session.CommitResult(winningPlayer);
                return false;
            }

            if (!Rewards.TryGrant(Session.MatchId, Session.Mode, out grantedXp))
                return false;

            try
            {
                AtomicFileSave.Write(RewardsPath, Rewards.ToDocument());
            }
            catch (System.Exception)
            {
                if (DiskAlreadyHasGrant(Session.MatchId))
                {
                    Session.CommitResult(winningPlayer);
                    return true;
                }

                Rewards.UndoGrant(Session.MatchId, grantedXp);
                grantedXp = 0;
                return false;
            }

            Session.CommitResult(winningPlayer);
            return true;
        }

        static bool DiskAlreadyHasGrant(string matchId)
        {
            if (!AtomicFileSave.TryRead(RewardsPath, out string document, out _))
                return false;
            return MatchRewardLedger.TryParseDocument(document, out _, out System.Collections.Generic.List<string> ids)
                && ids.Contains(matchId);
        }

        public static void ResetForTests()
        {
            Session = new OfflineMatchSession();
            Rewards = new MatchRewardLedger();
        }
    }
}
