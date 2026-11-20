#if UNITY_EDITOR
using System;
using System.IO;
using DemonIdol.Launch;
using UnityEngine;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Calls the production launch types. This file does not record a pass by itself.
    /// The suite reports a pass only when Run is executed inside the editor.
    /// </summary>
    public static class LaunchSystemVerification
    {
        public static void Run(Action<bool, string, string> assert)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            bool filesExist = FighterVisualCatalog.AllReferencedFilesExist(root, out string missing);
            assert(filesExist, "L1.1: Fighter visual catalog paths exist on disk", missing ?? "");
            assert(FighterVisualCatalog.Find("C01") != null && FighterVisualCatalog.Find("C06") != null,
                "L1.2: Catalog has Kai and Nox", "");
            assert(FighterVisualCatalog.All.Count == 25, "L1.3: Roster is 25 after Ren, Valen, Jin, Malakar, and Garrick were removed", "");

            var session = new OfflineMatchSession();
            bool online = session.TrySelectMode("PvP", out string onlineReason);
            assert(!online && onlineReason.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0,
                "L2.1: Online mode is rejected", onlineReason);
            assert(session.TrySelectMode("Arcade", out _), "L2.2: Arcade mode is accepted", "");
            string first = session.BeginMatch("C01", "C06", "ST01");
            assert(session.Player2Id == "C02", "L2.3: Arcade opponent comes from the arcade order, not the passed id", session.Player2Id);
            assert(session.CommitResult(1), "L2.4: First result commit succeeds", "");
            assert(!session.CommitResult(1), "L2.5: Second result commit is refused", "");
            assert(session.TryAdvanceArcade(out string next) && next == "C04", "L2.6: Arcade advances to C04", next);
            session.TrySelectMode("LocalVersus", out _);
            session.BeginMatch("C04", "C05", "ST04");
            assert(!session.P2IsCpu && session.Player2Id == "C05", "L2.7: Local Versus keeps the second human", session.Player2Id);

            var ledger = new MatchRewardLedger();
            bool firstGrant = ledger.TryGrant(first, PlayModeKind.Arcade, out int xp);
            bool secondGrant = ledger.TryGrant(first, PlayModeKind.Arcade, out int xpAgain);
            assert(firstGrant && xp == 25 && !secondGrant && xpAgain == 0 && ledger.CosmeticXp == 25,
                "L3.1: Cosmetic XP is granted once per match id", "xp=" + ledger.CosmeticXp);
            ledger.TryGrant("trainingMatch", PlayModeKind.Training, out int trainingXp);
            assert(trainingXp == 0 && ledger.WasGranted("trainingMatch"),
                "L3.2: Training records the match and grants no XP", "");
            var restored = new MatchRewardLedger();
            assert(restored.TryLoadDocument(ledger.ToDocument()), "L3.3: Valid ledger document loads", "");
            assert(restored.CosmeticXp == 25 && restored.WasGranted(first) && !restored.TryGrant(first, PlayModeKind.Story, out _),
                "L3.3: Reloaded ledger still refuses the same match id", "");
            assert(!restored.TryLoadDocument("{\"version\":1,\"cosmeticXp\":5,\"matchIds\":[}"),
                "L3.4: Malformed ledger is rejected", "");
            assert(restored.CosmeticXp == 25 && restored.WasGranted(first),
                "L3.5: Rejected document does not clear a loaded ledger", "");

            string dir = Path.Combine(Path.GetTempPath(), "demonidol-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "story.json");
                AtomicFileSave.Write(path, "{\"version\":1}");
                AtomicFileSave.Write(path, "{\"version\":2}");
                assert(File.Exists(path + ".bak"), "L4.1: Second write keeps a backup", "");
                File.WriteAllText(path, "{\"version\":1,}");
                bool read = AtomicFileSave.TryRead(path, out string recovered, out bool fromBackup);
                assert(read && fromBackup && recovered.Contains("\"version\":1") && !recovered.Contains("\"version\":2"),
                    "L4.2: Trailing-comma document falls back to the backup", recovered);
                assert(!JsonSyntax.IsValidDocument("{\"cosmeticXp\":1,\"matchIds\":[}")
                    && !MatchRewardLedger.TryParseDocument("{\"version\":1,\"cosmeticXp\":\"25\",\"matchIds\":[]}", out _, out _),
                    "L4.3: Broken arrays and string XP are rejected", "");
            }
            finally
            {
                Directory.Delete(dir, true);
            }

            assert(GraphicsProfiles.ScaleForTier(0, 1f) == GraphicsProfiles.LowScale
                && GraphicsProfiles.ScaleForTier(1, 1f) == GraphicsProfiles.BalancedScale
                && GraphicsProfiles.ScaleForTier(2, 1f) == GraphicsProfiles.HighScale
                && Math.Abs(GraphicsProfiles.ScaleForTier(3, 0.92f) - 0.92f) < 0.001f,
                "L5.1: Low/Balanced/High/Custom resolution scales", "");
            assert(!CosmeticsAccess.StoreConfigured, "L6.1: Store provider is not configured", "");
            assert(!CosmeticsAccess.TryClaimPremium(2, false, out string storeReason)
                && storeReason == CosmeticsAccess.StoreUnavailableReason,
                "L6.2: Premium claim stays closed without a receipt", storeReason);
            assert(!CosmeticsAccess.SandboxAllowed(true, true) && CosmeticsAccess.SandboxAllowed(true, false),
                "L6.3: Release define strips the cosmetics sandbox", "");
            assert(CosmeticsAccess.TierCount == 30 && CosmeticsAccess.UnlockedFreeTier(25) == 1,
                "L6.4: Thirty cosmetic tiers and the first free tier at 25 XP", "");
        }
    }
}
#endif
