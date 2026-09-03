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

        private static readonly string[] SpriteFileNames =
        {
            "Charmander_Front.png",
            "Charmander_Back.png",
            "Caterpie_Front.png",
            "Caterpie_Back.png",
            "Squirtle_Front.png",
            "Squirtle_Back.png"
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
            if (charmander == null || caterpie == null)
            {
                return;
            }

            charmander.SpriteFront = charmanderFront;
            charmander.SpriteBack = charmanderBack;
            charmander.Icon = charmanderFront;
            caterpie.SpriteFront = caterpieFront;
            caterpie.SpriteBack = caterpieBack;
            caterpie.Icon = caterpieFront;

            if (squirtle != null && squirtleFront != null && squirtleBack != null)
            {
                squirtle.SpriteFront = squirtleFront;
                squirtle.SpriteBack = squirtleBack;
                squirtle.Icon = squirtleFront;
                EditorUtility.SetDirty(squirtle);
            }

            EditorUtility.SetDirty(charmander);
            EditorUtility.SetDirty(caterpie);
            AssetDatabase.SaveAssets();
            Debug.Log("PokeIdle: sprites de Charmander, Caterpie e Squirtle atribuídos aos assets.");
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
