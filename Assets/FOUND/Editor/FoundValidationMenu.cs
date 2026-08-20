#if UNITY_EDITOR
using System;
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
                StateAlbumDefinition ny = catalog.GetState("NY");
                if (ny.stampDesignIds.Count != 10) throw new InvalidOperationException("New York must contain exactly 10 destination stamps in the current baseline.");
                StampDesign gold = catalog.GetDesign(ny.completionDesignId);
                if (!gold.isCompletionReward || gold.tradeable) throw new InvalidOperationException("New York Gold Foil configuration is invalid.");
                if (catalog.GetRarityRule(StampRarity.GoldFoil).rollable) throw new InvalidOperationException("Gold Foil cannot be randomly rolled.");
                if (catalog.GetRarityRule(StampRarity.Limited).editionLimit != 500) throw new InvalidOperationException("Limited Issue must be numbered 1-500.");

                FoundSaveData save = new FoundSaveData();
                save.profile.playerId = "validation-player";
                FoundCollectionService collection = new FoundCollectionService(catalog);
                FoundEconomyService economy = new FoundEconomyService();
                FoundProgressionService progression = new FoundProgressionService(catalog, collection, economy);
                FoundAcquisitionService acquisition = new FoundAcquisitionService(
                    catalog,
                    collection,
                    new FoundRarityRoller(catalog, 149),
                    new LocalEditionNumberAuthority(),
                    new FoundLocationService(),
                    progression);

                for (int i = 0; i < ny.stampDesignIds.Count; i++)
                    acquisition.Acquire(save, ny.stampDesignIds[i], AcquisitionSource.FieldRoute);

                if (!collection.IsStateComplete(save, "NY")) throw new InvalidOperationException("New York completion was not recognized.");
                if (collection.CountCopies(save, ny.completionDesignId) != 1) throw new InvalidOperationException("Gold Foil completion reward was not awarded exactly once.");
                CollectibleInstance reward = collection.GetCopies(save, ny.completionDesignId)[0];
                if (reward.rarity != StampRarity.GoldFoil || reward.hasEditionNumber) throw new InvalidOperationException("Gold Foil must be unnumbered.");

                Debug.Log("FOUND validation passed: catalog, collection, rarity, progression, and 2D Gold Foil rules are coherent.");
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                throw;
            }
        }
    }
}
#endif
