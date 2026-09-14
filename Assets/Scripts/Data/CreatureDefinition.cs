using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    [CreateAssetMenu(fileName = "Creature", menuName = "PokeIdle/Creature Definition")]
    public sealed class CreatureDefinition : ScriptableObject
    {
        [Header("Identity")]
        public int Id;
        public string CreatureName;
        public ElementalType PrimaryType;
        public bool HasSecondaryType;
        public ElementalType SecondaryType;
        public FarmClass FarmAI = FarmClass.Attacker;
        public Rarity BaseRarity = Rarity.Common;

        [Header("Encounter")]
        [Min(1)] public int WildSpawnWeight = 1;

        [Header("Base stats")]
        public BaseStats BaseStats;

        [Header("Content")]
        public Sprite SpriteFront;
        public Sprite SpriteBack;
        public Sprite Icon;
        public List<LearnableMove> Learnset = new List<LearnableMove>();
        public List<SkillNode> SkillTree = new List<SkillNode>();

        [Header("Evolution")]
        public CreatureDefinition EvolutionTarget;
        public int EvolutionLevel;
        [Min(0)] public int EvolutionCost;

        public BaseStats GetStatsAtLevel(int level)
        {
            int normalizedLevel = Mathf.Max(1, level);
            int levelGrowth = normalizedLevel - 1;

            return new BaseStats(
                BaseStats.HP + levelGrowth * 3,
                BaseStats.Attack + levelGrowth * 2,
                BaseStats.Defense + levelGrowth * 2,
                BaseStats.SpAttack + levelGrowth * 2,
                BaseStats.SpDefense + levelGrowth * 2,
                BaseStats.Speed + levelGrowth * 2);
        }
    }
}
