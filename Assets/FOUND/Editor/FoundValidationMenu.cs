#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Found.Core;
using UnityEditor;
using UnityEngine;

namespace Found.EditorTools
{
    public static class FoundValidationMenu
    {
        [MenuItem("FOUND/Validate Core Content")]
        public static void ValidateCoreContent()
        {
            try
            {
                FoundCatalogService catalog = FoundCatalogService.LoadDefault();
                if (catalog.Data.states.Count != 50) throw new InvalidOperationException("FOUND must register all 50 states.");
                StateAlbumDefinition ny = catalog.GetState("NY");
                if (ny.stampDesignIds.Count != 10) throw new InvalidOperationException("New York must contain exactly 10 destination stamps in the current baseline.");
                StampDesign gold = catalog.GetDesign(ny.completionDesignId);
                if (!gold.isCompletionReward || gold.tradeable) throw new InvalidOperationException("New York Gold Foil configuration is invalid.");
                if (catalog.GetRarityRule(StampRarity.GoldFoil).rollable) throw new InvalidOperationException("Gold Foil cannot be randomly rolled.");
                if (catalog.GetRarityRule(StampRarity.Limited).editionLimit != 500) throw new InvalidOperationException("Limited Issue must be numbered 1-500.");

                FoundSaveData save = NewSave("validation-player");
                FoundCollectionService collection = new FoundCollectionService(catalog);
                FoundEconomyService economy = new FoundEconomyService();
                FoundProgressionService progression = new FoundProgressionService(catalog, collection, economy);
                FoundLocationService location = new FoundLocationService();
                FoundAcquisitionService acquisition = new FoundAcquisitionService(
                    catalog,
                    collection,
                    new FoundRarityRoller(catalog, 149),
                    new LocalEditionNumberAuthority(),
                    location,
                    progression);

                for (int i = 0; i < ny.stampDesignIds.Count; i++)
                    acquisition.Acquire(save, ny.stampDesignIds[i], AcquisitionSource.FieldRoute);

                if (!collection.IsStateComplete(save, "NY")) throw new InvalidOperationException("New York completion was not recognized.");
                if (collection.CountCopies(save, ny.completionDesignId) != 1) throw new InvalidOperationException("Gold Foil completion reward was not awarded exactly once.");
                CollectibleInstance reward = collection.GetCopies(save, ny.completionDesignId)[0];
                if (reward.rarity != StampRarity.GoldFoil || reward.hasEditionNumber) throw new InvalidOperationException("Gold Foil must be unnumbered.");

                StampDesign syracuse = catalog.GetDesign("NY-001");
                LocalPostmark localMark = location.TryCreatePostmark(syracuse, 43.0481, -76.1474);
                if (localMark == null || localMark.place != "Syracuse") throw new InvalidOperationException("Local Postmark failed inside Syracuse zone.");
                if (location.TryCreatePostmark(syracuse, 40.7128, -74.0060) != null) throw new InvalidOperationException("Local Postmark incorrectly awarded outside Syracuse zone.");

                FoundSaveData sender = NewSave("sender");
                acquisition.Acquire(sender, "NY-001", AcquisitionSource.FieldRoute);
                acquisition.Acquire(sender, "NY-001", AcquisitionSource.FieldRoute);
                List<CollectibleInstance> senderCopies = collection.GetCopies(sender, "NY-001");
                string transferInstanceId = senderCopies[1].instanceId;
                FoundTradeService trading = new FoundTradeService(catalog, collection, progression);
                string code = trading.CreateDirectTradeCode(sender, new[] { transferInstanceId });
                if (collection.CountCopies(sender, "NY-001") != 1) throw new InvalidOperationException("Outgoing trade did not remove exactly one duplicate.");

                FoundSaveData receiver = NewSave("receiver");
                List<CollectibleInstance> received = trading.RedeemDirectTradeCode(receiver, code);
                if (received.Count != 1 || received[0].instanceId != transferInstanceId) throw new InvalidOperationException("Trade did not preserve exact collectible identity.");
                if (received[0].provenance == null || received[0].provenance.Count < 2) throw new InvalidOperationException("Trade provenance was not preserved.");

                Debug.Log("FOUND validation passed: 50-state registry, catalog, collecting, local postmarks, trading, progression, and 2D Gold Foil rules are coherent.");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                throw;
            }
        }

        private static FoundSaveData NewSave(string playerId)
        {
            FoundSaveData save = new FoundSaveData();
            save.profile.playerId = playerId;
            return save;
        }
    }
}
#endif
