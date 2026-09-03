# PokeIdle

## Back-end de progressao e economia

O back-end agora possui:

- curva de XP centralizada em `ProgressionRules`, mais lenta para evitar niveis subindo em poucos minutos;
- inventario persistente com stacks de itens e validacao de dados;
- catalogo inicial de `Pocao` e `Super Pocao`, com preco e efeito de cura;
- tabela de drops com chance por derrota e bonus garantido ao concluir uma rota;
- metodos `TryUseItem` e `TryBuyItem` no `GameLoopManager`, prontos para a futura UI;
- estado de batalha explicito (`Searching`, `Battling` e `Recovering`);
- save local atualizado para a versao 2, incluindo o inventario.

Para testar do zero, selecione o objeto `PokeIdleApp`, abra o menu de contexto do componente `GameLoopManager` e use `Reset Progress (Testing)`. Isso substitui o save local atual.

Protótipo de um RPG idle para Windows inspirado na ideia de um jogo na barra de tarefas, usando criaturas placeholder até a criação da identidade visual original.

## Estado atual

O primeiro vertical slice contém:

- loop de combate automático com um tick por segundo;
- criatura do jogador e inimigo definidos por `ScriptableObject`;
- atributos básicos, golpes físicos/especiais e tabela de efetividade dos 18 tipos;
- ganho de XP, níveis, ouro e progresso de rota;
- salvamento local em JSON;
- faixa compacta com o Pokemon ativo centralizado e HUD essencial no canto direito;
- cenário procedural deslizando, inimigos se aproximando em linha reta e hordas visuais;
- barras de HP sobre o Pokemon do jogador e o inimigo em combate;
- menu expandido funcional com status, XP, party inicial, inventário, compra/uso de itens e salvar;
- integração Windows isolada para iniciar compacta, aceitar arraste e expandir o menu para cima.

## Encontros de teste

- `Caterpie` aparece desde o estágio 1;
- `Squirtle` aparece a partir do estágio 5 e é mais resistente;
- o golpe `Brasa` do Charmander causa dano reduzido contra o tipo Água.

## Criar a cena demo

No Unity, use o menu `PokeIdle > Create Demo Scene`. Isso cria os assets em `Assets/Resources/PokeIdle/Demo` e a cena `Assets/Scenes/Main.unity`.

Depois, abra `Main.unity` e pressione Play. O build Windows aplicará a integração com a taskbar; no Editor ela permanece desativada para facilitar o desenvolvimento.

## Editar sem mexer no código

Na Hierarchy, o objeto `PokeIdleApp` contém os componentes principais e os filhos visuais da arena. Se a cena antiga ainda mostrar apenas o objeto raiz, execute novamente `PokeIdle > Create Demo Scene`.

Para mudar o ritmo do jogo, selecione `PokeIdleApp` e edite `Tick Interval Seconds` e `Enemies Per Route` no componente `GameLoopManager`. Para mudar a organização visual, edite os campos do componente `BattleArenaView`.

Para trocar os placeholders, importe os PNGs em `Assets/Art/Placeholder` e arraste-os para `Sprite Front` e `Sprite Back` dos assets de criatura em `Assets/Resources/PokeIdle/Demo/Creatures`. Há um guia rápido em `Assets/Art/Placeholder/README.md`.

O direcionamento da faixa compacta e do menu expandido está documentado em `docs/design_direction.md`.

## Próximas fases

1. Validar o vertical slice em um build Windows compacto.
2. Evoluir a party para múltiplos Pokemon selecionáveis e troca de líder.
3. Adicionar ovos, incubadora, bosses e captura automática/manual.
4. Criar PC Box, Pokédex e rotas com dificuldade/boss.
5. Substituir placeholders por criaturas e arte originais.
6. Avaliar PvP somente depois de o modo idle estar estável.
