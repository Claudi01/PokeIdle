using System;
using UnityEngine;

namespace PokeIdle
{
    [Serializable]
    public sealed class CreatureInstance
    {
        public string InstanceId;
        public CreatureDefinition Definition;
        public int Level = 1;
        public int Experience;
        public int CurrentHP;

        public CreatureInstance()
        {
        }

        public CreatureInstance(CreatureDefinition definition, int level)
        {
            InstanceId = Guid.NewGuid().ToString("N");
            Definition = definition;
            Level = Mathf.Max(1, level);
            Experience = 0;
            CurrentHP = MaxHP;
        }

        public BaseStats Stats
        {
            get
            {
                return Definition == null
                    ? new BaseStats(1, 1, 1, 1, 1, 1)
                    : Definition.GetStatsAtLevel(Level);
            }
        }

        public int MaxHP
        {
            get { return Mathf.Max(1, Stats.HP); }
        }

        public bool IsFainted
        {
            get { return CurrentHP <= 0; }
        }

        public int ExperienceToNextLevel
        {
            get { return 20 + Level * 10; }
        }

        public void EnsureValid()
        {
            Level = Mathf.Max(1, Level);
            CurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);
            if (CurrentHP == 0)
            {
                CurrentHP = MaxHP;
            }
        }

        public void TakeDamage(int amount)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(0, amount));
        }

        public void HealFull()
        {
            CurrentHP = MaxHP;
        }

        public bool GainExperience(int amount)
        {
            bool leveledUp = false;
            Experience += Mathf.Max(0, amount);

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                HealFull();
                leveledUp = true;
            }

            return leveledUp;
        }

        public int GetAttackStat(MoveCategory category)
        {
            return category == MoveCategory.Special ? Stats.SpAttack : Stats.Attack;
        }

        public int GetDefenseStat(MoveCategory category)
        {
            return category == MoveCategory.Special ? Stats.SpDefense : Stats.Defense;
        }
    }
}
