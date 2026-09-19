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
        private float tickTimer;
        private float autoSaveTimer;
        private bool initialized;
        private bool hasRetryPhase;
        private int retryWorldNumber = 1;
        private int retryPhaseNumber;

        public static GameLoopManager Instance { get; private set; }
        public CreatureInstance PlayerCreature { get; private set; }
        public CreatureInstance CurrentEnemy { get; private set; }
        public int Gold { get; private set; }
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
            get { return PlayerCreature != null && Gold >= LevelUpCost; }
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
            if (initialized)
            {
                return;
            }

            content = DemoContent.Load();
            PlayerSaveData save = SaveService.Load();

            PlayerCreature = SaveService.RestoreCreature(save == null ? null : save.ActiveCreature, content, save != null && save.Version >= 6);

            bool loadedAfterDefeat = save != null
                && save.ActiveCreature != null
                && save.ActiveCreature.CreatureId == PlayerCreature.Definition.Id
                && save.ActiveCreature.CurrentHP <= 0;
            PlayerCreature.EnsureValid();
            Gold = save != null ? Mathf.Max(0, save.Gold) : 0;
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
            PlayerCreature.Level = Mathf.Clamp(PlayerCreature.Level, 1, WorldLevelCap);
            PlayerCreature.EnsureValid();
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

            SaveNowSilently();
            LastEvent = "Progresso salvo.";
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

            PlayerCreature = new CreatureInstance(content.Starter, ProgressionRules.StartingCreatureLevel);
            CurrentEnemy = null;
            Gold = 0;
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

        private void ProcessTick()
        {
            if (PlayerCreature == null)
            {
                return;
            }

            if (PlayerCreature.IsFainted)
            {
                HandlePlayerDefeat();
                return;
            }

            if (CurrentEnemy == null || CurrentEnemy.IsFainted)
            {
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
                HandlePlayerDefeat();
                return;
            }

            if (CurrentEnemy.IsFainted)
            {
                RewardEnemyDefeat();
            }

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
            int goldReward = ProgressionRules.GetGoldReward(defeatedLevel, worldAtDefeat);

            Gold += goldReward;
            TotalDefeated++;
            EncounterProgress++;
            LastEvent += " Recompensas: +" + goldReward + " ouro.";

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
            Phase = BattlePhase.Recovering;
            PlayerCreature.HealFull();

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
            CurrentEnemy = null;
            Phase = BattlePhase.Recovering;
            PlayerCreature.HealFull();
            LastEvent = "Retomando a fase " + CurrentPhaseLabel + ".";
            SaveNowSilently();
            NotifyStateChanged();
            return true;
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

        public bool TryLevelUpWithGold()
        {
            if (!initialized || PlayerCreature == null)
            {
                return false;
            }

            if (PlayerCreature.Level >= GlobalLevelCap)
            {
                LastEvent = "Nivel maximo global atingido (" + GlobalLevelCap + ").";
                NotifyStateChanged();
                return false;
            }

            if (PlayerCreature.Level >= WorldLevelCap)
            {
                LastEvent = "Limite do mundo atingido (nivel " + WorldLevelCap
                    + "). Avance para o proximo mundo para continuar evoluindo.";
                NotifyStateChanged();
                return false;
            }

            int cost = LevelUpCost;
            if (Gold < cost)
            {
                LastEvent = "Ouro insuficiente. O proximo nivel custa " + cost + " moedas.";
                NotifyStateChanged();
                return false;
            }

            int previousLevel = PlayerCreature.Level;
            Gold -= cost;
            PlayerCreature.Level = previousLevel + 1;
            PlayerCreature.HealFull();
            LastEvent = PlayerCreature.Definition.CreatureName
                + " subiu para o nivel " + PlayerCreature.Level
                + " por " + cost + " moedas. HP restaurado.";

            SaveNowSilently();
            NotifyStateChanged();
            return true;
        }

        public bool TryBuySkill(int moveId)
        {
            if (!initialized || PlayerCreature == null) return false;
            int wallet = Gold;
            string message;
            bool success = PlayerCreature.TryPurchaseSkill(moveId, ref wallet, out message);
            Gold = wallet;
            LastEvent = message;
            if (success) SaveNowSilently();
            NotifyStateChanged();
            return success;
        }

        public bool TryEquipMove(int moveId, int slot)
        {
            if (!initialized || PlayerCreature == null) return false;
            string message;
            bool success = PlayerCreature.TryEquipMove(moveId, slot, out message);
            LastEvent = message;
            if (success) SaveNowSilently();
            NotifyStateChanged();
            return success;
        }

        public bool TryEvolve()
        {
            if (!initialized || PlayerCreature == null) return false;
            int wallet = Gold;
            string message;
            bool success = PlayerCreature.TryEvolve(ref wallet, out message);
            Gold = wallet;
            LastEvent = message;
            if (success) SaveNowSilently();
            NotifyStateChanged();
            return success;
        }

        private void SaveNowSilently()
        {
            if (PlayerCreature == null || inventory == null)
            {
                return;
            }

            SaveService.Save(CreateSaveData());
            autoSaveTimer = 0f;
        }

        private PlayerSaveData CreateSaveData()
        {
            return new PlayerSaveData
            {
                Gold = Gold,
                TotalDefeated = TotalDefeated,
                WorldNumber = WorldNumber,
                PhaseNumber = PhaseNumber,
                EncounterProgress = EncounterProgress,
                HasRetryPhase = hasRetryPhase,
                RetryWorldNumber = retryWorldNumber,
                RetryPhaseNumber = retryPhaseNumber,
                PhaseDifficulties = difficulty.Export(),
                ActiveCreature = new CreatureSaveData
                {
                    CreatureId = PlayerCreature.Definition != null ? PlayerCreature.Definition.Id : 0,
                    Level = PlayerCreature.Level,
                    InstanceId = PlayerCreature.InstanceId,
                    LearnedMoveIds = new List<int>(PlayerCreature.LearnedMoveIds),
                    EquippedMoveIds = new List<int>(PlayerCreature.EquippedMoveIds),
                    CurrentHP = PlayerCreature.CurrentHP
                },
                Inventory = inventory != null ? inventory.Items : new List<InventoryItemStack>()
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
