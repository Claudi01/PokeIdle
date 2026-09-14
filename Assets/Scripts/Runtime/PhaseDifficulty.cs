using System;
using System.Collections.Generic;

namespace PokeIdle
{
    [Serializable]
    public sealed class PhaseDifficultyRecord
    {
        public int World;
        public int Phase;
        public int EnemyLevel;
    }

    public sealed class PhaseDifficulty
    {
        private readonly List<PhaseDifficultyRecord> records = new List<PhaseDifficultyRecord>();

        public int GetLevel(int world, int phase, int playerLevel, bool boss)
        {
            PhaseDifficultyRecord found = records.Find(r => r.World == world && r.Phase == phase);
            if (found == null)
            {
                found = new PhaseDifficultyRecord { World = world, Phase = phase,
                    EnemyLevel = ProgressionRules.GetEnemyLevel(world, phase, playerLevel) };
                records.Add(found);
            }
            return found.EnemyLevel + (boss ? ProgressionRules.BossLevelBonus : 0);
        }

        public void Restore(List<PhaseDifficultyRecord> saved)
        {
            records.Clear();
            if (saved == null) return;
            foreach (PhaseDifficultyRecord record in saved)
                if (record != null && record.World >= 1 && record.Phase >= 0 && record.EnemyLevel > 0
                    && record.EnemyLevel <= int.MaxValue - 100
                    && !records.Exists(r => r.World == record.World && r.Phase == record.Phase))
                    records.Add(new PhaseDifficultyRecord { World = record.World, Phase = record.Phase, EnemyLevel = record.EnemyLevel });
        }

        public List<PhaseDifficultyRecord> Export()
        {
            var result = new List<PhaseDifficultyRecord>();
            foreach (PhaseDifficultyRecord record in records)
                result.Add(new PhaseDifficultyRecord { World = record.World, Phase = record.Phase, EnemyLevel = record.EnemyLevel });
            return result;
        }
    }
}
