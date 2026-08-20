using System;
using Found.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Found.UI
{
    public sealed class FoundRoutePanelController : MonoBehaviour
    {
        [SerializeField] private string stateCode = "NY";
        [SerializeField] private Text destinationText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button completeButton;

        private FieldRouteSession session;

        public void StartRoute()
        {
            try
            {
                session = FoundGame.Instance.StartFieldRoute(stateCode);
                StampDesign design = FoundGame.Instance.Catalog.GetDesign(session.designId);
                if (destinationText != null) destinationText.text = design.placeName + "\n" + design.title;
                SetProgress();
                SetStatus("Survey the route to reveal the collectible.");
            }
            catch (Exception error)
            {
                SetStatus(error.Message);
            }
        }

        public void Survey()
        {
            try
            {
                if (session == null) throw new InvalidOperationException("Start a Field Route first.");
                FoundGame.Instance.Routes.AdvanceSurvey(session);
                SetProgress();
                SetStatus(session.surveyProgress >= 3 ? "Survey complete. Collect the stamp." : "Route surveyed.");
            }
            catch (Exception error)
            {
                SetStatus(error.Message);
            }
        }

        public void CompleteRoute()
        {
            try
            {
                if (session == null) throw new InvalidOperationException("Start a Field Route first.");
                AcquisitionResult result = FoundGame.Instance.CompleteFieldRoute(session);
                StampDesign design = FoundGame.Instance.Catalog.GetDesign(result.copy.designId);
                string message = "Found " + design.placeName + " • " + FoundGame.Instance.Catalog.GetRarityRule(result.copy.rarity).displayName;
                if (result.completionAward != null) message += "\nState album complete — Gold Foil awarded.";
                SetStatus(message);
                session = null;
                SetProgress();
            }
            catch (Exception error)
            {
                SetStatus(error.Message);
            }
        }

        private void SetProgress()
        {
            int progress = session == null ? 0 : session.surveyProgress;
            if (progressText != null) progressText.text = "SURVEY " + progress + " / 3";
            if (completeButton != null) completeButton.interactable = session != null && session.surveyProgress >= 3;
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message ?? string.Empty;
        }
    }
}
