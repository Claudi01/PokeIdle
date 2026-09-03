using System;

namespace PokeIdle
{
    public enum ElementalType
    {
        Normal,
        Fire,
        Water,
        Grass,
        Electric,
        Ice,
        Fighting,
        Poison,
        Ground,
        Flying,
        Psychic,
        Bug,
        Rock,
        Ghost,
        Dragon,
        Dark,
        Steel,
        Fairy
    }

    public enum FarmClass
    {
        Tank,
        Attacker,
        Support,
        Speedster
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythical
    }

    public enum MoveCategory
    {
        Physical,
        Special,
        Status
    }

    public enum BattlePhase
    {
        Searching,
        Battling,
        Recovering
    }

    [Serializable]
    public struct BaseStats
    {
        public int HP;
        public int Attack;
        public int Defense;
        public int SpAttack;
        public int SpDefense;
        public int Speed;

        public BaseStats(int hp, int attack, int defense, int spAttack, int spDefense, int speed)
        {
            HP = hp;
            Attack = attack;
            Defense = defense;
            SpAttack = spAttack;
            SpDefense = spDefense;
            Speed = speed;
        }
    }

    [Serializable]
    public struct LearnableMove
    {
        public int Level;
        public MoveDefinition Move;

        public LearnableMove(int level, MoveDefinition move)
        {
            Level = level;
            Move = move;
        }
    }
}
