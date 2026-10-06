using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PokeIdle.Editor
{
    public static class MilestoneValidation
    {
        private static int checks;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("PokeIdle/Validate Milestones")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Pare o Play Mode para validar.");
            ProgressionValidation.Run();
            checks = 0;
            var config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            config.ResetToDefaults();
            var random = UnityEngine.Random.state;
            var objects = new List<GameObject>();
            try
            {
                ProgressionRules.SetBalanceConfig(config);
                DemoContentSet content = DemoContent.Load();
                PlayerSaveData seed = Seed(content);
                GameLoopManager loop = Session(seed, config, objects);
                Check(loop.UnlockedPartySlots == 1 && loop.Party.Count == 1, "Only the starter slot is free");
                int coins = loop.Dindin;
                Check(loop.TryUnlockPartySlot() && loop.Dindin == coins - 150, "Second slot charges 150 Dindin");
                Check(loop.TryUnlockPartySlot() && loop.UnlockedPartySlots == 3, "Third slot unlocks");
                coins = loop.Dindin;
                Check(!loop.TryUnlockPartySlot() && coins == loop.Dindin, "Fourth slot rejected without charging");
                Check(loop.TryExpandBox() && loop.BoxCapacity == 40 && loop.Dindin == coins - 100, "Box expands for its price");
                var reserve = new CreatureInstance(content.FindCreature(7), 5);
                Check(loop.TryAddCreatureToCollection(reserve, false), "Different species can enter Box");
                Check(!loop.TryAddCreatureToCollection(reserve, false), "Same instance cannot be duplicated");
                Check(loop.TryMoveBoxToParty(reserve.InstanceId, 1) && loop.PCBox.Count == 0, "Box to Party transfer");
                CreatureInstance original = loop.PlayerCreature;
                Tick(loop); // approaching
                Check(loop.TryRequestLeaderChange(reserve.InstanceId) && loop.PlayerCreature == original,
                    "Leader remains until round end, including approach");
                Tick(loop);
                Check(loop.PlayerCreature == reserve && !loop.HasPendingLeaderChange, "Queued leader change resolves at round end");
                reserve.CurrentHP = 0;
                Tick(loop);
                Check(loop.PlayerCreature == original && loop.EncounterProgress >= 0 && reserve.IsFainted,
                    "Fainted leader auto-swaps without reviving reserve");
                foreach (CreatureInstance c in loop.Party) c.CurrentHP = 0;
                Tick(loop);
                Check(loop.Phase == BattlePhase.Recovering && loop.Party.TrueForAll(c => !c.IsFainted),
                    "Whole-party defeat resets attempt and recovers team");
                var replacement = new CreatureInstance(content.WildCreature, 3);
                loop.TryAddCreatureToCollection(replacement, false);
                Tick(loop);
                Check(loop.TrySwapBoxWithParty(replacement.InstanceId, 0) && loop.HasPendingBoxSwap
                    && loop.PlayerCreature != replacement, "Box leader swap is queued during encounter");
                Tick(loop);
                Check(loop.PlayerCreature == replacement && loop.PCBox.Count == 1,
                    "Box swap atomically preserves both creatures");
                foreach (CreatureInstance c in loop.Party) c.CurrentHP = 1;
                Set(loop, "<EncounterProgress>k__BackingField", loop.EnemiesPerPhase - 1);
                Set(loop, "<CurrentEnemy>k__BackingField", new CreatureInstance(content.WildCreature, 1) { CurrentHP = 0 });
                Invoke(loop, "RewardEnemyDefeat");
                Check(loop.PhaseNumber == 1 && loop.Party.TrueForAll(c => c.CurrentHP == c.MaxHP),
                    "Phase completion heals entire Party, including fainted members");

                PlayerSaveData exported = (PlayerSaveData)Invoke(loop, "CreateSaveData");
                GameLoopManager reload = Session(JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(exported)), config, objects);
                Check(reload.Party.Count == loop.Party.Count && reload.PCBox.Count == loop.PCBox.Count
                    && reload.PlayerCreature.InstanceId == loop.PlayerCreature.InstanceId && reload.BoxCapacity == 40,
                    "Collection order, leader, capacity and identities survive full session reload");
                seed = Seed(content);
                seed.Dindin = 0; seed.Gold = 900;
                seed.Party[0].Level = 17;
                GameLoopManager farm = Session(seed, config, objects);
                Check(farm.PlayerCreature.Level == 17 && farm.Dindin == 0,
                    "Reload in an older world never deletes levels or resurrects spent currency");
                seed.Version = 6; seed.Gold = 321; seed.Party = null;
                Check(Session(seed, config, objects).Dindin == 321, "Legacy currency and active creature migrate");
                TestDiskRecovery(Seed(content));
                Rect compact = TaskbarLayout.Arena(720, 160);
                Rect expanded = TaskbarLayout.Arena(720, 604);
                Check(compact.size == expanded.size && compact.x == expanded.x
                    && 160 - compact.yMax == 604 - expanded.yMax,
                    "Opening menu preserves arena size and bottom anchor");
                Rect compactDrag = TaskbarLayout.DragHandle(720, 160, 22);
                Rect expandedDrag = TaskbarLayout.DragHandle(720, 604, 22);
                Check(compactDrag.height == 22 && expandedDrag.height == 22
                    && compactDrag.x == expandedDrag.x && expandedDrag.y - compactDrag.y == 444,
                    "Window drag grip stays at the visible arena edge in compact and expanded modes");
                Check(TaskbarLayout.Menu(720, 604).yMax < expanded.y,
                    "Expanded menu never overlaps combat viewport");
                Debug.Log("MILESTONES VALIDATION PASSED: " + checks + " additional checks. Player save untouched.");
            }
            finally
            {
                foreach (GameObject obj in objects) UnityEngine.Object.DestroyImmediate(obj);
                UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Random.state = random;
                ProgressionRules.SetBalanceConfig(Resources.Load<GameBalanceConfig>("PokeIdle/Config/GameBalanceConfig"));
            }
        }

        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static PlayerSaveData Seed(DemoContentSet content)
        {
            var c = new CreatureInstance(content.Starter, 5);
            var saved = new CreatureSaveData { CreatureId = c.Definition.Id, InstanceId = c.InstanceId,
                Level = c.Level, CurrentHP = c.CurrentHP, LearnedMoveIds = c.LearnedMoveIds, EquippedMoveIds = c.EquippedMoveIds };
            return new PlayerSaveData { Dindin = 3000, Gold = 3000, ActiveCreature = saved,
                Party = new List<CreatureSaveData> { saved }, PCBox = new List<CreatureSaveData>(), BoxCapacity = 30 };
        }

        private static GameLoopManager Session(PlayerSaveData save, GameBalanceConfig config, List<GameObject> objects)
        {
            var obj = new GameObject("Isolated milestone validation");
            obj.SetActive(false);
            objects.Add(obj);
            var loop = obj.AddComponent<GameLoopManager>();
            loop.ConfigureBalance(config);
            loop.InitializeSession(save, false);
            return loop;
        }

        private static void TestDiskRecovery(PlayerSaveData seed)
        {
            string directory = Path.Combine(Application.temporaryCachePath, "PokeIdleValidation-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "test.json");
            try
            {
                Check(SaveService.SaveToPath(seed, path), "Atomic first save");
                seed.Dindin = 444;
                Check(SaveService.SaveToPath(seed, path) && SaveService.LoadFromPath(path).Dindin == 444,
                    "Atomic replace returns the new generation");
                File.WriteAllText(path, "{}");
                Check(SaveService.LoadFromPath(path).Dindin == 3000, "Structurally corrupt save recovers backup");
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(seed));
                Check(SaveService.LoadFromPath(path).Dindin == 444, "Complete temp save recovers interrupted replace");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static object Invoke(GameLoopManager loop, string method) { return typeof(GameLoopManager).GetMethod(method, Private).Invoke(loop, null); }
        private static void Tick(GameLoopManager loop) { Invoke(loop, "ProcessTick"); }
        private static void Set(GameLoopManager loop, string field, object value) { typeof(GameLoopManager).GetField(field, Private).SetValue(loop, value); }
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("FAILED: " + label);
            checks++;
            Debug.Log("PASS: " + label);
        }
    }
}
