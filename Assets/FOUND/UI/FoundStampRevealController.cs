using Found.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Found.UI
{
    public sealed class FoundStampRevealController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private StampCardView stampCard;
        [SerializeField] private Text discoveryText;
        [SerializeField] private Text completionText;

        private AcquisitionResult pending;

        private void OnEnable()
        {
            if (FoundGame.Instance != null) FoundGame.Instance.StampAcquired += OnStampAcquired;
        }

        private void OnDisable()
        {
            if (FoundGame.Instance != null) FoundGame.Instance.StampAcquired -= OnStampAcquired;
        }

        public void Show(AcquisitionResult result)
        {
            if (result == null || result.copy == null || FoundGame.Instance == null) return;
            pending = result;
            StampDesign design = FoundGame.Instance.Catalog.GetDesign(result.copy.designId);
            if (stampCard != null) stampCard.Bind(FoundGame.Instance.Catalog, design, result.copy);
            if (discoveryText != null)
                discoveryText.text = result.firstDiscovery ? "NEW DESTINATION DISCOVERED" : "DUPLICATE COPY ADDED";
            if (completionText != null)
            {
                bool completion = result.completionAward != null;
                completionText.gameObject.SetActive(completion);
                completionText.text = completion ? "STATE ALBUM COMPLETE • GOLD FOIL AWARDED" : string.Empty;
            }
            if (root != null) root.SetActive(true);
        }

        public void Close()
        {
            pending = null;
            if (root != null) root.SetActive(false);
        }

        public AcquisitionResult CurrentResult { get { return pending; } }

        private void OnStampAcquired(AcquisitionResult result)
        {
            Show(result);
        }
    }
}
