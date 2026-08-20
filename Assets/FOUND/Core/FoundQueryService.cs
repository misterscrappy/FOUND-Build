using System;
using System.Collections.Generic;

namespace Found.Core
{
    public sealed class StateAlbumSummary
    {
        public StateAlbumDefinition state;
        public int foundCount;
        public int totalCount;
        public bool complete;
        public bool contentAvailable;
    }

    public sealed class OwnedStampSummary
    {
        public StampDesign design;
        public CollectibleInstance copy;
        public int copyCountForDesign;
        public string rarityLabel;
        public string traitLabel;
    }

    public sealed class FoundQueryService
    {
        private readonly FoundCatalogService catalog;
        private readonly FoundCollectionService collection;

        public FoundQueryService(FoundCatalogService catalog, FoundCollectionService collection)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
            this.collection = collection ?? throw new ArgumentNullException("collection");
        }

        public List<StateAlbumSummary> GetStateAlbums(FoundSaveData save)
        {
            List<StateAlbumSummary> result = new List<StateAlbumSummary>(catalog.Data.states.Count);
            for (int i = 0; i < catalog.Data.states.Count; i++)
            {
                StateAlbumDefinition state = catalog.Data.states[i];
                int total = state.stampDesignIds == null ? 0 : state.stampDesignIds.Count;
                int found = total == 0 ? 0 : collection.CountDistinctDestinations(save, state.code);
                result.Add(new StateAlbumSummary
                {
                    state = state,
                    foundCount = found,
                    totalCount = total,
                    complete = total > 0 && found == total,
                    contentAvailable = total > 0
                });
            }
            result.Sort((a, b) => string.Compare(a.state.name, b.state.name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public List<OwnedStampSummary> GetOwnedCopies(FoundSaveData save, string stateCode = null)
        {
            List<OwnedStampSummary> result = new List<OwnedStampSummary>();
            if (save == null) return result;
            for (int i = 0; i < save.collection.Count; i++)
            {
                CollectionBucket bucket = save.collection[i];
                if (bucket == null || bucket.copies == null) continue;
                StampDesign design;
                if (!catalog.TryGetDesign(bucket.designId, out design)) continue;
                if (!string.IsNullOrWhiteSpace(stateCode) && !string.Equals(design.stateCode, stateCode, StringComparison.OrdinalIgnoreCase)) continue;
                for (int j = 0; j < bucket.copies.Count; j++)
                {
                    CollectibleInstance copy = bucket.copies[j];
                    if (copy == null) continue;
                    result.Add(new OwnedStampSummary
                    {
                        design = design,
                        copy = copy,
                        copyCountForDesign = bucket.copies.Count,
                        rarityLabel = catalog.GetRarityRule(copy.rarity).displayName,
                        traitLabel = copy.bonusTrait == BonusTrait.None ? string.Empty : catalog.GetTraitRule(copy.bonusTrait).displayName
                    });
                }
            }
            result.Sort(CompareOwned);
            return result;
        }

        public StampDesign GetNearestDestination(string stateCode, double latitude, double longitude)
        {
            List<StampDesign> designs = catalog.GetDestinationDesigns(stateCode);
            StampDesign nearest = null;
            double best = double.MaxValue;
            for (int i = 0; i < designs.Count; i++)
            {
                StampDesign design = designs[i];
                if (design.coordinates == null) continue;
                double miles = FoundLocationService.HaversineMiles(latitude, longitude, design.coordinates.latitude, design.coordinates.longitude);
                if (miles < best)
                {
                    best = miles;
                    nearest = design;
                }
            }
            return nearest;
        }

        private int CompareOwned(OwnedStampSummary left, OwnedStampSummary right)
        {
            int state = string.Compare(left.design.stateCode, right.design.stateCode, StringComparison.OrdinalIgnoreCase);
            if (state != 0) return state;
            int album = left.design.albumNumber.CompareTo(right.design.albumNumber);
            if (album != 0) return album;
            int rarity = catalog.GetRarityRule(right.copy.rarity).rank.CompareTo(catalog.GetRarityRule(left.copy.rarity).rank);
            if (rarity != 0) return rarity;
            return string.Compare(right.copy.foundAtUtc, left.copy.foundAtUtc, StringComparison.Ordinal);
        }
    }
}
