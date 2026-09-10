# Guia rapido para editar o prototipo

## Onde mudar o balanceamento

Abra `Assets/Resources/PokeIdle/Config/GameBalanceConfig.asset` na janela Project da Unity. Os valores aparecem no Inspector e podem ser alterados sem editar o codigo.

- `Starting Creature Level`: nivel inicial do Charmander.
- `Tick Interval Seconds`: intervalo entre as acoes automaticas.
- `Phases Per World`: quantidade de fases numeradas de 1 a 10 em cada mundo.
- `Enemies Per Phase`: quantidade de inimigos em cada fase; o ultimo e o boss.
- `Base Experience...`: curva e recompensa de XP.
- `Base Gold...`: recompensa de ouro.
- `Boss Level Bonus` e `Boss Health Multiplier`: dificuldade dos bosses.

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
- `Assets/Scripts/Runtime/GameLoopManager.cs`: fluxo de fases, combate, XP, drops e save.
- `Assets/Scripts/Runtime/CreatureInstance.cs`: nivel, XP, HP e cura ao subir de nivel.
- `Assets/Scripts/Runtime/GameLoopManager.cs`: tambem cura o Pokemon ao concluir uma fase.
- `Assets/Scripts/Runtime/ProgressionRules.cs`: ponte entre a configuracao e as regras de progressao.
- `Assets/Scripts/Runtime/UI/MainWindowUI.cs`: HUD e menu.
- `Assets/Scripts/Runtime/UI/BattleArenaView.cs`: apresentacao da arena.
- `Assets/Resources/PokeIdle/Demo/Creatures`: dados editaveis dos Pokemon placeholder.
- `Assets/Resources/PokeIdle/Demo/Moves`: dados editaveis dos golpes placeholder.

Depois de criar novas criaturas ou golpes, use `PokeIdle > Create Demo Content` apenas se quiser regenerar o conteudo de demonstracao. Esse comando atualiza os assets placeholder existentes.
