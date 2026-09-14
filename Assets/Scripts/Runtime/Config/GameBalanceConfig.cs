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

        public int StartingCreatureLevel { get { return Mathf.Max(1, startingCreatureLevel); } }
        public float TickIntervalSeconds { get { return Mathf.Max(0.1f, tickIntervalSeconds); } }
        public int PhasesPerWorld { get { return Mathf.Max(1, phasesPerWorld); } }
        public int EnemiesPerPhase { get { return Mathf.Max(1, enemiesPerPhase); } }
        public float BossHealthMultiplier { get { return Mathf.Max(1f, bossHealthMultiplier); } }

        public int GetLevelUpCost(int currentLevel)
        {
            int normalizedLevel = Mathf.Max(1, currentLevel);
            return Mathf.Max(1, baseLevelUpCost + (normalizedLevel - 1) * levelUpCostGrowth);
        }

        public int GetEnemyLevel(int worldNumber, int phaseNumber, int playerLevel)
        {
            return ProgressionRules.CalculateEnemyLevel(worldNumber, phaseNumber, playerLevel,
                PhasesPerWorld, enemyLevelsPerPhase, enemyPlayerLevelRatio);
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
            baseGoldReward = 4;
            goldWorldBonus = 1;
            baseLevelUpCost = 25;
            levelUpCostGrowth = 15;
            enemyLevelsPerPhase = 1.25f;
            enemyPlayerLevelRatio = 0.9f;
            bossLevelBonus = 2;
            bossHealthMultiplier = 1.6f;
        }
    }
}
