using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    public sealed class MainWindowUI : MonoBehaviour
    {
        private GameLoopManager loop;
        private TaskbarIntegration taskbar;
        private bool menuOpen;
        private int tab, selectedMoveSlot;
        private string selectedId, notice;
        private float noticeUntil;
        private Vector2 scroll;
        private float contentHeight = 400;
        private string dragId;
        private bool dragging, dragFromBox;
        private Vector2 dragStart;
        private Rect boxDropArea;
        private readonly List<CardTarget> cards = new List<CardTarget>();
        private static readonly string[] Tabs = { "BOX", "POKÉMON", "GOLPES", "ITENS", "ROTA" };
        private struct CardTarget { public Rect Rect; public string Id; public int Slot; public bool Box; }
        public bool IsMenuOpen { get { return menuOpen; } }

        private void Awake()
        {
            loop = GetComponent<GameLoopManager>();
            taskbar = GetComponent<TaskbarIntegration>();
        }

        private void OnGUI()
        {
            if (loop == null || loop.PlayerCreature == null) return;
            IdleGui.Ensure();
            GUI.depth = -10;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
            {
                SetMenuOpen(!menuOpen); Event.current.Use();
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && menuOpen)
            {
                SetMenuOpen(false); Event.current.Use();
            }
            cards.Clear();
            boxDropArea = Rect.zero;
            Rect strip = TaskbarLayout.Strip(Screen.width, Screen.height);
            // Cover only unused editor space; never paint over the arena.
            Rect menu = TaskbarLayout.Menu(Screen.width, Screen.height);
            if (menu.y > 0) IdleGui.Fill(new Rect(0, 0, Screen.width, menu.y), IdleGui.Back);
            if (!menuOpen && strip.y > 0) IdleGui.Fill(new Rect(0, 0, Screen.width, strip.y), IdleGui.Back);
            DrawStrip(strip);
            if (menuOpen)
            {
                if (menu.height > 210) DrawMenu(menu);
                else GUI.Label(new Rect(8, 8, Screen.width - 16, 28), "Aumente a altura da aba Game para visualizar o menu.", IdleGui.Body);
            }
            HandleCards();
        }

        private CreatureInstance Selected
        {
            get { return loop.FindOwnedCreature(selectedId) ?? loop.PlayerCreature; }
        }

        private void DrawStrip(Rect strip)
        {
            Rect arena = TaskbarLayout.Arena(Screen.width, Screen.height);
            IdleGui.Fill(new Rect(strip.x, strip.y, strip.width, 3), IdleGui.Edge);
            IdleGui.Fill(new Rect(strip.x, strip.yMax - 3, strip.width, 3), IdleGui.Edge);
            IdleGui.Fill(new Rect(strip.x, strip.y, 3, strip.height), IdleGui.Edge);
            Rect dragGrip = new Rect(arena.center.x - 16, arena.y + 3, 32, 17);
            IdleGui.Box(dragGrip, IdleGui.Card);
            for (int i = 0; i < 3; i++)
                IdleGui.Fill(new Rect(arena.center.x - 7, arena.y + 5 + i * 4, 14, 2), IdleGui.Muted);
            Rect rail = new Rect(arena.xMax + 3, strip.y, TaskbarLayout.RailWidth, strip.height);
            IdleGui.Box(rail, IdleGui.Panel);
            GUI.BeginGroup(rail);
            GUI.Label(new Rect(12, 9, 154, 20), "FASE " + loop.CurrentPhaseLabel, IdleGui.Title);
            GUI.Label(new Rect(12, 32, 154, 18),
                loop.IsPaused ? "PAUSADO" : loop.CurrentEnemy != null && loop.CurrentEnemy.IsBoss ? "BOSS EM COMBATE" : "EXPEDIÇÃO ATIVA", IdleGui.Small);
            GUI.Label(new Rect(12, 54, 154, 18), loop.EncounterProgress + " / " + loop.EnemiesPerPhase + " encontros", IdleGui.Body);
            IdleGui.Bar(new Rect(12, 76, 150, 5), loop.EncounterProgress, loop.EnemiesPerPhase, IdleGui.Gold);
            GUI.Label(new Rect(12, 89, 154, 19), loop.Dindin + " Dindin", IdleGui.Body);
            if (IdleGui.Button(new Rect(12, 119, 150, 29), menuOpen ? "FECHAR  [Tab]" : "MENU  [Tab]"))
                SetMenuOpen(!menuOpen);
            GUI.EndGroup();
        }

        private void DrawMenu(Rect rect)
        {
            IdleGui.Box(rect, IdleGui.Back);
            GUI.BeginGroup(rect);
            GUI.Label(new Rect(16, 10, 210, 24), "POKEIDLE  /  ACAMPAMENTO", IdleGui.Title);
            GUI.Label(new Rect(300, 12, 170, 22), loop.Dindin + " Dindin", IdleGui.Title);
            GUI.Label(new Rect(rect.width - 194, 13, 153, 20),
                loop.IsPaused ? "Expedição pausada" : "Combate em andamento", IdleGui.Small);
            if (IdleGui.Button(new Rect(rect.width - 34, 9, 24, 24), "X")) SetMenuOpen(false);
            IdleGui.Fill(new Rect(12, 42, rect.width - 24, 1), IdleGui.Edge);
            float leftWidth = 184;
            DrawParty(new Rect(12, 52, leftWidth, rect.height - 96));
            float rightX = leftWidth + 24, rightW = rect.width - rightX - 12;
            float tw = (rightW - (Tabs.Length - 1) * 4) / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
                if (IdleGui.Button(new Rect(rightX + i * (tw + 4), 52, tw, 27), Tabs[i], true, tab == i))
                { tab = i; scroll = Vector2.zero; }
            Rect viewport = new Rect(rightX, 88, rightW, rect.height - 134);
            if (tab == 0) boxDropArea = ToScreen(viewport);
            IdleGui.Box(viewport, IdleGui.Panel);
            Rect inside = new Rect(viewport.x + 8, viewport.y + 8, viewport.width - 16, viewport.height - 16);
            Rect screenClip = ToScreen(inside);
            scroll = GUI.BeginScrollView(inside, scroll, new Rect(0, 0, inside.width - 16, Mathf.Max(inside.height, contentHeight)));
            float y = 0, w = inside.width - 16;
            switch (tab)
            {
                case 0: DrawBox(w, ref y, screenClip); break;
                case 1: DrawPokemon(w, ref y); break;
                case 2: DrawMoves(w, ref y); break;
                case 3: DrawItems(w, ref y); break;
                case 4: DrawRoute(w, ref y); break;
            }
            contentHeight = y + 8;
            GUI.EndScrollView();
            string message = Time.unscaledTime < noticeUntil ? notice : loop.HasPendingLeaderChange || loop.HasPendingBoxSwap
                ? "Troca agendada para o fim do round." : loop.IsPaused ? "O combate está pausado." : "Sua equipe continua explorando.";
            GUI.Label(new Rect(14, rect.height - 36, rect.width - 280, 25), message, IdleGui.Small);
            if (IdleGui.Button(new Rect(rect.width - 262, rect.height - 36, 92, 26), loop.IsPaused ? "RETOMAR" : "PAUSAR")) loop.TogglePause();
            if (IdleGui.Button(new Rect(rect.width - 164, rect.height - 36, 82, 26), "SALVAR")) Act(() => loop.SaveNow());
            if (IdleGui.Button(new Rect(rect.width - 76, rect.height - 36, 64, 26), "SAIR")) { loop.SaveNow(); Application.Quit(); }
            GUI.EndGroup();
        }

        private void DrawParty(Rect rect)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 18), "SUA PARTY  " + loop.Party.Count + "/" + loop.UnlockedPartySlots, IdleGui.Small);
            float y = rect.y + 25;
            for (int i = 0; i < loop.MaxPartySlots; i++)
            {
                Rect card = new Rect(rect.x, y, rect.width, 72);
                if (i >= loop.UnlockedPartySlots)
                {
                    IdleGui.Box(card, IdleGui.Panel);
                    GUI.Label(new Rect(card.x + 10, card.y + 8, card.width - 20, 18), "SLOT " + (i + 1) + "  ·  BLOQUEADO", IdleGui.Small);
                    if (IdleGui.Button(new Rect(card.x + 10, card.y + 33, card.width - 20, 28),
                        "Liberar · " + loop.NextPartySlotCost, loop.Dindin >= loop.NextPartySlotCost))
                        Act(() => loop.TryUnlockPartySlot());
                }
                else
                {
                    CreatureInstance c = i < loop.Party.Count ? loop.Party[i] : null;
                    DrawCreatureCard(card, c, i == 0 ? "LÍDER" : "RESERVA", c != null && Selected.InstanceId == c.InstanceId);
                    cards.Add(new CardTarget { Rect = ToScreen(card), Id = c == null ? null : c.InstanceId, Slot = i });
                }
                y += 80;
            }
            GUI.Label(new Rect(rect.x, y + 2, rect.width, 34), "Arraste para organizar.\nClique para ver o Pokémon.", IdleGui.Wrapped);
        }

        private void DrawCreatureCard(Rect r, CreatureInstance c, string tag, bool selected)
        {
            IdleGui.Box(r, IdleGui.Card, selected);
            if (c == null)
            {
                GUI.Label(new Rect(r.x + 12, r.y + 12, r.width - 24, 22), "ESPAÇO LIVRE", IdleGui.Body);
                GUI.Label(new Rect(r.x + 12, r.y + 35, r.width - 24, 28), "Arraste da Box", IdleGui.Small);
                return;
            }
            IdleGui.Sprite(new Rect(r.x + 1, r.y + 4, 62, 62), c.Definition.SpriteFront);
            GUI.Label(new Rect(r.x + 63, r.y + 7, r.width - 67, 19), c.Definition.CreatureName, IdleGui.Body);
            GUI.Label(new Rect(r.x + 63, r.y + 27, r.width - 67, 17), "Nv " + c.Level + " · " + (c.IsFainted ? "DESMAIADO" : tag), IdleGui.Small);
            IdleGui.Bar(new Rect(r.x + 63, r.y + 51, r.width - 75, 6), c.CurrentHP, c.MaxHP, IdleGui.Green);
        }

        private void DrawBox(float w, ref float y, Rect clip)
        {
            GUI.Label(new Rect(0, y, w * .5f, 22), "PC BOX  ·  " + loop.PCBox.Count + "/" + loop.BoxCapacity, IdleGui.Title);
            if (IdleGui.Button(new Rect(w - 196, y, 196, 27), "+" + loop.BoxExpansionSize + " espaços · " + loop.BoxExpansionCost,
                loop.Dindin >= loop.BoxExpansionCost)) Act(() => loop.TryExpandBox());
            y += 39;
            GUI.Label(new Rect(0, y, w, 34), "Arraste para um slot da Party. Um slot ocupado troca os dois Pokémon.", IdleGui.Wrapped);
            y += 44;
            if (loop.PCBox.Count == 0)
            {
                IdleGui.Box(new Rect(0, y, w, 100), IdleGui.Back);
                GUI.Label(new Rect(18, y + 16, w - 36, 23), "Seu próximo companheiro vem aí.", IdleGui.Body);
                GUI.Label(new Rect(18, y + 46, w - 36, 42), "Os Pokémon da coleção ficam aqui.\nGuarde reservas sem interromper a expedição.", IdleGui.Wrapped);
                y += 112;
                return;
            }
            float cw = (w - 8) * .5f;
            for (int i = 0; i < loop.PCBox.Count; i++)
            {
                CreatureInstance c = loop.PCBox[i];
                Rect r = new Rect((i % 2) * (cw + 8), y + (i / 2) * 80, cw, 72);
                DrawCreatureCard(r, c, IdleGui.TypeName(c.Definition.PrimaryType), Selected.InstanceId == c.InstanceId);
                Rect screen = ToScreen(r);
                screen = Intersect(screen, clip);
                if (screen.height > 0) cards.Add(new CardTarget { Rect = screen, Id = c.InstanceId, Slot = -1, Box = true });
            }
            y += Mathf.CeilToInt(loop.PCBox.Count / 2f) * 80;
        }

        private void DrawPokemon(float w, ref float y)
        {
            CreatureInstance c = Selected;
            IdleGui.Sprite(new Rect(0, y, 96, 96), c.Definition.SpriteFront);
            GUI.Label(new Rect(106, y + 6, w - 106, 25), c.Definition.CreatureName + " · Nv " + c.Level, IdleGui.Title);
            GUI.Label(new Rect(106, y + 35, w - 106, 20), IdleGui.TypeName(c.Definition.PrimaryType)
                + (c.Definition.HasSecondaryType ? " / " + IdleGui.TypeName(c.Definition.SecondaryType) : ""), IdleGui.Body);
            GUI.Label(new Rect(106, y + 59, w - 106, 20), c.CurrentHP + " / " + c.MaxHP + " HP", IdleGui.Small);
            IdleGui.Bar(new Rect(106, y + 83, w - 110, 7), c.CurrentHP, c.MaxHP, IdleGui.Green);
            y += 107;
            int partyIndex = loop.Party.IndexOf(c);
            if (partyIndex < 0)
            {
                if (IdleGui.Button(new Rect(0, y, w, 28), loop.Party.Count < loop.UnlockedPartySlots ? "ENVIAR PARA PARTY" : "TROCAR COM O LÍDER"))
                    Act(() => { if (loop.Party.Count < loop.UnlockedPartySlots) loop.TryMoveBoxToParty(c.InstanceId, loop.Party.Count);
                        else loop.TrySwapBoxWithParty(c.InstanceId, 0); });
            }
            else
            {
                if (IdleGui.Button(new Rect(0, y, (w - 8) / 2, 28), partyIndex == 0 ? "LÍDER ATUAL" : "TORNAR LÍDER", partyIndex > 0 && !c.IsFainted))
                    Act(() => loop.TryRequestLeaderChange(c.InstanceId));
                if (IdleGui.Button(new Rect((w + 8) / 2, y, (w - 8) / 2, 28), "GUARDAR NA BOX", loop.Party.Count > 1))
                    Act(() => loop.TryMovePartyToBox(c.InstanceId));
            }
            y += 38;
            bool capped = c.Level >= Mathf.Min(loop.WorldLevelCap, loop.GlobalLevelCap);
            int price = ProgressionRules.GetLevelUpCost(c.Level);
            if (IdleGui.Button(new Rect(0, y, w, 30), capped ? "LIMITE DO MUNDO · Nv " + loop.WorldLevelCap
                : "SUBIR PARA Nv " + (c.Level + 1) + " · " + price + " Dindin", !capped && loop.Dindin >= price))
                Act(() => loop.TryLevelUpWithDindin(c.InstanceId));
            y += 43;
            BaseStats s = c.Stats;
            string[] labels = { "HP", "ATAQUE", "DEFESA", "ATQ. ESP.", "DEF. ESP.", "VELOCIDADE" };
            int[] values = { s.HP, s.Attack, s.Defense, s.SpAttack, s.SpDefense, s.Speed };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect r = new Rect((i % 3) * (w + 6) / 3, y + (i / 3) * 51, (w - 12) / 3, 45);
                IdleGui.Box(r, IdleGui.Back);
                GUI.Label(new Rect(r.x + 9, r.y + 4, r.width - 18, 16), labels[i], IdleGui.Small);
                GUI.Label(new Rect(r.x + 9, r.y + 21, r.width - 18, 20), values[i].ToString(), IdleGui.Body);
            }
            y += 112;
            if (c.Definition.EvolutionTarget != null)
            {
                string block = c.GetEvolutionBlock(loop.Dindin);
                GUI.Label(new Rect(0, y, w, 23), "Evolução · " + c.Definition.EvolutionTarget.CreatureName, IdleGui.Title); y += 29;
                if (IdleGui.Button(new Rect(0, y, w, 29), "EVOLUIR · " + c.Definition.EvolutionCost + " Dindin", block == null))
                    Act(() => loop.TryEvolve(c.InstanceId));
                y += 34;
                GUI.Label(new Rect(0, y, w, 30), block ?? "Mantém nível, golpes e identidade.", IdleGui.Small); y += 36;
            }
        }

        private void DrawMoves(float w, ref float y)
        {
            CreatureInstance c = Selected;
            GUI.Label(new Rect(0, y, w, 24), c.Definition.CreatureName + " / GOLPES", IdleGui.Title); y += 32;
            for (int i = 0; i < CreatureInstance.MaxEquippedMoves; i++)
            {
                MoveDefinition move = i < c.EquippedMoveIds.Count ? c.FindMove(c.EquippedMoveIds[i]) : null;
                if (IdleGui.Button(new Rect((i % 2) * (w + 8) / 2, y + (i / 2) * 34, (w - 8) / 2, 28),
                    (i + 1) + " · " + (move == null ? "Vazio" : move.MoveName), true, selectedMoveSlot == i)) selectedMoveSlot = i;
            }
            y += 77;
            GUI.Label(new Rect(0, y, w, 34), "Selecione um slot. A IA usa o melhor golpe equipado para cada inimigo.", IdleGui.Wrapped); y += 44;
            var listed = new HashSet<int>();
            if (c.Definition.SkillTree != null)
                foreach (SkillNode node in c.Definition.SkillTree)
                {
                    if (node == null || node.Move == null) continue;
                    listed.Add(node.Move.Id);
                    DrawMove(w, ref y, c, node.Move, node.RequiredLevel, node.Cost);
                }
            foreach (int id in c.LearnedMoveIds)
                if (!listed.Contains(id) && c.FindMove(id) != null) DrawMove(w, ref y, c, c.FindMove(id), 1, 0);
        }

        private void DrawMove(float w, ref float y, CreatureInstance c, MoveDefinition move, int level, int cost)
        {
            bool learned = c.LearnedMoveIds.Contains(move.Id), equipped = c.EquippedMoveIds.Contains(move.Id);
            string block = learned ? null : c.GetSkillPurchaseBlock(move.Id, loop.Dindin);
            IdleGui.Box(new Rect(0, y, w, 77), IdleGui.Card);
            GUI.Label(new Rect(10, y + 6, w - 114, 22), move.MoveName, IdleGui.Body);
            GUI.Label(new Rect(10, y + 30, w - 20, 18), IdleGui.TypeName(move.Type) + " · Poder " + move.Power + " · Nv " + level, IdleGui.Small);
            GUI.Label(new Rect(10, y + 52, w - 20, 18), equipped ? "Equipado" : learned ? "Aprendido" : block ?? cost + " Dindin", IdleGui.Small);
            if (IdleGui.Button(new Rect(w - 100, y + 8, 90, 26), equipped ? "EM USO" : learned ? "EQUIPAR" : "COMPRAR", !equipped && block == null))
                Act(() => { if (learned) loop.TryEquipMove(move.Id, selectedMoveSlot, c.InstanceId); else loop.TryBuySkill(move.Id, c.InstanceId); });
            y += 85;
        }

        private void DrawItems(float w, ref float y)
        {
            GUI.Label(new Rect(0, y, w, 24), "BOLSA DA EXPEDIÇÃO", IdleGui.Title); y += 36;
            foreach (InventoryItemId id in Enum.GetValues(typeof(InventoryItemId)))
            {
                ItemInfo item = ItemCatalog.Get(id);
                if (item == null) continue;
                IdleGui.Box(new Rect(0, y, w, 72), IdleGui.Card);
                GUI.Label(new Rect(12, y + 9, w - 110, 23), item.DisplayName + "  ×" + loop.GetItemQuantity(id), IdleGui.Body);
                GUI.Label(new Rect(12, y + 36, w - 110, 28), item.Description, IdleGui.Small);
                if (IdleGui.Button(new Rect(w - 89, y + 19, 77, 28), "USAR", loop.GetItemQuantity(id) > 0))
                    Act(() => loop.TryUseItem(id));
                y += 82;
            }
        }

        private void DrawRoute(float w, ref float y)
        {
            GUI.Label(new Rect(0, y, w, 25), "EXPEDIÇÃO / FASE " + loop.CurrentPhaseLabel, IdleGui.Title); y += 38;
            GUI.Label(new Rect(0, y, w, 23), loop.EncounterProgress + " de " + loop.EnemiesPerPhase + " encontros vencidos", IdleGui.Body); y += 30;
            IdleGui.Bar(new Rect(0, y, w, 8), loop.EncounterProgress, loop.EnemiesPerPhase, IdleGui.Gold); y += 27;
            GUI.Label(new Rect(0, y, w, 24), "Inimigos Nv " + loop.NormalEnemyLevel + " · Limite do mundo Nv " + loop.WorldLevelCap, IdleGui.Body); y += 34;
            GUI.Label(new Rect(0, y, w, 55), "Cada fase termina com um boss. Ao vencer, toda a Party recupera o HP e segue para a próxima fase.", IdleGui.Wrapped); y += 67;
            if (loop.CanReturnToFailedPhase)
            {
                if (IdleGui.Button(new Rect(0, y, w, 31), "TENTAR NOVAMENTE A FASE " + loop.FailedPhaseLabel)) Act(() => loop.ReturnToFailedPhase());
                y += 42;
            }
            GUI.Label(new Rect(0, y, w, 26), "Total de vitórias: " + loop.TotalDefeated, IdleGui.Small); y += 35;
        }

        private void Act(Action action) { action(); notice = loop.LastEvent; noticeUntil = Time.unscaledTime + 5; }
        public void SetMenuOpen(bool open)
        {
            menuOpen = open; dragging = false; dragId = null;
            if (taskbar != null) taskbar.SetMenuExpanded(open);
        }
        private static Rect ToScreen(Rect r) { Vector2 p = GUIUtility.GUIToScreenPoint(r.position); return new Rect(p, r.size); }
        private static Rect Intersect(Rect a, Rect b)
        {
            float x = Mathf.Max(a.x, b.x), y = Mathf.Max(a.y, b.y);
            return new Rect(x, y, Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - x), Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - y));
        }

        private void HandleCards()
        {
            Event e = Event.current;
            Vector2 mouse = GUIUtility.GUIToScreenPoint(e.mousePosition);
            if (e.type == EventType.MouseDown && e.button == 0)
                foreach (CardTarget card in cards)
                    if (card.Id != null && card.Rect.Contains(mouse))
                    { dragId = card.Id; dragFromBox = card.Box; dragStart = mouse; dragging = false; e.Use(); break; }
            if (e.type == EventType.MouseDrag && dragId != null && Vector2.Distance(mouse, dragStart) > 5) { dragging = true; e.Use(); }
            if (e.type == EventType.MouseUp && dragId != null)
            {
                string source = dragId;
                if (!dragging) { selectedId = source; tab = 1; scroll = Vector2.zero; }
                else
                {
                    bool applied = false;
                    foreach (CardTarget target in cards)
                    {
                        if (target.Box || !target.Rect.Contains(mouse)) continue;
                        if (dragFromBox) Act(() => loop.TrySwapBoxWithParty(source, target.Slot));
                        else
                        {
                            int from = loop.Party.FindIndex(c => c.InstanceId == source);
                            Act(() => loop.TryReorderParty(from, target.Slot));
                        }
                        applied = true; break;
                    }
                    if (!applied && !dragFromBox && boxDropArea.Contains(mouse)) Act(() => loop.TryMovePartyToBox(source));
                }
                dragId = null; dragging = false; e.Use();
            }
            if (dragging && dragId != null)
            {
                CreatureInstance c = loop.FindOwnedCreature(dragId);
                if (c != null)
                {
                    Rect ghost = new Rect(e.mousePosition.x + 12, e.mousePosition.y + 12, 170, 30);
                    IdleGui.Box(ghost, IdleGui.Card, true);
                    GUI.Label(new Rect(ghost.x + 8, ghost.y, ghost.width - 16, ghost.height), c.Definition.CreatureName, IdleGui.Body);
                }
            }
        }
    }
}
