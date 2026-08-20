using System;
using System.Collections.Generic;
using UnityEngine;

namespace Found.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class FoundGame : MonoBehaviour
    {
        public static FoundGame Instance { get; private set; }

        public FoundCatalogService Catalog { get; private set; }
        public FoundCollectionService Collection { get; private set; }
        public FoundAcquisitionService Acquisition { get; private set; }
        public FoundTradeService Trading { get; private set; }
        public FoundRouteService Routes { get; private set; }
        public FoundQueryService Queries { get; private set; }
        public FoundLocationService Locations { get; private set; }
        public FoundEconomyService Economy { get; private set; }
        public FoundProgressionService Progression { get; private set; }
        public FoundSaveData SaveData { get; private set; }

        public event Action SaveChanged;
        public event Action<AcquisitionResult> StampAcquired;
        public event Action<IReadOnlyList<CollectibleInstance>> TradeReceived;

        private IFoundSaveRepository saveRepository;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        public void Initialize()
        {
            Catalog = FoundCatalogService.LoadDefault();
            saveRepository = new JsonFileSaveRepository();
            SaveData = saveRepository.Load();
            Collection = new FoundCollectionService(Catalog);
            Locations = new FoundLocationService();
            Economy = new FoundEconomyService();
            Progression = new FoundProgressionService(Catalog, Collection, Economy);
            Acquisition = new FoundAcquisitionService(
                Catalog,
                Collection,
                new FoundRarityRoller(Catalog),
                new LocalEditionNumberAuthority(),
                Locations,
                Progression);
            Trading = new FoundTradeService(Catalog, Collection, Progression);
            Routes = new FoundRouteService(Catalog, Collection, Acquisition);
            Queries = new FoundQueryService(Catalog, Collection);

            RepairCompletionAwards();
            SaveNow();
        }

        public FieldRouteSession StartFieldRoute(string stateCode)
        {
            return Routes.StartRoute(SaveData, stateCode);
        }

        public AcquisitionResult CompleteFieldRoute(FieldRouteSession session)
        {
            AcquisitionResult result = Routes.CompleteRoute(SaveData, session);
            SaveNow();
            if (StampAcquired != null) StampAcquired(result);
            return result;
        }

        public AcquisitionResult CollectFromFieldRoute(string designId)
        {
            AcquisitionResult result = Acquisition.Acquire(SaveData, designId, AcquisitionSource.FieldRoute);
            SaveNow();
            if (StampAcquired != null) StampAcquired(result);
            return result;
        }

        public AcquisitionResult CollectFromCheckIn(string designId, double latitude, double longitude, bool testLocation = false)
        {
            AcquisitionResult result = Acquisition.Acquire(SaveData, designId, AcquisitionSource.CheckIn, latitude, longitude, testLocation);
            SaveNow();
            if (StampAcquired != null) StampAcquired(result);
            return result;
        }

        public string CreateTradeCode(IList<string> instanceIds)
        {
            string code = Trading.CreateDirectTradeCode(SaveData, instanceIds);
            SaveNow();
            return code;
        }

        public List<CollectibleInstance> RedeemTradeCode(string code)
        {
            List<CollectibleInstance> received = Trading.RedeemDirectTradeCode(SaveData, code);
            SaveNow();
            if (TradeReceived != null) TradeReceived(received);
            return received;
        }

        public void SetDisplayName(string displayName)
        {
            string cleaned = (displayName ?? string.Empty).Trim();
            SaveData.profile.displayName = string.IsNullOrEmpty(cleaned) ? "Collector" : cleaned.Substring(0, Math.Min(32, cleaned.Length));
            SaveNow();
        }

        public void SaveNow()
        {
            saveRepository.Save(SaveData);
            if (SaveChanged != null) SaveChanged();
        }

        public void ResetLocalSave()
        {
            saveRepository.Delete();
            SaveData = saveRepository.Load();
            SaveNow();
        }

        private void RepairCompletionAwards()
        {
            for (int i = 0; i < Catalog.Data.states.Count; i++)
            {
                StateAlbumDefinition state = Catalog.Data.states[i];
                if (state == null || state.stampDesignIds == null || state.stampDesignIds.Count == 0) continue;
                Progression.TryAwardGoldFoil(SaveData, state);
            }
        }
    }
}
