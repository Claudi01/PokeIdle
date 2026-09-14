using UnityEngine;

namespace PokeIdle
{
    public sealed class MainWindowUI : MonoBehaviour
    {
        private GameLoopManager loop;
        private TaskbarIntegration taskbarIntegration;
        private bool menuOpen;
        private int menuTab;
        private int selectedMoveSlot;
        private Vector2 menuScroll;
        private readonly float[] menuContentHeights = { 360f, 1200f, 100f };
        private string progressionMessage;

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
            float desiredHeight = loop.CanReturnToFailedPhase ? 72f : 46f;
            float hudHeight = Mathf.Min(desiredHeight, Mathf.Max(34f, Screen.height - 12f));
            Rect panel = new Rect(Screen.width - hudWidth - 6f, 6f, hudWidth, hudHeight);
            DrawPanel(panel, new Color(0.025f, 0.04f, 0.07f, 0.9f));

            float contentWidth = panel.width - 78f;
            GUI.Label(new Rect(panel.x + 8f, panel.y + 3f, contentWidth, 16f),
                "FASE " + loop.CurrentPhaseLabel,
                labelStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 21f, contentWidth, 16f),
                "Ouro " + loop.Gold + "  |  KOs " + loop.TotalDefeated,
                smallLabelStyle);

            float buttonX = panel.x + panel.width - 68f;
            if (GUI.Button(new Rect(buttonX, panel.y + 5f, 60f, 27f), menuOpen ? "FECHAR" : "MENU", buttonStyle))
            {
                SetMenuOpen(!menuOpen);
            }

            if (loop.CanReturnToFailedPhase && hudHeight >= 65f)
            {
                if (GUI.Button(new Rect(panel.x + 8f, panel.y + 43f, panel.width - 16f, 23f), "TENTAR FASE " + loop.FailedPhaseLabel, buttonStyle))
                {
                    loop.ReturnToFailedPhase();
                }
            }
        }

        private void DrawExpandedMenu()
        {
            float menuWidth = Mathf.Min(430f, Screen.width - 12f);
            float menuHeight = Mathf.Min(Screen.height - 12f, 530f);
            Rect panel = new Rect(Screen.width - menuWidth - 6f, 6f, menuWidth, menuHeight);
            DrawPanel(panel, new Color(0.025f, 0.035f, 0.055f, 0.98f));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 60f, 24f),
                loop.PlayerCreature.Definition.CreatureName + "  |  Nv " + loop.PlayerCreature.Level, menuHeaderStyle);
            if (GUI.Button(new Rect(panel.xMax - 42f, panel.y + 7f, 30f, 25f), "X", buttonStyle))
            {
                SetMenuOpen(false);
                return;
            }
            GUI.Label(new Rect(panel.x + 12f, panel.y + 34f, panel.width - 24f, 20f),
                "MOEDAS " + loop.Gold + "   |   FASE " + loop.CurrentPhaseLabel
                + "   |   INIMIGOS Nv " + loop.NormalEnemyLevel, smallLabelStyle);
            int tab = GUI.Toolbar(new Rect(panel.x + 12f, panel.y + 58f, panel.width - 24f, 26f),
                menuTab, new[] { "POKEMON", "GOLPES", "ITENS" }, buttonStyle);
            if (tab != menuTab) { menuTab = tab; menuScroll = Vector2.zero; }
            Rect viewport = new Rect(panel.x + 12f, panel.y + 92f, panel.width - 24f, Mathf.Max(30f, panel.height - 182f));
            float width = viewport.width - 18f;
            menuScroll = GUI.BeginScrollView(viewport, menuScroll,
                new Rect(0f, 0f, width, Mathf.Max(viewport.height, menuContentHeights[menuTab])));
            float y = 0f;
            if (menuTab == 0) DrawPokemonMenu(width, ref y);
            else if (menuTab == 1) DrawSkillsMenu(width, ref y);
            else
            {
                GUI.Label(new Rect(0f, y, width, 24f), "ITENS DA EXPEDICAO", sectionStyle);
                y += 30f;
                DrawInventoryRow(0f, ref y, width, InventoryItemId.Potion);
                DrawInventoryRow(0f, ref y, width, InventoryItemId.SuperPotion);
            }
            menuContentHeights[menuTab] = y + 8f;
            GUI.EndScrollView();
            GUI.Label(new Rect(panel.x + 12f, panel.yMax - 83f, panel.width - 24f, 44f),
                string.IsNullOrEmpty(progressionMessage) ? loop.LastEvent : progressionMessage, tinyLabelStyle);
            float half = (panel.width - 30f) * 0.5f;
            if (GUI.Button(new Rect(panel.x + 12f, panel.yMax - 34f, half, 26f),
                loop.IsPaused ? "RETOMAR" : "PAUSAR", buttonStyle)) loop.TogglePause();
            if (GUI.Button(new Rect(panel.x + 18f + half, panel.yMax - 34f, half, 26f), "SALVAR", buttonStyle))
            {
                loop.SaveNow();
                progressionMessage = loop.LastEvent;
            }
        }

        private void DrawPokemonMenu(float width, ref float y)
        {
            CreatureInstance player = loop.PlayerCreature;
            BaseStats stats = player.Stats;
            if (loop.CanReturnToFailedPhase)
            {
                if (GUI.Button(new Rect(0f, y, width, 26f), "TENTAR FASE " + loop.FailedPhaseLabel, buttonStyle))
                    loop.ReturnToFailedPhase();
                y += 34f;
            }
            GUI.Label(new Rect(0f, y, width, 20f), "LIDER DA PARTY  |  " + player.CurrentHP + "/" + player.MaxHP + " HP", sectionStyle);
            y += 25f;
            DrawHealthBar(new Rect(0f, y, width, 8f), player.CurrentHP, player.MaxHP, new Color(0.25f, 0.88f, 0.42f));
            y += 18f;
            if (GUI.Button(new Rect(0f, y, width, 28f), "UPAR  |  " + loop.LevelUpCost + " moedas", buttonStyle))
            {
                loop.TryLevelUpWithGold();
                progressionMessage = loop.LastEvent;
            }
            y += 36f;
            CreatureDefinition definition = player.Definition;
            if (definition.EvolutionTarget != null)
            {
                GUI.Label(new Rect(0f, y, width, 20f),
                    "EVOLUCAO: " + definition.EvolutionTarget.CreatureName + "  |  Nv " + definition.EvolutionLevel, sectionStyle);
                y += 24f;
                if (GUI.Button(new Rect(0f, y, width, 28f),
                    "EVOLUIR  |  " + definition.EvolutionCost + " moedas", buttonStyle))
                {
                    loop.TryEvolve();
                    progressionMessage = loop.LastEvent;
                }
                y += 32f;
                GUI.Label(new Rect(0f, y, width, 28f),
                    player.GetEvolutionBlock(loop.Gold) ?? "Disponivel! Mantem o nivel e os golpes comprados.", tinyLabelStyle);
                y += 34f;
            }
            GUI.Label(new Rect(0f, y, width, 20f), "STATUS", sectionStyle);
            y += 25f;
            GUI.Label(new Rect(0f, y, width, 20f),
                "HP " + stats.HP + "   ATK " + stats.Attack + "   DEF " + stats.Defense, smallLabelStyle);
            y += 22f;
            GUI.Label(new Rect(0f, y, width, 20f),
                "SP.A " + stats.SpAttack + "   SP.D " + stats.SpDefense + "   VEL " + stats.Speed, smallLabelStyle);
            y += 32f;
            GUI.Label(new Rect(0f, y, width, 20f), "PARTY", sectionStyle);
            y += 25f;
            float slotWidth = (width - 8f) / 3f;
            DrawPartySlot(new Rect(0f, y, slotWidth, 32f), player.Definition.CreatureName, "LIDER");
            DrawPartySlot(new Rect(slotWidth + 4f, y, slotWidth, 32f), "VAGO", "VAZIO");
            DrawPartySlot(new Rect((slotWidth + 4f) * 2f, y, slotWidth, 32f), "VAGO", "VAZIO");
            y += 42f;
        }

        private void DrawSkillsMenu(float width, ref float y)
        {
            CreatureInstance player = loop.PlayerCreature;
            GUI.Label(new Rect(0f, y, width, 20f), "GOLPES EQUIPADOS  |  " + player.GetEquippedMoves().Count + "/4", sectionStyle);
            y += 25f;
            float slotWidth = (width - 6f) * 0.5f;
            for (int slot = 0; slot < CreatureInstance.MaxEquippedMoves; slot++)
            {
                MoveDefinition move = slot < player.EquippedMoveIds.Count ? player.FindMove(player.EquippedMoveIds[slot]) : null;
                string label = (selectedMoveSlot == slot ? "> " : "") + (slot + 1) + ": " + (move == null ? "VAGO" : move.MoveName);
                if (GUI.Button(new Rect((slot % 2) * (slotWidth + 6f), y + (slot / 2) * 32f, slotWidth, 28f), label, buttonStyle))
                    selectedMoveSlot = slot;
            }
            y += 68f;
            GUI.Label(new Rect(0f, y, width, 34f),
                "Selecione um slot acima e use EQUIPAR. Trocas sao gratuitas; golpes comprados ficam aprendidos.", tinyLabelStyle);
            y += 40f;
            MoveDefinition best = AutoBattleEngine.ChooseMove(player, loop.CurrentEnemy);
            GUI.Label(new Rect(0f, y, width, 30f),
                best == null ? "A IA escolhe o melhor dano entre os quatro equipados."
                    : "Melhor golpe contra " + loop.CurrentEnemy.Definition.CreatureName + ": " + best.MoveName, tinyLabelStyle);
            y += 36f;
            GUI.Label(new Rect(0f, y, width, 20f), "ARVORE DE HABILIDADES", sectionStyle);
            y += 25f;
            if (player.Definition.SkillTree == null || player.Definition.SkillTree.Count == 0)
            {
                GUI.Label(new Rect(0f, y, width, 30f), "Sem habilidades configuradas para este Pokemon.", tinyLabelStyle);
                y += 36f;
                return;
            }
            foreach (SkillNode node in player.Definition.SkillTree)
            {
                if (node == null || node.Move == null) continue;
                bool learned = player.LearnedMoveIds.Contains(node.Move.Id);
                bool equipped = player.EquippedMoveIds.Contains(node.Move.Id);
                DrawPanel(new Rect(0f, y, width, 96f), new Color(0.08f, 0.1f, 0.14f));
                string route = node.Prerequisite == null ? "INICIAL" : node.Prerequisite.MoveName + "  >";
                GUI.Label(new Rect(8f, y + 4f, width - 16f, 18f), route, tinyLabelStyle);
                GUI.Label(new Rect(8f, y + 24f, width - 102f, 20f), node.Move.MoveName, labelStyle);
                GUI.Label(new Rect(8f, y + 47f, width - 16f, 18f),
                    TypeLabel(node.Move.Type) + "  |  Poder " + node.Move.Power + "  |  Nv " + node.RequiredLevel
                    + (learned ? "" : "  |  " + node.Cost + " moedas"), smallLabelStyle);
                string state = equipped ? "Equipado" : learned ? "Aprendido" : player.GetSkillPurchaseBlock(node.Move.Id, loop.Gold) ?? "Disponivel para comprar";
                GUI.Label(new Rect(8f, y + 69f, width - 16f, 24f), state, tinyLabelStyle);
                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && !equipped;
                if (GUI.Button(new Rect(width - 88f, y + 24f, 80f, 24f), equipped ? "EQUIPADO" : learned ? "EQUIPAR" : "COMPRAR", buttonStyle))
                {
                    if (learned) loop.TryEquipMove(node.Move.Id, selectedMoveSlot);
                    else loop.TryBuySkill(node.Move.Id);
                    progressionMessage = loop.LastEvent;
                }
                GUI.enabled = wasEnabled;
                y += 102f;
            }
        }

        private static string TypeLabel(ElementalType type)
        {
            switch (type)
            {
                case ElementalType.Fire: return "Fogo";
                case ElementalType.Water: return "Agua";
                case ElementalType.Electric: return "Eletrico";
                case ElementalType.Fighting: return "Lutador";
                case ElementalType.Steel: return "Aco";
                case ElementalType.Dragon: return "Dragao";
                default: return type.ToString();
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
            if (GUI.Button(new Rect(x + width - buttonWidth, y - 1f, buttonWidth, 24f), "USAR", buttonStyle))
            {
                loop.TryUseItem(itemId);
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
