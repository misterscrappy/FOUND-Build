using System;
using System.Collections.Generic;
using UnityEngine;

namespace Found.Core
{
    public sealed class FoundCatalogService
    {
        private const string ResourcePath = "FOUND/Content/catalog";
        private readonly FoundCatalogData data;
        private readonly Dictionary<string, StampDesign> designsById = new Dictionary<string, StampDesign>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StateAlbumDefinition> statesByCode = new Dictionary<string, StateAlbumDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<StampRarity, RarityRule> rarityRules = new Dictionary<StampRarity, RarityRule>();
        private readonly Dictionary<BonusTrait, TraitRule> traitRules = new Dictionary<BonusTrait, TraitRule>();

        public FoundCatalogData Data { get { return data; } }

        public FoundCatalogService(FoundCatalogData source)
        {
            if (source == null) throw new ArgumentNullException("source");
            data = source;
            BuildIndexesAndValidate();
        }

        public static FoundCatalogService LoadDefault()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException("FOUND catalog missing. Expected Resources/" + ResourcePath + ".json");
            }

            FoundCatalogData parsed = JsonUtility.FromJson<FoundCatalogData>(asset.text);
            if (parsed == null) throw new InvalidOperationException("FOUND catalog JSON could not be parsed.");
            return new FoundCatalogService(parsed);
        }

        public StampDesign GetDesign(string designId)
        {
            StampDesign design;
            if (string.IsNullOrWhiteSpace(designId) || !designsById.TryGetValue(designId, out design))
            {
                throw new KeyNotFoundException("Unknown stamp design: " + designId);
            }
            return design;
        }

        public bool TryGetDesign(string designId, out StampDesign design)
        {
            return !string.IsNullOrWhiteSpace(designId) && designsById.TryGetValue(designId, out design);
        }

        public StateAlbumDefinition GetState(string stateCode)
        {
            StateAlbumDefinition state;
            if (string.IsNullOrWhiteSpace(stateCode) || !statesByCode.TryGetValue(stateCode, out state))
            {
                throw new KeyNotFoundException("Unknown state: " + stateCode);
            }
            return state;
        }

        public RarityRule GetRarityRule(StampRarity rarity)
        {
            RarityRule rule;
            if (!rarityRules.TryGetValue(rarity, out rule)) throw new KeyNotFoundException("Missing rarity rule: " + rarity);
            return rule;
        }

        public TraitRule GetTraitRule(BonusTrait trait)
        {
            TraitRule rule;
            if (!traitRules.TryGetValue(trait, out rule)) throw new KeyNotFoundException("Missing trait rule: " + trait);
            return rule;
        }

        public List<StampDesign> GetDestinationDesigns(string stateCode)
        {
            StateAlbumDefinition state = GetState(stateCode);
            List<StampDesign> result = new List<StampDesign>(state.stampDesignIds.Count);
            for (int i = 0; i < state.stampDesignIds.Count; i++) result.Add(GetDesign(state.stampDesignIds[i]));
            return result;
        }

        private void BuildIndexesAndValidate()
        {
            if (data.schemaVersion != 1) throw new InvalidOperationException("Unsupported catalog schema: " + data.schemaVersion);
            if (data.states == null || data.designs == null || data.rarities == null || data.traits == null)
                throw new InvalidOperationException("FOUND catalog contains null collections.");

            for (int i = 0; i < data.states.Count; i++)
            {
                StateAlbumDefinition state = data.states[i];
                if (state == null || string.IsNullOrWhiteSpace(state.code)) throw new InvalidOperationException("State with no code at index " + i);
                if (statesByCode.ContainsKey(state.code)) throw new InvalidOperationException("Duplicate state code: " + state.code);
                if (state.stampDesignIds == null) state.stampDesignIds = new List<string>();
                statesByCode.Add(state.code, state);
            }

            HashSet<string> catalogNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < data.designs.Count; i++)
            {
                StampDesign design = data.designs[i];
                if (design == null || string.IsNullOrWhiteSpace(design.id)) throw new InvalidOperationException("Stamp design with no id at index " + i);
                if (designsById.ContainsKey(design.id)) throw new InvalidOperationException("Duplicate stamp design id: " + design.id);
                if (!statesByCode.ContainsKey(design.stateCode)) throw new InvalidOperationException("Stamp references unknown state: " + design.id);
                if (string.IsNullOrWhiteSpace(design.catalogNumber) || !catalogNumbers.Add(design.catalogNumber))
                    throw new InvalidOperationException("Duplicate or missing catalog number: " + design.catalogNumber);
                if (design.observations == null) design.observations = new List<string>();
                designsById.Add(design.id, design);
            }

            for (int i = 0; i < data.states.Count; i++)
            {
                StateAlbumDefinition state = data.states[i];
                HashSet<string> stateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < state.stampDesignIds.Count; j++)
                {
                    string id = state.stampDesignIds[j];
                    StampDesign design = GetDesign(id);
                    if (!string.Equals(design.stateCode, state.code, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("State " + state.code + " references stamp from " + design.stateCode + ": " + id);
                    if (design.isCompletionReward) throw new InvalidOperationException("Completion stamp cannot be a destination: " + id);
                    if (!stateIds.Add(id)) throw new InvalidOperationException("Duplicate stamp in state album: " + id);
                }

                if (!string.IsNullOrWhiteSpace(state.completionDesignId))
                {
                    StampDesign completion = GetDesign(state.completionDesignId);
                    if (!completion.isCompletionReward) throw new InvalidOperationException("Completion design not marked as completion reward: " + completion.id);
                    if (!string.Equals(completion.stateCode, state.code, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Completion design belongs to wrong state: " + completion.id);
                    if (completion.tradeable) throw new InvalidOperationException("Completion rewards must be non-tradeable: " + completion.id);
                }
            }

            for (int i = 0; i < data.rarities.Count; i++)
            {
                RarityRule rule = data.rarities[i];
                StampRarity rarity = rule.ParsedRarity;
                if (rarityRules.ContainsKey(rarity)) throw new InvalidOperationException("Duplicate rarity rule: " + rarity);
                rarityRules.Add(rarity, rule);
            }

            if (!rarityRules.ContainsKey(StampRarity.Standard) || !rarityRules.ContainsKey(StampRarity.GoldFoil))
                throw new InvalidOperationException("Catalog must define Standard and GoldFoil rarities.");
            if (GetRarityRule(StampRarity.GoldFoil).rollable)
                throw new InvalidOperationException("Gold Foil must be completion-only and not rollable.");
            RarityRule limited = GetRarityRule(StampRarity.Limited);
            if (!limited.requiresEditionNumber || limited.editionLimit != 500)
                throw new InvalidOperationException("Limited Issue must use edition numbers 1-500.");

            for (int i = 0; i < data.traits.Count; i++)
            {
                TraitRule rule = data.traits[i];
                BonusTrait trait = rule.ParsedTrait;
                if (traitRules.ContainsKey(trait)) throw new InvalidOperationException("Duplicate trait rule: " + trait);
                traitRules.Add(trait, rule);
            }
            if (!traitRules.ContainsKey(BonusTrait.None)) throw new InvalidOperationException("Catalog must define the None bonus trait.");
        }
    }
}
