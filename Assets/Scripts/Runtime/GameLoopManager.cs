using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    public sealed class GameLoopManager : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig balanceConfig;

        private DemoContentSet content;
        private AutoBattleEngine battleEngine;
        private PlayerInventory inventory = new PlayerInventory();
        private PhaseDifficulty difficulty = new PhaseDifficulty();
        private readonly List<CreatureInstance> party = new List<CreatureInstance>();
        private readonly List<CreatureInstance> pcBox = new List<CreatureInstance>();
        private int unlockedPartySlots = 1;
        private int boxCapacity = 30;
        private string pendingLeaderInstanceId;
        private string pendingBoxInstanceId;
        private string pendingPartyInstanceId;
        private float tickTimer;
        private float autoSaveTimer;
        private bool initialized;
        private bool persistProgress = true;
        private bool hasRetryPhase;
        private int retryWorldNumber = 1;
        private int retryPhaseNumber;

        public static GameLoopManager Instance { get; private set; }
        public CreatureInstance PlayerCreature { get; private set; }
        public CreatureInstance CurrentEnemy { get; private set; }
        public int Dindin { get; private set; }
        // Compatibility alias for scripts or scenes that still reference Gold.
        public int Gold { get { return Dindin; } }
        public int TotalDefeated { get; private set; }
        public int WorldNumber { get; private set; } = 1;
        public int StageNumber { get { return WorldNumber; } }
        public int PhaseNumber { get; private set; }
        public int EncounterProgress { get; private set; }
        public int FirstPhaseNumber { get { return ProgressionRules.GetFirstPhaseNumber(WorldNumber); } }
        public int LastPhaseNumber { get { return ProgressionRules.GetLastPhaseNumber(WorldNumber); } }
        public int PhasesPerWorld { get { return ProgressionRules.GetPhaseCount(WorldNumber); } }
        public int EnemiesPerPhase { get { return ProgressionRules.GetEnemiesInPhase(WorldNumber, PhaseNumber); } }
        public int GlobalLevelCap { get { return ProgressionRules.GetGlobalMaxLevel(); } }
        public int WorldLevelCap { get { return ProgressionRules.GetWorldLevelCap(WorldNumber); } }
        public bool IsAtLevelCap
        {
            get { return PlayerCreature != null && PlayerCreature.Level >= Mathf.Min(GlobalLevelCap, WorldLevelCap); }
        }
        public List<CreatureInstance> Party { get { return party; } }
        public List<CreatureInstance> PCBox { get { return pcBox; } }
        public int MaxPartySlots { get { return ProgressionRules.GetMaxPartySlots(); } }
        public int UnlockedPartySlots { get { return Mathf.Clamp(unlockedPartySlots, 1, MaxPartySlots); } }
        public int BoxCapacity { get { return Mathf.Max(1, boxCapacity); } }
        public int BoxExpansionSize { get { return ProgressionRules.GetBoxExpansionSize(); } }
        public bool CanExpandBox { get { return true; } }
        public int BoxExpansionCost { get { return ProgressionRules.GetBoxExpansionCost(BoxCapacity); } }
        public int NextPartySlotCost
        {
            get { return ProgressionRules.GetPartySlotCost(UnlockedPartySlots + 1); }
        }
        public bool HasPendingLeaderChange { get { return !string.IsNullOrEmpty(pendingLeaderInstanceId); } }
        public string PendingLeaderInstanceId { get { return pendingLeaderInstanceId; } }
        public bool HasPendingBoxSwap { get { return !string.IsNullOrEmpty(pendingBoxInstanceId); } }
        private bool HasLiveEncounter { get { return CurrentEnemy != null && !CurrentEnemy.IsFainted; } }
        public bool IsPaused { get; private set; }
        public BattlePhase Phase { get; private set; } = BattlePhase.Searching;
        public PlayerInventory Inventory { get { return inventory; } }
        public bool CanReturnToFailedPhase { get { return hasRetryPhase; } }
        public string CurrentPhaseLabel { get { return FormatPhaseLabel(WorldNumber, PhaseNumber); } }
        public string FailedPhaseLabel { get { return FormatPhaseLabel(retryWorldNumber, retryPhaseNumber); } }
        public int LevelUpCost
        {
            get { return ProgressionRules.GetLevelUpCost(PlayerCreature == null ? 1 : PlayerCreature.Level); }
        }
        public bool CanLevelUp
        {
            get { return PlayerCreature != null && Dindin >= LevelUpCost; }
        }
        public int NormalEnemyLevel { get { return PlayerCreature == null ? 1 : difficulty.GetLevel(WorldNumber, PhaseNumber, PlayerCreature.Level, false); } }
        public string LastEvent { get; private set; } = "Preparando a expedicao...";

        public event Action StateChanged;
        public event Action InventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ProgressionRules.SetBalanceConfig(balanceConfig);
            battleEngine = new AutoBattleEngine();
        }

        private void OnValidate()
        {
            ProgressionRules.SetBalanceConfig(balanceConfig);
        }

        private void Start()
        {
            InitializeSession();
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            autoSaveTimer += Time.unscaledDeltaTime;
            if (autoSaveTimer >= ProgressionRules.GetAutoSaveIntervalSeconds())
            {
                SaveNowSilently();
            }

            if (IsPaused)
            {
                return;
            }

            tickTimer += Time.unscaledDeltaTime;
            if (tickTimer < ProgressionRules.GetTickIntervalSeconds())
            {
                return;
            }

            tickTimer -= ProgressionRules.GetTickIntervalSeconds();
            ProcessTick();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        public void InitializeSession()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pokeidle-review") >= 0)
            {
                DemoContentSet demo = DemoContent.Load();
                var seed = new PlayerSaveData { Dindin = 3000, UnlockedPartySlots = 3, BoxCapacity = 30,
                    Party = new List<CreatureSaveData>(), PCBox = new List<CreatureSaveData>() };
                seed.Party.Add(CreateCreatureSaveData(new CreatureInstance(demo.Starter, 5)));
                seed.Party.Add(CreateCreatureSaveData(new CreatureInstance(demo.FindCreature(7), 5)));
                seed.Party.Add(CreateCreatureSaveData(new CreatureInstance(demo.FindCreature(16), 4)));
                foreach (CreatureDefinition definition in demo.Creatures)
                    seed.PCBox.Add(CreateCreatureSaveData(new CreatureInstance(definition, 3)));
                seed.Inventory.Add(new InventoryItemStack(InventoryItemId.Potion, 5));
                InitializeSession(seed, false);
                return;
            }
#endif
            InitializeSession(SaveService.Load(), true);
        }

        // Also used by isolated validation sessions, which never write the player's save.
        public void InitializeSession(PlayerSaveData save, bool saveProgress)
        {
            if (initialized)
            {
                return;
            }

            content = DemoContent.Load();
            persistProgress = saveProgress;
            if (battleEngine == null) battleEngine = new AutoBattleEngine();

            bool hasMoveLoadout = save != null && save.Version >= 6;
            RestorePartyFromSave(save, content, hasMoveLoadout);
            PlayerCreature = party[0];

            bool loadedAfterDefeat = save != null
                && save.Version < ProgressionRules.SaveVersion
                && save.ActiveCreature != null
                && save.ActiveCreature.CreatureId == PlayerCreature.Definition.Id
                && save.ActiveCreature.CurrentHP <= 0;
            int savedDindin = save == null ? 0 : save.Version >= 7 ? save.Dindin : save.Gold;
            Dindin = Mathf.Max(0, savedDindin);
            TotalDefeated = save != null ? Mathf.Max(0, save.TotalDefeated) : 0;
            bool usesPhaseProgression = save != null && save.Version >= ProgressionRules.PhaseProgressionSaveVersion;
            WorldNumber = usesPhaseProgression
                ? Mathf.Max(1, save.WorldNumber)
                : save != null ? Mathf.Max(1, save.RouteNumber) : 1;
            PhaseNumber = usesPhaseProgression
                ? Mathf.Max(0, save.PhaseNumber)
                : 0;
            PhaseNumber = Mathf.Clamp(PhaseNumber, FirstPhaseNumber, LastPhaseNumber);
            EncounterProgress = usesPhaseProgression
                ? Mathf.Clamp(save.EncounterProgress, 0, EnemiesPerPhase - 1)
                : save != null ? Mathf.Clamp(save.RouteProgress, 0, EnemiesPerPhase - 1) : 0;
            if (loadedAfterDefeat)
            {
                EncounterProgress = 0;
            }

            hasRetryPhase = usesPhaseProgression && save.HasRetryPhase;
            retryWorldNumber = usesPhaseProgression ? Mathf.Max(1, save.RetryWorldNumber) : 1;
            retryPhaseNumber = usesPhaseProgression
                ? Mathf.Clamp(save.RetryPhaseNumber,
                    ProgressionRules.GetFirstPhaseNumber(retryWorldNumber),
                    ProgressionRules.GetLastPhaseNumber(retryWorldNumber))
                : 0;
            // Returning to an older world must never erase levels bought in a later world.
            PlayerCreature.Level = Mathf.Clamp(PlayerCreature.Level, 1, GlobalLevelCap);
            PlayerCreature.EnsureValid(!hasMoveLoadout || !PlayerCreature.IsFainted);
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != null) party[i].EnsureValid(false);
            }
            difficulty.Restore(save == null ? null : save.PhaseDifficulties);
            difficulty.GetLevel(WorldNumber, PhaseNumber, PlayerCreature.Level, false);

            inventory = new PlayerInventory();
            if (save != null && save.Inventory != null)
            {
                for (int i = 0; i < save.Inventory.Count; i++)
                {
                    InventoryItemStack savedStack = save.Inventory[i];
                    if (savedStack != null)
                    {
                        inventory.Add(savedStack.ItemId, savedStack.Quantity);
                    }
                }
            }

            inventory.EnsureValid();
            Phase = BattlePhase.Searching;
            autoSaveTimer = 0f;
            initialized = true;
            LastEvent = "Expedicao iniciada. O combate acontece a cada segundo.";
            NotifyInventoryChanged();
            NotifyStateChanged();
        }

        private void RestorePartyFromSave(PlayerSaveData save, DemoContentSet content, bool hasMoveLoadout)
        {
            party.Clear();
            pcBox.Clear();
            unlockedPartySlots = ProgressionRules.GetStartingPartySlots();
            boxCapacity = ProgressionRules.GetInitialBoxCapacity();
            pendingLeaderInstanceId = null;
            pendingBoxInstanceId = save == null ? null : save.PendingBoxInstanceId;
            pendingPartyInstanceId = save == null ? null : save.PendingPartyInstanceId;

            bool hasPartySave = save != null
                && save.Version >= 7
                && save.Party != null
                && save.Party.Count > 0;
            bool preserveFainted = hasPartySave;

            if (hasPartySave)
            {
                unlockedPartySlots = Mathf.Clamp(
                    Mathf.Max(ProgressionRules.GetStartingPartySlots(), save.UnlockedPartySlots),
                    1, MaxPartySlots);
                boxCapacity = Mathf.Max(ProgressionRules.GetInitialBoxCapacity(), save.BoxCapacity);
                for (int i = 0; i < save.Party.Count; i++)
                {
                    CreatureSaveData savedCreature = save.Party[i];
                    if (savedCreature == null) continue;
                    CreatureInstance restored = SaveService.RestoreCreature(savedCreature, content, hasMoveLoadout, false);
                    if (restored == null || ContainsInstanceId(party, restored.InstanceId)
                        || ContainsInstanceId(pcBox, restored.InstanceId)) continue;
                    if (party.Count < MaxPartySlots) party.Add(restored);
                    else pcBox.Add(restored);
                }

                if (!string.IsNullOrEmpty(save.PendingLeaderInstanceId))
                {
                    pendingLeaderInstanceId = save.PendingLeaderInstanceId;
                }

                if (save.PCBox != null)
                {
                    for (int i = 0; i < save.PCBox.Count; i++)
                    {
                        CreatureSaveData savedCreature = save.PCBox[i];
                        if (savedCreature == null) continue;
                        CreatureInstance restored = SaveService.RestoreCreature(savedCreature, content, hasMoveLoadout, false);
                        if (restored != null
                            && !ContainsInstanceId(party, restored.InstanceId)
                            && !ContainsInstanceId(pcBox, restored.InstanceId))
                        {
                            pcBox.Add(restored);
                        }
                    }
                }
            }
            else
            {
                CreatureInstance restored = SaveService.RestoreCreature(
                    save == null ? null : save.ActiveCreature, content, hasMoveLoadout, true);
                if (restored != null) party.Add(restored);
            }

            if (party.Count == 0)
            {
                party.Add(new CreatureInstance(content.Starter, ProgressionRules.StartingCreatureLevel));
            }

            unlockedPartySlots = Mathf.Clamp(Mathf.Max(unlockedPartySlots, party.Count), 1, MaxPartySlots);
            if (pcBox.Count > boxCapacity) boxCapacity = pcBox.Count;
            PlayerCreature = party[0];
            if (!preserveFainted) PlayerCreature.EnsureValid();
            CreatureInstance requested = FindPartyMember(pendingLeaderInstanceId);
            if (requested == null || requested.IsFainted) pendingLeaderInstanceId = null;
            if (FindBoxIndex(pendingBoxInstanceId) < 0 || FindPartyMember(pendingPartyInstanceId) == null)
                pendingBoxInstanceId = pendingPartyInstanceId = null;
        }

        private static bool ContainsInstanceId(List<CreatureInstance> creatures, string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return false;
            for (int i = 0; i < creatures.Count; i++)
            {
                if (creatures[i] != null && creatures[i].InstanceId == instanceId) return true;
            }

            return false;
        }

        public void ConfigureBalance(GameBalanceConfig config)
        {
            balanceConfig = config;
            ProgressionRules.SetBalanceConfig(balanceConfig);
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            LastEvent = IsPaused ? "Expedicao pausada." : "Expedicao retomada.";
            NotifyStateChanged();
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            NotifyStateChanged();
        }

        public void SaveNow()
        {
            if (!initialized || PlayerCreature == null)
            {
                return;
            }

            LastEvent = SaveNowSilently() ? "Progresso salvo." : "Falha ao salvar. Veja o Console/log.";
            NotifyStateChanged();
        }

        /// <summary>
        /// Acao de teste disponivel no menu de contexto do componente no Inspector.
        /// Nao e chamada automaticamente e substitui o save local atual.
        /// </summary>
        [ContextMenu("Reset Progress (Testing)")]
        public void ResetProgressForTesting()
        {
            if (content == null)
            {
                content = DemoContent.Load();
            }

            party.Clear();
            pcBox.Clear();
            unlockedPartySlots = ProgressionRules.GetStartingPartySlots();
            boxCapacity = ProgressionRules.GetInitialBoxCapacity();
            pendingLeaderInstanceId = null;
            PlayerCreature = new CreatureInstance(content.Starter, ProgressionRules.StartingCreatureLevel);
            pendingBoxInstanceId = pendingPartyInstanceId = null;
            party.Add(PlayerCreature);
            CurrentEnemy = null;
            Dindin = 0;
            TotalDefeated = 0;
            WorldNumber = 1;
            PhaseNumber = 0;
            EncounterProgress = 0;
            hasRetryPhase = false;
            retryWorldNumber = 1;
            retryPhaseNumber = 0;
            Phase = BattlePhase.Searching;
            IsPaused = false;
            tickTimer = 0f;
            autoSaveTimer = 0f;
            inventory = new PlayerInventory();
            initialized = true;

            difficulty = new PhaseDifficulty();
            difficulty.GetLevel(WorldNumber, PhaseNumber, PlayerCreature.Level, false);

            SaveNowSilently();
            LastEvent = "Progresso de teste resetado.";
            NotifyInventoryChanged();
            NotifyStateChanged();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Add Test Squirtle To PC Box")]
        private void AddTestSquirtleToBox()
        {
            if (!initialized) InitializeSession();
            if (content == null) content = DemoContent.Load();
            CreatureDefinition squirtle = content.FindCreature(7);
            if (squirtle != null)
            {
                TryAddCreatureToCollection(new CreatureInstance(squirtle, Mathf.Max(1, WorldLevelCap / 2)), false);
            }
        }
#endif

        private void ProcessTick()
        {
            if (PlayerCreature == null)
            {
                return;
            }

            if (PlayerCreature.IsFainted)
            {
                if (!TryAutoSwapAfterFaint()) HandlePlayerDefeat();
                else NotifyStateChanged();
                return;
            }

            if (CurrentEnemy == null || CurrentEnemy.IsFainted)
            {
                ApplyPendingLeaderChange();
                SpawnEnemy();
                // O encontro ocupa um tick de aproximação antes do primeiro golpe.
                // Isso dá tempo para a arena mostrar o inimigo entrando em linha reta.
                NotifyStateChanged();
                return;
            }

            Phase = BattlePhase.Battling;
            AttackResult playerAttack = battleEngine.ExecuteAttack(PlayerCreature, CurrentEnemy);
            LastEvent = playerAttack.Message;

            if (!CurrentEnemy.IsFainted)
            {
                AttackResult enemyAttack = battleEngine.ExecuteAttack(CurrentEnemy, PlayerCreature);
                LastEvent += " | " + enemyAttack.Message;
            }

            if (PlayerCreature.IsFainted)
            {
                if (!TryAutoSwapAfterFaint()) HandlePlayerDefeat();
                else NotifyStateChanged();
                return;
            }

            if (CurrentEnemy.IsFainted)
            {
                RewardEnemyDefeat();
            }

            ApplyPendingLeaderChange();

            NotifyStateChanged();
        }

        private void SpawnEnemy()
        {
            bool isBoss = EncounterProgress >= EnemiesPerPhase - 1;
            CreatureDefinition wildCreature = isBoss ? SelectBossCreature() : SelectWildCreature();
            if (wildCreature == null)
            {
                LastEvent = "Nenhum encontro esta configurado para esta fase.";
                return;
            }

            int enemyLevel = difficulty.GetLevel(WorldNumber, PhaseNumber, PlayerCreature.Level, isBoss);
            CurrentEnemy = new CreatureInstance(wildCreature, enemyLevel, isBoss);
            Phase = BattlePhase.Searching;
            LastEvent = isBoss
                ? "Um BOSS " + CurrentEnemy.Definition.CreatureName + " apareceu no final da fase " + CurrentPhaseLabel + "."
                : "Um " + CurrentEnemy.Definition.CreatureName + " apareceu na fase " + CurrentPhaseLabel + ".";
        }

        private CreatureDefinition SelectWildCreature()
        {
            WildCreatureEncounter encounter = SelectWildEncounter();
            return encounter != null && encounter.Creature != null
                ? encounter.Creature
                : content == null ? null : content.WildCreature;
        }

        private CreatureDefinition SelectBossCreature()
        {
            WildCreatureEncounter dominantEncounter = SelectDominantEncounter();
            if (dominantEncounter == null || dominantEncounter.Creature == null)
            {
                return content == null ? null : content.WildCreature;
            }

            return dominantEncounter.Creature.EvolutionTarget != null
                ? dominantEncounter.Creature.EvolutionTarget
                : dominantEncounter.Creature;
        }

        private WildCreatureEncounter SelectWildEncounter()
        {
            List<WildCreatureEncounter> availableEncounters = GetAvailableEncounters();
            if (availableEncounters.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            for (int i = 0; i < availableEncounters.Count; i++)
            {
                totalWeight += Mathf.Max(1, availableEncounters[i].SpawnWeight);
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            for (int i = 0; i < availableEncounters.Count; i++)
            {
                roll -= Mathf.Max(1, availableEncounters[i].SpawnWeight);
                if (roll < 0)
                {
                    return availableEncounters[i];
                }
            }

            return availableEncounters[availableEncounters.Count - 1];
        }

        private WildCreatureEncounter SelectDominantEncounter()
        {
            List<WildCreatureEncounter> availableEncounters = GetAvailableEncounters();
            WildCreatureEncounter dominantEncounter = null;
            for (int i = 0; i < availableEncounters.Count; i++)
            {
                WildCreatureEncounter encounter = availableEncounters[i];
                if (dominantEncounter == null || encounter.SpawnWeight > dominantEncounter.SpawnWeight)
                {
                    dominantEncounter = encounter;
                }
            }

            return dominantEncounter;
        }

        private List<WildCreatureEncounter> GetAvailableEncounters()
        {
            var availableEncounters = new List<WildCreatureEncounter>();
            if (content == null)
            {
                return availableEncounters;
            }

            if (content.WildEncounters == null || content.WildEncounters.Count == 0)
            {
                if (content.WildCreature != null)
                {
                    availableEncounters.Add(new WildCreatureEncounter(content.WildCreature, 1));
                }

                return availableEncounters;
            }

            for (int i = 0; i < content.WildEncounters.Count; i++)
            {
                WildCreatureEncounter encounter = content.WildEncounters[i];
                if (encounter != null && encounter.Creature != null && StageNumber >= encounter.MinimumStage)
                {
                    availableEncounters.Add(encounter);
                }
            }

            if (availableEncounters.Count == 0 && content.WildCreature != null)
            {
                availableEncounters.Add(new WildCreatureEncounter(content.WildCreature, 1));
            }

            return availableEncounters;
        }

        private void RewardEnemyDefeat()
        {
            int defeatedLevel = CurrentEnemy.Level;
            int worldAtDefeat = WorldNumber;
            int dindinReward = ProgressionRules.GetDindinReward(defeatedLevel, worldAtDefeat);

            Dindin += dindinReward;
            TotalDefeated++;
            EncounterProgress++;
            LastEvent += " Recompensas: +" + dindinReward + " Dindin.";

            List<InventoryItemStack> drops = LootTable.RollDrops(defeatedLevel, worldAtDefeat);
            for (int i = 0; i < drops.Count; i++)
            {
                InventoryItemStack drop = drops[i];
                inventory.Add(drop.ItemId, drop.Quantity);
            }

            if (drops.Count > 0)
            {
                LastEvent += " Itens: " + FormatDrops(drops) + ".";
                NotifyInventoryChanged();
            }
            else
            {
                LastEvent += " Nenhum item caiu.";
            }

            if (EncounterProgress >= EnemiesPerPhase)
            {
                AdvanceToNextPhase();
                HealEntireParty();
                inventory.Add(InventoryItemId.Potion, 1);
                LastEvent += " Fase concluida! Proxima fase desbloqueada e HP restaurado.";
                LastEvent += " Bonus da fase: +1 Pocao.";
                NotifyInventoryChanged();
                SaveNowSilently();
            }

            // Mantem o inimigo derrotado ate o proximo tick para evitar um frame vazio.
            Phase = BattlePhase.Searching;
        }

        private void AdvanceToNextPhase()
        {
            if (PhaseNumber < LastPhaseNumber)
            {
                PhaseNumber++;
            }
            else
            {
                WorldNumber++;
                PhaseNumber = ProgressionRules.GetFirstPhaseNumber(WorldNumber);
            }

            EncounterProgress = 0;
            PlayerCreature.HealFull();
            difficulty.GetLevel(WorldNumber, PhaseNumber, PlayerCreature.Level, false);
            if (hasRetryPhase && IsPhaseAtOrAfter(WorldNumber, PhaseNumber, retryWorldNumber, retryPhaseNumber))
            {
                hasRetryPhase = false;
            }
        }

        private bool TryAutoSwapAfterFaint()
        {
            ApplyPendingBoxSwap();
            if (!PlayerCreature.IsFainted) return true;
            CreatureInstance nextLeader = FindPartyMember(pendingLeaderInstanceId);
            if (nextLeader == null || nextLeader == PlayerCreature || nextLeader.IsFainted)
            {
                nextLeader = null;
                for (int i = 0; i < party.Count; i++)
                {
                    CreatureInstance candidate = party[i];
                    if (candidate != null && candidate != PlayerCreature && !candidate.IsFainted)
                    {
                        nextLeader = candidate;
                        break;
                    }
                }
            }

            if (nextLeader == null)
            {
                return false;
            }

            ApplyLeaderInternal(nextLeader);
            pendingLeaderInstanceId = null;
            Phase = BattlePhase.Recovering;
            LastEvent = PlayerCreature.Definition.CreatureName
                + " entrou automaticamente porque o lider anterior desmaiou.";
            SaveNowSilently();
            return true;
        }

        private void ApplyPendingLeaderChange()
        {
            ApplyPendingBoxSwap();
            if (string.IsNullOrEmpty(pendingLeaderInstanceId))
            {
                return;
            }

            CreatureInstance pendingLeader = FindPartyMember(pendingLeaderInstanceId);
            pendingLeaderInstanceId = null;
            if (pendingLeader == null || pendingLeader == PlayerCreature || pendingLeader.IsFainted)
            {
                return;
            }

            ApplyLeaderInternal(pendingLeader);
            LastEvent += " Lider alterado para " + PlayerCreature.Definition.CreatureName + " no fim do round.";
            SaveNowSilently();
        }

        private void ApplyLeaderInternal(CreatureInstance newLeader)
        {
            if (newLeader == null || !party.Contains(newLeader)) return;
            party.Remove(newLeader);
            party.Insert(0, newLeader);
            PlayerCreature = newLeader;
        }

        private CreatureInstance FindPartyMember(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != null && party[i].InstanceId == instanceId) return party[i];
            }

            return null;
        }

        public CreatureInstance FindOwnedCreature(string instanceId)
        {
            CreatureInstance member = FindPartyMember(instanceId);
            int boxIndex = FindBoxIndex(instanceId);
            return member ?? (boxIndex < 0 ? null : pcBox[boxIndex]);
        }

        public bool TrySwapBoxWithParty(string instanceId, int targetSlot)
        {
            int boxIndex = FindBoxIndex(instanceId);
            if (boxIndex < 0 || targetSlot < 0 || targetSlot >= UnlockedPartySlots) return false;
            if (targetSlot >= party.Count) return TryMoveBoxToParty(instanceId, targetSlot);
            if (targetSlot == 0 && pcBox[boxIndex].IsFainted)
            {
                LastEvent = "Um Pokemon desmaiado nao pode assumir a lideranca.";
                NotifyStateChanged();
                return false;
            }
            pendingBoxInstanceId = instanceId;
            pendingPartyInstanceId = party[targetSlot].InstanceId;
            if (targetSlot == 0 && HasLiveEncounter)
                LastEvent = "Troca com a Box agendada para o fim do round.";
            else ApplyPendingBoxSwap();
            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        private void ApplyPendingBoxSwap()
        {
            int boxIndex = FindBoxIndex(pendingBoxInstanceId);
            CreatureInstance outgoing = FindPartyMember(pendingPartyInstanceId);
            pendingBoxInstanceId = pendingPartyInstanceId = null;
            if (boxIndex < 0 || outgoing == null) return;
            CreatureInstance incoming = pcBox[boxIndex];
            if (outgoing == PlayerCreature && incoming.IsFainted) return;
            party[party.IndexOf(outgoing)] = incoming;
            pcBox[boxIndex] = outgoing;
            if (outgoing == PlayerCreature) PlayerCreature = incoming;
            if (pendingLeaderInstanceId == outgoing.InstanceId) pendingLeaderInstanceId = null;
            LastEvent = incoming.Definition.CreatureName + " entrou na Party. "
                + outgoing.Definition.CreatureName + " foi para a Box.";
            SaveNowSilently();
        }

        private void HealEntireParty()
        {
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != null) party[i].HealFull();
            }
        }

        private void HandlePlayerDefeat()
        {
            int failedWorld = WorldNumber;
            int failedPhase = PhaseNumber;
            int previousWorld = WorldNumber;
            int previousPhase = PhaseNumber;
            int firstPhase = ProgressionRules.GetFirstPhaseNumber(previousWorld);

            if (previousPhase > firstPhase)
            {
                previousPhase--;
            }
            else if (previousWorld > 1)
            {
                previousWorld--;
                previousPhase = ProgressionRules.GetLastPhaseNumber(previousWorld);
            }
            else
            {
                previousPhase = firstPhase;
            }

            hasRetryPhase = previousWorld != failedWorld || previousPhase != failedPhase;
            if (hasRetryPhase)
            {
                retryWorldNumber = failedWorld;
                retryPhaseNumber = failedPhase;
            }

            WorldNumber = previousWorld;
            PhaseNumber = previousPhase;
            // A migrated save may lack a snapshot for an earlier, already-cleared phase.
            difficulty.GetLevel(WorldNumber, PhaseNumber, 1, false);
            EncounterProgress = 0;
            CurrentEnemy = null;
            pendingLeaderInstanceId = null;
            Phase = BattlePhase.Recovering;
            HealEntireParty();

            LastEvent = PlayerCreature.Definition.CreatureName + " foi derrotado. ";
            LastEvent += hasRetryPhase
                ? "Retornou para a fase " + CurrentPhaseLabel + ". Você pode tentar novamente a fase " + FailedPhaseLabel + "."
                : "Permaneceu na fase " + CurrentPhaseLabel + ".";

            SaveNowSilently();
            NotifyStateChanged();
        }

        public bool ReturnToFailedPhase()
        {
            if (!initialized || !hasRetryPhase || PlayerCreature == null)
            {
                return false;
            }

            WorldNumber = retryWorldNumber;
            PhaseNumber = retryPhaseNumber;
            EncounterProgress = 0;
            hasRetryPhase = false;
            pendingLeaderInstanceId = null;
            CurrentEnemy = null;
            Phase = BattlePhase.Recovering;
            HealEntireParty();
            LastEvent = "Retomando a fase " + CurrentPhaseLabel + ".";
            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryUnlockPartySlot()
        {
            if (!initialized) return false;
            if (UnlockedPartySlots >= MaxPartySlots)
            {
                LastEvent = "Todos os slots da Party ja estao desbloqueados.";
                NotifyStateChanged();
                return false;
            }

            int cost = NextPartySlotCost;
            if (Dindin < cost)
            {
                LastEvent = "Dindin insuficiente. O proximo slot custa " + cost + ".";
                NotifyStateChanged();
                return false;
            }

            Dindin -= cost;
            unlockedPartySlots++;
            LastEvent = "Slot " + UnlockedPartySlots + " da Party desbloqueado por " + cost + " Dindin.";
            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryExpandBox()
        {
            if (!initialized) return false;

            int cost = BoxExpansionCost;
            if (Dindin < cost)
            {
                LastEvent = "Dindin insuficiente. A proxima expansao da Box custa " + cost + ".";
                NotifyStateChanged();
                return false;
            }

            Dindin -= cost;
            boxCapacity += BoxExpansionSize;
            LastEvent = "PC Box expandida para " + boxCapacity + " espacos por " + cost + " Dindin.";
            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryRequestLeaderChange(string instanceId)
        {
            CreatureInstance requestedLeader = FindPartyMember(instanceId);
            if (requestedLeader == null)
            {
                LastEvent = "Esse Pokemon nao esta na Party.";
                NotifyStateChanged();
                return false;
            }

            if (requestedLeader == PlayerCreature)
            {
                LastEvent = requestedLeader.Definition.CreatureName + " ja e o lider da Party.";
                NotifyStateChanged();
                return false;
            }

            if (requestedLeader.IsFainted)
            {
                LastEvent = "Um Pokemon desmaiado nao pode assumir a lideranca.";
                NotifyStateChanged();
                return false;
            }

            if (HasLiveEncounter)
            {
                pendingLeaderInstanceId = requestedLeader.InstanceId;
                LastEvent = "Troca para " + requestedLeader.Definition.CreatureName + " agendada para o fim do round.";
            }
            else
            {
                ApplyLeaderInternal(requestedLeader);
                pendingLeaderInstanceId = null;
                LastEvent = requestedLeader.Definition.CreatureName + " agora e o lider da Party.";
            }

            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryReorderParty(int fromSlot, int toSlot)
        {
            if (fromSlot < 0 || fromSlot >= party.Count || toSlot < 0 || toSlot >= party.Count)
            {
                return false;
            }

            if (toSlot == 0 && fromSlot != 0)
            {
                return TryRequestLeaderChange(party[fromSlot].InstanceId);
            }

            if (fromSlot == 0 && toSlot != 0)
            {
                return TryRequestLeaderChange(party[toSlot].InstanceId);
            }

            CreatureInstance temporary = party[fromSlot];
            party[fromSlot] = party[toSlot];
            party[toSlot] = temporary;
            LastEvent = "Ordem da Party atualizada.";
            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryMovePartyToBox(string instanceId)
        {
            if (pcBox.Count >= BoxCapacity)
            {
                LastEvent = "A PC Box esta cheia. Expanda a Box antes de guardar outro Pokemon.";
                NotifyStateChanged();
                return false;
            }

            CreatureInstance creature = FindPartyMember(instanceId);
            if (creature == null) return false;
            if (party.Count <= 1)
            {
                LastEvent = "A Party precisa manter pelo menos um Pokemon ativo.";
                NotifyStateChanged();
                return false;
            }

            if (creature == PlayerCreature && HasLiveEncounter)
            {
                LastEvent = "Troque o lider primeiro; a troca sera aplicada no fim do round.";
                NotifyStateChanged();
                return false;
            }

            party.Remove(creature);
            pcBox.Add(creature);
            if (pendingLeaderInstanceId == instanceId) pendingLeaderInstanceId = null;
            if (creature == PlayerCreature)
            {
                PlayerCreature = party[0];
                pendingLeaderInstanceId = null;
            }

            LastEvent = creature.Definition.CreatureName + " foi guardado na PC Box.";
            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryMoveBoxToParty(string instanceId, int targetSlot)
        {
            if (party.Count >= UnlockedPartySlots)
            {
                LastEvent = "Nao ha slot livre na Party. Desbloqueie outro slot com Dindin.";
                NotifyStateChanged();
                return false;
            }

            int boxIndex = FindBoxIndex(instanceId);
            if (boxIndex < 0) return false;

            CreatureInstance creature = pcBox[boxIndex];
            pcBox.RemoveAt(boxIndex);
            int insertSlot = party.Count == 0 ? 0 : Mathf.Clamp(targetSlot, 1, party.Count);
            party.Insert(insertSlot, creature);
            if (party.Count == 1) PlayerCreature = creature;

            LastEvent = creature.Definition.CreatureName + " foi movido da Box para a Party.";
            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryAddCreatureToCollection(CreatureInstance creature, bool preferParty)
        {
            if (!initialized || creature == null || creature.Definition == null
                || ContainsInstanceId(party, creature.InstanceId) || ContainsInstanceId(pcBox, creature.InstanceId)) return false;
            if (preferParty && party.Count < UnlockedPartySlots)
            {
                party.Add(creature);
                LastEvent = creature.Definition.CreatureName + " entrou na Party.";
            }
            else if (pcBox.Count < BoxCapacity)
            {
                pcBox.Add(creature);
                LastEvent = creature.Definition.CreatureName + " foi enviado para a PC Box.";
            }
            else
            {
                LastEvent = "Nao ha espaco para guardar " + creature.Definition.CreatureName + ".";
                return false;
            }

            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        private int FindBoxIndex(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return -1;
            for (int i = 0; i < pcBox.Count; i++)
            {
                if (pcBox[i] != null && pcBox[i].InstanceId == instanceId) return i;
            }

            return -1;
        }

        public int GetItemQuantity(InventoryItemId itemId)
        {
            return inventory == null ? 0 : inventory.GetQuantity(itemId);
        }

        public bool TryUseItem(InventoryItemId itemId)
        {
            if (!initialized || PlayerCreature == null || inventory == null)
            {
                return false;
            }

            ItemInfo item = ItemCatalog.Get(itemId);
            if (item == null || item.HealAmount <= 0 || inventory.GetQuantity(itemId) <= 0)
            {
                return false;
            }

            if (PlayerCreature.CurrentHP >= PlayerCreature.MaxHP)
            {
                LastEvent = PlayerCreature.Definition.CreatureName + " ja esta com o HP cheio.";
                NotifyStateChanged();
                return false;
            }

            inventory.TryRemove(itemId, 1);
            int healedAmount = PlayerCreature.Heal(item.HealAmount);
            LastEvent = "Usou " + item.DisplayName + " e recuperou " + healedAmount + " HP.";
            SaveNowSilently();
            NotifyInventoryChanged();
            NotifyStateChanged();
            return true;
        }

        public bool TryLevelUpWithDindin(string creatureId = null)
        {
            CreatureInstance target = creatureId == null ? PlayerCreature : FindOwnedCreature(creatureId);
            if (!initialized || target == null)
            {
                return false;
            }

            if (target.Level >= GlobalLevelCap)
            {
                LastEvent = "Nivel maximo global atingido (" + GlobalLevelCap + ").";
                NotifyStateChanged();
                return false;
            }

            if (target.Level >= WorldLevelCap)
            {
                LastEvent = "Limite do mundo atingido (nivel " + WorldLevelCap
                    + "). Avance para o proximo mundo para continuar evoluindo.";
                NotifyStateChanged();
                return false;
            }

            int cost = ProgressionRules.GetLevelUpCost(target.Level);
            if (Dindin < cost)
            {
                LastEvent = "Dindin insuficiente. O proximo nivel custa " + cost + " Dindin.";
                NotifyStateChanged();
                return false;
            }

            int previousLevel = target.Level;
            Dindin -= cost;
            target.Level = previousLevel + 1;
            target.HealFull();
            LastEvent = target.Definition.CreatureName
                + " subiu para o nivel " + target.Level
                + " por " + cost + " Dindin. HP restaurado.";

            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryLevelUpWithGold()
        {
            return TryLevelUpWithDindin();
        }

        public bool TryBuySkill(int moveId, string creatureId = null)
        {
            CreatureInstance target = creatureId == null ? PlayerCreature : FindOwnedCreature(creatureId);
            if (!initialized || target == null) return false;
            int wallet = Dindin;
            string message;
            bool success = target.TryPurchaseSkill(moveId, ref wallet, out message);
            Dindin = wallet;
            LastEvent = message;
            if (success) SaveNowSilently();
            NotifyStateChanged();
            return success;
        }

        public bool TryEquipMove(int moveId, int slot, string creatureId = null)
        {
            CreatureInstance target = creatureId == null ? PlayerCreature : FindOwnedCreature(creatureId);
            if (!initialized || target == null) return false;
            string message;
            bool success = target.TryEquipMove(moveId, slot, out message);
            LastEvent = message;
            if (success) SaveNowSilently();
            NotifyStateChanged();
            return success;
        }

        public bool TryEvolve(string creatureId = null)
        {
            CreatureInstance target = creatureId == null ? PlayerCreature : FindOwnedCreature(creatureId);
            if (!initialized || target == null) return false;
            int wallet = Dindin;
            string message;
            bool success = target.TryEvolve(ref wallet, out message);
            Dindin = wallet;
            LastEvent = message;
            if (success) SaveNowSilently();
            NotifyStateChanged();
            return success;
        }

        private bool SaveNowSilently()
        {
            if (PlayerCreature == null || inventory == null)
            {
                return false;
            }

            bool saved = !persistProgress || SaveService.Save(CreateSaveData());
            autoSaveTimer = 0f;
            return saved;
        }

        private PlayerSaveData CreateSaveData()
        {
            return new PlayerSaveData
            {
                Dindin = Dindin,
                Gold = Dindin,
                TotalDefeated = TotalDefeated,
                WorldNumber = WorldNumber,
                PhaseNumber = PhaseNumber,
                EncounterProgress = EncounterProgress,
                HasRetryPhase = hasRetryPhase,
                RetryWorldNumber = retryWorldNumber,
                RetryPhaseNumber = retryPhaseNumber,
                UnlockedPartySlots = UnlockedPartySlots,
                BoxCapacity = BoxCapacity,
                PendingLeaderInstanceId = pendingLeaderInstanceId,
                PendingBoxInstanceId = pendingBoxInstanceId,
                PendingPartyInstanceId = pendingPartyInstanceId,
                PhaseDifficulties = difficulty.Export(),
                ActiveCreature = CreateCreatureSaveData(PlayerCreature),
                Party = CreateCreatureSaveDataList(party),
                PCBox = CreateCreatureSaveDataList(pcBox),
                Inventory = inventory != null ? inventory.Items : new List<InventoryItemStack>()
            };
        }

        private static List<CreatureSaveData> CreateCreatureSaveDataList(List<CreatureInstance> creatures)
        {
            var result = new List<CreatureSaveData>();
            if (creatures == null) return result;
            for (int i = 0; i < creatures.Count; i++)
            {
                CreatureSaveData saved = CreateCreatureSaveData(creatures[i]);
                if (saved != null) result.Add(saved);
            }

            return result;
        }

        private static CreatureSaveData CreateCreatureSaveData(CreatureInstance creature)
        {
            if (creature == null || creature.Definition == null) return null;
            return new CreatureSaveData
            {
                CreatureId = creature.Definition.Id,
                Level = creature.Level,
                InstanceId = creature.InstanceId,
                LearnedMoveIds = new List<int>(creature.LearnedMoveIds),
                EquippedMoveIds = new List<int>(creature.EquippedMoveIds),
                CurrentHP = creature.CurrentHP
            };
        }

        private static string FormatDrops(List<InventoryItemStack> drops)
        {
            string result = string.Empty;
            for (int i = 0; i < drops.Count; i++)
            {
                InventoryItemStack drop = drops[i];
                ItemInfo item = ItemCatalog.Get(drop.ItemId);
                string itemName = item == null ? drop.ItemId.ToString() : item.DisplayName;
                if (i > 0)
                {
                    result += ", ";
                }

                result += "+" + drop.Quantity + " " + itemName;
            }

            return result;
        }

        private void NotifyInventoryChanged()
        {
            if (InventoryChanged != null)
            {
                InventoryChanged.Invoke();
            }
        }

        private void NotifyStateChanged()
        {
            if (StateChanged != null)
            {
                StateChanged.Invoke();
            }
        }

        private static string FormatPhaseLabel(int worldNumber, int phaseNumber)
        {
            return Mathf.Max(1, worldNumber) + "-" + Mathf.Max(0, phaseNumber);
        }

        private static bool IsPhaseAtOrAfter(int worldNumber, int phaseNumber, int otherWorldNumber, int otherPhaseNumber)
        {
            return worldNumber > otherWorldNumber
                || (worldNumber == otherWorldNumber && phaseNumber >= otherPhaseNumber);
        }
    }
}
