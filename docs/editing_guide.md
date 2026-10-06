# Guia rapido para editar o prototipo

## Onde mudar o balanceamento

Abra `Assets/Resources/PokeIdle/Config/GameBalanceConfig.asset` na janela Project da Unity. Os valores aparecem no Inspector e podem ser alterados sem editar o codigo.

- `Starting Creature Level`: nivel inicial do Charmander.
- `Tick Interval Seconds`: intervalo entre as acoes automaticas.
- `Phases Per World`: ultima fase padrão. O mundo 1 usa `1-0` até `1-N`; os demais usam `2-1` até `2-N`.
- `Enemies Per Phase`: quantidade de inimigos em cada fase; o ultimo e o boss.
- `Global Max Level`: limite absoluto de nivel (100 por padrao; libera o Prestige no futuro).
- `Default World Level Cap` e `World Level Cap Growth`: limite do Pokemon por mundo. O padrao e 10 no mundo 1, 20 no mundo 2, 30 no mundo 3 etc.
- `Auto Save Interval Seconds`: save automatico entre 30 e 60 segundos. Compras, derrotas e conclusoes de fase salvam imediatamente.
- `Base Dindin...`: recompensa de Dindin.
- `Base Level Up Cost`: custo em Dindin para subir do nivel atual para o proximo.
- `Level Up Cost Growth`: aumento do custo a cada novo nivel.
- `Boss Level Bonus` e `Boss Health Multiplier`: dificuldade dos bosses.
- `Enemy Levels Per Phase`: crescimento base por fase (1.25 por padrao).
- `Enemy Player Level Ratio`: piso relativo ao nivel do jogador na primeira visita (0.9 = 90%).

A dificuldade e fixada ao entrar pela primeira vez em uma fase e salva. Upar dentro dela ou voltar para farmar nao aumenta os inimigos daquela fase. O calculo inicial usa o maior valor entre a curva acumulada de fases e `floor(nivel do jogador * Enemy Player Level Ratio)`, sempre respeitando o limite do mundo. O boss recebe o `Boss Level Bonus`, sem ultrapassar esse limite. Por exemplo: chegando a `2-1` no nivel 17, os inimigos ficam no nivel 15 e o boss no 17; voltar depois no nivel 30 nao muda esse snapshot. Alteracoes na curva afetam fases ainda nao visitadas; use o reset apenas se quiser testar uma nova run completa.

Para criar um mundo com quantidade ou limite diferente, adicione uma entrada em `World Overrides` no Inspector. `Last Phase Number` define o ultimo identificador da fase, `Level Cap` define o limite daquele mundo e `Enemies Per Phase` altera a quantidade padrão de encontros. Para uma excecao isolada, use `Phase Overrides` e informe `World Number`, `Phase Number`, `Normal Enemy Level` ou `Enemies In Phase`; valor zero usa a regra global. Assim, o balanceamento combina fórmula geral com ajustes manuais por mundo/fase.

Para alterar os encontros, abra um asset de criatura em `Assets/Resources/PokeIdle/Demo/Creatures`. `Wild Spawn Weight` controla a frequência de aparição; a criatura com maior peso entre as disponíveis define o boss da fase. `Evolution Target` define qual evolução será usada como boss.

O objeto `PokeIdleApp` usa esse asset no componente `GameLoopManager`. Se o campo estiver vazio, o jogo usa valores padrao seguros.

## Como testar do zero

1. Selecione `PokeIdleApp` na Hierarchy.
2. No componente `GameLoopManager`, abra o menu de contexto.
3. Use `Reset Progress (Testing)`.
4. Pressione Play.

Isso apaga apenas o save local do prototipo e inicia em `1-0` com o nivel definido em `GameBalanceConfig`.

## Onde cada parte fica

- `Assets/Scripts/Runtime/Config/GameBalanceConfig.cs`: valores editaveis do jogo.
- `Assets/Scripts/Runtime/GameLoopManager.cs`: fluxo de fases, combate, moedas, drops e save.
- `Assets/Scripts/Runtime/CreatureInstance.cs`: nivel, HP e cura ao subir de nivel.
- `Assets/Scripts/Runtime/GameLoopManager.cs`: tambem cura o Pokemon ao concluir uma fase.
- `Assets/Scripts/Runtime/ProgressionRules.cs`: ponte entre a configuracao e as regras de progressao.
- `Assets/Scripts/Runtime/UI/MainWindowUI.cs`: HUD e menu.
- `Assets/Scripts/Runtime/UI/BattleArenaView.cs`: apresentacao da arena.
- `Assets/Resources/PokeIdle/Demo/Creatures`: dados editaveis dos Pokemon placeholder.
- `Assets/Resources/PokeIdle/Demo/Moves`: dados editaveis dos golpes placeholder.

Para subir de nivel, abra o `MENU` durante o Play Mode. O botao `UPAR` mostra o custo do proximo nivel, desconta Dindin e restaura o HP do Pokemon. As derrotas continuam gerando Dindin, mas o nivel nao sobe automaticamente.

## Party e PC Box

O save v7 guarda a Party e a PC Box. O primeiro slot da Party inicia liberado; os demais, ate o limite configurado de 3, sao comprados com Dindin. `Party Slot Base Cost` e `Party Slot Cost Growth` controlam os precos. A Box comeca com 30 espacos e pode ser expandida em blocos de 10 por Dindin; esses valores ficam na secao `PC Box` do `GameBalanceConfig`.

Durante o combate, escolher outro membro agenda a troca para o fim do round. Se o lider desmaiar, o primeiro membro saudavel assume automaticamente; a derrota so acontece quando todos os membros estiverem desmaiados. Ao concluir uma fase, toda a Party e curada. Arraste um membro pelo cabecalho do slot ate outro slot para reorganizar ou ate o slot do lider para solicitar a troca.

A aba `POKEMON` mostra os slots da Party, custos de desbloqueio, capacidade da Box e criaturas armazenadas. A captura ainda sera conectada a `TryAddCreatureToCollection` na proxima milestone.

Para testar a transferencia antes da captura existir, selecione `PokeIdleApp` na Hierarchy e use o menu de contexto `Add Test Squirtle To PC Box` no componente `GameLoopManager`. Esse atalho existe apenas no Editor.

Depois de criar novas criaturas ou golpes, use `PokeIdle > Create Demo Content` apenas se quiser regenerar o conteudo de demonstracao. Esse comando atualiza os assets placeholder existentes.

## Comprar golpes e evoluir

Abra `MENU > GOLPES`. Cada cartao mostra o nivel necessario, o preco e o golpe anterior exigido. Comprar aprende o golpe, mas nao o equipa automaticamente. Selecione um dos quatro slots no topo e clique em `EQUIPAR` no golpe aprendido. Substituir e gratuito e preserva o golpe antigo para futuras trocas. A IA usa somente os quatro equipados e compara dano esperado, efetividade, ataque/defesa fisicos ou especiais, bonus do proprio tipo e precisao. Os golpes desta etapa causam dano; efeitos secundarios e PP ainda nao sao simulados.

| Golpe | Nivel | Moedas | Requisito |
| --- | ---: | ---: | --- |
| Garra de Metal | 4 | 70 | Golpe Rapido |
| Talho | 7 | 110 | Golpe Rapido |
| Quebra-Telha | 10 | 160 | Garra de Metal |
| Lanca-Chamas | 12 | 250 | Brasa |
| Sopro do Dragao | 14 | 280 | Talho |
| Soco Trovao | 18 | 400 | Quebra-Telha |

As arvores estao em `Skill Tree` dos assets `Ember` (Charmander) e `Charmeleon`, em `Assets/Resources/PokeIdle/Demo/Creatures`. Edite `Move`, `Required Level`, `Cost` e `Prerequisite` no Inspector. `Learnset` define apenas os golpes iniciais/inimigos; golpes pagos ficam em `Skill Tree`.

Em `MENU > POKEMON`, Charmander pode evoluir para Charmeleon a partir do nivel 16 por 400 moedas. `Evolution Target`, `Evolution Level` e `Evolution Cost` no asset controlam isso. A evolucao melhora atributos, cura e preserva nivel, identidade, compras e os slots. Charmeleon possui sprites de frente e costas em `Assets/Art/Placeholder/Pokemon`.

Squirtle entra nos encontros do mundo 2, com peso 4; com os outros pesos atuais, tem 25% de chance por encontro normal. A disponibilidade por mundo esta em `DemoContent.cs`. O peso e editavel no asset `Squirtle`.

Saves v5 mantem moedas, nivel, itens, fase e retorno para a fase perdida. Recebem os dois golpes iniciais. Saves v6 guardam compras, slots, forma evoluida e dificuldade por fase. Saves v7 guardam Dindin, Party, PC Box, slots desbloqueados e capacidade da Box. O campo `Gold` antigo continua sendo lido como fallback para migracao. O arquivo agora e escrito primeiro como `pokeidle_save.json.tmp` e substituido atomicamente; se a Unity for encerrada durante a troca, o carregamento tenta recuperar o arquivo temporario completo.

Para conferir a regressao sem alterar seu save, pare o Play Mode e use `PokeIdle > Validate Progression`. Os testes usam instancias separadas em memoria e aparecem no Console.
