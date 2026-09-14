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
        public int SpawnWeight;

        public WildCreatureEncounter(CreatureDefinition creature, int minimumStage)
            : this(creature, minimumStage, 0)
        {
        }

        public WildCreatureEncounter(CreatureDefinition creature, int minimumStage, int spawnWeight)
        {
            Creature = creature;
            MinimumStage = Mathf.Max(1, minimumStage);
            SpawnWeight = Mathf.Max(1, spawnWeight > 0 ? spawnWeight : creature == null ? 1 : creature.WildSpawnWeight);
        }
    }

    public sealed class DemoContentSet
    {
        public CreatureDefinition Starter;
        public CreatureDefinition WildCreature;
        public List<WildCreatureEncounter> WildEncounters = new List<WildCreatureEncounter>();

        public CreatureDefinition FindCreature(int id)
        {
            CreatureDefinition creature = Starter;
            for (int i = 0; creature != null && i < 10; i++, creature = creature.EvolutionTarget)
                if (creature.Id == id) return creature;
            return null;
        }
    }

    public static class DemoContent
    {
        public static DemoContentSet Load()
        {
            CreatureDefinition starter = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Ember");
            CreatureDefinition caterpie = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Buglet");
            CreatureDefinition metapod = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Metapod");
            CreatureDefinition weedle = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Weedle");
            CreatureDefinition pidgey = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Pidgey");
            CreatureDefinition rattata = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Rattata");
            CreatureDefinition squirtle = Resources.Load<CreatureDefinition>("PokeIdle/Demo/Creatures/Squirtle");

            if (starter != null && caterpie != null)
            {
                if (caterpie.EvolutionTarget == null && metapod != null)
                {
                    caterpie.EvolutionTarget = metapod;
                    caterpie.EvolutionLevel = 7;
                }

                var encounters = new List<WildCreatureEncounter>
                {
                    new WildCreatureEncounter(caterpie, 1)
                };

                AddEncounterIfAvailable(encounters, weedle, 1);
                AddEncounterIfAvailable(encounters, pidgey, 1);
                AddEncounterIfAvailable(encounters, rattata, 1);

                if (squirtle != null)
                {
                    encounters.Add(new WildCreatureEncounter(squirtle, 2));
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
            MoveDefinition gust = CreateMove("Rajada de Vento", ElementalType.Flying, MoveCategory.Special, 40, 5);
            MoveDefinition poisonSting = CreateMove("Ferroada", ElementalType.Poison, MoveCategory.Physical, 40, 6);
            MoveDefinition metalClaw = CreateMove("Garra de Metal", ElementalType.Steel, MoveCategory.Physical, 50, 7);
            MoveDefinition dragonBreath = CreateMove("Sopro do Dragao", ElementalType.Dragon, MoveCategory.Special, 60, 8);
            MoveDefinition brickBreak = CreateMove("Quebra-Telha", ElementalType.Fighting, MoveCategory.Physical, 75, 9);
            MoveDefinition flamethrower = CreateMove("Lanca-Chamas", ElementalType.Fire, MoveCategory.Special, 90, 10);
            MoveDefinition thunderPunch = CreateMove("Soco Trovao", ElementalType.Electric, MoveCategory.Physical, 75, 11);
            MoveDefinition slash = CreateMove("Talho", ElementalType.Normal, MoveCategory.Physical, 70, 12);

            CreatureDefinition starter = CreateCreature(
                4,
                "Charmander",
                ElementalType.Fire,
                FarmClass.Attacker,
                new BaseStats(45, 55, 40, 60, 50, 65),
                new LearnableMove(1, quickHit),
                new LearnableMove(1, ember));

            CreatureDefinition charmeleon = CreateCreature(5, "Charmeleon", ElementalType.Fire,
                FarmClass.Attacker, new BaseStats(58, 64, 58, 80, 65, 80),
                new LearnableMove(1, quickHit), new LearnableMove(1, ember));
            StarterSkillContent.Configure(starter, charmeleon, quickHit, ember, metalClaw,
                slash, brickBreak, flamethrower, dragonBreath, thunderPunch);

            CreatureDefinition caterpie = CreateCreature(
                10,
                "Caterpie",
                ElementalType.Bug,
                FarmClass.Attacker,
                new BaseStats(38, 48, 35, 30, 35, 45),
                new LearnableMove(1, tackle));

            CreatureDefinition metapod = CreateCreature(
                11,
                "Metapod",
                ElementalType.Bug,
                FarmClass.Tank,
                new BaseStats(50, 35, 55, 25, 35, 30),
                new LearnableMove(1, tackle));

            caterpie.EvolutionTarget = metapod;
            caterpie.EvolutionLevel = 7;
            caterpie.WildSpawnWeight = 6;

            CreatureDefinition weedle = CreateCreature(
                13,
                "Weedle",
                ElementalType.Bug,
                FarmClass.Attacker,
                new BaseStats(40, 35, 30, 20, 20, 50),
                new LearnableMove(1, poisonSting));

            CreatureDefinition pidgey = CreateCreature(
                16,
                "Pidgey",
                ElementalType.Flying,
                FarmClass.Speedster,
                new BaseStats(40, 45, 40, 35, 35, 56),
                new LearnableMove(1, gust));

            CreatureDefinition rattata = CreateCreature(
                19,
                "Rattata",
                ElementalType.Normal,
                FarmClass.Attacker,
                new BaseStats(30, 56, 35, 25, 35, 72),
                new LearnableMove(1, tackle));

            weedle.WildSpawnWeight = 2;
            pidgey.WildSpawnWeight = 2;
            rattata.WildSpawnWeight = 2;

            CreatureDefinition squirtle = CreateCreature(
                7,
                "Squirtle",
                ElementalType.Water,
                FarmClass.Tank,
                new BaseStats(44, 48, 65, 50, 64, 43),
                new LearnableMove(1, waterGun));
            squirtle.WildSpawnWeight = 4;

            return new DemoContentSet
            {
                Starter = starter,
                WildCreature = caterpie,
                WildEncounters = new List<WildCreatureEncounter>
                {
                    new WildCreatureEncounter(caterpie, 1),
                    new WildCreatureEncounter(weedle, 1),
                    new WildCreatureEncounter(pidgey, 1),
                    new WildCreatureEncounter(rattata, 1),
                    new WildCreatureEncounter(squirtle, 2)
                }
            };
        }

        private static void AddEncounterIfAvailable(
            List<WildCreatureEncounter> encounters,
            CreatureDefinition creature,
            int minimumStage)
            {
            if (creature != null)
            {
                encounters.Add(new WildCreatureEncounter(creature, minimumStage));
            }
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
