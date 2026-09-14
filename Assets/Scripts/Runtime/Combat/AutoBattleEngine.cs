using UnityEngine;

namespace PokeIdle
{
    public sealed class AttackResult
    {
        public string AttackerName;
        public string DefenderName;
        public string MoveName;
        public int Damage;
        public float Effectiveness;
        public bool Hit;
        public string Message;
    }

    public sealed class AutoBattleEngine
    {
        public AttackResult ExecuteAttack(CreatureInstance attacker, CreatureInstance defender)
        {
            var result = new AttackResult
            {
                AttackerName = GetCreatureName(attacker),
                DefenderName = GetCreatureName(defender),
                Hit = false,
                Damage = 0,
                Effectiveness = 1f
            };

            if (attacker == null || defender == null || attacker.IsFainted || defender.IsFainted)
            {
                result.Message = "O ataque não pôde ser executado.";
                return result;
            }

            MoveDefinition move = ChooseMove(attacker, defender);
            if (move == null)
            {
                result.Message = result.AttackerName + " não possui golpes equipados.";
                return result;
            }

            result.MoveName = string.IsNullOrEmpty(move.MoveName) ? "Golpe" : move.MoveName;
            if (move.Accuracy < 100 && Random.Range(1, 101) > move.Accuracy)
            {
                result.Message = result.AttackerName + " usou " + result.MoveName + ", mas errou.";
                return result;
            }

            result.Hit = true;
            if (!move.DealsDamage)
            {
                result.Message = result.AttackerName + " usou " + result.MoveName + ".";
                return result;
            }

            int attackStat = Mathf.Max(1, attacker.GetAttackStat(move.Category));
            int defenseStat = Mathf.Max(1, defender.GetDefenseStat(move.Category));
            int level = Mathf.Max(1, attacker.Level);

            float baseDamage = ((((2f * level) / 5f) + 2f) * move.Power * attackStat / defenseStat / 50f) + 2f;
            float stab = HasType(attacker.Definition, move.Type) ? 1.5f : 1f;
            float effectiveness = TypeChart.GetEffectiveness(move.Type, defender.Definition);
            float randomFactor = Random.Range(0.85f, 1.0001f);

            result.Effectiveness = effectiveness;
            result.Damage = effectiveness <= 0f
                ? 0
                : Mathf.Max(1, Mathf.FloorToInt(baseDamage * stab * effectiveness * randomFactor));

            defender.TakeDamage(result.Damage);
            result.Message = result.AttackerName + " usou " + result.MoveName + " e causou " + result.Damage + " de dano";
            if (effectiveness > 1f)
            {
                result.Message += " (super efetivo)";
            }
            else if (effectiveness > 0f && effectiveness < 1f)
            {
                result.Message += " (pouco efetivo)";
            }
            else if (effectiveness <= 0f)
            {
                result.Message = result.AttackerName + " usou " + result.MoveName + ", mas não causou dano.";
            }

            return result;
        }

        public static MoveDefinition ChooseMove(CreatureInstance attacker, CreatureInstance defender)
        {
            MoveDefinition selected = null;
            float selectedScore = -1f;

            if (attacker == null || defender == null || attacker.Definition == null)
            {
                return null;
            }

            foreach (MoveDefinition move in attacker.GetEquippedMoves())
            {
                if (!move.DealsDamage) continue;
                float score = EstimateDamage(attacker, defender, move) * Mathf.Clamp(move.Accuracy, 0, 100) / 100f;
                if (score > selectedScore)
                {
                    selected = move;
                    selectedScore = score;
                }
            }

            return selected;
        }

        public static float EstimateDamage(CreatureInstance attacker, CreatureInstance defender, MoveDefinition move)
        {
            if (attacker == null || defender == null || move == null || !move.DealsDamage) return 0f;
            float effectiveness = TypeChart.GetEffectiveness(move.Type, defender.Definition);
            if (effectiveness <= 0f) return 0f;
            float baseDamage = ((2f * Mathf.Max(1, attacker.Level) / 5f + 2f) * move.Power
                * Mathf.Max(1, attacker.GetAttackStat(move.Category))
                / Mathf.Max(1, defender.GetDefenseStat(move.Category)) / 50f) + 2f;
            return Mathf.Max(1f, baseDamage * (HasType(attacker.Definition, move.Type) ? 1.5f : 1f) * effectiveness);
        }

        private static bool HasType(CreatureDefinition creature, ElementalType type)
        {
            return creature != null && (creature.PrimaryType == type || (creature.HasSecondaryType && creature.SecondaryType == type));
        }

        private static string GetCreatureName(CreatureInstance creature)
        {
            if (creature == null || creature.Definition == null || string.IsNullOrEmpty(creature.Definition.CreatureName))
            {
                return "Criatura";
            }

            return creature.Definition.CreatureName;
        }
    }
}
