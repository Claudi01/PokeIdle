using UnityEngine;

namespace PokeIdle
{
    public sealed class MainWindowUI : MonoBehaviour
    {
        private GameLoopManager loop;
        private GUIStyle labelStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            loop = GetComponent<GameLoopManager>();
        }

        private void OnEnable()
        {
            if (loop != null)
            {
                loop.StateChanged += Repaint;
            }
        }

        private void OnDisable()
        {
            if (loop != null)
            {
                loop.StateChanged -= Repaint;
            }
        }

        private void OnGUI()
        {
            if (loop == null || loop.PlayerCreature == null)
            {
                return;
            }

            EnsureStyles();

            Rect panel = new Rect(0f, 0f, Screen.width, 48f);
            Color previousColor = GUI.color;
            // Mantém a arena visível por trás da HUD quando a janela tiver apenas 48 px de altura.
            GUI.color = new Color(0.035f, 0.045f, 0.07f, 0.52f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = previousColor;

            CreatureInstance player = loop.PlayerCreature;
            CreatureInstance enemy = loop.CurrentEnemy;
            string enemyName = enemy == null || enemy.Definition == null ? "Procurando..." : enemy.Definition.CreatureName + " Nv " + enemy.Level;

            GUI.Label(new Rect(8f, 2f, 230f, 20f), "ROTA " + loop.RouteNumber + "  •  " + player.Definition.CreatureName + " Nv " + player.Level, labelStyle);
            DrawHealthBar(new Rect(8f, 25f, 220f, 13f), player.CurrentHP, player.MaxHP, new Color(0.25f, 0.85f, 0.42f));
            GUI.Label(new Rect(65f, 24f, 160f, 16f), player.CurrentHP + " / " + player.MaxHP + " HP", smallLabelStyle);

            GUI.Label(new Rect(245f, 2f, 270f, 20f), "Inimigo: " + enemyName, labelStyle);
            if (enemy != null)
            {
                DrawHealthBar(new Rect(245f, 25f, 220f, 13f), enemy.CurrentHP, enemy.MaxHP, new Color(0.93f, 0.35f, 0.32f));
                GUI.Label(new Rect(302f, 24f, 160f, 16f), enemy.CurrentHP + " / " + enemy.MaxHP + " HP", smallLabelStyle);
            }

            GUI.Label(new Rect(490f, 4f, 360f, 18f), "Ouro: " + loop.Gold + "   Derrotados: " + loop.TotalDefeated + "   Progresso: " + loop.RouteProgress + "/" + loop.EnemiesPerRoute, labelStyle);
            GUI.Label(new Rect(490f, 25f, Mathf.Max(220f, Screen.width - 760f), 17f), loop.LastEvent, smallLabelStyle);

            float buttonX = Mathf.Max(700f, Screen.width - 150f);
            if (GUI.Button(new Rect(buttonX, 4f, 68f, 35f), loop.IsPaused ? "Retomar" : "Pausar", buttonStyle))
            {
                loop.TogglePause();
            }

            if (GUI.Button(new Rect(buttonX + 72f, 4f, 68f, 35f), "Salvar", buttonStyle))
            {
                loop.SaveNow();
            }
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.82f, 0.86f, 0.94f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                padding = new RectOffset(2, 2, 1, 1)
            };
        }

        private static void DrawHealthBar(Rect rect, int current, int maximum, Color fillColor)
        {
            float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
            Color previousColor = GUI.color;
            GUI.color = new Color(0.12f, 0.14f, 0.2f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * ratio, rect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static void Repaint()
        {
            // OnGUI repaints automatically; this callback is intentionally lightweight.
        }
    }
}
