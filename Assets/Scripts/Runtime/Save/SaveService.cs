using System;
using System.IO;
using UnityEngine;

namespace PokeIdle
{
    [Serializable]
    public sealed class CreatureSaveData
    {
        public int CreatureId;
        public int Level;
        public int Experience;
        public int CurrentHP;
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int Version = 1;
        public int Gold;
        public int TotalDefeated;
        public int RouteNumber = 1;
        public int RouteProgress;
        public CreatureSaveData ActiveCreature;
    }

    public static class SaveService
    {
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
