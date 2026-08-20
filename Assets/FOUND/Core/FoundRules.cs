using System;
using System.Collections.Generic;

namespace Found.Core
{
    public static class FoundRules
    {
        public const int DiscoveryCoinReward = 150;
        public const int DiscoveryXpReward = 75;
        public const int LevelCoinReward = 500;
        public const int MaxLedgerEntries = 100;
        public const int MaxTradeHistory = 100;
        public const int MaxProvenanceEvents = 30;
        public const bool RequireRetainedDesignCopyForTrade = true;

        public static int XpNeededForLevel(int level)
        {
            return 400 + (Math.Max(1, level) - 1) * 250;
        }

        public static int CalculateValue(StampDesign design, CollectibleInstance copy, FoundCatalogService catalog)
        {
            if (design == null || copy == null || catalog == null) return 1;
            float rarity = catalog.GetRarityRule(copy.rarity).valueMultiplier;
            float trait = catalog.GetTraitRule(copy.bonusTrait).valueMultiplier;
            float local = copy.postmark != null ? 1.25f : 1f;
            return Math.Max(1, (int)Math.Round(Math.Max(1, design.baseValue) * rarity * trait * local));
        }

        public static int[] MilestoneCountsFor(int totalDestinationCount)
        {
            if (totalDestinationCount <= 0) return new int[0];
            HashSet<int> counts = new HashSet<int>();
            counts.Add(Math.Max(1, (int)Math.Ceiling(totalDestinationCount * 0.30f)));
            counts.Add(Math.Max(1, (int)Math.Ceiling(totalDestinationCount * 0.50f)));
            counts.Add(Math.Max(1, (int)Math.Ceiling(totalDestinationCount * 0.80f)));
            counts.Add(totalDestinationCount);
            int[] result = new int[counts.Count];
            counts.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        public static void MilestoneReward(int count, int total, out int coins, out int xp, out string label)
        {
            float ratio = total <= 0 ? 0f : (float)count / total;
            if (ratio >= 0.999f)
            {
                coins = 1500;
                xp = 200;
                label = "Album Complete";
            }
            else if (ratio >= 0.79f)
            {
                coins = 900;
                xp = 120;
                label = "Route Explorer";
            }
            else if (ratio >= 0.49f)
            {
                coins = 600;
                xp = 80;
                label = "Halfway Through";
            }
            else
            {
                coins = 300;
                xp = 50;
                label = "Route Started";
            }
        }
    }
}
