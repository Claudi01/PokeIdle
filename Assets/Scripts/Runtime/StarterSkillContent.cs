using System.Collections.Generic;

namespace PokeIdle
{
    // Shared defaults for the editor generator and the resource-free demo fallback.
    // Live games read the editable CreatureDefinition assets instead.
    public static class StarterSkillContent
    {
        public static void Configure(CreatureDefinition starter, CreatureDefinition charmeleon,
            MoveDefinition quickHit, MoveDefinition ember, MoveDefinition metalClaw,
            MoveDefinition slash, MoveDefinition brickBreak, MoveDefinition flamethrower,
            MoveDefinition dragonBreath, MoveDefinition thunderPunch)
        {
            starter.EvolutionTarget = charmeleon;
            starter.EvolutionLevel = 16;
            starter.EvolutionCost = 400;
            foreach (CreatureDefinition creature in new[] { starter, charmeleon })
            {
                creature.SkillTree = new List<SkillNode>
                {
                    new SkillNode(quickHit, 1, 0),
                    new SkillNode(ember, 1, 0),
                    new SkillNode(metalClaw, 4, 70, quickHit),
                    new SkillNode(slash, 7, 110, quickHit),
                    new SkillNode(brickBreak, 10, 160, metalClaw),
                    new SkillNode(flamethrower, 12, 250, ember),
                    new SkillNode(dragonBreath, 14, 280, slash),
                    new SkillNode(thunderPunch, 18, 400, brickBreak)
                };
            }
        }
    }
}
