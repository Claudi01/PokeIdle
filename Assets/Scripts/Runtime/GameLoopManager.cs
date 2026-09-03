using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    public sealed class GameLoopManager : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float tickIntervalSeconds = 1f;
        [SerializeField, Min(1)] private int enemiesPerRoute = 10;

        private DemoContentSet content;
        private AutoBattleEngine battleEngine;
        private PlayerInventory inventory = new PlayerInventory();
        private float tickTimer;
        private bool initialized;

        public static GameLoopManager Instance { get; private set; }
        public CreatureInstance PlayerCreature { get; private set; }
        public CreatureInstance CurrentEnemy { get; private set; }
        public int Gold { get; private set; }
        public int TotalDefeated { get; private set; }
        public int RouteNumber { get; private set; } = 1;
        public int StageNumber { get { return RouteNumber; } }
        public int RouteProgress { get; private set; }
        public int EnemiesPerRoute { get { return enemiesPerRoute; } }
        public bool IsPaused { get; private set; }
        public BattlePhase Phase { get; private set; } = BattlePhase.Searching;
        public PlayerInventory Inventory { get { return inventory; } }
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
            battleEngine = new AutoBattleEngine();
        }

        private void Start()
        {
            InitializeSession();
        }

        private void Update()
        {
            if (!initialized || IsPaused)
            {
                return;
            }

            tickTimer += Time.unscaledDeltaTime;
            if (tickTimer < tickIntervalSeconds)
            {
                return;
            }

            tickTimer -= tickIntervalSeconds;
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

            PlayerCreature = new CreatureInstance(
                content.Starter,
                save != null && save.ActiveCreature != null
                    ? save.ActiveCreature.Level
                    : ProgressionRules.StartingCreatureLevel);

            if (save != null && save.ActiveCreature != null && save.ActiveCreature.CreatureId == content.Starter.Id)
            {
                PlayerCreature.Experience = Mathf.Max(0, save.ActiveCreature.Experience);
                PlayerCreature.CurrentHP = save.ActiveCreature.CurrentHP;
            }

            PlayerCreature.EnsureValid();
            Gold = save != null ? Mathf.Max(0, save.Gold) : 0;
            TotalDefeated = save != null ? Mathf.Max(0, save.TotalDefeated) : 0;
            RouteNumber = save != null ? Mathf.Max(1, save.RouteNumber) : 1;
            RouteProgress = save != null ? Mathf.Clamp(save.RouteProgress, 0, enemiesPerRoute - 1) : 0;

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
            initialized = true;
            LastEvent = "Expedicao iniciada. O combate acontece a cada segundo.";
            NotifyInventoryChanged();
            NotifyStateChanged();
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

            SaveService.Save(CreateSaveData());
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
            RouteNumber = 1;
            RouteProgress = 0;
            Phase = BattlePhase.Searching;
            IsPaused = false;
            tickTimer = 0f;
            inventory = new PlayerInventory();
            initialized = true;

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
                Phase = BattlePhase.Recovering;
                PlayerCreature.HealFull();
                LastEvent = PlayerCreature.Definition.CreatureName + " se recuperou e voltou ao combate.";
                SaveNowSilently();
                NotifyStateChanged();
                return;
            }

            if (CurrentEnemy == null || CurrentEnemy.IsFainted)
            {
                SpawnEnemy();
                // O encontro ocupa um tick de aproximação antes do primeiro golpe.
                // Isso dá tempo para a arena mostrar o inimigo entrando em linha reta.
                SaveNowSilently();
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

            if (CurrentEnemy.IsFainted)
            {
                RewardEnemyDefeat();
            }

            SaveNowSilently();
            NotifyStateChanged();
        }

        private void SpawnEnemy()
        {
            CreatureDefinition wildCreature = SelectWildCreature();
            if (wildCreature == null)
            {
                LastEvent = "Nenhum encontro esta configurado para este estagio.";
                return;
            }

            int enemyLevel = Mathf.Max(1, 3 + RouteNumber + RouteProgress / 3);
            CurrentEnemy = new CreatureInstance(wildCreature, enemyLevel);
            Phase = BattlePhase.Searching;
            LastEvent = "Um " + CurrentEnemy.Definition.CreatureName + " apareceu na rota.";
        }

        private CreatureDefinition SelectWildCreature()
        {
            if (content == null)
            {
                return null;
            }

            if (content.WildEncounters == null || content.WildEncounters.Count == 0)
            {
                return content.WildCreature;
            }

            var availableCreatures = new List<CreatureDefinition>();
            for (int i = 0; i < content.WildEncounters.Count; i++)
            {
                WildCreatureEncounter encounter = content.WildEncounters[i];
                if (encounter != null && encounter.Creature != null && StageNumber >= encounter.MinimumStage)
                {
                    availableCreatures.Add(encounter.Creature);
                }
            }

            if (availableCreatures.Count == 0)
            {
                return content.WildCreature;
            }

            return availableCreatures[UnityEngine.Random.Range(0, availableCreatures.Count)];
        }

        private void RewardEnemyDefeat()
        {
            int defeatedLevel = CurrentEnemy.Level;
            int routeAtDefeat = RouteNumber;
            int goldReward = ProgressionRules.GetGoldReward(defeatedLevel, routeAtDefeat);
            int experienceReward = ProgressionRules.GetExperienceReward(defeatedLevel, routeAtDefeat);
            bool leveledUp = PlayerCreature.GainExperience(experienceReward);

            Gold += goldReward;
            TotalDefeated++;
            RouteProgress++;
            LastEvent += " Recompensas: +" + goldReward + " ouro, +" + experienceReward + " XP.";

            List<InventoryItemStack> drops = LootTable.RollDrops(defeatedLevel, routeAtDefeat);
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

            if (leveledUp)
            {
                LastEvent += " " + PlayerCreature.Definition.CreatureName + " alcancou o nivel " + PlayerCreature.Level + ".";
            }

            if (RouteProgress >= enemiesPerRoute)
            {
                RouteNumber++;
                RouteProgress = 0;
                inventory.Add(InventoryItemId.Potion, 1);
                LastEvent += " Rota concluida! Proxima rota desbloqueada.";
                LastEvent += " Bonus da rota: +1 Pocao.";
                NotifyInventoryChanged();
            }

            // Mantem o inimigo derrotado ate o proximo tick para evitar um frame vazio.
            Phase = BattlePhase.Searching;
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

        public bool TryBuyItem(InventoryItemId itemId, int quantity)
        {
            if (!initialized || inventory == null || quantity <= 0)
            {
                return false;
            }

            ItemInfo item = ItemCatalog.Get(itemId);
            if (item == null)
            {
                return false;
            }

            long totalCost = (long)item.ShopPrice * quantity;
            if (totalCost > Gold || totalCost > int.MaxValue)
            {
                LastEvent = "Ouro insuficiente para comprar " + quantity + "x " + item.DisplayName + ".";
                NotifyStateChanged();
                return false;
            }

            Gold -= (int)totalCost;
            inventory.Add(itemId, quantity);
            LastEvent = "Comprou " + quantity + "x " + item.DisplayName + " por " + totalCost + " ouro.";
            SaveNowSilently();
            NotifyInventoryChanged();
            NotifyStateChanged();
            return true;
        }

        private void SaveNowSilently()
        {
            if (PlayerCreature == null || inventory == null)
            {
                return;
            }

            SaveService.Save(CreateSaveData());
        }

        private PlayerSaveData CreateSaveData()
        {
            return new PlayerSaveData
            {
                Gold = Gold,
                TotalDefeated = TotalDefeated,
                RouteNumber = RouteNumber,
                RouteProgress = RouteProgress,
                ActiveCreature = new CreatureSaveData
                {
                    CreatureId = PlayerCreature.Definition != null ? PlayerCreature.Definition.Id : 0,
                    Level = PlayerCreature.Level,
                    Experience = PlayerCreature.Experience,
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
    }
}
