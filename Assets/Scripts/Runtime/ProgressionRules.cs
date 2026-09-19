using System;
using UnityEngine;

namespace PokeIdle
{
    /// <summary>
    /// Regras centrais de progressao. Manter estes valores juntos facilita o balanceamento
    /// enquanto o projeto ainda nao possui uma tela de configuracao.
    /// </summary>
    public static class ProgressionRules
    {
        public const int SaveVersion = 6;
        public const int PhaseProgressionSaveVersion = 5;

        private static GameBalanceConfig activeConfig;

        public static int StartingCreatureLevel
        {
            get { return activeConfig == null ? 1 : activeConfig.StartingCreatureLevel; }
        }

        public static float BossHealthMultiplier
        {
            get { return activeConfig == null ? 1.6f : activeConfig.BossHealthMultiplier; }
        }

        public static void SetBalanceConfig(GameBalanceConfig config)
        {
            activeConfig = config;
        }

        public static float GetTickIntervalSeconds()
        {
            return activeConfig == null ? 1f : activeConfig.TickIntervalSeconds;
        }

        public static int GetPhasesPerWorld()
        {
            return activeConfig == null ? 10 : activeConfig.PhasesPerWorld;
        }

        public static int GetFirstPhaseNumber(int world)
        {
            return activeConfig == null
                ? Mathf.Max(1, world) == 1 ? 0 : 1
                : activeConfig.GetFirstPhaseNumber(world);
        }

        public static int GetLastPhaseNumber(int world)
        {
            return activeConfig == null
                ? GetPhasesPerWorld()
                : activeConfig.GetLastPhaseNumber(world);
        }

        public static int GetPhaseCount(int world)
        {
            int firstPhase = GetFirstPhaseNumber(world);
            return Mathf.Max(1, GetLastPhaseNumber(world) - firstPhase + 1);
        }

        public static int GetEnemiesInPhase(int world, int phase)
        {
            return activeConfig == null
                ? 10
                : activeConfig.GetEnemiesInPhase(world, phase);
        }

        public static int GetGlobalMaxLevel()
        {
            return activeConfig == null ? 100 : activeConfig.GlobalMaxLevel;
        }

        public static int GetWorldLevelCap(int world)
        {
            if (activeConfig != null) return activeConfig.GetWorldLevelCap(world);

            int normalizedWorld = Mathf.Max(1, world);
            return Mathf.Clamp(10 + (normalizedWorld - 1) * 10, 1, GetGlobalMaxLevel());
        }

        public static float GetAutoSaveIntervalSeconds()
        {
            return activeConfig == null ? 45f : activeConfig.AutoSaveIntervalSeconds;
        }

        public static int GetEnemiesPerPhase()
        {
            return activeConfig == null ? 10 : activeConfig.EnemiesPerPhase;
        }

        public static int GetEnemyLevel(int world, int phase, int playerLevel)
        {
            return activeConfig == null
                ? CalculateEnemyLevel(world, phase, playerLevel, 10, 1.25f, 0.9f)
                : activeConfig.GetEnemyLevel(world, phase, playerLevel);
        }

        public static int GetBossLevel(int world, int phase, int playerLevel)
        {
            return activeConfig == null
                ? Mathf.Clamp(GetEnemyLevel(world, phase, playerLevel) + BossLevelBonus, 1, GetWorldLevelCap(world))
                : activeConfig.GetBossLevel(world, phase, playerLevel);
        }

        public static int ClampLevelToWorld(int world, int level)
        {
            return Mathf.Clamp(level, 1, GetWorldLevelCap(world));
        }

        public static int BossLevelBonus { get { return activeConfig == null ? 2 : activeConfig.BossLevelBonus; } }

        public static int CalculateEnemyLevel(int world, int phase, int playerLevel,
            int phasesPerWorld, float levelsPerPhase, float playerRatio)
        {
            long index = (long)(Mathf.Max(1, world) - 1) * Mathf.Max(1, phasesPerWorld) + Mathf.Max(0, phase);
            return CalculateEnemyLevelFromIndex(index, playerLevel, levelsPerPhase, playerRatio);
        }

        public static int CalculateEnemyLevelFromIndex(long phaseIndex, int playerLevel,
            float levelsPerPhase, float playerRatio)
        {
            double normalizedIndex = Math.Max(0L, phaseIndex);
            double phaseLevel = 1d + Math.Floor(normalizedIndex * Math.Max(0.1d, levelsPerPhase));
            double playerFloor = Math.Floor(Mathf.Max(1, playerLevel) * Math.Round(Mathf.Clamp01(playerRatio), 4));
            return (int)Math.Min(int.MaxValue - 100, Math.Max(phaseLevel, playerFloor));
        }

        public static int GetGoldReward(int enemyLevel, int routeNumber)
        {
            return activeConfig == null
                ? Mathf.Max(1, 4 + Mathf.Max(1, enemyLevel) + Mathf.Max(0, routeNumber - 1))
                : activeConfig.GetGoldReward(enemyLevel, routeNumber);
        }

        public static int GetLevelUpCost(int currentLevel)
        {
            return activeConfig == null
                ? Mathf.Max(1, 25 + (Mathf.Max(1, currentLevel) - 1) * 15)
                : activeConfig.GetLevelUpCost(currentLevel);
        }
    }
}
