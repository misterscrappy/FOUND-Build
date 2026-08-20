using System;
using System.Collections.Generic;

namespace Found.Core
{
    public sealed class FoundRouteService
    {
        private readonly FoundCatalogService catalog;
        private readonly FoundCollectionService collection;
        private readonly FoundAcquisitionService acquisition;
        private readonly System.Random random = new System.Random();

        public FoundRouteService(FoundCatalogService catalog, FoundCollectionService collection, FoundAcquisitionService acquisition)
        {
            this.catalog = catalog ?? throw new ArgumentNullException("catalog");
            this.collection = collection ?? throw new ArgumentNullException("collection");
            this.acquisition = acquisition ?? throw new ArgumentNullException("acquisition");
        }

        public FieldRouteSession StartRoute(FoundSaveData save, string stateCode)
        {
            if (save == null) throw new ArgumentNullException("save");
            StateAlbumDefinition state = catalog.GetState(stateCode);
            if (state.stampDesignIds == null || state.stampDesignIds.Count == 0)
                throw new InvalidOperationException(state.name + " content has not been authored yet.");

            List<string> missing = new List<string>();
            for (int i = 0; i < state.stampDesignIds.Count; i++)
                if (!collection.HasDesign(save, state.stampDesignIds[i])) missing.Add(state.stampDesignIds[i]);

            // Missing destinations are favored but never become an unlock gate.
            bool chooseMissing = missing.Count > 0 && random.NextDouble() < 0.70d;
            List<string> pool = chooseMissing ? missing : state.stampDesignIds;
            string designId = pool[random.Next(0, pool.Count)];
            return new FieldRouteSession
            {
                sessionId = Guid.NewGuid().ToString("N"),
                stateCode = state.code,
                designId = designId,
                startedAtUtc = DateTime.UtcNow.ToString("o"),
                surveyProgress = 0
            };
        }

        public int AdvanceSurvey(FieldRouteSession session, int amount = 1)
        {
            if (session == null) throw new ArgumentNullException("session");
            session.surveyProgress = Math.Max(0, Math.Min(3, session.surveyProgress + Math.Max(1, amount)));
            return session.surveyProgress;
        }

        public AcquisitionResult CompleteRoute(FoundSaveData save, FieldRouteSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (session.surveyProgress < 3) throw new InvalidOperationException("Survey the route before collecting the stamp.");
            StampDesign design = catalog.GetDesign(session.designId);
            if (!string.Equals(design.stateCode, session.stateCode, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Route content mismatch.");
            return acquisition.Acquire(save, design.id, AcquisitionSource.FieldRoute);
        }
    }
}
