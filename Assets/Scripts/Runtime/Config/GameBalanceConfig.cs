using UnityEngine;

namespace PokeIdle
{
    /// <summary>
    /// Valores de balanceamento editaveis pelo Inspector.
    /// Crie/edite o asset em Assets/Resources/PokeIdle/Config.
    /// </summary>
    [CreateAssetMenu(menuName = "PokeIdle/Game Balance Config", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Inicio")]
        [SerializeField, Min(1)] private int startingCreatureLevel = 1;

        [Header("Ritmo e progressao")]
        [SerializeField, Min(0.1f)] private float tickIntervalSeconds = 1f;
        [SerializeField, Min(1)] private int phasesPerWorld = 10;
        [SerializeField, Min(1)] private int enemiesPerPhase = 10;

        [Header("Experiencia")]
        [SerializeField, Min(1)] private int baseExperienceToNextLevel = 75;
        [SerializeField, Min(0)] private int experienceGrowthPerLevel = 35;
        [SerializeField, Min(0)] private int baseExperienceReward = 7;
        [SerializeField, Min(0)] private int experienceWorldBonus = 1;

        [Header("Ouro")]
        [SerializeField, Min(0)] private int baseGoldReward = 4;
        [SerializeField, Min(0)] private int goldWorldBonus = 1;

        [Header("Boss")]
        [SerializeField, Min(0)] private int bossLevelBonus = 2;
        [SerializeField, Min(1f)] private float bossHealthMultiplier = 1.6f;

        public int StartingCreatureLevel { get { return Mathf.Max(1, startingCreatureLevel); } }
        public float TickIntervalSeconds { get { return Mathf.Max(0.1f, tickIntervalSeconds); } }
        public int PhasesPerWorld { get { return Mathf.Max(1, phasesPerWorld); } }
        public int EnemiesPerPhase { get { return Mathf.Max(1, enemiesPerPhase); } }
        public float BossHealthMultiplier { get { return Mathf.Max(1f, bossHealthMultiplier); } }

        public int GetExperienceToNextLevel(int level)
        {
            int normalizedLevel = Mathf.Max(1, level);
            return Mathf.Max(1, baseExperienceToNextLevel + (normalizedLevel - 1) * experienceGrowthPerLevel);
        }

        public int GetExperienceReward(int enemyLevel, int worldNumber)
        {
            int normalizedEnemyLevel = Mathf.Max(1, enemyLevel);
            int worldBonus = Mathf.Max(0, worldNumber - 1) * experienceWorldBonus;
            return Mathf.Max(1, baseExperienceReward + normalizedEnemyLevel + worldBonus);
        }

        public int GetEnemyLevel(int worldNumber, bool isBoss)
        {
            int bossBonus = isBoss ? bossLevelBonus : 0;
            return Mathf.Max(1, Mathf.Max(1, worldNumber) + bossBonus);
        }

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
            baseExperienceToNextLevel = 75;
            experienceGrowthPerLevel = 35;
            baseExperienceReward = 7;
            experienceWorldBonus = 1;
            baseGoldReward = 4;
            goldWorldBonus = 1;
            bossLevelBonus = 2;
            bossHealthMultiplier = 1.6f;
        }
    }
}
