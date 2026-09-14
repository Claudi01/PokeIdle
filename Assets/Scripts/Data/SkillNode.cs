using System;
using UnityEngine;

namespace PokeIdle
{
    [Serializable]
    public sealed class SkillNode
    {
        public MoveDefinition Move;
        [Min(1)] public int RequiredLevel = 1;
        [Min(0)] public int Cost;
        public MoveDefinition Prerequisite;

        public SkillNode(MoveDefinition move, int level, int cost, MoveDefinition prerequisite = null)
        {
            Move = move;
            RequiredLevel = level;
            Cost = cost;
            Prerequisite = prerequisite;
        }
    }
}
