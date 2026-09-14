using UnityEditor;
using UnityEngine;

namespace PokeIdle.Editor
{
    [InitializeOnLoad]
    public static class PlaceholderSpriteSetup
    {
        private const string PlaceholderFolder = "Assets/Art/Placeholder/Pokemon";
        private const string CharmanderAssetPath = "Assets/Resources/PokeIdle/Demo/Creatures/Ember.asset";
        private const string CaterpieAssetPath = "Assets/Resources/PokeIdle/Demo/Creatures/Buglet.asset";
        private const string SquirtleAssetPath = "Assets/Resources/PokeIdle/Demo/Creatures/Squirtle.asset";
        private const string MetapodAssetPath = "Assets/Resources/PokeIdle/Demo/Creatures/Metapod.asset";
        private const string WeedleAssetPath = "Assets/Resources/PokeIdle/Demo/Creatures/Weedle.asset";
        private const string PidgeyAssetPath = "Assets/Resources/PokeIdle/Demo/Creatures/Pidgey.asset";
        private const string RattataAssetPath = "Assets/Resources/PokeIdle/Demo/Creatures/Rattata.asset";

        private static readonly string[] SpriteFileNames =
        {
            "Charmander_Front.png",
            "Charmander_Back.png",
            "Charmeleon_Front.png",
            "Charmeleon_Back.png",
            "Caterpie_Front.png",
            "Caterpie_Back.png",
            "Squirtle_Front.png",
            "Squirtle_Back.png",
            "Metapod_Front.png",
            "Metapod_Back.png",
            "Weedle_Front.png",
            "Weedle_Back.png",
            "Pidgey_Front.png",
            "Pidgey_Back.png",
            "Rattata_Front.png",
            "Rattata_Back.png"
        };

        static PlaceholderSpriteSetup()
        {
            EditorApplication.delayCall += RefreshAndAssign;
        }

        [MenuItem("PokeIdle/Assign Placeholder Sprites")]
        public static void AssignPlaceholderSprites()
        {
            AssetDatabase.Refresh();
            ConfigureAllImporters();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            TryAssignIfReady();
            Debug.Log("PokeIdle: sprites placeholder preparados e atribuidos.");
        }

        private static void RefreshAndAssign()
        {
            EditorApplication.delayCall -= RefreshAndAssign;
            AssetDatabase.Refresh();
            EditorApplication.delayCall += TryAssignIfReady;
        }

        private static void TryAssignIfReady()
        {
            EditorApplication.delayCall -= TryAssignIfReady;

            Sprite charmanderFront = LoadSprite("Charmander_Front.png");
            Sprite charmanderBack = LoadSprite("Charmander_Back.png");
            Sprite caterpieFront = LoadSprite("Caterpie_Front.png");
            Sprite caterpieBack = LoadSprite("Caterpie_Back.png");
            Sprite squirtleFront = LoadSprite("Squirtle_Front.png");
            Sprite squirtleBack = LoadSprite("Squirtle_Back.png");
            Sprite metapodFront = LoadSprite("Metapod_Front.png");
            Sprite metapodBack = LoadSprite("Metapod_Back.png");
            Sprite weedleFront = LoadSprite("Weedle_Front.png");
            Sprite weedleBack = LoadSprite("Weedle_Back.png");
            Sprite pidgeyFront = LoadSprite("Pidgey_Front.png");
            Sprite pidgeyBack = LoadSprite("Pidgey_Back.png");
            Sprite rattataFront = LoadSprite("Rattata_Front.png");
            Sprite rattataBack = LoadSprite("Rattata_Back.png");

            if (charmanderFront == null || charmanderBack == null || caterpieFront == null || caterpieBack == null)
            {
                bool foundTexture = false;
                for (int i = 0; i < SpriteFileNames.Length; i++)
                {
                    string path = PlaceholderFolder + "/" + SpriteFileNames[i];
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                    {
                        ConfigureImporter(SpriteFileNames[i]);
                        foundTexture = true;
                    }
                }

                if (foundTexture)
                {
                    EditorApplication.delayCall += TryAssignIfReady;
                }
                return;
            }

            CreatureDefinition charmander = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(CharmanderAssetPath);
            CreatureDefinition caterpie = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(CaterpieAssetPath);
            CreatureDefinition squirtle = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(SquirtleAssetPath);
            CreatureDefinition metapod = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(MetapodAssetPath);
            CreatureDefinition weedle = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(WeedleAssetPath);
            CreatureDefinition pidgey = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(PidgeyAssetPath);
            CreatureDefinition rattata = AssetDatabase.LoadAssetAtPath<CreatureDefinition>(RattataAssetPath);
            if (charmander == null || caterpie == null)
            {
                return;
            }

            AssignCreatureSprites(charmander, charmanderFront, charmanderBack);
            AssignCreatureSprites(caterpie, caterpieFront, caterpieBack);
            AssignCreatureSprites(squirtle, squirtleFront, squirtleBack);
            AssignCreatureSprites(metapod, metapodFront, metapodBack);
            AssignCreatureSprites(weedle, weedleFront, weedleBack);
            AssignCreatureSprites(pidgey, pidgeyFront, pidgeyBack);
            AssignCreatureSprites(rattata, rattataFront, rattataBack);
            AssignCreatureSprites(
                AssetDatabase.LoadAssetAtPath<CreatureDefinition>("Assets/Resources/PokeIdle/Demo/Creatures/Charmeleon.asset"),
                LoadSprite("Charmeleon_Front.png"), LoadSprite("Charmeleon_Back.png"));

            AssetDatabase.SaveAssets();
            Debug.Log("PokeIdle: sprites placeholder atribuidos aos assets de criatura.");
        }

        private static void AssignCreatureSprites(CreatureDefinition creature, Sprite front, Sprite back)
        {
            if (creature == null || front == null || back == null)
            {
                return;
            }

            creature.SpriteFront = front;
            creature.SpriteBack = back;
            creature.Icon = front;
            EditorUtility.SetDirty(creature);
        }

        private static void ConfigureAllImporters()
        {
            for (int i = 0; i < SpriteFileNames.Length; i++)
            {
                ConfigureImporter(SpriteFileNames[i]);
            }
        }

        private static void ConfigureImporter(string fileName)
        {
            string path = PlaceholderFolder + "/" + fileName;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderFolder + "/" + fileName);
        }
    }
}
