using System;
using System.Collections.Generic;
using Found.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Found.UI
{
    public sealed class FoundTradePanelController : MonoBehaviour
    {
        [SerializeField] private Dropdown sendDropdown;
        [SerializeField] private InputField outgoingCode;
        [SerializeField] private InputField incomingCode;
        [SerializeField] private Text statusText;

        private readonly List<CollectibleInstance> sendable = new List<CollectibleInstance>();

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            sendable.Clear();
            if (sendDropdown != null) sendDropdown.ClearOptions();
            FoundGame game = FoundGame.Instance;
            if (game == null) return;

            sendable.AddRange(game.Collection.GetTradeableCopies(game.SaveData));
            List<string> labels = new List<string>(sendable.Count);
            for (int i = 0; i < sendable.Count; i++)
            {
                CollectibleInstance copy = sendable[i];
                StampDesign design = game.Catalog.GetDesign(copy.designId);
                string label = design.placeName + " • " + game.Catalog.GetRarityRule(copy.rarity).displayName;
                if (copy.hasEditionNumber) label += " • No. " + copy.editionNumber;
                if (copy.postmark != null) label += " • Local";
                if (copy.bonusTrait != BonusTrait.None) label += " • " + game.Catalog.GetTraitRule(copy.bonusTrait).displayName;
                labels.Add(label);
            }
            if (sendDropdown != null)
            {
                if (labels.Count == 0) labels.Add("No sendable duplicates");
                sendDropdown.AddOptions(labels);
                sendDropdown.interactable = sendable.Count > 0;
            }
            SetStatus(string.Empty);
        }

        public void CreateTradeCode()
        {
            try
            {
                if (sendable.Count == 0) throw new InvalidOperationException("No sendable duplicate stamps.");
                int index = sendDropdown == null ? 0 : Mathf.Clamp(sendDropdown.value, 0, sendable.Count - 1);
                string code = FoundGame.Instance.CreateTradeCode(new[] { sendable[index].instanceId });
                if (outgoingCode != null) outgoingCode.text = code;
                GUIUtility.systemCopyBuffer = code;
                SetStatus("Trade code created and copied. The exact stamp has left this collection.");
                RefreshAfterMessage();
            }
            catch (Exception error)
            {
                SetStatus(error.Message);
            }
        }

        public void CopyOutgoingCode()
        {
            string code = outgoingCode == null ? string.Empty : outgoingCode.text;
            if (string.IsNullOrWhiteSpace(code)) return;
            GUIUtility.systemCopyBuffer = code;
            SetStatus("Trade code copied.");
        }

        public void RedeemIncomingCode()
        {
            try
            {
                string code = incomingCode == null ? string.Empty : incomingCode.text;
                List<CollectibleInstance> received = FoundGame.Instance.RedeemTradeCode(code);
                SetStatus(received.Count == 1 ? "Stamp received." : received.Count + " stamps received.");
                if (incomingCode != null) incomingCode.text = string.Empty;
                RefreshAfterMessage();
            }
            catch (Exception error)
            {
                SetStatus(error.Message);
            }
        }

        private void RefreshAfterMessage()
        {
            string message = statusText == null ? string.Empty : statusText.text;
            Refresh();
            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message ?? string.Empty;
        }
    }
}
