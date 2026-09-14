# Guia rapido para editar o prototipo

## Onde mudar o balanceamento

Abra `Assets/Resources/PokeIdle/Config/GameBalanceConfig.asset` na janela Project da Unity. Os valores aparecem no Inspector e podem ser alterados sem editar o codigo.

- `Starting Creature Level`: nivel inicial do Charmander.
- `Tick Interval Seconds`: intervalo entre as acoes automaticas.
- `Phases Per World`: quantidade de fases numeradas de 1 a 10 em cada mundo.
- `Enemies Per Phase`: quantidade de inimigos em cada fase; o ultimo e o boss.
- `Base Gold...`: recompensa de ouro.
- `Base Level Up Cost`: custo em moedas para subir do nivel atual para o proximo.
- `Level Up Cost Growth`: aumento do custo a cada novo nivel.
- `Boss Level Bonus` e `Boss Health Multiplier`: dificuldade dos bosses.
- `Enemy Levels Per Phase`: crescimento base por fase (1.25 por padrao).
- `Enemy Player Level Ratio`: piso relativo ao nivel do jogador na primeira visita (0.9 = 90%).

A dificuldade e fixada ao entrar pela primeira vez em uma fase e salva. Upar dentro dela ou voltar para farmar nao aumenta os inimigos daquela fase. O calculo inicial e o maior entre `1 + floor(((mundo - 1) * Phases Per World + fase) * Enemy Levels Per Phase)` e `floor(nivel do jogador * Enemy Player Level Ratio)`. O boss recebe mais 2 niveis. Por exemplo: chegando a `2-1` no nivel 17, os inimigos ficam no nivel 15 e o boss no 17. Alteracoes na curva afetam fases ainda nao visitadas; use o reset apenas se quiser testar uma nova run completa.

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

Para subir de nivel, abra o `MENU` durante o Play Mode. O botao `UPAR` mostra o custo do proximo nivel, desconta as moedas e restaura o HP do Pokemon. As derrotas continuam gerando ouro, mas o nivel nao sobe automaticamente.

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

Saves v5 mantem moedas, nivel, itens, fase e retorno para a fase perdida. Recebem os dois golpes iniciais. Saves v6 tambem guardam compras, slots, forma evoluida e dificuldade por fase.

Para conferir a regressao sem alterar seu save, pare o Play Mode e use `PokeIdle > Validate Progression`. Os testes usam instancias separadas em memoria e aparecem no Console.
