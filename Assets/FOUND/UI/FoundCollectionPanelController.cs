using System.Collections.Generic;
using Found.Core;
using UnityEngine;

namespace Found.UI
{
    public sealed class FoundCollectionPanelController : MonoBehaviour
    {
        [SerializeField] private string stateCode = "NY";
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StampCardView stampCardPrefab;

        private readonly List<GameObject> spawned = new List<GameObject>();

        private void OnEnable()
        {
            Rebuild();
        }

        public void SetState(string code)
        {
            stateCode = code;
            Rebuild();
        }

        public void Rebuild()
        {
            Clear();
            if (FoundGame.Instance == null || contentRoot == null || stampCardPrefab == null) return;
            List<OwnedStampSummary> owned = FoundGame.Instance.Queries.GetOwnedCopies(FoundGame.Instance.SaveData, stateCode);
            for (int i = 0; i < owned.Count; i++)
            {
                OwnedStampSummary entry = owned[i];
                StampCardView card = Instantiate(stampCardPrefab, contentRoot);
                card.Bind(FoundGame.Instance.Catalog, entry.design, entry.copy);
                spawned.Add(card.gameObject);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < spawned.Count; i++)
                if (spawned[i] != null) Destroy(spawned[i]);
            spawned.Clear();
        }
    }
}
