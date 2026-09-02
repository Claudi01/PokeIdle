using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    public sealed class BattleArenaView : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private Camera arenaCamera;
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private SpriteRenderer enemyRenderer;
        [SerializeField] private SpriteRenderer playerShadowRenderer;
        [SerializeField] private SpriteRenderer enemyShadowRenderer;
        [SerializeField] private SpriteRenderer groundRenderer;
        [SerializeField] private List<SpriteRenderer> hordeRenderers = new List<SpriteRenderer>();

        [Header("Layout")]
        [SerializeField, Range(0.25f, 0.5f)] private float playerViewportX = 0.42f;
        [SerializeField, Range(0.5f, 0.75f)] private float enemyViewportX = 0.58f;
        [SerializeField, Range(0.2f, 0.8f)] private float playerViewportY = 0.39f;
        [SerializeField, Range(0.2f, 0.8f)] private float enemyViewportY = 0.49f;
        [SerializeField, Min(0.5f)] private float cameraSizeInEditor = 3.2f;
        [SerializeField, Min(0.5f)] private float cameraSizeInBuild = 2.2f;

        private GameLoopManager loop;
        private Sprite fallbackPlayerSprite;
        private Sprite fallbackEnemySprite;
        private Sprite fallbackShadowSprite;
        private Sprite fallbackGroundSprite;
        private Sprite currentPlayerSprite;
        private Sprite currentEnemySprite;

        private void Awake()
        {
            loop = GetComponent<GameLoopManager>();
            EnsureSceneReferences();
            CreateFallbackSprites();
        }

        private void OnEnable()
        {
            if (loop != null)
            {
                loop.StateChanged += RefreshNow;
            }
        }

        private void OnDisable()
        {
            if (loop != null)
            {
                loop.StateChanged -= RefreshNow;
            }
        }

        private void Start()
        {
            RefreshNow();
        }

        private void LateUpdate()
        {
            RefreshNow();
            AnimateBattle();
        }

        public void ConfigureReferences(
            Camera camera,
            SpriteRenderer player,
            SpriteRenderer enemy,
            SpriteRenderer playerShadow,
            SpriteRenderer enemyShadow,
            SpriteRenderer ground,
            List<SpriteRenderer> horde)
        {
            arenaCamera = camera;
            playerRenderer = player;
            enemyRenderer = enemy;
            playerShadowRenderer = playerShadow;
            enemyShadowRenderer = enemyShadow;
            groundRenderer = ground;
            hordeRenderers = horde ?? new List<SpriteRenderer>();
        }

        private void EnsureSceneReferences()
        {
            if (arenaCamera == null)
            {
                GameObject cameraObject = new GameObject("ArenaCamera");
                cameraObject.transform.SetParent(transform);
                arenaCamera = cameraObject.AddComponent<Camera>();
            }

            arenaCamera.orthographic = true;
            arenaCamera.clearFlags = CameraClearFlags.SolidColor;
            arenaCamera.backgroundColor = new Color(0.035f, 0.06f, 0.12f, 1f);
            arenaCamera.cullingMask = -1;

            if (playerRenderer == null)
            {
                playerRenderer = CreateRenderer("PlayerVisual", 10);
            }

            if (enemyRenderer == null)
            {
                enemyRenderer = CreateRenderer("EnemyVisual", 10);
            }

            if (playerShadowRenderer == null)
            {
                playerShadowRenderer = CreateRenderer("PlayerShadow", 5);
            }

            if (enemyShadowRenderer == null)
            {
                enemyShadowRenderer = CreateRenderer("EnemyShadow", 5);
            }

            if (groundRenderer == null)
            {
                groundRenderer = CreateRenderer("ArenaGround", -10);
            }

            if (hordeRenderers == null)
            {
                hordeRenderers = new List<SpriteRenderer>();
            }

            while (hordeRenderers.Count < 6)
            {
                hordeRenderers.Add(CreateRenderer("HordeVisual_" + (hordeRenderers.Count + 1).ToString("00"), 2));
            }
        }

        private SpriteRenderer CreateRenderer(string objectName, int sortingOrder)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(transform);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void CreateFallbackSprites()
        {
            fallbackPlayerSprite = CreateCreatureSprite(
                new Color(0.95f, 0.34f, 0.12f),
                new Color(1f, 0.78f, 0.22f),
                false,
                "Fallback_Ember");
            fallbackEnemySprite = CreateCreatureSprite(
                new Color(0.25f, 0.72f, 0.32f),
                new Color(0.7f, 0.95f, 0.25f),
                true,
                "Fallback_Buglet");
            fallbackShadowSprite = CreateShadowSprite();
            fallbackGroundSprite = CreateGroundSprite();
            playerShadowRenderer.sprite = fallbackShadowSprite;
            enemyShadowRenderer.sprite = fallbackShadowSprite;
            groundRenderer.sprite = fallbackGroundSprite;
        }

        private void RefreshNow()
        {
            if (loop == null || loop.PlayerCreature == null || arenaCamera == null)
            {
                return;
            }

            arenaCamera.orthographicSize = Application.isEditor ? cameraSizeInEditor : cameraSizeInBuild;
            arenaCamera.transform.position = new Vector3(0f, 0f, -10f);

            CreatureInstance player = loop.PlayerCreature;
            CreatureInstance enemy = loop.CurrentEnemy;
            Sprite playerSprite = GetPlayerSprite(player);
            Sprite enemySprite = GetEnemySprite(enemy);

            if (currentPlayerSprite != playerSprite)
            {
                currentPlayerSprite = playerSprite;
                playerRenderer.sprite = playerSprite;
            }

            if (currentEnemySprite != enemySprite)
            {
                currentEnemySprite = enemySprite;
                enemyRenderer.sprite = enemySprite;
            }

            playerRenderer.enabled = player != null;
            enemyRenderer.enabled = enemy != null;
            playerShadowRenderer.enabled = player != null;
            enemyShadowRenderer.enabled = enemy != null;

            FitRendererToArena(playerRenderer, playerViewportX, playerViewportY, 0.49f, 0.44f, true);
            if (enemy != null)
            {
                FitRendererToArena(enemyRenderer, enemyViewportX, enemyViewportY, 0.49f, 0.44f, false);
            }

            FitShadowToArena(playerShadowRenderer, playerViewportX, playerViewportY);
            FitShadowToArena(enemyShadowRenderer, enemyViewportX, enemyViewportY);
            FitGroundToArena();

            for (int i = 0; i < hordeRenderers.Count; i++)
            {
                SpriteRenderer hordeRenderer = hordeRenderers[i];
                hordeRenderer.sprite = enemySprite;
                hordeRenderer.enabled = enemy != null;
                FitHordeRenderer(hordeRenderer, i);
            }
        }

        private void AnimateBattle()
        {
            if (loop == null || loop.CurrentEnemy == null || loop.IsPaused || !playerRenderer.enabled)
            {
                return;
            }

            float phase = Mathf.Repeat(Time.unscaledTime, 2f);
            float playerLunge = phase < 0.32f ? Mathf.Sin(phase / 0.32f * Mathf.PI) : 0f;
            float enemyLunge = phase >= 1f && phase < 1.32f ? Mathf.Sin((phase - 1f) / 0.32f * Mathf.PI) : 0f;
            float worldWidth = GetWorldWidth();

            playerRenderer.transform.position += Vector3.right * (worldWidth * 0.035f * playerLunge);
            enemyRenderer.transform.position += Vector3.left * (worldWidth * 0.035f * enemyLunge);
            AnimateHorde();
        }

        private void AnimateHorde()
        {
            if (hordeRenderers == null || hordeRenderers.Count == 0 || loop == null || loop.CurrentEnemy == null)
            {
                return;
            }

            for (int i = 0; i < hordeRenderers.Count; i++)
            {
                SpriteRenderer renderer = hordeRenderers[i];
                bool comesFromLeft = i % 2 == 0;
                int lane = i / 2;
                float cycle = Mathf.Repeat(Time.unscaledTime * 0.12f + i * 0.16f, 1f);
                float startX = comesFromLeft ? 0.015f : 0.985f;
                float endX = comesFromLeft ? 0.34f : 0.66f;
                float viewportX = Mathf.Lerp(startX, endX, cycle);
                float viewportY = 0.29f + lane * 0.13f;
                Vector3 position = ViewportToWorld(viewportX, viewportY);
                renderer.transform.position = new Vector3(position.x, position.y, 0.2f);
                renderer.flipX = !comesFromLeft;
            }
        }

        private void FitRendererToArena(SpriteRenderer renderer, float viewportX, float viewportY, float widthRatio, float heightRatio, bool flip)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            Vector3 position = ViewportToWorld(viewportX, viewportY);
            renderer.transform.position = new Vector3(position.x, position.y, 0f);
            renderer.flipX = flip;

            float targetHeight = arenaCamera.orthographicSize * heightRatio;
            float spriteHeight = Mathf.Max(0.01f, renderer.sprite.bounds.size.y);
            float scale = targetHeight / spriteHeight;
            renderer.transform.localScale = Vector3.one * scale;
        }

        private void FitShadowToArena(SpriteRenderer renderer, float viewportX, float viewportY)
        {
            if (renderer == null)
            {
                return;
            }

            Vector3 position = ViewportToWorld(viewportX, 0.24f);
            renderer.transform.position = new Vector3(position.x, position.y, 0f);
            renderer.transform.localScale = new Vector3(arenaCamera.orthographicSize * 0.55f, arenaCamera.orthographicSize * 0.08f, 1f);
        }

        private void FitHordeRenderer(SpriteRenderer renderer, int index)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            bool comesFromLeft = index % 2 == 0;
            int lane = index / 2;
            float viewportX = comesFromLeft ? 0.015f + lane * 0.02f : 0.985f - lane * 0.02f;
            float viewportY = 0.29f + lane * 0.13f;
            Vector3 position = ViewportToWorld(viewportX, viewportY);
            renderer.transform.position = new Vector3(position.x, position.y, 0.2f);
            renderer.transform.localScale = Vector3.one * (arenaCamera.orthographicSize * 0.22f / Mathf.Max(0.01f, renderer.sprite.bounds.size.y));
            renderer.flipX = !comesFromLeft;
        }

        private void FitGroundToArena()
        {
            if (groundRenderer == null || groundRenderer.sprite == null)
            {
                return;
            }

            Vector3 bottomLeft = ViewportToWorld(0f, 0f);
            Vector3 topRight = ViewportToWorld(1f, 1f);
            groundRenderer.transform.position = new Vector3((bottomLeft.x + topRight.x) * 0.5f, bottomLeft.y + (topRight.y - bottomLeft.y) * 0.2f, 1f);
            groundRenderer.transform.localScale = new Vector3(
                (topRight.x - bottomLeft.x) / Mathf.Max(0.01f, groundRenderer.sprite.bounds.size.x),
                arenaCamera.orthographicSize * 0.42f / Mathf.Max(0.01f, groundRenderer.sprite.bounds.size.y),
                1f);
            groundRenderer.color = new Color(0.08f, 0.18f, 0.22f, 1f);
        }

        private Vector3 ViewportToWorld(float x, float y)
        {
            return arenaCamera.ViewportToWorldPoint(new Vector3(x, y, 10f));
        }

        private float GetWorldWidth()
        {
            return Vector3.Distance(ViewportToWorld(0f, 0.5f), ViewportToWorld(1f, 0.5f));
        }

        private Sprite GetPlayerSprite(CreatureInstance creature)
        {
            if (creature != null && creature.Definition != null)
            {
                if (creature.Definition.SpriteBack != null)
                {
                    return creature.Definition.SpriteBack;
                }

                if (creature.Definition.SpriteFront != null)
                {
                    return creature.Definition.SpriteFront;
                }
            }

            return fallbackPlayerSprite;
        }

        private Sprite GetEnemySprite(CreatureInstance creature)
        {
            if (creature != null && creature.Definition != null && creature.Definition.SpriteFront != null)
            {
                return creature.Definition.SpriteFront;
            }

            return fallbackEnemySprite;
        }

        private static Sprite CreateCreatureSprite(Color bodyColor, Color accentColor, bool bugLike, string name)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name + "Texture";
            texture.filterMode = FilterMode.Point;

            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(0.03f, 0.04f, 0.06f, 1f);
            Color eye = Color.white;
            Color pupil = new Color(0.02f, 0.02f, 0.02f, 1f);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = transparent;
            }

            FillEllipse(pixels, size, 32, 28, bugLike ? 22 : 18, bugLike ? 15 : 19, outline);
            FillEllipse(pixels, size, 32, 29, bugLike ? 18 : 15, bugLike ? 12 : 16, bodyColor);
            FillEllipse(pixels, size, 35, 44, bugLike ? 17 : 13, bugLike ? 11 : 14, outline);
            FillEllipse(pixels, size, 35, 44, bugLike ? 14 : 10, bugLike ? 8 : 11, accentColor);
            FillEllipse(pixels, size, 40, 46, 4, 5, eye);
            FillEllipse(pixels, size, 40, 46, 2, 3, pupil);

            if (bugLike)
            {
                FillRect(pixels, size, 14, 17, 19, 20, outline);
                FillRect(pixels, size, 16, 18, 17, 18, accentColor);
                FillRect(pixels, size, 45, 17, 49, 20, outline);
                FillRect(pixels, size, 46, 18, 48, 18, accentColor);
            }
            else
            {
                FillEllipse(pixels, size, 14, 25, 9, 7, outline);
                FillEllipse(pixels, size, 12, 26, 6, 5, accentColor);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateShadowSprite()
        {
            const int width = 64;
            const int height = 16;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Fallback_ShadowTexture";
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = (x - width * 0.5f) / (width * 0.5f);
                    float normalizedY = (y - height * 0.5f) / (height * 0.5f);
                    float distance = normalizedX * normalizedX + normalizedY * normalizedY;
                    pixels[y * width + x] = distance <= 1f ? new Color(0f, 0f, 0f, 0.38f) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
        }

        private static Sprite CreateGroundSprite()
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = "Fallback_GroundTexture";
            texture.SetPixels(new[]
            {
                new Color(0.08f, 0.18f, 0.22f, 1f),
                new Color(0.08f, 0.18f, 0.22f, 1f),
                new Color(0.11f, 0.24f, 0.25f, 1f),
                new Color(0.11f, 0.24f, 0.25f, 1f)
            });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static void FillEllipse(Color[] pixels, int size, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    float dx = (x - centerX) / (float)Mathf.Max(1, radiusX);
                    float dy = (y - centerY) / (float)Mathf.Max(1, radiusY);
                    if (dx * dx + dy * dy <= 1f && x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        private static void FillRect(Color[] pixels, int size, int minX, int minY, int maxX, int maxY, Color color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }
    }
}
