using System;
using System.IO;
using UnityEngine;

namespace Found.Core
{
    public interface IFoundSaveRepository
    {
        FoundSaveData Load();
        void Save(FoundSaveData save);
        void Delete();
    }

    public sealed class JsonFileSaveRepository : IFoundSaveRepository
    {
        private const string FileName = "found-save.json";
        private readonly string path;
        private readonly string backupPath;

        public JsonFileSaveRepository()
        {
            path = Path.Combine(Application.persistentDataPath, FileName);
            backupPath = path + ".bak";
        }

        public FoundSaveData Load()
        {
            FoundSaveData loaded = TryLoad(path) ?? TryLoad(backupPath) ?? NewSave();
            Normalize(loaded);
            return loaded;
        }

        public void Save(FoundSaveData save)
        {
            if (save == null) throw new ArgumentNullException("save");
            Normalize(save);
            Directory.CreateDirectory(Application.persistentDataPath);

            string temp = path + ".tmp";
            string json = JsonUtility.ToJson(save, true);
            File.WriteAllText(temp, json);

            if (File.Exists(path)) File.Copy(path, backupPath, true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        public void Delete()
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(backupPath)) File.Delete(backupPath);
        }

        private static FoundSaveData TryLoad(string file)
        {
            if (!File.Exists(file)) return null;
            try
            {
                string json = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<FoundSaveData>(json);
            }
            catch (Exception error)
            {
                Debug.LogWarning("FOUND save read failed for " + file + ": " + error.Message);
                return null;
            }
        }

        private static FoundSaveData NewSave()
        {
            FoundSaveData save = new FoundSaveData();
            save.profile.playerId = Guid.NewGuid().ToString("N");
            return save;
        }

        private static void Normalize(FoundSaveData save)
        {
            if (save.profile == null) save.profile = new PlayerProfile();
            if (string.IsNullOrWhiteSpace(save.profile.playerId)) save.profile.playerId = Guid.NewGuid().ToString("N");
            if (save.profile.level < 1) save.profile.level = 1;
            if (save.collection == null) save.collection = new System.Collections.Generic.List<CollectionBucket>();
            if (save.discoveries == null) save.discoveries = new System.Collections.Generic.List<DiscoveryRecord>();
            if (save.stateProgress == null) save.stateProgress = new System.Collections.Generic.List<StateProgress>();
            if (save.ledger == null) save.ledger = new System.Collections.Generic.List<LedgerEntry>();
            if (save.trades == null) save.trades = new System.Collections.Generic.List<TradeRecord>();
            if (save.redeemedTradeIds == null) save.redeemedTradeIds = new System.Collections.Generic.List<string>();
        }
    }
}
