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
        public bool IsBoss { get; private set; }

        public CreatureInstance()
        {
        }

        public CreatureInstance(CreatureDefinition definition, int level)
            : this(definition, level, false)
        {
        }

        public CreatureInstance(CreatureDefinition definition, int level, bool isBoss)
        {
            InstanceId = Guid.NewGuid().ToString("N");
            Definition = definition;
            Level = Mathf.Max(1, level);
            Experience = 0;
            IsBoss = isBoss;
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
            get { return Mathf.Max(1, IsBoss ? Mathf.CeilToInt(Stats.HP * ProgressionRules.BossHealthMultiplier) : Stats.HP); }
        }

        public bool IsFainted
        {
            get { return CurrentHP <= 0; }
        }

        public int ExperienceToNextLevel
        {
            get { return ProgressionRules.GetExperienceToNextLevel(Level); }
        }

        public float ExperienceProgress
        {
            get
            {
                int required = ExperienceToNextLevel;
                return required <= 0 ? 0f : Mathf.Clamp01((float)Experience / required);
            }
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

        public int Heal(int amount)
        {
            int previousHP = CurrentHP;
            CurrentHP = Mathf.Clamp(CurrentHP + Mathf.Max(0, amount), 0, MaxHP);
            return CurrentHP - previousHP;
        }

        public bool GainExperience(int amount)
        {
            bool leveledUp = false;
            Experience += Mathf.Max(0, amount);

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                // Subir de nivel restaura completamente o Pokemon ativo.
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
