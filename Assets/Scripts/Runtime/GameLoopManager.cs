using System;
using UnityEngine;

namespace PokeIdle
{
    public sealed class GameLoopManager : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float tickIntervalSeconds = 1f;
        [SerializeField, Min(1)] private int enemiesPerRoute = 10;

        private DemoContentSet content;
        private AutoBattleEngine battleEngine;
        private float tickTimer;
        private bool initialized;

        public static GameLoopManager Instance { get; private set; }
        public CreatureInstance PlayerCreature { get; private set; }
        public CreatureInstance CurrentEnemy { get; private set; }
        public int Gold { get; private set; }
        public int TotalDefeated { get; private set; }
        public int RouteNumber { get; private set; } = 1;
        public int RouteProgress { get; private set; }
        public int EnemiesPerRoute { get { return enemiesPerRoute; } }
        public bool IsPaused { get; private set; }
        public string LastEvent { get; private set; } = "Preparando a expedição...";

        public event Action StateChanged;

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

            PlayerCreature = new CreatureInstance(content.Starter, save != null && save.ActiveCreature != null ? save.ActiveCreature.Level : 5);
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
            initialized = true;
            LastEvent = "Expedição iniciada. O combate acontece a cada segundo.";
            NotifyStateChanged();
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            LastEvent = IsPaused ? "Expedição pausada." : "Expedição retomada.";
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

            SaveService.Save(new PlayerSaveData
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
                }
            });

            LastEvent = "Progresso salvo.";
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
                PlayerCreature.HealFull();
                LastEvent = PlayerCreature.Definition.CreatureName + " se recuperou e voltou ao combate.";
                NotifyStateChanged();
                return;
            }

            if (CurrentEnemy == null || CurrentEnemy.IsFainted)
            {
                SpawnEnemy();
            }

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
            int enemyLevel = Mathf.Max(1, 3 + RouteNumber + RouteProgress / 3);
            CurrentEnemy = new CreatureInstance(content.WildCreature, enemyLevel);
            LastEvent = "Um " + CurrentEnemy.Definition.CreatureName + " apareceu na rota.";
        }

        private void RewardEnemyDefeat()
        {
            int defeatedLevel = CurrentEnemy.Level;
            int goldReward = 5 + defeatedLevel;
            int experienceReward = 8 + defeatedLevel * 2;
            bool leveledUp = PlayerCreature.GainExperience(experienceReward);

            Gold += goldReward;
            TotalDefeated++;
            RouteProgress++;
            LastEvent += " Recompensas: +" + goldReward + " ouro, +" + experienceReward + " XP.";

            if (leveledUp)
            {
                LastEvent += " " + PlayerCreature.Definition.CreatureName + " alcançou o nível " + PlayerCreature.Level + ".";
            }

            if (RouteProgress >= enemiesPerRoute)
            {
                RouteNumber++;
                RouteProgress = 0;
                LastEvent += " Rota concluída! Próxima rota desbloqueada.";
            }

            CurrentEnemy = null;
        }

        private void SaveNowSilently()
        {
            if (PlayerCreature == null)
            {
                return;
            }

            SaveService.Save(new PlayerSaveData
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
                }
            });
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
