using System;
using System.Collections.Generic;

namespace Found.Core
{
    public enum StampRarity
    {
        Standard,
        Special,
        Limited,
        Proof,
        Alternate,
        GoldFoil
    }

    public enum BonusTrait
    {
        None,
        Misprint,
        DoublePrint,
        Imperforate,
        ColorTrial
    }

    public enum AcquisitionSource
    {
        FieldRoute,
        CheckIn,
        Trade,
        Completion,
        Migration
    }

    public enum TradeDirection
    {
        Outgoing,
        Incoming
    }

    public enum TradeStatus
    {
        Created,
        Redeemed,
        Rejected
    }

    [Serializable]
    public sealed class GeoPoint
    {
        public double latitude;
        public double longitude;
        public float radiusMiles = 10f;
    }

    [Serializable]
    public sealed class LocalPostmark
    {
        public string place;
        public string dateUtc;
        public bool test;
    }

    [Serializable]
    public sealed class ProvenanceEvent
    {
        public string eventId;
        public string action;
        public string atUtc;
        public string fromPlayerId;
        public string toPlayerId;
        public string tradeId;
    }

    [Serializable]
    public sealed class StampDesign
    {
        public string id;
        public string catalogNumber;
        public int albumNumber;
        public string stateCode;
        public string placeName;
        public string place;
        public string title;
        public string artworkKey;
        public List<string> observations = new List<string>();
        public GeoPoint coordinates;
        public string lore;
        public string history;
        public int baseValue;
        public bool isCompletionReward;
        public bool tradeable = true;
    }

    [Serializable]
    public sealed class StateAlbumDefinition
    {
        public string code;
        public string name;
        public string albumName;
        public string subtitle;
        public string completionText;
        public string completionDesignId;
        public List<string> stampDesignIds = new List<string>();
    }

    [Serializable]
    public sealed class RarityRule
    {
        public string rarity;
        public string displayName;
        public float weight;
        public int rank;
        public float valueMultiplier = 1f;
        public bool rollable = true;
        public bool requiresEditionNumber;
        public int editionLimit;

        public StampRarity ParsedRarity
        {
            get
            {
                StampRarity parsed;
                return Enum.TryParse(rarity, true, out parsed) ? parsed : StampRarity.Standard;
            }
        }
    }

    [Serializable]
    public sealed class TraitRule
    {
        public string trait;
        public string displayName;
        public float weight;
        public int rank;
        public float valueMultiplier = 1f;

        public BonusTrait ParsedTrait
        {
            get
            {
                BonusTrait parsed;
                return Enum.TryParse(trait, true, out parsed) ? parsed : BonusTrait.None;
            }
        }
    }

    [Serializable]
    public sealed class FoundCatalogData
    {
        public int schemaVersion = 1;
        public List<StateAlbumDefinition> states = new List<StateAlbumDefinition>();
        public List<StampDesign> designs = new List<StampDesign>();
        public List<RarityRule> rarities = new List<RarityRule>();
        public List<TraitRule> traits = new List<TraitRule>();
    }

    [Serializable]
    public sealed class CollectibleInstance
    {
        public string instanceId;
        public string designId;
        public StampRarity rarity;
        public BonusTrait bonusTrait;
        public int editionNumber;
        public bool hasEditionNumber;
        public int value;
        public string foundAtUtc;
        public AcquisitionSource source;
        public LocalPostmark postmark;
        public List<ProvenanceEvent> provenance = new List<ProvenanceEvent>();
    }

    [Serializable]
    public sealed class CollectionBucket
    {
        public string designId;
        public List<CollectibleInstance> copies = new List<CollectibleInstance>();
    }

    [Serializable]
    public sealed class DiscoveryRecord
    {
        public string designId;
        public string firstFoundAtUtc;
    }

    [Serializable]
    public sealed class StateProgress
    {
        public string stateCode;
        public List<int> claimedMilestoneCounts = new List<int>();
        public bool completionAwarded;
    }

    [Serializable]
    public sealed class LedgerEntry
    {
        public string id;
        public string atUtc;
        public int amount;
        public string label;
    }

    [Serializable]
    public sealed class PlayerProfile
    {
        public string playerId;
        public string displayName = "Collector";
        public int coins;
        public int level = 1;
        public int xp;
        public int totalEarned;
    }

    [Serializable]
    public sealed class TradeRecord
    {
        public string tradeId;
        public TradeDirection direction;
        public TradeStatus status;
        public string atUtc;
        public List<string> instanceIds = new List<string>();
        public string summary;
    }

    [Serializable]
    public sealed class FoundSaveData
    {
        public int schemaVersion = 1;
        public PlayerProfile profile = new PlayerProfile();
        public List<CollectionBucket> collection = new List<CollectionBucket>();
        public List<DiscoveryRecord> discoveries = new List<DiscoveryRecord>();
        public List<StateProgress> stateProgress = new List<StateProgress>();
        public List<LedgerEntry> ledger = new List<LedgerEntry>();
        public List<TradeRecord> trades = new List<TradeRecord>();
        public List<string> redeemedTradeIds = new List<string>();
    }

    [Serializable]
    public sealed class StampTransferPayload
    {
        public string designId;
        public CollectibleInstance copy;
    }

    [Serializable]
    public sealed class DirectTradePackage
    {
        public string schema = "FOUND_DIRECT_TRADE";
        public int version = 2;
        public string tradeId;
        public string createdAtUtc;
        public string senderPlayerId;
        public List<StampTransferPayload> stamps = new List<StampTransferPayload>();
    }

    [Serializable]
    public sealed class FieldRouteSession
    {
        public string sessionId;
        public string stateCode;
        public string designId;
        public string startedAtUtc;
        public int surveyProgress;
    }

    public sealed class AcquisitionResult
    {
        public CollectibleInstance copy;
        public bool firstDiscovery;
        public CollectibleInstance completionAward;
    }
}
