using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokeIdle.Editor
{
    public static class ProjectBootstrap
    {
        private const string DemoRoot = "Assets/Resources/PokeIdle/Demo";
        private const string DemoMoves = DemoRoot + "/Moves";
        private const string DemoCreatures = DemoRoot + "/Creatures";
        private const string ConfigRoot = "Assets/Resources/PokeIdle/Config";
        private const string BalanceConfigPath = ConfigRoot + "/GameBalanceConfig.asset";
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("PokeIdle/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            CreateDemoContent();
            GameBalanceConfig balanceConfig = CreateOrLoadGameBalanceConfig();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject app = new GameObject("PokeIdleApp");
            app.AddComponent<PokeIdleApp>();
            GameLoopManager loop = app.AddComponent<GameLoopManager>();
            loop.ConfigureBalance(balanceConfig);
            app.AddComponent<MainWindowUI>();
            app.AddComponent<TaskbarIntegration>();

            BattleArenaView arena = app.AddComponent<BattleArenaView>();
            ConfigureArenaHierarchy(app.transform, arena);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = app;
            Debug.Log("PokeIdle: cena demo criada em " + ScenePath);
        }

        [MenuItem("PokeIdle/Add or Repair Arena in Current Scene")]
        public static void AddOrRepairArenaInCurrentScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Pare o Play Mode antes de reparar a arena da cena.");
                return;
            }

            GameObject app = GameObject.Find("PokeIdleApp");
            if (app == null)
            {
                app = new GameObject("PokeIdleApp");
            }

            if (app.GetComponent<PokeIdleApp>() == null)
            {
                app.AddComponent<PokeIdleApp>();
            }

            GameLoopManager loop = app.GetComponent<GameLoopManager>();
            if (loop == null)
            {
                loop = app.AddComponent<GameLoopManager>();
            }

            loop.ConfigureBalance(CreateOrLoadGameBalanceConfig());

            if (app.GetComponent<MainWindowUI>() == null)
            {
                app.AddComponent<MainWindowUI>();
            }

            if (app.GetComponent<TaskbarIntegration>() == null)
            {
                app.AddComponent<TaskbarIntegration>();
            }

            BattleArenaView arena = app.GetComponent<BattleArenaView>();
            if (arena == null)
            {
                arena = app.AddComponent<BattleArenaView>();
            }

            ConfigureArenaHierarchy(app.transform, arena);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("PokeIdle: arena adicionada/reparada na cena atual.");
        }

        private static void ConfigureArenaHierarchy(Transform appTransform, BattleArenaView arena)
        {
            Camera arenaCamera = GetOrCreateSceneCamera(appTransform);
            SpriteRenderer playerRenderer = CreateSceneRenderer(appTransform, "PlayerVisual", 10);
            SpriteRenderer enemyRenderer = CreateSceneRenderer(appTransform, "EnemyVisual", 10);
            SpriteRenderer playerShadowRenderer = CreateSceneRenderer(appTransform, "PlayerShadow", 5);
            SpriteRenderer enemyShadowRenderer = CreateSceneRenderer(appTransform, "EnemyShadow", 5);
            SpriteRenderer groundRenderer = CreateSceneRenderer(appTransform, "ArenaGround", -10);
            arena.ConfigureReferences(
                arenaCamera,
                playerRenderer,
                enemyRenderer,
                playerShadowRenderer,
                enemyShadowRenderer,
                groundRenderer,
                new System.Collections.Generic.List<SpriteRenderer>());
        }

        private static Camera GetOrCreateSceneCamera(Transform parent)
        {
            Transform existing = parent.Find("ArenaCamera");
            GameObject cameraObject = existing != null ? existing.gameObject : new GameObject("ArenaCamera");
            cameraObject.transform.SetParent(parent);
            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.06f, 0.12f, 1f);
            camera.transform.localPosition = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static SpriteRenderer CreateSceneRenderer(Transform parent, string name, int sortingOrder)
        {
            Transform existing = parent.Find(name);
            GameObject child = existing != null ? existing.gameObject : new GameObject(name);
            child.transform.SetParent(parent);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.AddComponent<SpriteRenderer>();
            }
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        [MenuItem("PokeIdle/Create Demo Content")]
        public static void CreateDemoContent()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "PokeIdle");
            EnsureFolder("Assets/Resources/PokeIdle", "Demo");
            EnsureFolder(DemoRoot, "Moves");
            EnsureFolder(DemoRoot, "Creatures");

            MoveDefinition quickHit = UpsertMove(DemoMoves + "/QuickHit.asset", 1, "Golpe Rápido", ElementalType.Normal, MoveCategory.Physical, 40);
            MoveDefinition ember = UpsertMove(DemoMoves + "/Ember.asset", 2, "Brasa", ElementalType.Fire, MoveCategory.Special, 50);
            MoveDefinition tackle = UpsertMove(DemoMoves + "/Tackle.asset", 3, "Investida", ElementalType.Normal, MoveCategory.Physical, 40);
            MoveDefinition waterGun = UpsertMove(DemoMoves + "/WaterGun.asset", 4, "Jato de Agua", ElementalType.Water, MoveCategory.Special, 40);
            MoveDefinition gust = UpsertMove(DemoMoves + "/Gust.asset", 5, "Rajada de Vento", ElementalType.Flying, MoveCategory.Special, 40);
            MoveDefinition poisonSting = UpsertMove(DemoMoves + "/PoisonSting.asset", 6, "Ferroada", ElementalType.Poison, MoveCategory.Physical, 40);
            MoveDefinition metalClaw = UpsertMove(DemoMoves + "/MetalClaw.asset", 7, "Garra de Metal", ElementalType.Steel, MoveCategory.Physical, 50);
            MoveDefinition dragonBreath = UpsertMove(DemoMoves + "/DragonBreath.asset", 8, "Sopro do Dragao", ElementalType.Dragon, MoveCategory.Special, 60);
            MoveDefinition brickBreak = UpsertMove(DemoMoves + "/BrickBreak.asset", 9, "Quebra-Telha", ElementalType.Fighting, MoveCategory.Physical, 75);
            MoveDefinition flamethrower = UpsertMove(DemoMoves + "/Flamethrower.asset", 10, "Lanca-Chamas", ElementalType.Fire, MoveCategory.Special, 90);
            MoveDefinition thunderPunch = UpsertMove(DemoMoves + "/ThunderPunch.asset", 11, "Soco Trovao", ElementalType.Electric, MoveCategory.Physical, 75);
            MoveDefinition slash = UpsertMove(DemoMoves + "/Slash.asset", 12, "Talho", ElementalType.Normal, MoveCategory.Physical, 70);

            CreatureDefinition starter = UpsertCreature(
                DemoCreatures + "/Ember.asset",
                4,
                "Charmander",
                ElementalType.Fire,
                FarmClass.Attacker,
                new BaseStats(45, 55, 40, 60, 50, 65),
                new LearnableMove(1, quickHit),
                new LearnableMove(1, ember));

            CreatureDefinition charmeleon = UpsertCreature(
                DemoCreatures + "/Charmeleon.asset", 5, "Charmeleon", ElementalType.Fire,
                FarmClass.Attacker, new BaseStats(58, 64, 58, 80, 65, 80),
                new LearnableMove(1, quickHit), new LearnableMove(1, ember));
            StarterSkillContent.Configure(starter, charmeleon, quickHit, ember, metalClaw,
                slash, brickBreak, flamethrower, dragonBreath, thunderPunch);
            EditorUtility.SetDirty(charmeleon);

            CreatureDefinition caterpie = UpsertCreature(
                DemoCreatures + "/Buglet.asset",
                10,
                "Caterpie",
                ElementalType.Bug,
                FarmClass.Attacker,
                new BaseStats(38, 48, 35, 30, 35, 45),
                new LearnableMove(1, tackle));

            CreatureDefinition metapod = UpsertCreature(
                DemoCreatures + "/Metapod.asset",
                11,
                "Metapod",
                ElementalType.Bug,
                FarmClass.Tank,
                new BaseStats(50, 35, 55, 25, 35, 30),
                new LearnableMove(1, tackle));

            caterpie.EvolutionTarget = metapod;
            caterpie.EvolutionLevel = 7;
            EditorUtility.SetDirty(caterpie);

            CreatureDefinition weedle = UpsertCreature(
                DemoCreatures + "/Weedle.asset",
                13,
                "Weedle",
                ElementalType.Bug,
                FarmClass.Attacker,
                new BaseStats(40, 35, 30, 20, 20, 50),
                new LearnableMove(1, poisonSting));

            CreatureDefinition pidgey = UpsertCreature(
                DemoCreatures + "/Pidgey.asset",
                16,
                "Pidgey",
                ElementalType.Flying,
                FarmClass.Speedster,
                new BaseStats(40, 45, 40, 35, 35, 56),
                new LearnableMove(1, gust));

            CreatureDefinition rattata = UpsertCreature(
                DemoCreatures + "/Rattata.asset",
                19,
                "Rattata",
                ElementalType.Normal,
                FarmClass.Attacker,
                new BaseStats(30, 56, 35, 25, 35, 72),
                new LearnableMove(1, tackle));

            caterpie.WildSpawnWeight = 6;
            weedle.WildSpawnWeight = 2;
            pidgey.WildSpawnWeight = 2;
            rattata.WildSpawnWeight = 2;
            EditorUtility.SetDirty(caterpie);
            EditorUtility.SetDirty(weedle);
            EditorUtility.SetDirty(pidgey);
            EditorUtility.SetDirty(rattata);

            CreatureDefinition squirtle = UpsertCreature(
                DemoCreatures + "/Squirtle.asset",
                7,
                "Squirtle",
                ElementalType.Water,
                FarmClass.Tank,
                new BaseStats(44, 48, 65, 50, 64, 43),
                new LearnableMove(1, waterGun));
            squirtle.WildSpawnWeight = 4;
            EditorUtility.SetDirty(squirtle);

            EditorUtility.SetDirty(starter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("PokeIdle/Create Game Balance Config")]
        public static void CreateGameBalanceConfigAsset()
        {
            GameBalanceConfig config = CreateOrLoadGameBalanceConfig();
            Selection.activeObject = config;
            Debug.Log("PokeIdle: configuracao de balanceamento selecionada em " + BalanceConfigPath);
        }

        private static GameBalanceConfig CreateOrLoadGameBalanceConfig()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "PokeIdle");
            EnsureFolder("Assets/Resources/PokeIdle", "Config");

            GameBalanceConfig config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalanceConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameBalanceConfig>();
                config.ResetToDefaults();
                AssetDatabase.CreateAsset(config, BalanceConfigPath);
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return config;
        }

        private static MoveDefinition UpsertMove(string path, int id, string name, ElementalType type, MoveCategory category, int power)
        {
            MoveDefinition asset = AssetDatabase.LoadAssetAtPath<MoveDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MoveDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Id = id;
            asset.MoveName = name;
            asset.Type = type;
            asset.Category = category;
            asset.Power = power;
            asset.Accuracy = 100;
            asset.MaxPP = 20;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CreatureDefinition UpsertCreature(
            string path,
            int id,
            string name,
            ElementalType primaryType,
            FarmClass farmClass,
            BaseStats stats,
            params LearnableMove[] learnset)
        {
            CreatureDefinition asset = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CreatureDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Id = id;
            asset.CreatureName = name;
            asset.PrimaryType = primaryType;
            asset.HasSecondaryType = false;
            asset.FarmAI = farmClass;
            asset.BaseRarity = Rarity.Common;
            asset.BaseStats = stats;
            if (asset.Learnset == null)
            {
                asset.Learnset = new System.Collections.Generic.List<LearnableMove>();
            }
            asset.Learnset.Clear();
            asset.Learnset.AddRange(learnset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
