using System;
using System.Collections.Generic;

namespace Found.Core
{
    public interface IEditionNumberAuthority
    {
        bool TryAllocate(FoundSaveData save, string designId, StampRarity rarity, int limit, out int editionNumber);
    }

    // Development-safe local allocator. It never duplicates an edition number on the same save.
    // Production global uniqueness must replace this with a server-backed authority.
    public sealed class LocalEditionNumberAuthority : IEditionNumberAuthority
    {
        public bool TryAllocate(FoundSaveData save, string designId, StampRarity rarity, int limit, out int editionNumber)
        {
            editionNumber = 0;
            if (save == null || limit <= 0) return false;
            HashSet<int> used = new HashSet<int>();
            for (int i = 0; i < save.collection.Count; i++)
            {
                CollectionBucket bucket = save.collection[i];
                if (bucket == null || !string.Equals(bucket.designId, designId, StringComparison.OrdinalIgnoreCase) || bucket.copies == null) continue;
                for (int j = 0; j < bucket.copies.Count; j++)
                {
                    CollectibleInstance copy = bucket.copies[j];
                    if (copy != null && copy.rarity == rarity && copy.hasEditionNumber) used.Add(copy.editionNumber);
                }
            }
            for (int candidate = 1; candidate <= limit; candidate++)
            {
                if (!used.Contains(candidate))
                {
                    editionNumber = candidate;
                    return true;
                }
            }
            return false;
        }
    }

    public sealed class FoundRarityRoller
    {
        private readonly FoundCatalogService catalog;
        private readonly System.Random random;

        public FoundRarityRoller(FoundCatalogService catalog, int? seed = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
            random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public StampRarity RollRarity(bool allowLimited = true)
        {
            List<RarityRule> candidates = new List<RarityRule>();
            float total = 0f;
            for (int i = 0; i < catalog.Data.rarities.Count; i++)
            {
                RarityRule rule = catalog.Data.rarities[i];
                StampRarity rarity = rule.ParsedRarity;
                if (!rule.rollable || rule.weight <= 0f) continue;
                if (!allowLimited && rarity == StampRarity.Limited) continue;
                candidates.Add(rule);
                total += rule.weight;
            }
            if (candidates.Count == 0 || total <= 0f) return StampRarity.Standard;
            double roll = random.NextDouble() * total;
            float cursor = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                cursor += candidates[i].weight;
                if (roll <= cursor) return candidates[i].ParsedRarity;
            }
            return candidates[candidates.Count - 1].ParsedRarity;
        }

        public BonusTrait RollTrait()
        {
            float total = 0f;
            for (int i = 0; i < catalog.Data.traits.Count; i++) total += Math.Max(0f, catalog.Data.traits[i].weight);
            if (total <= 0f) return BonusTrait.None;
            double roll = random.NextDouble() * total;
            float cursor = 0f;
            for (int i = 0; i < catalog.Data.traits.Count; i++)
            {
                cursor += Math.Max(0f, catalog.Data.traits[i].weight);
                if (roll <= cursor) return catalog.Data.traits[i].ParsedTrait;
            }
            return BonusTrait.None;
        }
    }

    public sealed class FoundLocationService
    {
        public bool IsInsideLocalPostmarkZone(StampDesign design, double latitude, double longitude)
        {
            if (design == null || design.coordinates == null) return false;
            double miles = HaversineMiles(latitude, longitude, design.coordinates.latitude, design.coordinates.longitude);
            return miles <= Math.Max(0.1f, design.coordinates.radiusMiles);
        }

        public LocalPostmark TryCreatePostmark(StampDesign design, double latitude, double longitude, bool test = false)
        {
            if (!IsInsideLocalPostmarkZone(design, latitude, longitude)) return null;
            return new LocalPostmark
            {
                place = design.placeName,
                dateUtc = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                test = test
            };
        }

        public static double HaversineMiles(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthMiles = 3958.7613;
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2d) * Math.Sin(dLat / 2d)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2d) * Math.Sin(dLon / 2d);
            double c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
            return earthMiles * c;
        }

        private static double DegreesToRadians(double degrees) { return degrees * Math.PI / 180d; }
    }

    public sealed class FoundEconomyService
    {
        public void AddCoins(FoundSaveData save, int amount, string label)
        {
            if (save == null || amount <= 0) return;
            save.profile.coins = Math.Max(0, save.profile.coins) + amount;
            save.profile.totalEarned = Math.Max(0, save.profile.totalEarned) + amount;
            AddLedger(save, amount, label);
        }

        public void GrantXp(FoundSaveData save, int amount)
        {
            if (save == null || amount <= 0) return;
            save.profile.level = Math.Max(1, save.profile.level);
            save.profile.xp = Math.Max(0, save.profile.xp) + amount;
            while (save.profile.xp >= FoundRules.XpNeededForLevel(save.profile.level))
            {
                save.profile.xp -= FoundRules.XpNeededForLevel(save.profile.level);
                save.profile.level++;
                AddCoins(save, FoundRules.LevelCoinReward, "Collector level " + save.profile.level);
            }
        }

        private static void AddLedger(FoundSaveData save, int amount, string label)
        {
            save.ledger.Insert(0, new LedgerEntry
            {
                id = Guid.NewGuid().ToString("N"),
                atUtc = DateTime.UtcNow.ToString("o"),
                amount = amount,
                label = string.IsNullOrWhiteSpace(label) ? "FOUND activity" : label
            });
            if (save.ledger.Count > FoundRules.MaxLedgerEntries)
                save.ledger.RemoveRange(FoundRules.MaxLedgerEntries, save.ledger.Count - FoundRules.MaxLedgerEntries);
        }
    }

    public sealed class FoundProgressionService
    {
        private readonly FoundCatalogService catalog;
        private readonly FoundCollectionService collection;
        private readonly FoundEconomyService economy;

        public FoundProgressionService(FoundCatalogService catalog, FoundCollectionService collection, FoundEconomyService economy)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
            this.collection = collection ?? throw new ArgumentNullException("collection");
            this.economy = economy ?? throw new ArgumentNullException("economy");
        }

        public CollectibleInstance OnCopyAdded(FoundSaveData save, StampDesign design, CollectibleInstance copy, bool firstDiscovery)
        {
            if (firstDiscovery && !design.isCompletionReward)
            {
                collection.RecordDiscovery(save, design.id, copy.foundAtUtc);
                economy.AddCoins(save, FoundRules.DiscoveryCoinReward, "New discovery: " + design.placeName);
                economy.GrantXp(save, FoundRules.DiscoveryXpReward);
            }

            StateProgress progress = GetOrCreateProgress(save, design.stateCode);
            int found = collection.CountDistinctDestinations(save, design.stateCode);
            StateAlbumDefinition state = catalog.GetState(design.stateCode);
            int total = state.stampDesignIds.Count;
            int[] milestones = FoundRules.MilestoneCountsFor(total);
            for (int i = 0; i < milestones.Length; i++)
            {
                int count = milestones[i];
                if (found < count || progress.claimedMilestoneCounts.Contains(count)) continue;
                progress.claimedMilestoneCounts.Add(count);
                int coins;
                int xp;
                string label;
                FoundRules.MilestoneReward(count, total, out coins, out xp, out label);
                economy.AddCoins(save, coins, state.name + " • " + label);
                economy.GrantXp(save, xp);
            }

            return TryAwardGoldFoil(save, state, progress);
        }

        public CollectibleInstance TryAwardGoldFoil(FoundSaveData save, StateAlbumDefinition state, StateProgress progress = null)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.completionDesignId) || state.stampDesignIds.Count == 0) return null;
            if (!collection.IsStateComplete(save, state.code)) return null;
            if (progress == null) progress = GetOrCreateProgress(save, state.code);
            if (progress.completionAwarded || collection.HasDesign(save, state.completionDesignId))
            {
                progress.completionAwarded = true;
                return null;
            }

            StampDesign completionDesign = catalog.GetDesign(state.completionDesignId);
            string now = DateTime.UtcNow.ToString("o");
            CollectibleInstance reward = new CollectibleInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                designId = completionDesign.id,
                rarity = StampRarity.GoldFoil,
                bonusTrait = BonusTrait.None,
                editionNumber = 0,
                hasEditionNumber = false,
                foundAtUtc = now,
                source = AcquisitionSource.Completion,
                postmark = null
            };
            reward.provenance.Add(new ProvenanceEvent
            {
                eventId = Guid.NewGuid().ToString("N"),
                action = "completion-award",
                atUtc = now,
                toPlayerId = save.profile.playerId
            });
            reward.value = FoundRules.CalculateValue(completionDesign, reward, catalog);
            collection.AddCopy(save, reward);
            collection.RecordDiscovery(save, completionDesign.id, now);
            progress.completionAwarded = true;
            return reward;
        }

        private static StateProgress GetOrCreateProgress(FoundSaveData save, string stateCode)
        {
            for (int i = 0; i < save.stateProgress.Count; i++)
            {
                StateProgress entry = save.stateProgress[i];
                if (entry != null && string.Equals(entry.stateCode, stateCode, StringComparison.OrdinalIgnoreCase))
                {
                    if (entry.claimedMilestoneCounts == null) entry.claimedMilestoneCounts = new List<int>();
                    return entry;
                }
            }
            StateProgress created = new StateProgress { stateCode = stateCode };
            save.stateProgress.Add(created);
            return created;
        }
    }

    public sealed class FoundAcquisitionService
    {
        private readonly FoundCatalogService catalog;
        private readonly FoundCollectionService collection;
        private readonly FoundRarityRoller roller;
        private readonly IEditionNumberAuthority editions;
        private readonly FoundLocationService locations;
        private readonly FoundProgressionService progression;

        public FoundAcquisitionService(
            FoundCatalogService catalog,
            FoundCollectionService collection,
            FoundRarityRoller roller,
            IEditionNumberAuthority editions,
            FoundLocationService locations,
            FoundProgressionService progression)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
            this.collection = collection ?? throw new ArgumentNullException("collection");
            this.roller = roller ?? throw new ArgumentNullException("roller");
            this.editions = editions ?? throw new ArgumentNullException("editions");
            this.locations = locations ?? throw new ArgumentNullException("locations");
            this.progression = progression ?? throw new ArgumentNullException("progression");
        }

        public AcquisitionResult Acquire(FoundSaveData save, string designId, AcquisitionSource source, double? latitude = null, double? longitude = null, bool testLocation = false)
        {
            if (save == null) throw new ArgumentNullException("save");
            StampDesign design = catalog.GetDesign(designId);
            if (design.isCompletionReward) throw new InvalidOperationException("Completion stamps are awarded by album completion and cannot be rolled.");
            if (source == AcquisitionSource.Trade || source == AcquisitionSource.Completion)
                throw new InvalidOperationException("Use the trade/completion service for that acquisition source.");

            StampRarity rarity = roller.RollRarity(true);
            int edition = 0;
            bool hasEdition = false;
            if (rarity == StampRarity.Limited)
            {
                RarityRule limited = catalog.GetRarityRule(StampRarity.Limited);
                if (editions.TryAllocate(save, design.id, rarity, limited.editionLimit, out edition)) hasEdition = true;
                else rarity = roller.RollRarity(false);
            }

            LocalPostmark postmark = null;
            if (source == AcquisitionSource.CheckIn && latitude.HasValue && longitude.HasValue)
                postmark = locations.TryCreatePostmark(design, latitude.Value, longitude.Value, testLocation);

            string now = DateTime.UtcNow.ToString("o");
            CollectibleInstance copy = new CollectibleInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                designId = design.id,
                rarity = rarity,
                bonusTrait = roller.RollTrait(),
                editionNumber = edition,
                hasEditionNumber = hasEdition,
                foundAtUtc = now,
                source = source,
                postmark = postmark
            };
            copy.provenance.Add(new ProvenanceEvent
            {
                eventId = Guid.NewGuid().ToString("N"),
                action = "found",
                atUtc = now,
                toPlayerId = save.profile.playerId
            });
            copy.value = FoundRules.CalculateValue(design, copy, catalog);

            bool first = collection.IsFirstDiscovery(save, design.id);
            if (!collection.AddCopy(save, copy)) throw new InvalidOperationException("Duplicate collectible instance id generated.");
            CollectibleInstance completion = progression.OnCopyAdded(save, design, copy, first);
            return new AcquisitionResult { copy = copy, firstDiscovery = first, completionAward = completion };
        }
    }
}
