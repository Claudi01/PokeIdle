using UnityEngine;

namespace PokeIdle
{
    public sealed class DemoContentSet
    {
        public CreatureDefinition Starter;
        public CreatureDefinition WildCreature;
    }

    public static class DemoContent
    {
        public static DemoContentSet Load()
        {
            CreatureDefinition starter = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Ember");
            CreatureDefinition wild = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Buglet");

            if (starter != null && wild != null)
            {
                return new DemoContentSet { Starter = starter, WildCreature = wild };
            }

            return CreateFallbackContent();
        }

        private static DemoContentSet CreateFallbackContent()
        {
            MoveDefinition quickHit = CreateMove("Golpe Rápido", ElementalType.Normal, MoveCategory.Physical, 40, 1);
            MoveDefinition ember = CreateMove("Brasa", ElementalType.Fire, MoveCategory.Special, 45, 2);
            MoveDefinition tackle = CreateMove("Investida", ElementalType.Normal, MoveCategory.Physical, 40, 1);

            CreatureDefinition starter = CreateCreature(
                4,
                "Charmander",
                ElementalType.Fire,
                FarmClass.Attacker,
                new BaseStats(45, 55, 40, 60, 50, 65),
                new LearnableMove(1, quickHit),
                new LearnableMove(1, ember));

            CreatureDefinition wild = CreateCreature(
                10,
                "Caterpie",
                ElementalType.Bug,
                FarmClass.Attacker,
                new BaseStats(38, 48, 35, 30, 35, 45),
                new LearnableMove(1, tackle));

            return new DemoContentSet { Starter = starter, WildCreature = wild };
        }

        private static MoveDefinition CreateMove(string name, ElementalType type, MoveCategory category, int power, int id)
        {
            MoveDefinition move = ScriptableObject.CreateInstance<MoveDefinition>();
            move.hideFlags = HideFlags.HideAndDontSave;
            move.Id = id;
            move.MoveName = name;
            move.Type = type;
            move.Category = category;
            move.Power = power;
            move.Accuracy = 100;
            move.MaxPP = 20;
            return move;
        }

        private static CreatureDefinition CreateCreature(
            int id,
            string name,
            ElementalType type,
            FarmClass farmClass,
            BaseStats stats,
            params LearnableMove[] moves)
        {
            CreatureDefinition creature = ScriptableObject.CreateInstance<CreatureDefinition>();
            creature.hideFlags = HideFlags.HideAndDontSave;
            creature.Id = id;
            creature.CreatureName = name;
            creature.PrimaryType = type;
            creature.FarmAI = farmClass;
            creature.BaseRarity = Rarity.Common;
            creature.BaseStats = stats;
            creature.Learnset.AddRange(moves);
            return creature;
        }
    }
}
