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
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("PokeIdle/Create Demo Scene")]
        public static void CreateDemoScene()
        {
            CreateDemoContent();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject app = new GameObject("PokeIdleApp");
            app.AddComponent<PokeIdleApp>();
            app.AddComponent<GameLoopManager>();
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

            if (app.GetComponent<GameLoopManager>() == null)
            {
                app.AddComponent<GameLoopManager>();
            }

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
            var hordeRenderers = new System.Collections.Generic.List<SpriteRenderer>();
            for (int i = 0; i < 6; i++)
            {
                hordeRenderers.Add(CreateSceneRenderer(appTransform, "HordeVisual_" + (i + 1).ToString("00"), 2));
            }
            arena.ConfigureReferences(arenaCamera, playerRenderer, enemyRenderer, playerShadowRenderer, enemyShadowRenderer, groundRenderer, hordeRenderers);
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
            MoveDefinition ember = UpsertMove(DemoMoves + "/Ember.asset", 2, "Brasa", ElementalType.Fire, MoveCategory.Special, 45);
            MoveDefinition tackle = UpsertMove(DemoMoves + "/Tackle.asset", 3, "Investida", ElementalType.Normal, MoveCategory.Physical, 40);

            CreatureDefinition starter = UpsertCreature(
                DemoCreatures + "/Ember.asset",
                4,
                "Charmander",
                ElementalType.Fire,
                FarmClass.Attacker,
                new BaseStats(45, 55, 40, 60, 50, 65),
                new LearnableMove(1, quickHit),
                new LearnableMove(1, ember));

            UpsertCreature(
                DemoCreatures + "/Buglet.asset",
                10,
                "Caterpie",
                ElementalType.Bug,
                FarmClass.Attacker,
                new BaseStats(38, 48, 35, 30, 35, 45),
                new LearnableMove(1, tackle));

            EditorUtility.SetDirty(starter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
