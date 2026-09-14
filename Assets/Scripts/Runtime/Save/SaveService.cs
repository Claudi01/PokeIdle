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

        public static void Save(PlayerSaveData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogError("Não foi possível salvar o progresso: " + exception.Message);
            }
        }

        public static PlayerSaveData Load()
        {
            if (!File.Exists(SavePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerSaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError("Não foi possível carregar o progresso: " + exception.Message);
                return null;
            }
        }
    }
}
