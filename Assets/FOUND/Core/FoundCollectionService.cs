using System;
using System.Collections.Generic;

namespace Found.Core
{
    public sealed class FoundCollectionService
    {
        private readonly FoundCatalogService catalog;

        public FoundCollectionService(FoundCatalogService catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
        }

        public List<CollectibleInstance> GetCopies(FoundSaveData save, string designId)
        {
            CollectionBucket bucket = FindBucket(save, designId);
            return bucket == null ? new List<CollectibleInstance>() : new List<CollectibleInstance>(bucket.copies);
        }

        public int CountCopies(FoundSaveData save, string designId)
        {
            CollectionBucket bucket = FindBucket(save, designId);
            return bucket == null || bucket.copies == null ? 0 : bucket.copies.Count;
        }

        public bool HasDesign(FoundSaveData save, string designId)
        {
            return CountCopies(save, designId) > 0;
        }

        public CollectibleInstance FindOwnedInstance(FoundSaveData save, string instanceId, out StampDesign design)
        {
            design = null;
            if (save == null || string.IsNullOrWhiteSpace(instanceId)) return null;
            for (int i = 0; i < save.collection.Count; i++)
            {
                CollectionBucket bucket = save.collection[i];
                if (bucket == null || bucket.copies == null) continue;
                for (int j = 0; j < bucket.copies.Count; j++)
                {
                    CollectibleInstance copy = bucket.copies[j];
                    if (copy != null && string.Equals(copy.instanceId, instanceId, StringComparison.Ordinal))
                    {
                        catalog.TryGetDesign(bucket.designId, out design);
                        return copy;
                    }
                }
            }
            return null;
        }

        public bool AddCopy(FoundSaveData save, CollectibleInstance copy)
        {
            if (save == null) throw new ArgumentNullException("save");
            if (copy == null || string.IsNullOrWhiteSpace(copy.instanceId) || string.IsNullOrWhiteSpace(copy.designId))
                throw new ArgumentException("Invalid collectible instance.");
            StampDesign ignored;
            catalog.GetDesign(copy.designId);
            if (FindOwnedInstance(save, copy.instanceId, out ignored) != null) return false;

            CollectionBucket bucket = GetOrCreateBucket(save, copy.designId);
            if (bucket.copies == null) bucket.copies = new List<CollectibleInstance>();
            bucket.copies.Add(copy);
            return true;
        }

        public CollectibleInstance RemoveCopy(FoundSaveData save, string instanceId)
        {
            if (save == null || string.IsNullOrWhiteSpace(instanceId)) return null;
            for (int i = 0; i < save.collection.Count; i++)
            {
                CollectionBucket bucket = save.collection[i];
                if (bucket == null || bucket.copies == null) continue;
                for (int j = 0; j < bucket.copies.Count; j++)
                {
                    CollectibleInstance copy = bucket.copies[j];
                    if (copy != null && string.Equals(copy.instanceId, instanceId, StringComparison.Ordinal))
                    {
                        bucket.copies.RemoveAt(j);
                        return copy;
                    }
                }
            }
            return null;
        }

        public int CountDistinctDestinations(FoundSaveData save, string stateCode)
        {
            StateAlbumDefinition state = catalog.GetState(stateCode);
            int found = 0;
            for (int i = 0; i < state.stampDesignIds.Count; i++)
                if (HasDesign(save, state.stampDesignIds[i])) found++;
            return found;
        }

        public bool IsStateComplete(FoundSaveData save, string stateCode)
        {
            StateAlbumDefinition state = catalog.GetState(stateCode);
            return state.stampDesignIds.Count > 0 && CountDistinctDestinations(save, stateCode) == state.stampDesignIds.Count;
        }

        public bool IsFirstDiscovery(FoundSaveData save, string designId)
        {
            if (save == null) return true;
            for (int i = 0; i < save.discoveries.Count; i++)
                if (string.Equals(save.discoveries[i].designId, designId, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        public void RecordDiscovery(FoundSaveData save, string designId, string atUtc)
        {
            if (!IsFirstDiscovery(save, designId)) return;
            save.discoveries.Add(new DiscoveryRecord { designId = designId, firstFoundAtUtc = atUtc });
        }

        public bool CanTrade(FoundSaveData save, CollectibleInstance copy, out string reason)
        {
            reason = null;
            if (copy == null) { reason = "Stamp not found."; return false; }
            StampDesign design = catalog.GetDesign(copy.designId);
            if (!design.tradeable || design.isCompletionReward || copy.rarity == StampRarity.GoldFoil)
            {
                reason = "Completion rewards cannot be traded.";
                return false;
            }
            if (FoundRules.RequireRetainedDesignCopyForTrade && CountCopies(save, copy.designId) < 2)
            {
                reason = "Keep at least one copy of this destination in your album.";
                return false;
            }
            return true;
        }

        public List<CollectibleInstance> GetTradeableCopies(FoundSaveData save)
        {
            List<CollectibleInstance> result = new List<CollectibleInstance>();
            if (save == null) return result;
            for (int i = 0; i < save.collection.Count; i++)
            {
                CollectionBucket bucket = save.collection[i];
                if (bucket == null || bucket.copies == null) continue;
                for (int j = 0; j < bucket.copies.Count; j++)
                {
                    string reason;
                    if (CanTrade(save, bucket.copies[j], out reason)) result.Add(bucket.copies[j]);
                }
            }
            return result;
        }

        private static CollectionBucket FindBucket(FoundSaveData save, string designId)
        {
            if (save == null || save.collection == null) return null;
            for (int i = 0; i < save.collection.Count; i++)
            {
                CollectionBucket bucket = save.collection[i];
                if (bucket != null && string.Equals(bucket.designId, designId, StringComparison.OrdinalIgnoreCase)) return bucket;
            }
            return null;
        }

        private static CollectionBucket GetOrCreateBucket(FoundSaveData save, string designId)
        {
            CollectionBucket existing = FindBucket(save, designId);
            if (existing != null) return existing;
            CollectionBucket bucket = new CollectionBucket { designId = designId };
            save.collection.Add(bucket);
            return bucket;
        }
    }
}
