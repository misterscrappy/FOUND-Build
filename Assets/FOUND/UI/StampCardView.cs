using System;
using System.Text;
using Found.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Found.UI
{
    // One flat 2D stamp layout for every rarity, including Gold Foil.
    // Rarity changes styling/overlays only; it never swaps to another renderer or geometry.
    public sealed class StampCardView : MonoBehaviour
    {
        [Header("Shared stamp layout")]
        [SerializeField] private Image paper;
        [SerializeField] private Image artwork;
        [SerializeField] private Image frame;
        [SerializeField] private Image rarityBand;
        [SerializeField] private Text catalogText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text placeText;
        [SerializeField] private Text rarityText;
        [SerializeField] private Text editionText;
        [SerializeField] private Text traitText;
        [SerializeField] private Text postmarkText;
        [SerializeField] private Text proofOverlayText;
        [SerializeField] private Text detailsText;

        private static readonly Color Paper = new Color32(244, 235, 211, 255);
        private static readonly Color Standard = new Color32(111, 119, 115, 255);
        private static readonly Color Special = new Color32(31, 113, 71, 255);
        private static readonly Color Limited = new Color32(35, 95, 153, 255);
        private static readonly Color Proof = new Color32(109, 79, 135, 255);
        private static readonly Color Alternate = new Color32(184, 135, 46, 255);
        private static readonly Color GoldFoil = new Color32(216, 174, 85, 255);
        private static readonly Color GoldArtwork = new Color32(196, 143, 40, 255);

        public void Bind(FoundCatalogService catalog, StampDesign design, CollectibleInstance copy)
        {
            if (catalog == null) throw new ArgumentNullException("catalog");
            if (design == null) throw new ArgumentNullException("design");
            if (copy == null) throw new ArgumentNullException("copy");

            bool isGold = copy.rarity == StampRarity.GoldFoil;
            Color accent = RarityColor(copy.rarity);
            if (paper != null) paper.color = isGold ? new Color32(247, 231, 177, 255) : Paper;
            if (frame != null) frame.color = accent;
            if (rarityBand != null) rarityBand.color = accent;

            if (artwork != null)
            {
                Sprite sprite = string.IsNullOrWhiteSpace(design.artworkKey) ? null : Resources.Load<Sprite>(design.artworkKey);
                artwork.sprite = sprite;
                artwork.preserveAspect = true;
                // Gold Foil deliberately uses the same artwork box as every other stamp. Until a flat
                // state-completion illustration is authored, the box becomes a clean gold field rather
                // than falling back to the removed 3D asset.
                artwork.enabled = sprite != null || isGold;
                artwork.color = sprite != null ? Color.white : (isGold ? GoldArtwork : Color.white);
            }

            Set(catalogText, design.catalogNumber);
            Set(titleText, design.title);
            Set(placeText, design.place);
            Set(rarityText, catalog.GetRarityRule(copy.rarity).displayName.ToUpperInvariant());
            Set(editionText, copy.hasEditionNumber ? "NO. " + copy.editionNumber + " / 500" : string.Empty);
            Set(traitText, copy.bonusTrait == BonusTrait.None ? string.Empty : catalog.GetTraitRule(copy.bonusTrait).displayName.ToUpperInvariant());

            if (postmarkText != null)
            {
                bool show = copy.postmark != null;
                postmarkText.gameObject.SetActive(show);
                postmarkText.text = show ? (copy.postmark.place + "\n" + copy.postmark.dateUtc).ToUpperInvariant() : string.Empty;
            }

            if (proofOverlayText != null)
            {
                bool showProof = copy.rarity == StampRarity.Proof;
                proofOverlayText.gameObject.SetActive(showProof);
                proofOverlayText.text = showProof ? "PRINTER'S PROOF" : string.Empty;
            }

            Set(detailsText, BuildDetails(design));
        }

        private static string BuildDetails(StampDesign design)
        {
            StringBuilder text = new StringBuilder();
            if (design.observations != null && design.observations.Count > 0)
            {
                text.Append("OBSERVATIONS • ");
                for (int i = 0; i < design.observations.Count; i++)
                {
                    if (i > 0) text.Append(" • ");
                    text.Append(design.observations[i]);
                }
                text.AppendLine();
                text.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(design.lore))
            {
                text.AppendLine(design.lore);
                text.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(design.history))
            {
                text.AppendLine(design.history);
                text.AppendLine();
            }
            text.Append("ORIGINAL ART • DIGITAL SOUVENIR • NOT POSTAGE");
            return text.ToString().Trim();
        }

        private static Color RarityColor(StampRarity rarity)
        {
            switch (rarity)
            {
                case StampRarity.Special: return Special;
                case StampRarity.Limited: return Limited;
                case StampRarity.Proof: return Proof;
                case StampRarity.Alternate: return Alternate;
                case StampRarity.GoldFoil: return GoldFoil;
                default: return Standard;
            }
        }

        private static void Set(Text target, string value)
        {
            if (target == null) return;
            target.text = value ?? string.Empty;
            target.gameObject.SetActive(!string.IsNullOrWhiteSpace(target.text));
        }
    }
}
