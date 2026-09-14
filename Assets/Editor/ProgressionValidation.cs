using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PokeIdle.Editor
{
    public static class ProgressionValidation
    {
        private static int checks;

        [MenuItem("PokeIdle/Validate Progression")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Pare o Play Mode para validar.");
            checks = 0;
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            UnityEngine.Random.State randomState = UnityEngine.Random.state;
            try
            {
                config.ResetToDefaults();
                ProgressionRules.SetBalanceConfig(config);
                DemoContentSet content = DemoContent.Load();
                Check(content.Starter != null && content.Starter.SkillTree.Count == 8, "Eight skill nodes loaded");
                Check(content.Starter.EvolutionTarget != null && content.Starter.EvolutionTarget.CreatureName == "Charmeleon", "Evolution asset linked");
                Check(content.Starter.EvolutionTarget.SpriteFront != null && content.Starter.EvolutionTarget.SpriteBack != null, "Charmeleon sprites imported");
                TestDifficulty();
                TestMoves(content);
                TestEvolutionAndSave(content);
                TestEncounters(content);
                TestCombatDifficulty(content);
                Debug.Log("PROGRESSION VALIDATION PASSED: " + checks + " checks. No player save was modified.");
            }
            finally
            {
                UnityEngine.Random.state = randomState;
                ProgressionRules.SetBalanceConfig(Resources.Load<GameBalanceConfig>("PokeIdle/Config/GameBalanceConfig"));
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void TestDifficulty()
        {
            var difficulty = new PhaseDifficulty();
            int start = difficulty.GetLevel(1, 0, 1, false);
            int next = difficulty.GetLevel(1, 1, 2, false);
            Check(start == 1 && next > start, "Difficulty grows within world one");
            int secondWorld = difficulty.GetLevel(2, 1, 17, false);
            Check(secondWorld == 15, "Level 17 player encounters level 15 enemies in world two");
            Check(difficulty.GetLevel(2, 1, 17, true) == 17, "Boss is normal level plus two");
            Check(difficulty.GetLevel(2, 1, 30, false) == secondWorld, "Level purchases do not strengthen the same phase");
            Check(difficulty.GetLevel(1, 0, 30, false) == start, "Farmed phase stays easier");
            Check(difficulty.GetLevel(2, 2, 30, false) == 27, "New phase responds to stronger player");
            var restored = new PhaseDifficulty();
            restored.Restore(difficulty.Export());
            Check(restored.GetLevel(2, 1, 80, false) == secondWorld, "Difficulty snapshot survives restore");
            Check(ProgressionRules.CalculateEnemyLevel(1, 1, 20, 10, 1.25f, 0.9f) == 18, "Ratio boundary rounding");
        }

        private static void TestMoves(DemoContentSet content)
        {
            var player = new CreatureInstance(content.Starter, 1);
            int gold = 1000;
            string message;
            Check(!player.TryPurchaseSkill(7, ref gold, out message) && gold == 1000, "Purchase locked by level");
            player.Level = 18;
            Check(!player.TryPurchaseSkill(11, ref gold, out message) && gold == 1000, "Purchase locked by prerequisite");
            gold = 69;
            Check(!player.TryPurchaseSkill(7, ref gold, out message) && gold == 69, "Insufficient funds change nothing");
            gold = 70;
            Check(player.TryPurchaseSkill(7, ref gold, out message) && gold == 0, "Exact-cost purchase succeeds");
            gold = 1000;
            Check(!player.TryPurchaseSkill(7, ref gold, out message) && gold == 1000, "Duplicate purchase does not charge");
            Check(!player.TryEquipMove(10, 0, out message), "Unpurchased move cannot be equipped");
            Check(player.TryPurchaseSkill(9, ref gold, out message), "Prerequisite path unlocks");
            Check(player.TryPurchaseSkill(11, ref gold, out message), "Electric coverage unlocked");
            Check(player.TryPurchaseSkill(10, ref gold, out message), "Fire branch unlocked independently");
            Check(player.TryEquipMove(7, 2, out message) && player.TryEquipMove(9, 3, out message), "Four slots can be filled");
            Check(!player.TryEquipMove(11, 4, out message) && player.EquippedMoveIds.Count == 4, "Fifth slot rejected");
            Check(player.TryEquipMove(11, 2, out message) && player.LearnedMoveIds.Contains(7), "Replacing keeps old learned move");
            Check(!player.TryEquipMove(11, 0, out message), "Duplicate equipped move rejected");
            var squirtle = new CreatureInstance(content.WildEncounters.Find(e => e.Creature.CreatureName == "Squirtle").Creature, 15);
            var caterpie = new CreatureInstance(content.WildCreature, 15);
            Check(AutoBattleEngine.ChooseMove(player, squirtle).Id == 11, "AI chooses electric against Squirtle");
            Check(AutoBattleEngine.ChooseMove(player, caterpie).Id == 2, "AI chooses fire against Caterpie; learned unequipped Flamethrower excluded");
            Check(player.TryEquipMove(10, 1, out message), "Flamethrower equipped");
            Check(AutoBattleEngine.ChooseMove(player, caterpie).Id == 10, "AI uses stronger equipped fire move");
            Check(AutoBattleEngine.ChooseMove(player, squirtle).Id == 11, "AI keeps coverage even against high-power resisted move");
            var other = new CreatureInstance(content.Starter, 18);
            Check(!other.LearnedMoveIds.Contains(11), "Purchases belong to the individual Pokemon");
            var immune = ScriptableObject.CreateInstance<CreatureDefinition>();
            try
            {
                immune.PrimaryType = ElementalType.Ground;
                immune.BaseStats = new BaseStats(50, 50, 50, 50, 50, 50);
                Check(AutoBattleEngine.ChooseMove(player, new CreatureInstance(immune, 15)).Id != 11, "AI avoids type immunity");
            }
            finally { UnityEngine.Object.DestroyImmediate(immune); }
        }

        private static void TestEvolutionAndSave(DemoContentSet content)
        {
            var player = new CreatureInstance(content.Starter, 15);
            int gold = 1000;
            string message;
            Check(!player.TryEvolve(ref gold, out message) && gold == 1000, "Evolution locked below 16");
            player.Level = 16;
            gold = 399;
            Check(!player.TryEvolve(ref gold, out message) && gold == 399, "Evolution requires full price");
            gold = 470;
            player.TryPurchaseSkill(7, ref gold, out message);
            player.TryEquipMove(7, 2, out message);
            string identity = player.InstanceId;
            int oldAttack = player.Stats.Attack;
            Check(player.TryEvolve(ref gold, out message) && gold == 0, "Evolution charges exact 400");
            Check(player.Definition.CreatureName == "Charmeleon" && player.Level == 16 && player.InstanceId == identity, "Evolution preserves level and identity");
            Check(player.Stats.Attack > oldAttack && player.CurrentHP == player.MaxHP, "Evolution improves stats and heals");
            Check(player.LearnedMoveIds.Contains(7) && player.EquippedMoveIds.Contains(7), "Evolution preserves purchased and equipped moves");
            Check(!player.TryEvolve(ref gold, out message) && gold == 0, "Evolution cannot be purchased twice");
            var saved = new PlayerSaveData
            {
                Gold = 200, WorldNumber = 2, PhaseNumber = 4, EncounterProgress = 7,
                HasRetryPhase = true, RetryWorldNumber = 2, RetryPhaseNumber = 5,
                ActiveCreature = new CreatureSaveData { CreatureId = player.Definition.Id, InstanceId = identity,
                    Level = player.Level, CurrentHP = player.CurrentHP,
                    LearnedMoveIds = player.LearnedMoveIds, EquippedMoveIds = player.EquippedMoveIds }
            };
            PlayerSaveData loaded = JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(saved));
            CreatureInstance restored = SaveService.RestoreCreature(loaded.ActiveCreature, content, true);
            Check(restored.Definition.CreatureName == "Charmeleon" && restored.InstanceId == identity, "Save restores evolved species");
            Check(restored.EquippedMoveIds.Contains(7) && restored.LearnedMoveIds.Contains(7), "Save restores loadout");
            Check(loaded.Gold == 200 && loaded.PhaseNumber == 4 && loaded.EncounterProgress == 7 && loaded.RetryPhaseNumber == 5, "Save preserves money, phase and retry");
            PlayerSaveData legacy = JsonUtility.FromJson<PlayerSaveData>("{\"Version\":5,\"Gold\":333,\"WorldNumber\":2,\"PhaseNumber\":3,\"ActiveCreature\":{\"CreatureId\":4,\"Level\":17,\"CurrentHP\":55}}");
            CreatureInstance migrated = SaveService.RestoreCreature(legacy.ActiveCreature, content, false);
            Check(legacy.Version >= ProgressionRules.PhaseProgressionSaveVersion && legacy.Gold == 333 && legacy.PhaseNumber == 3, "Version 5 remains phase-compatible");
            Check(migrated.Level == 17 && migrated.CurrentHP == 55 && migrated.EquippedMoveIds.Count == 2 && migrated.LearnedMoveIds.Count == 2, "Old save receives only starting moves");
            migrated.RestoreMoves(new List<int> { 1, 1, 2, 999 }, new List<int> { 1, 1, 2, 999, 888 });
            Check(migrated.LearnedMoveIds.Count == 2 && migrated.GetEquippedMoves().Count == 2, "Invalid and duplicate IDs sanitized");
            gold = 100;
            migrated.TryPurchaseSkill(7, ref gold, out message);
            Check(migrated.TryEquipMove(7, 3, out message) && migrated.EquippedMoveIds[3] == 7, "Equipment respects the selected fourth slot");
        }

        private static void TestEncounters(DemoContentSet content)
        {
            WildCreatureEncounter squirtle = content.WildEncounters.Find(e => e.Creature.CreatureName == "Squirtle");
            Check(squirtle != null && squirtle.MinimumStage == 2 && squirtle.SpawnWeight == 4, "Squirtle unlocks world two with weight four");
            int weight = 0;
            foreach (WildCreatureEncounter entry in content.WildEncounters)
                if (entry.MinimumStage <= 2) weight += entry.SpawnWeight;
            Check(weight == 16, "Squirtle is 4/16 normal encounters in world two");
            Check(content.WildCreature.EvolutionTarget.CreatureName == "Metapod", "Dominant Caterpie boss remains Metapod");
        }

        private static void TestCombatDifficulty(DemoContentSet content)
        {
            var player = new CreatureInstance(content.Starter, 17);
            var oldEnemy = new CreatureInstance(content.WildCreature, 2);
            var newEnemy = new CreatureInstance(content.WildCreature, ProgressionRules.GetEnemyLevel(2, 1, 17));
            var engine = new AutoBattleEngine();
            UnityEngine.Random.InitState(42);
            engine.ExecuteAttack(player, oldEnemy);
            UnityEngine.Random.InitState(42);
            engine.ExecuteAttack(player, newEnemy);
            Check(oldEnemy.IsFainted && !newEnemy.IsFainted, "World two enemy now survives an opening hit that killed old level two enemy");
            int health = player.CurrentHP;
            AttackResult retaliation = engine.ExecuteAttack(newEnemy, player);
            Check(retaliation.Hit && player.CurrentHP < health, "Higher-level encounter can retaliate");
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FAILED: " + message);
            checks++;
            Debug.Log("PASS: " + message);
        }
    }
}
