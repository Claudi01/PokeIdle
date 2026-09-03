using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    [Serializable]
    public sealed class WildCreatureEncounter
    {
        public CreatureDefinition Creature;
        public int MinimumStage;

        public WildCreatureEncounter(CreatureDefinition creature, int minimumStage)
        {
            Creature = creature;
            MinimumStage = Mathf.Max(1, minimumStage);
        }
    }

    public sealed class DemoContentSet
    {
        public CreatureDefinition Starter;
        public CreatureDefinition WildCreature;
        public List<WildCreatureEncounter> WildEncounters = new List<WildCreatureEncounter>();
    }

    public static class DemoContent
    {
        public static DemoContentSet Load()
        {
            CreatureDefinition starter = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Ember");
            CreatureDefinition caterpie = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Buglet");
            CreatureDefinition squirtle = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Squirtle");

            if (starter != null && caterpie != null)
            {
                var encounters = new List<WildCreatureEncounter>
                {
                    new WildCreatureEncounter(caterpie, 1)
                };

                if (squirtle != null)
                {
                    encounters.Add(new WildCreatureEncounter(squirtle, 5));
                }

                return new DemoContentSet
                {
                    Starter = starter,
                    WildCreature = caterpie,
                    WildEncounters = encounters
                };
            }

            return CreateFallbackContent();
        }

        private static DemoContentSet CreateFallbackContent()
        {
            MoveDefinition quickHit = CreateMove("Golpe Rapido", ElementalType.Normal, MoveCategory.Physical, 40, 1);
            MoveDefinition ember = CreateMove("Brasa", ElementalType.Fire, MoveCategory.Special, 50, 2);
            MoveDefinition tackle = CreateMove("Investida", ElementalType.Normal, MoveCategory.Physical, 40, 3);
            MoveDefinition waterGun = CreateMove("Jato de Agua", ElementalType.Water, MoveCategory.Special, 40, 4);

            CreatureDefinition starter = CreateCreature(
                4,
                "Charmander",
                ElementalType.Fire,
                FarmClass.Attacker,
                new BaseStats(45, 55, 40, 60, 50, 65),
                new LearnableMove(1, quickHit),
                new LearnableMove(1, ember));

            CreatureDefinition caterpie = CreateCreature(
                10,
                "Caterpie",
                ElementalType.Bug,
                FarmClass.Attacker,
                new BaseStats(38, 48, 35, 30, 35, 45),
                new LearnableMove(1, tackle));

            CreatureDefinition squirtle = CreateCreature(
                7,
                "Squirtle",
                ElementalType.Water,
                FarmClass.Tank,
                new BaseStats(44, 48, 65, 50, 64, 43),
                new LearnableMove(1, waterGun));

            return new DemoContentSet
            {
                Starter = starter,
                WildCreature = caterpie,
                WildEncounters = new List<WildCreatureEncounter>
                {
                    new WildCreatureEncounter(caterpie, 1),
                    new WildCreatureEncounter(squirtle, 5)
                }
            };
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
