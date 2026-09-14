using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    [Serializable]
    public sealed class CreatureInstance
    {
        public string InstanceId;
        public CreatureDefinition Definition;
        public int Level = 1;
        public int CurrentHP;
        public bool IsBoss { get; private set; }
        public const int MaxEquippedMoves = 4;
        public List<int> LearnedMoveIds = new List<int>();
        public List<int> EquippedMoveIds = new List<int>();

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
            IsBoss = isBoss;
            CurrentHP = MaxHP;
            InitializeMoves();
        }

        public void InitializeMoves()
        {
            LearnedMoveIds.Clear();
            EquippedMoveIds.Clear();
            if (Definition == null || Definition.Learnset == null) return;
            foreach (LearnableMove entry in Definition.Learnset)
            {
                if (entry.Move == null || entry.Level > Level || LearnedMoveIds.Contains(entry.Move.Id)) continue;
                LearnedMoveIds.Add(entry.Move.Id);
                if (EquippedMoveIds.Count < MaxEquippedMoves) EquippedMoveIds.Add(entry.Move.Id);
            }
        }

        public MoveDefinition FindMove(int id)
        {
            if (Definition == null) return null;
            if (Definition.Learnset != null)
                foreach (LearnableMove entry in Definition.Learnset)
                    if (entry.Move != null && entry.Move.Id == id) return entry.Move;
            SkillNode node = FindSkill(id);
            return node == null ? null : node.Move;
        }

        public SkillNode FindSkill(int id)
        {
            if (Definition != null && Definition.SkillTree != null)
                foreach (SkillNode node in Definition.SkillTree)
                    if (node != null && node.Move != null && node.Move.Id == id) return node;
            return null;
        }

        public List<MoveDefinition> GetEquippedMoves()
        {
            var moves = new List<MoveDefinition>();
            if (EquippedMoveIds == null) return moves;
            foreach (int id in EquippedMoveIds)
            {
                MoveDefinition move = FindMove(id);
                if (move != null && LearnedMoveIds.Contains(id) && !moves.Contains(move)) moves.Add(move);
                if (moves.Count == MaxEquippedMoves) break;
            }
            return moves;
        }

        // Save v5 had no loadout. New saves preserve purchased moves and the chosen slots.
        public void RestoreMoves(List<int> learned, List<int> equipped)
        {
            if (learned == null || equipped == null) { InitializeMoves(); return; }
            LearnedMoveIds = new List<int>();
            EquippedMoveIds = new List<int>();
            foreach (int id in learned)
            {
                SkillNode skill = FindSkill(id);
                if (FindMove(id) != null && (skill == null || Level >= skill.RequiredLevel)
                    && !LearnedMoveIds.Contains(id)) LearnedMoveIds.Add(id);
            }
            foreach (int id in equipped)
            {
                if (EquippedMoveIds.Count >= MaxEquippedMoves) break;
                EquippedMoveIds.Add(LearnedMoveIds.Contains(id) && !EquippedMoveIds.Contains(id) ? id : 0);
            }
            if (LearnedMoveIds.Count == 0) InitializeMoves();
            else if (GetEquippedMoves().Count == 0)
            {
                if (EquippedMoveIds.Count == 0) EquippedMoveIds.Add(LearnedMoveIds[0]);
                else EquippedMoveIds[0] = LearnedMoveIds[0];
            }
        }

        public string GetSkillPurchaseBlock(int id, int gold)
        {
            SkillNode node = FindSkill(id);
            if (node == null) return "Golpe indisponivel para este Pokemon.";
            if (LearnedMoveIds.Contains(id)) return "Golpe ja aprendido.";
            if (Level < node.RequiredLevel) return "Requer nivel " + node.RequiredLevel + ".";
            if (node.Prerequisite != null && !LearnedMoveIds.Contains(node.Prerequisite.Id))
                return "Aprenda " + node.Prerequisite.MoveName + " primeiro.";
            if (gold < Math.Max(0, node.Cost)) return "Moedas insuficientes.";
            return null;
        }

        public bool TryPurchaseSkill(int id, ref int gold, out string message)
        {
            message = GetSkillPurchaseBlock(id, gold);
            if (message != null) return false;
            SkillNode node = FindSkill(id);
            gold -= Math.Max(0, node.Cost);
            LearnedMoveIds.Add(id);
            message = "Aprendeu " + node.Move.MoveName + ". Equipe-o em um dos quatro slots.";
            return true;
        }

        public bool TryEquipMove(int id, int slot, out string message)
        {
            message = "Escolha um golpe aprendido e um dos quatro slots.";
            if (!LearnedMoveIds.Contains(id) || FindMove(id) == null || slot < 0 || slot >= MaxEquippedMoves) return false;
            if (EquippedMoveIds.Contains(id)) { message = "Este golpe ja esta equipado."; return false; }
            // Preserve the selected slot, including gaps; zero denotes an empty slot.
            while (EquippedMoveIds.Count <= slot) EquippedMoveIds.Add(0);
            EquippedMoveIds[slot] = id;
            message = "Equipou " + FindMove(id).MoveName + ".";
            return true;
        }

        public string GetEvolutionBlock(int gold)
        {
            if (Definition == null || Definition.EvolutionTarget == null) return "Sem evolucao disponivel.";
            if (Level < Definition.EvolutionLevel) return "Requer nivel " + Definition.EvolutionLevel + ".";
            if (gold < Math.Max(0, Definition.EvolutionCost)) return "Moedas insuficientes.";
            return null;
        }

        public bool TryEvolve(ref int gold, out string message)
        {
            message = GetEvolutionBlock(gold);
            if (message != null) return false;
            gold -= Math.Max(0, Definition.EvolutionCost);
            Definition = Definition.EvolutionTarget;
            HealFull();
            message = "Evoluiu para " + Definition.CreatureName + "! HP restaurado.";
            return true;
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
