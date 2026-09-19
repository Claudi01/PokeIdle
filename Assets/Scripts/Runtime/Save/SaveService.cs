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
        public List<PhaseDifficultyRecord> PhaseDifficulties;
        public List<InventoryItemStack> Inventory = new List<InventoryItemStack>();
    }

    public static class SaveService
    {
        private const string TemporarySaveSuffix = ".tmp";

        public static CreatureInstance RestoreCreature(CreatureSaveData saved, DemoContentSet content, bool hasMoveLoadout)
        {
            CreatureDefinition definition = content.FindCreature(saved == null ? content.Starter.Id : saved.CreatureId) ?? content.Starter;
            var creature = new CreatureInstance(definition, saved == null
                ? ProgressionRules.StartingCreatureLevel : Mathf.Max(ProgressionRules.StartingCreatureLevel, saved.Level));
            if (saved != null && saved.CreatureId == definition.Id)
            {
                creature.CurrentHP = saved.CurrentHP;
                if (!string.IsNullOrEmpty(saved.InstanceId)) creature.InstanceId = saved.InstanceId;
                if (hasMoveLoadout) creature.RestoreMoves(saved.LearnedMoveIds, saved.EquippedMoveIds);
            }
            creature.EnsureValid();
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

        public static void Save(PlayerSaveData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the complete file first, then replace the previous save in
                // one filesystem operation. A crash cannot leave half a JSON file.
                File.WriteAllText(TemporarySavePath, json);
                if (File.Exists(SavePath))
                {
                    File.Replace(TemporarySavePath, SavePath, null);
                }
                else
                {
                    File.Move(TemporarySavePath, SavePath);
                }
            }
            catch (Exception exception)
            {
                try
                {
                    if (File.Exists(TemporarySavePath)) File.Delete(TemporarySavePath);
                }
                catch (Exception cleanupException)
                {
                    Debug.LogWarning("Nao foi possivel limpar o save temporario: " + cleanupException.Message);
                }

                Debug.LogError("Nao foi possivel salvar o progresso: " + exception.Message);
            }
        }

        public static PlayerSaveData Load()
        {
            PlayerSaveData save = TryLoad(SavePath);
            if (save != null)
            {
                return save;
            }

            // A temporary file may be the only complete file if the process ended
            // between writing it and the final rename.
            return TryLoad(TemporarySavePath);
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
                return JsonUtility.FromJson<PlayerSaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Nao foi possivel carregar o save em " + path + ": " + exception.Message);
                return null;
            }
        }
    }
}
