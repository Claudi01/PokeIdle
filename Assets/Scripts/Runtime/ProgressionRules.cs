using UnityEngine;

namespace PokeIdle
{
    /// <summary>
    /// Regras centrais de progressao. Manter estes valores juntos facilita o balanceamento
    /// enquanto o projeto ainda nao possui uma tela de configuracao.
    /// </summary>
    public static class ProgressionRules
    {
        public const int SaveVersion = 5;

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

        public static int GetEnemiesPerPhase()
        {
            return activeConfig == null ? 10 : activeConfig.EnemiesPerPhase;
        }

        public static int GetExperienceToNextLevel(int level)
        {
            return activeConfig == null
                ? 75 + (Mathf.Max(1, level) - 1) * 35
                : activeConfig.GetExperienceToNextLevel(level);
        }

        public static int GetExperienceReward(int enemyLevel, int routeNumber)
        {
            return activeConfig == null
                ? Mathf.Max(1, 7 + Mathf.Max(1, enemyLevel) + Mathf.Max(0, routeNumber - 1))
                : activeConfig.GetExperienceReward(enemyLevel, routeNumber);
        }

        public static int GetEnemyLevel(int routeNumber, bool isBoss)
        {
            return activeConfig == null
                ? Mathf.Max(1, Mathf.Max(1, routeNumber) + (isBoss ? 2 : 0))
                : activeConfig.GetEnemyLevel(routeNumber, isBoss);
        }

        public static int GetGoldReward(int enemyLevel, int routeNumber)
        {
            return activeConfig == null
                ? Mathf.Max(1, 4 + Mathf.Max(1, enemyLevel) + Mathf.Max(0, routeNumber - 1))
                : activeConfig.GetGoldReward(enemyLevel, routeNumber);
        }
    }
}
