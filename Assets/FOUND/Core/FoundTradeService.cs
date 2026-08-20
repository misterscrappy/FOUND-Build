using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Found.Core
{
    public sealed class FoundTradeService
    {
        private const string CodePrefix = "FOUNDTRADE2";
        private readonly FoundCatalogService catalog;
        private readonly FoundCollectionService collection;
        private readonly FoundProgressionService progression;

        public FoundTradeService(FoundCatalogService catalog, FoundCollectionService collection, FoundProgressionService progression)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
            this.collection = collection ?? throw new ArgumentNullException("collection");
            this.progression = progression ?? throw new ArgumentNullException("progression");
        }

        public string CreateDirectTradeCode(FoundSaveData save, IList<string> instanceIds)
        {
            if (save == null) throw new ArgumentNullException("save");
            if (instanceIds == null || instanceIds.Count == 0) throw new InvalidOperationException("Choose at least one stamp to trade.");

            HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, int> outgoingPerDesign = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<CollectibleInstance> outgoing = new List<CollectibleInstance>();
            for (int i = 0; i < instanceIds.Count; i++)
            {
                string instanceId = instanceIds[i];
                if (string.IsNullOrWhiteSpace(instanceId) || !uniqueIds.Add(instanceId))
                    throw new InvalidOperationException("Trade contains a duplicate or invalid stamp id.");

                StampDesign design;
                CollectibleInstance copy = collection.FindOwnedInstance(save, instanceId, out design);
                if (copy == null || design == null) throw new InvalidOperationException("A selected stamp is no longer in the collection.");
                string reason;
                if (!collection.CanTrade(save, copy, out reason)) throw new InvalidOperationException(reason);
                outgoing.Add(copy);
                int count;
                outgoingPerDesign.TryGetValue(copy.designId, out count);
                outgoingPerDesign[copy.designId] = count + 1;
            }

            if (FoundRules.RequireRetainedDesignCopyForTrade)
            {
                foreach (KeyValuePair<string, int> pair in outgoingPerDesign)
                {
                    if (collection.CountCopies(save, pair.Key) - pair.Value < 1)
                        throw new InvalidOperationException("A trade must leave at least one copy of each destination in your album.");
                }
            }

            string tradeId = Guid.NewGuid().ToString("N");
            string now = DateTime.UtcNow.ToString("o");
            DirectTradePackage package = new DirectTradePackage
            {
                tradeId = tradeId,
                createdAtUtc = now,
                senderPlayerId = save.profile.playerId
            };

            for (int i = 0; i < outgoing.Count; i++)
            {
                CollectibleInstance copy = outgoing[i];
                StampDesign design = catalog.GetDesign(copy.designId);
                CollectibleInstance transferCopy = CloneCopy(copy);
                AppendProvenance(transferCopy, "trade-out", now, save.profile.playerId, null, tradeId);
                package.stamps.Add(new StampTransferPayload { designId = design.id, copy = transferCopy });
            }

            string json = JsonUtility.ToJson(package);
            string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            string code = CodePrefix + "." + payload + "." + Digest(payload);

            // Direct codes have no central escrow, so ownership leaves this save when the code is created.
            // That prevents the sender from intentionally keeping the same exact instance locally.
            for (int i = 0; i < outgoing.Count; i++) collection.RemoveCopy(save, outgoing[i].instanceId);

            AddTradeRecord(save, new TradeRecord
            {
                tradeId = tradeId,
                direction = TradeDirection.Outgoing,
                status = TradeStatus.Created,
                atUtc = now,
                instanceIds = new List<string>(instanceIds),
                summary = BuildSummary(package.stamps)
            });
            return code;
        }

        public List<CollectibleInstance> RedeemDirectTradeCode(FoundSaveData save, string code)
        {
            if (save == null) throw new ArgumentNullException("save");
            DirectTradePackage package = DecodeAndValidate(code);
            if (string.Equals(package.senderPlayerId, save.profile.playerId, StringComparison.Ordinal))
                throw new InvalidOperationException("This trade code was created by this player profile.");
            if (save.redeemedTradeIds.Contains(package.tradeId))
                throw new InvalidOperationException("This trade code has already been redeemed by this profile.");
            if (package.stamps == null || package.stamps.Count == 0)
                throw new InvalidOperationException("Trade code contains no stamps.");

            HashSet<string> packageInstances = new HashSet<string>(StringComparer.Ordinal);
            List<StampTransferPayload> validated = new List<StampTransferPayload>();
            for (int i = 0; i < package.stamps.Count; i++)
            {
                StampTransferPayload payload = package.stamps[i];
                if (payload == null || payload.copy == null || string.IsNullOrWhiteSpace(payload.designId))
                    throw new InvalidOperationException("Trade contains an invalid stamp.");
                StampDesign design = catalog.GetDesign(payload.designId);
                if (!design.tradeable || design.isCompletionReward)
                    throw new InvalidOperationException("Trade contains a non-tradeable completion reward.");
                CollectibleInstance copy = payload.copy;
                if (!string.Equals(copy.designId, design.id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Trade stamp design identity mismatch.");
                if (string.IsNullOrWhiteSpace(copy.instanceId) || !packageInstances.Add(copy.instanceId))
                    throw new InvalidOperationException("Trade contains duplicate stamp identities.");
                StampDesign ignored;
                if (collection.FindOwnedInstance(save, copy.instanceId, out ignored) != null)
                    throw new InvalidOperationException("This exact stamp is already in your collection.");
                ValidateVariant(copy);
                if (copy.rarity == StampRarity.Limited && HasLimitedEdition(save, copy.designId, copy.editionNumber))
                    throw new InvalidOperationException("That Limited edition number already exists in your collection.");
                validated.Add(payload);
            }

            string now = DateTime.UtcNow.ToString("o");
            List<CollectibleInstance> received = new List<CollectibleInstance>();
            for (int i = 0; i < validated.Count; i++)
            {
                StampTransferPayload payload = validated[i];
                StampDesign design = catalog.GetDesign(payload.designId);
                CollectibleInstance copy = CloneCopy(payload.copy);
                bool first = collection.IsFirstDiscovery(save, design.id);
                AppendProvenance(copy, "trade-in", now, package.senderPlayerId, save.profile.playerId, package.tradeId);
                copy.source = AcquisitionSource.Trade;
                copy.value = FoundRules.CalculateValue(design, copy, catalog);
                if (!collection.AddCopy(save, copy)) throw new InvalidOperationException("Could not add traded stamp to collection.");
                progression.OnCopyAdded(save, design, copy, first);
                received.Add(copy);
            }

            save.redeemedTradeIds.Add(package.tradeId);
            AddTradeRecord(save, new TradeRecord
            {
                tradeId = package.tradeId,
                direction = TradeDirection.Incoming,
                status = TradeStatus.Redeemed,
                atUtc = now,
                instanceIds = GetInstanceIds(received),
                summary = BuildSummary(validated)
            });
            return received;
        }

        public DirectTradePackage InspectDirectTradeCode(string code)
        {
            return DecodeAndValidate(code);
        }

        private DirectTradePackage DecodeAndValidate(string code)
        {
            string[] parts = (code ?? string.Empty).Trim().Split('.');
            if (parts.Length != 3 || !string.Equals(parts[0], CodePrefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid FOUND trade code.");
            if (!string.Equals(Digest(parts[1]), parts[2], StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Trade code failed its integrity check.");

            try
            {
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                DirectTradePackage package = JsonUtility.FromJson<DirectTradePackage>(json);
                if (package == null || package.schema != "FOUND_DIRECT_TRADE" || package.version != 2 || string.IsNullOrWhiteSpace(package.tradeId))
                    throw new InvalidOperationException("Unsupported FOUND trade code.");
                return package;
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Trade code payload is not valid Base64.");
            }
        }

        private void ValidateVariant(CollectibleInstance copy)
        {
            RarityRule rarity = catalog.GetRarityRule(copy.rarity);
            catalog.GetTraitRule(copy.bonusTrait);
            if (!rarity.rollable || copy.rarity == StampRarity.GoldFoil)
                throw new InvalidOperationException("Trade contains a completion-only rarity.");
            if (copy.rarity == StampRarity.Limited)
            {
                if (!copy.hasEditionNumber || copy.editionNumber < 1 || copy.editionNumber > rarity.editionLimit)
                    throw new InvalidOperationException("Limited Issue edition number is invalid.");
            }
            else if (copy.hasEditionNumber)
            {
                throw new InvalidOperationException("Only Limited Issue stamps may carry edition numbers.");
            }
        }

        private bool HasLimitedEdition(FoundSaveData save, string designId, int editionNumber)
        {
            List<CollectibleInstance> copies = collection.GetCopies(save, designId);
            for (int i = 0; i < copies.Count; i++)
            {
                CollectibleInstance owned = copies[i];
                if (owned != null && owned.rarity == StampRarity.Limited && owned.hasEditionNumber && owned.editionNumber == editionNumber)
                    return true;
            }
            return false;
        }

        private static void AppendProvenance(CollectibleInstance copy, string action, string atUtc, string fromPlayerId, string toPlayerId, string tradeId)
        {
            if (copy.provenance == null) copy.provenance = new List<ProvenanceEvent>();
            copy.provenance.Add(new ProvenanceEvent
            {
                eventId = Guid.NewGuid().ToString("N"),
                action = action,
                atUtc = atUtc,
                fromPlayerId = fromPlayerId,
                toPlayerId = toPlayerId,
                tradeId = tradeId
            });
            if (copy.provenance.Count > FoundRules.MaxProvenanceEvents)
                copy.provenance.RemoveRange(0, copy.provenance.Count - FoundRules.MaxProvenanceEvents);
        }

        private static CollectibleInstance CloneCopy(CollectibleInstance copy)
        {
            return JsonUtility.FromJson<CollectibleInstance>(JsonUtility.ToJson(copy));
        }

        private static List<string> GetInstanceIds(List<CollectibleInstance> copies)
        {
            List<string> result = new List<string>(copies.Count);
            for (int i = 0; i < copies.Count; i++) result.Add(copies[i].instanceId);
            return result;
        }

        private static string BuildSummary(IList<StampTransferPayload> stamps)
        {
            if (stamps == null || stamps.Count == 0) return "No stamps";
            if (stamps.Count == 1) return stamps[0].designId + " • " + stamps[0].copy.rarity;
            return stamps.Count + " stamps";
        }

        private static void AddTradeRecord(FoundSaveData save, TradeRecord record)
        {
            save.trades.Insert(0, record);
            if (save.trades.Count > FoundRules.MaxTradeHistory)
                save.trades.RemoveRange(FoundRules.MaxTradeHistory, save.trades.Count - FoundRules.MaxTradeHistory);
        }

        private static string Digest(string payload)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                StringBuilder builder = new StringBuilder(32);
                for (int i = 0; i < 16; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
