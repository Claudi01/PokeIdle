using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    [Serializable]
    public sealed class WorldBalanceSettings
    {
        [Min(1)] public int WorldNumber = 1;

        // The first world uses phases 0..N. Later worlds use phases 1..N.
        // Therefore, world 1 with LastPhaseNumber = 5 has six phases.
        [Min(0)] public int LastPhaseNumber = 10;

        // Zero means use the default world-cap formula.
        [Min(0)] public int LevelCap;

        // Zero means use the global default for this world.
        [Min(0)] public int EnemiesPerPhase;
    }

    [Serializable]
    public sealed class PhaseBalanceSettings
    {
        [Min(1)] public int WorldNumber = 1;
        [Min(0)] public int PhaseNumber;

        // Zero means calculate the level from the global formula and snapshot it.
        [Min(0)] public int NormalEnemyLevel;

        // Zero means use the world/global encounter count.
        [Min(0)] public int EnemiesInPhase;
    }

    /// <summary>
    /// Valores de balanceamento editaveis pelo Inspector. Regras especificas de
    /// mundo/fase ficam nas listas abaixo; entradas com valor zero usam a formula
    /// global. Isso permite balancear sem alterar codigo.
    /// Crie/edite o asset em Assets/Resources/PokeIdle/Config.
    /// </summary>
    [CreateAssetMenu(menuName = "PokeIdle/Game Balance Config", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Inicio")]
        [SerializeField, Min(1)] private int startingCreatureLevel = 1;

        [Header("Ritmo e progressao")]
        [SerializeField, Min(0.1f)] private float tickIntervalSeconds = 1f;
        [SerializeField, Min(0)] private int phasesPerWorld = 10;
        [SerializeField, Min(1)] private int enemiesPerPhase = 10;

        [Header("Limites de nivel")]
        [SerializeField, Min(1)] private int globalMaxLevel = 100;
        [SerializeField, Min(1)] private int defaultWorldLevelCap = 10;
        [SerializeField, Min(0)] private int worldLevelCapGrowth = 10;

        [Header("Mundos e fases (opcional)")]
        [SerializeField] private List<WorldBalanceSettings> worldOverrides = new List<WorldBalanceSettings>();
        [SerializeField] private List<PhaseBalanceSettings> phaseOverrides = new List<PhaseBalanceSettings>();

        [Header("Ouro")]
        [SerializeField, Min(0)] private int baseGoldReward = 4;
        [SerializeField, Min(0)] private int goldWorldBonus = 1;

        [Header("Nivel por moedas")]
        [SerializeField, Min(1)] private int baseLevelUpCost = 25;
        [SerializeField, Min(0)] private int levelUpCostGrowth = 15;

        [Header("Dificuldade por fase (fixada na primeira visita)")]
        [SerializeField, Min(0.1f)] private float enemyLevelsPerPhase = 1.25f;
        [SerializeField, Range(0f, 1f)] private float enemyPlayerLevelRatio = 0.9f;

        [Header("Boss")]
        [SerializeField, Min(0)] private int bossLevelBonus = 2;
        [SerializeField, Min(1f)] private float bossHealthMultiplier = 1.6f;

        [Header("Save")]
        [SerializeField, Range(30f, 60f)] private float autoSaveIntervalSeconds = 45f;

        public int StartingCreatureLevel { get { return Mathf.Max(1, startingCreatureLevel); } }
        public float TickIntervalSeconds { get { return Mathf.Max(0.1f, tickIntervalSeconds); } }
        // Legacy name kept for existing Inspector assets and scripts. It is the
        // last phase number of a world without an explicit override.
        public int PhasesPerWorld { get { return Mathf.Max(0, phasesPerWorld); } }
        public int EnemiesPerPhase { get { return Mathf.Max(1, enemiesPerPhase); } }
        public int GlobalMaxLevel { get { return Mathf.Max(1, globalMaxLevel); } }
        public float BossHealthMultiplier { get { return Mathf.Max(1f, bossHealthMultiplier); } }
        public float AutoSaveIntervalSeconds { get { return Mathf.Clamp(autoSaveIntervalSeconds, 30f, 60f); } }

        public int GetFirstPhaseNumber(int worldNumber)
        {
            return Mathf.Max(1, worldNumber) == 1 ? 0 : 1;
        }

        public int GetLastPhaseNumber(int worldNumber)
        {
            WorldBalanceSettings settings = FindWorldSettings(worldNumber);
            return settings == null ? PhasesPerWorld : Mathf.Max(GetFirstPhaseNumber(worldNumber), settings.LastPhaseNumber);
        }

        public int GetPhaseCount(int worldNumber)
        {
            int firstPhase = GetFirstPhaseNumber(worldNumber);
            return Mathf.Max(1, GetLastPhaseNumber(worldNumber) - firstPhase + 1);
        }

        public int GetWorldLevelCap(int worldNumber)
        {
            int normalizedWorld = Mathf.Max(1, worldNumber);
            WorldBalanceSettings settings = FindWorldSettings(normalizedWorld);
            long calculatedCap = (long)Mathf.Max(1, defaultWorldLevelCap)
                + (long)(normalizedWorld - 1) * Mathf.Max(0, worldLevelCapGrowth);
            int cap = settings != null && settings.LevelCap > 0
                ? settings.LevelCap
                : calculatedCap > int.MaxValue ? int.MaxValue : (int)calculatedCap;
            return Mathf.Clamp(cap, 1, GlobalMaxLevel);
        }

        public int GetEnemiesInPhase(int worldNumber, int phaseNumber)
        {
            PhaseBalanceSettings phaseSettings = FindPhaseSettings(worldNumber, phaseNumber);
            if (phaseSettings != null && phaseSettings.EnemiesInPhase > 0)
            {
                return phaseSettings.EnemiesInPhase;
            }

            WorldBalanceSettings worldSettings = FindWorldSettings(worldNumber);
            return worldSettings != null && worldSettings.EnemiesPerPhase > 0
                ? worldSettings.EnemiesPerPhase
                : EnemiesPerPhase;
        }

        public int GetLevelUpCost(int currentLevel)
        {
            int normalizedLevel = Mathf.Max(1, currentLevel);
            return Mathf.Max(1, baseLevelUpCost + (normalizedLevel - 1) * levelUpCostGrowth);
        }

        public int GetEnemyLevel(int worldNumber, int phaseNumber, int playerLevel)
        {
            PhaseBalanceSettings phaseSettings = FindPhaseSettings(worldNumber, phaseNumber);
            int calculatedLevel = phaseSettings != null && phaseSettings.NormalEnemyLevel > 0
                ? phaseSettings.NormalEnemyLevel
                : ProgressionRules.CalculateEnemyLevelFromIndex(
                    GetWorldPhaseIndex(worldNumber, phaseNumber), playerLevel,
                    enemyLevelsPerPhase, enemyPlayerLevelRatio);
            return Mathf.Clamp(calculatedLevel, 1, GetWorldLevelCap(worldNumber));
        }

        public int GetBossLevel(int worldNumber, int phaseNumber, int playerLevel)
        {
            int normalLevel = GetEnemyLevel(worldNumber, phaseNumber, playerLevel);
            return Mathf.Clamp(normalLevel + BossLevelBonus, 1, GetWorldLevelCap(worldNumber));
        }

        public int BossLevelBonus { get { return Mathf.Max(0, bossLevelBonus); } }

        public int GetGoldReward(int enemyLevel, int worldNumber)
        {
            int normalizedEnemyLevel = Mathf.Max(1, enemyLevel);
            int worldBonus = Mathf.Max(0, worldNumber - 1) * goldWorldBonus;
            return Mathf.Max(1, baseGoldReward + normalizedEnemyLevel + worldBonus);
        }

        public void ResetToDefaults()
        {
            startingCreatureLevel = 1;
            tickIntervalSeconds = 1f;
            phasesPerWorld = 10;
            enemiesPerPhase = 10;
            globalMaxLevel = 100;
            defaultWorldLevelCap = 10;
            worldLevelCapGrowth = 10;
            worldOverrides = new List<WorldBalanceSettings>();
            phaseOverrides = new List<PhaseBalanceSettings>();
            baseGoldReward = 4;
            goldWorldBonus = 1;
            baseLevelUpCost = 25;
            levelUpCostGrowth = 15;
            enemyLevelsPerPhase = 1.25f;
            enemyPlayerLevelRatio = 0.9f;
            bossLevelBonus = 2;
            bossHealthMultiplier = 1.6f;
            autoSaveIntervalSeconds = 45f;
        }

        private int GetWorldPhaseIndex(int worldNumber, int phaseNumber)
        {
            int normalizedWorld = Mathf.Max(1, worldNumber);
            long index = 0;
            for (int world = 1; world < normalizedWorld; world++)
            {
                index += GetPhaseCount(world);
                if (index >= int.MaxValue - 100)
                {
                    return int.MaxValue - 100;
                }
            }

            int firstPhase = GetFirstPhaseNumber(normalizedWorld);
            int lastPhase = GetLastPhaseNumber(normalizedWorld);
            int normalizedPhase = Mathf.Clamp(phaseNumber, firstPhase, lastPhase);
            index += normalizedPhase - firstPhase;
            return index >= int.MaxValue - 100 ? int.MaxValue - 100 : (int)index;
        }

        private WorldBalanceSettings FindWorldSettings(int worldNumber)
        {
            if (worldOverrides == null) return null;
            int normalizedWorld = Mathf.Max(1, worldNumber);
            for (int i = 0; i < worldOverrides.Count; i++)
            {
                WorldBalanceSettings settings = worldOverrides[i];
                if (settings != null && settings.WorldNumber == normalizedWorld)
                {
                    return settings;
                }
            }

            return null;
        }

        private PhaseBalanceSettings FindPhaseSettings(int worldNumber, int phaseNumber)
        {
            if (phaseOverrides == null) return null;
            int normalizedWorld = Mathf.Max(1, worldNumber);
            int normalizedPhase = Mathf.Max(0, phaseNumber);
            for (int i = 0; i < phaseOverrides.Count; i++)
            {
                PhaseBalanceSettings settings = phaseOverrides[i];
                if (settings != null && settings.WorldNumber == normalizedWorld && settings.PhaseNumber == normalizedPhase)
                {
                    return settings;
                }
            }

            return null;
        }
    }
}
