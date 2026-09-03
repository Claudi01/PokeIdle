using UnityEngine;

namespace PokeIdle
{
    /// <summary>
    /// Regras centrais de progressao. Manter estes valores juntos facilita o balanceamento
    /// enquanto o projeto ainda nao possui uma tela de configuracao.
    /// </summary>
    public static class ProgressionRules
    {
        public const int SaveVersion = 2;
        public const int StartingCreatureLevel = 1;

        private const int BaseExperienceToNextLevel = 75;
        private const int ExperienceGrowthPerLevel = 35;

        public static int GetExperienceToNextLevel(int level)
        {
            int normalizedLevel = Mathf.Max(1, level);
            return BaseExperienceToNextLevel + (normalizedLevel - 1) * ExperienceGrowthPerLevel;
        }

        public static int GetExperienceReward(int enemyLevel, int routeNumber)
        {
            int normalizedEnemyLevel = Mathf.Max(1, enemyLevel);
            int routeBonus = Mathf.Max(0, routeNumber - 1);
            return Mathf.Max(1, 7 + normalizedEnemyLevel + routeBonus);
        }

        public static int GetGoldReward(int enemyLevel, int routeNumber)
        {
            int normalizedEnemyLevel = Mathf.Max(1, enemyLevel);
            int routeBonus = Mathf.Max(0, routeNumber - 1);
            return Mathf.Max(1, 4 + normalizedEnemyLevel + routeBonus);
        }
    }
}
