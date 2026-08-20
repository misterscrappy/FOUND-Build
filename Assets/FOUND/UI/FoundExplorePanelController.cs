using System;
using System.Collections;
using Found.Core;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Found.UI
{
    public sealed class FoundExplorePanelController : MonoBehaviour
    {
        [SerializeField] private string stateCode = "NY";
        [SerializeField] private Text albumProgressText;
        [SerializeField] private Text locationText;
        [SerializeField] private Text nearbyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button checkInButton;
        [SerializeField] private float locationTimeoutSeconds = 20f;

        private StampDesign nearest;
        private double latitude;
        private double longitude;
        private bool hasLocation;
        private Coroutine locationRoutine;

        private void OnEnable()
        {
            RefreshAlbumProgress();
            BeginLocationRefresh();
        }

        private void OnDisable()
        {
            if (locationRoutine != null) StopCoroutine(locationRoutine);
            locationRoutine = null;
            if (Input.location.status == LocationServiceStatus.Running) Input.location.Stop();
        }

        public void SetState(string code)
        {
            stateCode = code;
            RefreshAlbumProgress();
            BeginLocationRefresh();
        }

        public void BeginLocationRefresh()
        {
            if (locationRoutine != null) StopCoroutine(locationRoutine);
            locationRoutine = StartCoroutine(RefreshLocation());
        }

        public void CheckIn()
        {
            try
            {
                if (!hasLocation || nearest == null) throw new InvalidOperationException("Current location is not ready.");
                if (!FoundGame.Instance.Locations.IsInsideLocalPostmarkZone(nearest, latitude, longitude))
                    throw new InvalidOperationException("No Local Postmark is available here. Ordinary stamps remain collectable through Field Routes anywhere.");

                AcquisitionResult result = FoundGame.Instance.CollectFromCheckIn(nearest.id, latitude, longitude);
                string rarity = FoundGame.Instance.Catalog.GetRarityRule(result.copy.rarity).displayName;
                SetStatus("LOCAL POSTMARK • " + nearest.placeName + " • " + rarity);
                RefreshAlbumProgress();
            }
            catch (Exception error)
            {
                SetStatus(error.Message);
            }
        }

        private IEnumerator RefreshLocation()
        {
            hasLocation = false;
            nearest = null;
            UpdateCheckInButton();
            SetStatus("Checking location…");

#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
                float permissionWait = 0f;
                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && permissionWait < 8f)
                {
                    permissionWait += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
#endif
            if (!Input.location.isEnabledByUser)
            {
                SetStatus("Location is disabled. Enable device location to use Local Postmark check-ins.");
                yield break;
            }

            Input.location.Start(10f, 10f);
            float elapsed = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && elapsed < locationTimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                SetStatus("Could not get a current location. Field Routes still work without location access.");
                yield break;
            }

            LocationInfo info = Input.location.lastData;
            latitude = info.latitude;
            longitude = info.longitude;
            hasLocation = true;
            nearest = FoundGame.Instance.Queries.GetNearestDestination(stateCode, latitude, longitude);
            if (locationText != null) locationText.text = latitude.ToString("F4") + ", " + longitude.ToString("F4");

            if (nearest == null)
            {
                if (nearbyText != null) nearbyText.text = "NO AUTHORED DESTINATIONS";
                SetStatus("This state does not have collectible content yet.");
            }
            else
            {
                double miles = FoundLocationService.HaversineMiles(latitude, longitude, nearest.coordinates.latitude, nearest.coordinates.longitude);
                bool local = FoundGame.Instance.Locations.IsInsideLocalPostmarkZone(nearest, latitude, longitude);
                if (nearbyText != null)
                    nearbyText.text = nearest.placeName.ToUpperInvariant() + " • " + miles.ToString("F1") + " MI" + (local ? " • LOCAL POSTMARK" : string.Empty);
                SetStatus(local ? "Local Postmark available." : "Nearest Local Postmark is outside its collection radius.");
            }
            UpdateCheckInButton();
            Input.location.Stop();
            locationRoutine = null;
        }

        private void RefreshAlbumProgress()
        {
            if (FoundGame.Instance == null) return;
            StateAlbumDefinition state = FoundGame.Instance.Catalog.GetState(stateCode);
            int total = state.stampDesignIds == null ? 0 : state.stampDesignIds.Count;
            int found = total == 0 ? 0 : FoundGame.Instance.Collection.CountDistinctDestinations(FoundGame.Instance.SaveData, stateCode);
            if (albumProgressText != null) albumProgressText.text = state.name.ToUpperInvariant() + " ALBUM  " + found + " / " + total;
        }

        private void UpdateCheckInButton()
        {
            if (checkInButton == null) return;
            checkInButton.interactable = hasLocation && nearest != null && nearest.coordinates != null
                && FoundGame.Instance != null
                && FoundGame.Instance.Locations.IsInsideLocalPostmarkZone(nearest, latitude, longitude);
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message ?? string.Empty;
        }
    }
}
