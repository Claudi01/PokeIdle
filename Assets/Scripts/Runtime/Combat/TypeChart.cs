using System;
using System.Collections.Generic;

namespace PokeIdle
{
    public static class TypeChart
    {
        private static readonly Dictionary<ElementalType, Dictionary<ElementalType, float>> Multipliers = BuildChart();

        public static float GetEffectiveness(ElementalType attackType, CreatureDefinition defender)
        {
            if (defender == null)
            {
                return 1f;
            }

            float result = GetSingleTypeMultiplier(attackType, defender.PrimaryType);
            if (defender.HasSecondaryType)
            {
                result *= GetSingleTypeMultiplier(attackType, defender.SecondaryType);
            }

            return result;
        }

        private static float GetSingleTypeMultiplier(ElementalType attackType, ElementalType defenseType)
        {
            return Multipliers[attackType][defenseType];
        }

        private static Dictionary<ElementalType, Dictionary<ElementalType, float>> BuildChart()
        {
            var chart = new Dictionary<ElementalType, Dictionary<ElementalType, float>>();
            foreach (ElementalType attackingType in Enum.GetValues(typeof(ElementalType)))
            {
                chart[attackingType] = new Dictionary<ElementalType, float>();
                foreach (ElementalType defendingType in Enum.GetValues(typeof(ElementalType)))
                {
                    chart[attackingType][defendingType] = 1f;
                }
            }

            Set(chart, ElementalType.Normal, ElementalType.Rock, 0.5f);
            Set(chart, ElementalType.Normal, ElementalType.Ghost, 0f);
            Set(chart, ElementalType.Normal, ElementalType.Steel, 0.5f);

            Set(chart, ElementalType.Fire, ElementalType.Fire, 0.5f);
            Set(chart, ElementalType.Fire, ElementalType.Water, 0.5f);
            Set(chart, ElementalType.Fire, ElementalType.Grass, 2f);
            Set(chart, ElementalType.Fire, ElementalType.Ice, 2f);
            Set(chart, ElementalType.Fire, ElementalType.Bug, 2f);
            Set(chart, ElementalType.Fire, ElementalType.Rock, 0.5f);
            Set(chart, ElementalType.Fire, ElementalType.Dragon, 0.5f);
            Set(chart, ElementalType.Fire, ElementalType.Steel, 2f);

            Set(chart, ElementalType.Water, ElementalType.Fire, 2f);
            Set(chart, ElementalType.Water, ElementalType.Water, 0.5f);
            Set(chart, ElementalType.Water, ElementalType.Grass, 0.5f);
            Set(chart, ElementalType.Water, ElementalType.Ground, 2f);
            Set(chart, ElementalType.Water, ElementalType.Rock, 2f);
            Set(chart, ElementalType.Water, ElementalType.Dragon, 0.5f);

            Set(chart, ElementalType.Electric, ElementalType.Water, 2f);
            Set(chart, ElementalType.Electric, ElementalType.Electric, 0.5f);
            Set(chart, ElementalType.Electric, ElementalType.Grass, 0.5f);
            Set(chart, ElementalType.Electric, ElementalType.Ground, 0f);
            Set(chart, ElementalType.Electric, ElementalType.Flying, 2f);
            Set(chart, ElementalType.Electric, ElementalType.Dragon, 0.5f);

            Set(chart, ElementalType.Grass, ElementalType.Fire, 0.5f);
            Set(chart, ElementalType.Grass, ElementalType.Water, 2f);
            Set(chart, ElementalType.Grass, ElementalType.Grass, 0.5f);
            Set(chart, ElementalType.Grass, ElementalType.Poison, 0.5f);
            Set(chart, ElementalType.Grass, ElementalType.Ground, 2f);
            Set(chart, ElementalType.Grass, ElementalType.Flying, 0.5f);
            Set(chart, ElementalType.Grass, ElementalType.Bug, 0.5f);
            Set(chart, ElementalType.Grass, ElementalType.Rock, 2f);
            Set(chart, ElementalType.Grass, ElementalType.Dragon, 0.5f);
            Set(chart, ElementalType.Grass, ElementalType.Steel, 0.5f);

            Set(chart, ElementalType.Ice, ElementalType.Fire, 0.5f);
            Set(chart, ElementalType.Ice, ElementalType.Water, 0.5f);
            Set(chart, ElementalType.Ice, ElementalType.Grass, 2f);
            Set(chart, ElementalType.Ice, ElementalType.Ice, 0.5f);
            Set(chart, ElementalType.Ice, ElementalType.Ground, 2f);
            Set(chart, ElementalType.Ice, ElementalType.Flying, 2f);
            Set(chart, ElementalType.Ice, ElementalType.Dragon, 2f);
            Set(chart, ElementalType.Ice, ElementalType.Steel, 0.5f);

            Set(chart, ElementalType.Fighting, ElementalType.Normal, 2f);
            Set(chart, ElementalType.Fighting, ElementalType.Ice, 2f);
            Set(chart, ElementalType.Fighting, ElementalType.Rock, 2f);
            Set(chart, ElementalType.Fighting, ElementalType.Dark, 2f);
            Set(chart, ElementalType.Fighting, ElementalType.Steel, 2f);
            Set(chart, ElementalType.Fighting, ElementalType.Poison, 0.5f);
            Set(chart, ElementalType.Fighting, ElementalType.Flying, 0.5f);
            Set(chart, ElementalType.Fighting, ElementalType.Psychic, 0.5f);
            Set(chart, ElementalType.Fighting, ElementalType.Bug, 0.5f);
            Set(chart, ElementalType.Fighting, ElementalType.Fairy, 0.5f);
            Set(chart, ElementalType.Fighting, ElementalType.Ghost, 0f);

            Set(chart, ElementalType.Poison, ElementalType.Grass, 2f);
            Set(chart, ElementalType.Poison, ElementalType.Poison, 0.5f);
            Set(chart, ElementalType.Poison, ElementalType.Ground, 0.5f);
            Set(chart, ElementalType.Poison, ElementalType.Rock, 0.5f);
            Set(chart, ElementalType.Poison, ElementalType.Ghost, 0.5f);
            Set(chart, ElementalType.Poison, ElementalType.Steel, 0f);
            Set(chart, ElementalType.Poison, ElementalType.Fairy, 2f);

            Set(chart, ElementalType.Ground, ElementalType.Fire, 2f);
            Set(chart, ElementalType.Ground, ElementalType.Electric, 2f);
            Set(chart, ElementalType.Ground, ElementalType.Grass, 0.5f);
            Set(chart, ElementalType.Ground, ElementalType.Poison, 2f);
            Set(chart, ElementalType.Ground, ElementalType.Flying, 0f);
            Set(chart, ElementalType.Ground, ElementalType.Bug, 0.5f);
            Set(chart, ElementalType.Ground, ElementalType.Rock, 2f);
            Set(chart, ElementalType.Ground, ElementalType.Steel, 2f);

            Set(chart, ElementalType.Flying, ElementalType.Grass, 2f);
            Set(chart, ElementalType.Flying, ElementalType.Electric, 0.5f);
            Set(chart, ElementalType.Flying, ElementalType.Fighting, 2f);
            Set(chart, ElementalType.Flying, ElementalType.Bug, 2f);
            Set(chart, ElementalType.Flying, ElementalType.Rock, 0.5f);
            Set(chart, ElementalType.Flying, ElementalType.Steel, 0.5f);

            Set(chart, ElementalType.Psychic, ElementalType.Fighting, 2f);
            Set(chart, ElementalType.Psychic, ElementalType.Poison, 2f);
            Set(chart, ElementalType.Psychic, ElementalType.Psychic, 0.5f);
            Set(chart, ElementalType.Psychic, ElementalType.Steel, 0.5f);
            Set(chart, ElementalType.Psychic, ElementalType.Dark, 0f);

            Set(chart, ElementalType.Bug, ElementalType.Fire, 0.5f);
            Set(chart, ElementalType.Bug, ElementalType.Grass, 2f);
            Set(chart, ElementalType.Bug, ElementalType.Fighting, 0.5f);
            Set(chart, ElementalType.Bug, ElementalType.Poison, 0.5f);
            Set(chart, ElementalType.Bug, ElementalType.Flying, 0.5f);
            Set(chart, ElementalType.Bug, ElementalType.Psychic, 2f);
            Set(chart, ElementalType.Bug, ElementalType.Ghost, 0.5f);
            Set(chart, ElementalType.Bug, ElementalType.Dark, 2f);
            Set(chart, ElementalType.Bug, ElementalType.Steel, 0.5f);
            Set(chart, ElementalType.Bug, ElementalType.Fairy, 0.5f);

            Set(chart, ElementalType.Rock, ElementalType.Fire, 2f);
            Set(chart, ElementalType.Rock, ElementalType.Ice, 2f);
            Set(chart, ElementalType.Rock, ElementalType.Flying, 2f);
            Set(chart, ElementalType.Rock, ElementalType.Bug, 2f);
            Set(chart, ElementalType.Rock, ElementalType.Fighting, 0.5f);
            Set(chart, ElementalType.Rock, ElementalType.Ground, 0.5f);
            Set(chart, ElementalType.Rock, ElementalType.Steel, 0.5f);

            Set(chart, ElementalType.Ghost, ElementalType.Normal, 0f);
            Set(chart, ElementalType.Ghost, ElementalType.Psychic, 2f);
            Set(chart, ElementalType.Ghost, ElementalType.Ghost, 2f);
            Set(chart, ElementalType.Ghost, ElementalType.Dark, 0.5f);

            Set(chart, ElementalType.Dragon, ElementalType.Dragon, 2f);
            Set(chart, ElementalType.Dragon, ElementalType.Steel, 0.5f);
            Set(chart, ElementalType.Dragon, ElementalType.Fairy, 0f);

            Set(chart, ElementalType.Dark, ElementalType.Fighting, 0.5f);
            Set(chart, ElementalType.Dark, ElementalType.Psychic, 2f);
            Set(chart, ElementalType.Dark, ElementalType.Ghost, 2f);
            Set(chart, ElementalType.Dark, ElementalType.Dark, 0.5f);
            Set(chart, ElementalType.Dark, ElementalType.Fairy, 0.5f);

            Set(chart, ElementalType.Steel, ElementalType.Fire, 0.5f);
            Set(chart, ElementalType.Steel, ElementalType.Water, 0.5f);
            Set(chart, ElementalType.Steel, ElementalType.Electric, 0.5f);
            Set(chart, ElementalType.Steel, ElementalType.Ice, 2f);
            Set(chart, ElementalType.Steel, ElementalType.Rock, 2f);
            Set(chart, ElementalType.Steel, ElementalType.Steel, 0.5f);
            Set(chart, ElementalType.Steel, ElementalType.Fairy, 2f);

            Set(chart, ElementalType.Fairy, ElementalType.Fire, 0.5f);
            Set(chart, ElementalType.Fairy, ElementalType.Fighting, 2f);
            Set(chart, ElementalType.Fairy, ElementalType.Poison, 0.5f);
            Set(chart, ElementalType.Fairy, ElementalType.Dragon, 2f);
            Set(chart, ElementalType.Fairy, ElementalType.Dark, 2f);
            Set(chart, ElementalType.Fairy, ElementalType.Steel, 0.5f);

            return chart;
        }

        private static void Set(
            Dictionary<ElementalType, Dictionary<ElementalType, float>> chart,
            ElementalType attackingType,
            ElementalType defendingType,
            float multiplier)
        {
            chart[attackingType][defendingType] = multiplier;
        }
    }
}
