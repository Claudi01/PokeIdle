using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PokeIdle
{
    [Serializable]
    public sealed class CreatureSaveData
    {
        public int CreatureId;
        public int Level;
        public int CurrentHP;
        public string InstanceId;
        public List<int> LearnedMoveIds;
        public List<int> EquippedMoveIds;
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int Version = ProgressionRules.SaveVersion;
        public int Dindin;
        // Legacy field kept so saves from version 6 can be migrated safely.
        public int Gold;
        public int TotalDefeated;
        public int RouteNumber = 1;
        public int RouteProgress;
        public int WorldNumber = 1;
        public int PhaseNumber;
        public int EncounterProgress;
        public bool HasRetryPhase;
        public int RetryRouteNumber = 1;
        public int RetryRouteProgress;
        public int RetryWorldNumber = 1;
        public int RetryPhaseNumber;
        public CreatureSaveData ActiveCreature;
        public List<CreatureSaveData> Party;
        public List<CreatureSaveData> PCBox;
        public int UnlockedPartySlots = 1;
        public int BoxCapacity;
        public string PendingLeaderInstanceId;
        public string PendingBoxInstanceId;
        public string PendingPartyInstanceId;
        public List<PhaseDifficultyRecord> PhaseDifficulties;
        public List<InventoryItemStack> Inventory = new List<InventoryItemStack>();
    }

    public static class SaveService
    {
        private const string TemporarySaveSuffix = ".tmp";

        public static CreatureInstance RestoreCreature(CreatureSaveData saved, DemoContentSet content, bool hasMoveLoadout)
        {
            return RestoreCreature(saved, content, hasMoveLoadout, true);
        }

        public static CreatureInstance RestoreCreature(CreatureSaveData saved, DemoContentSet content,
            bool hasMoveLoadout, bool reviveIfFainted)
        {
            CreatureDefinition definition = content.FindCreature(saved == null ? content.Starter.Id : saved.CreatureId) ?? content.Starter;
            var creature = new CreatureInstance(definition, saved == null
                ? ProgressionRules.StartingCreatureLevel : Mathf.Clamp(saved.Level, 1, ProgressionRules.GetGlobalMaxLevel()));
            if (saved != null && saved.CreatureId == definition.Id)
            {
                creature.CurrentHP = saved.CurrentHP;
                if (!string.IsNullOrEmpty(saved.InstanceId)) creature.InstanceId = saved.InstanceId;
                if (hasMoveLoadout) creature.RestoreMoves(saved.LearnedMoveIds, saved.EquippedMoveIds);
            }
            creature.EnsureValid(reviveIfFainted);
            return creature;
        }

        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, "pokeidle_save.json"); }
        }

        private static string TemporarySavePath
        {
            get { return SavePath + TemporarySaveSuffix; }
        }

        public static bool Save(PlayerSaveData data)
        {
            return SaveToPath(data, SavePath);
        }

        public static bool SaveToPath(PlayerSaveData data, string path)
        {
            if (data == null)
            {
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the complete file first, then replace the previous save in
                // one filesystem operation. A crash cannot leave half a JSON file.
                File.WriteAllText(path + TemporarySaveSuffix, json);
                if (File.Exists(path))
                {
                    // Keep the last valid generation for recovery after disk corruption.
                    File.Replace(path + TemporarySaveSuffix, path, path + ".bak");
                }
                else
                {
                    File.Move(path + TemporarySaveSuffix, path);
                }
                return true;
            }
            catch (Exception exception)
            {
                // Retain a complete temporary file if the final replace failed.
                Debug.LogError("Nao foi possivel salvar o progresso: " + exception.Message);
                return false;
            }
        }

        public static PlayerSaveData Load()
        {
            return LoadFromPath(SavePath);
        }

        public static PlayerSaveData LoadFromPath(string path)
        {
            PlayerSaveData save = TryLoad(path);
            if (save != null)
            {
                return save;
            }

            // A temporary file may be the only complete file if the process ended
            // between writing it and the final rename.
            return TryLoad(path + TemporarySaveSuffix) ?? TryLoad(path + ".bak");
        }

        private static PlayerSaveData TryLoad(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                if (data == null || (!(data.ActiveCreature != null && data.ActiveCreature.CreatureId > 0
                    && data.ActiveCreature.Level > 0) && (data.Party == null || !data.Party.Exists(
                        c => c != null && c.CreatureId > 0 && c.Level > 0))))
                    return null;
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Nao foi possivel carregar o save em " + path + ": " + exception.Message);
                return null;
            }
        }
    }
}
