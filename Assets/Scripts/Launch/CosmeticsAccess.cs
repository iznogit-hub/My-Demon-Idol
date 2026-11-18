namespace DemonIdol.Launch
{
    public readonly struct CosmeticTier
    {
        public readonly int Tier;
        public readonly string RewardId;
        public readonly bool Premium;

        public CosmeticTier(int tier, string rewardId, bool premium)
        {
            Tier = tier;
            RewardId = rewardId;
            Premium = premium;
        }
    }

    /// <summary>
    /// Cosmetic-only 30-tier track. Paid claims stay closed until a receipt provider exists.
    /// The sandbox flag is development-only and is forced off when a release define is set.
    /// </summary>
    public static class CosmeticsAccess
    {
        public const int TierCount = 30;
        public const string StoreUnavailableReason = "Store purchases are unavailable. No receipt provider is configured.";

        public static bool StoreConfigured => false;

        public static bool SandboxAllowed(bool developmentBuild, bool releaseDefine)
        {
            return developmentBuild && !releaseDefine;
        }

        public static CosmeticTier GetTier(int tier)
        {
            int clamped = tier < 1 ? 1 : (tier > TierCount ? TierCount : tier);
            bool premium = clamped % 2 == 0;
            return new CosmeticTier(clamped, premium ? "cosmetic.premium." + clamped : "cosmetic.free." + clamped, premium);
        }

        public static int UnlockedFreeTier(int cosmeticXp)
        {
            if (cosmeticXp < 25)
                return 0;
            int grants = cosmeticXp / 25;
            int highest = 0;
            for (int tier = 1; tier <= TierCount; tier++)
            {
                if (GetTier(tier).Premium)
                    continue;
                if (grants <= 0)
                    break;
                highest = tier;
                grants--;
            }
            return highest;
        }

        public static bool TryClaimPremium(int tier, bool receiptValidated, out string rejection)
        {
            CosmeticTier entry = GetTier(tier);
            if (!entry.Premium)
            {
                rejection = null;
                return true;
            }
            if (!StoreConfigured || !receiptValidated)
            {
                rejection = StoreUnavailableReason;
                return false;
            }
            rejection = null;
            return true;
        }

        public static bool TrySandboxUnlock(int tier, bool developmentBuild, bool releaseDefine, out string rejection)
        {
            if (!SandboxAllowed(developmentBuild, releaseDefine))
            {
                rejection = "Cosmetics sandbox is disabled in release builds.";
                return false;
            }
            if (tier < 1 || tier > TierCount)
            {
                rejection = "Tier is outside 1..30.";
                return false;
            }
            rejection = null;
            return true;
        }
    }
}
