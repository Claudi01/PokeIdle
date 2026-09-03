using UnityEngine;

namespace PokeIdle
{
    public sealed class MainWindowUI : MonoBehaviour
    {
        private GameLoopManager loop;
        private TaskbarIntegration taskbarIntegration;
        private bool menuOpen;

        private GUIStyle labelStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle tinyLabelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle menuHeaderStyle;
        private GUIStyle sectionStyle;

        private void Awake()
        {
            loop = GetComponent<GameLoopManager>();
            taskbarIntegration = GetComponent<TaskbarIntegration>();
        }

        private void OnEnable()
        {
            if (loop != null)
            {
                loop.StateChanged += Repaint;
                loop.InventoryChanged += Repaint;
            }
        }

        private void OnDisable()
        {
            if (loop != null)
            {
                loop.StateChanged -= Repaint;
                loop.InventoryChanged -= Repaint;
            }
        }

        private void OnGUI()
        {
            if (loop == null || loop.PlayerCreature == null)
            {
                return;
            }

            EnsureStyles();
            DrawCompactPlayerTag();
            DrawCompactHud();

            if (menuOpen)
            {
                DrawExpandedMenu();
            }
        }

        private void DrawCompactPlayerTag()
        {
            if (Screen.width < 300f)
            {
                return;
            }

            float width = Mathf.Min(190f, Screen.width * 0.34f);
            Rect panel = new Rect(6f, 6f, width, 39f);
            DrawPanel(panel, new Color(0.025f, 0.04f, 0.07f, 0.84f));

            CreatureInstance player = loop.PlayerCreature;
            GUI.Label(new Rect(panel.x + 8f, panel.y + 3f, width - 16f, 16f),
                player.Definition.CreatureName + "  Nv " + player.Level,
                labelStyle);
            DrawHealthBar(new Rect(panel.x + 8f, panel.y + 23f, width - 16f, 8f), player.CurrentHP, player.MaxHP, new Color(0.25f, 0.88f, 0.42f));
        }

        private void DrawCompactHud()
        {
            float hudWidth = Mathf.Clamp(Screen.width * 0.3f, 205f, 265f);
            hudWidth = Mathf.Min(hudWidth, Screen.width - 12f);
            float hudHeight = Mathf.Min(46f, Mathf.Max(34f, Screen.height - 12f));
            Rect panel = new Rect(Screen.width - hudWidth - 6f, 6f, hudWidth, hudHeight);
            DrawPanel(panel, new Color(0.025f, 0.04f, 0.07f, 0.9f));

            float contentWidth = panel.width - 78f;
            GUI.Label(new Rect(panel.x + 8f, panel.y + 3f, contentWidth, 16f),
                "ESTAGIO " + loop.StageNumber + "  " + loop.RouteProgress + "/" + loop.EnemiesPerRoute,
                labelStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 21f, contentWidth, 16f),
                "Ouro " + loop.Gold + "  |  KOs " + loop.TotalDefeated,
                smallLabelStyle);

            float buttonX = panel.x + panel.width - 68f;
            if (GUI.Button(new Rect(buttonX, panel.y + 5f, 60f, 27f), menuOpen ? "FECHAR" : "MENU", buttonStyle))
            {
                SetMenuOpen(!menuOpen);
            }
        }

        private void DrawExpandedMenu()
        {
            float menuWidth = Mathf.Clamp(Screen.width * 0.42f, 330f, 430f);
            menuWidth = Mathf.Min(menuWidth, Screen.width - 12f);
            float menuHeight = Mathf.Min(Screen.height - 12f, 356f);
            Rect panel = new Rect(Screen.width - menuWidth - 6f, 6f, menuWidth, menuHeight);
            DrawPanel(panel, new Color(0.025f, 0.035f, 0.055f, 0.97f));

            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 60f, 24f), "MENU  |  EXPEDICAO", menuHeaderStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 42f, panel.y + 7f, 30f, 25f), "X", buttonStyle))
            {
                SetMenuOpen(false);
                return;
            }

            float x = panel.x + 12f;
            float width = panel.width - 24f;
            float y = panel.y + 38f;
            CreatureInstance player = loop.PlayerCreature;
            BaseStats stats = player.Stats;

            GUI.Label(new Rect(x, y, width, 18f), "POKEMON ATIVO  |  LIDER DA PARTY", sectionStyle);
            y += 22f;
            GUI.Label(new Rect(x, y, width * 0.58f, 18f), player.Definition.CreatureName + "  Nv " + player.Level, labelStyle);
            GUI.Label(new Rect(x + width * 0.58f, y, width * 0.42f, 18f), player.CurrentHP + "/" + player.MaxHP + " HP", smallLabelStyle);
            y += 20f;
            DrawHealthBar(new Rect(x, y, width, 8f), player.CurrentHP, player.MaxHP, new Color(0.25f, 0.88f, 0.42f));
            y += 13f;
            GUI.Label(new Rect(x, y, width, 15f), "XP  " + player.Experience + " / " + player.ExperienceToNextLevel, tinyLabelStyle);
            DrawHealthBar(new Rect(x, y + 17f, width, 6f), player.Experience, player.ExperienceToNextLevel, new Color(0.28f, 0.57f, 0.96f));
            y += 31f;

            GUI.Label(new Rect(x, y, width, 18f), "STATUS", sectionStyle);
            y += 21f;
            GUI.Label(new Rect(x, y, width, 17f),
                "HP " + stats.HP + "    ATK " + stats.Attack + "    DEF " + stats.Defense,
                smallLabelStyle);
            y += 17f;
            GUI.Label(new Rect(x, y, width, 17f),
                "SP.A " + stats.SpAttack + "    SP.D " + stats.SpDefense + "    VEL " + stats.Speed,
                smallLabelStyle);
            y += 27f;

            GUI.Label(new Rect(x, y, width, 18f), "PARTY", sectionStyle);
            y += 21f;
            float slotWidth = (width - 8f) / 3f;
            DrawPartySlot(new Rect(x, y, slotWidth, 32f), player.Definition.CreatureName, "LIDER");
            DrawPartySlot(new Rect(x + slotWidth + 4f, y, slotWidth, 32f), "VAGO", "VAZIO");
            DrawPartySlot(new Rect(x + (slotWidth + 4f) * 2f, y, slotWidth, 32f), "VAGO", "VAZIO");
            y += 45f;

            GUI.Label(new Rect(x, y, width, 18f), "INVENTARIO", sectionStyle);
            y += 21f;
            DrawInventoryRow(x, ref y, width, InventoryItemId.Potion);
            DrawInventoryRow(x, ref y, width, InventoryItemId.SuperPotion);
            y += 6f;

            GUI.Label(new Rect(x, y, width, 17f), "ULTIMO EVENTO: " + loop.LastEvent, tinyLabelStyle);
            y += 22f;
            float halfWidth = (width - 6f) * 0.5f;
            if (GUI.Button(new Rect(x, y, halfWidth, 27f), loop.IsPaused ? "RETOMAR" : "PAUSAR", buttonStyle))
            {
                loop.TogglePause();
            }

            if (GUI.Button(new Rect(x + halfWidth + 6f, y, halfWidth, 27f), "SALVAR", buttonStyle))
            {
                loop.SaveNow();
            }
        }

        private void DrawInventoryRow(float x, ref float y, float width, InventoryItemId itemId)
        {
            ItemInfo item = ItemCatalog.Get(itemId);
            if (item == null)
            {
                return;
            }

            int quantity = loop.GetItemQuantity(itemId);
            GUI.Label(new Rect(x, y, width * 0.45f, 22f), item.DisplayName + "  x" + quantity, smallLabelStyle);
            float buttonWidth = 54f;
            if (GUI.Button(new Rect(x + width - buttonWidth * 2f - 6f, y - 1f, buttonWidth, 24f), "USAR", buttonStyle))
            {
                loop.TryUseItem(itemId);
            }

            if (GUI.Button(new Rect(x + width - buttonWidth, y - 1f, buttonWidth, 24f), "+ " + item.ShopPrice, buttonStyle))
            {
                loop.TryBuyItem(itemId, 1);
            }

            y += 27f;
        }

        private void DrawPartySlot(Rect rect, string name, string subtitle)
        {
            DrawPanel(rect, new Color(0.08f, 0.1f, 0.14f, 1f));
            GUI.Label(new Rect(rect.x + 5f, rect.y + 3f, rect.width - 10f, 15f), name, tinyLabelStyle);
            GUI.Label(new Rect(rect.x + 5f, rect.y + 17f, rect.width - 10f, 12f), subtitle, tinyLabelStyle);
        }

        private void SetMenuOpen(bool open)
        {
            menuOpen = open;
            if (taskbarIntegration != null)
            {
                taskbarIntegration.SetMenuExpanded(menuOpen);
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
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.82f, 0.86f, 0.94f) }
            };
            tinyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                wordWrap = true,
                normal = { textColor = new Color(0.72f, 0.78f, 0.88f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(2, 2, 1, 1)
            };
            menuHeaderStyle = new GUIStyle(labelStyle)
            {
                fontSize = 15,
                normal = { textColor = new Color(1f, 0.78f, 0.3f) }
            };
            sectionStyle = new GUIStyle(labelStyle)
            {
                fontSize = 10,
                normal = { textColor = new Color(1f, 0.7f, 0.25f) }
            };
        }

        private static void DrawPanel(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none);
            GUI.color = previousColor;
        }

        private static void DrawHealthBar(Rect rect, int current, int maximum, Color fillColor)
        {
            float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
            Color previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.1f, 0.15f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * ratio, rect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static void Repaint()
        {
            // OnGUI repinta automaticamente; o callback existe para manter a assinatura dos eventos.
        }
    }
}
