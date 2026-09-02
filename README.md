# PokeIdle

Protótipo de um RPG idle para Windows inspirado na ideia de um jogo na barra de tarefas, usando criaturas placeholder até a criação da identidade visual original.

## Estado atual

O primeiro vertical slice contém:

- loop de combate automático com um tick por segundo;
- criatura do jogador e inimigo definidos por `ScriptableObject`;
- atributos básicos, golpes físicos/especiais e tabela de efetividade dos 18 tipos;
- ganho de XP, níveis, ouro e progresso de rota;
- salvamento local em JSON;
- UI temporária de 48 px com HP, inimigo, recompensas, pausa e salvar;
- arena 2D com câmera, sombras, animação de investida e sprites substituíveis;
- hordas visuais chegando pelas laterais para reforçar a leitura de ondas;
- integração Windows isolada para posicionar o build sobre a taskbar.

## Criar a cena demo

No Unity, use o menu `PokeIdle > Create Demo Scene`. Isso cria os assets em `Assets/Resources/PokeIdle/Demo` e a cena `Assets/Scenes/Main.unity`.

Depois, abra `Main.unity` e pressione Play. O build Windows aplicará a integração com a taskbar; no Editor ela permanece desativada para facilitar o desenvolvimento.

## Editar sem mexer no código

Na Hierarchy, o objeto `PokeIdleApp` contém os componentes principais e os filhos visuais da arena. Se a cena antiga ainda mostrar apenas o objeto raiz, execute novamente `PokeIdle > Create Demo Scene`.

Para mudar o ritmo do jogo, selecione `PokeIdleApp` e edite `Tick Interval Seconds` e `Enemies Per Route` no componente `GameLoopManager`. Para mudar a organização visual, edite os campos do componente `BattleArenaView`.

Para trocar os placeholders, importe os PNGs em `Assets/Art/Placeholder` e arraste-os para `Sprite Front` e `Sprite Back` dos assets de criatura em `Assets/Resources/PokeIdle/Demo/Creatures`. Há um guia rápido em `Assets/Art/Placeholder/README.md`.

O direcionamento da faixa compacta e do menu expandido está documentado em `docs/design_direction.md`.

## Próximas fases

1. Validar o vertical slice e balancear o loop.
2. Adicionar ovos, incubadora e inventário.
3. Adicionar bosses e captura automática/manual.
4. Criar telas expandidas de time, PC Box e Pokédex.
5. Substituir placeholders por criaturas e arte originais.
6. Avaliar PvP somente depois de o modo idle estar estável.
