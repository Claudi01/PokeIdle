# Direção visual e de interação

## Faixa de ação

O modo idle deve ser uma faixa muito pequena, posicionável em qualquer canto da tela:

- o jogador pode mover a janela para os cantos e laterais;
- o Pokémon ativo permanece visualmente no centro da faixa;
- a câmera acompanha o centro do jogador, sem deslocá-lo pela tela;
- o cenário se desloca horizontalmente para criar a sensação de corrida;
- inimigos entram em linha reta pelas laterais e podem formar grupos;
- quando um inimigo chega perto, a corrida dá lugar ao combate;
- barras de HP ficam acima do jogador e de cada inimigo;
- a leitura principal é formada pelo cenário, criaturas, HP e feedback curto de combate.

## HUD compacta

O canto direito deve conter apenas informações essenciais:

- estágio e progresso da rota;
- ouro e quantidade de inimigos derrotados;
- botão pequeno para abrir o menu;
- botão de pausa, quando necessário.

A HUD não deve cobrir a ação nem transformar a faixa em uma tela tradicional de RPG.

## Menu expandido

Ao abrir o menu, a janela cresce para cima da faixa sem interromper o progresso. O menu terá, por etapas:

1. Party: líder, ordem dos Pokémon e troca do time ativo;
2. Pokémon: nível, XP, HP, atributos, tipos, golpes e status;
3. Inventário: ouro, itens dropados, ovos e incubadora;
4. Rotas: região, estágio atual, dificuldade e boss;
5. Pokédex/PC: criaturas descobertas, capturadas e armazenadas.

## Encontros e estágios

Os encontros são dados de back-end com um estágio mínimo. O protótipo atual usa:

- Caterpie: disponível desde o estágio 1;
- Squirtle: disponível a partir do estágio 5, com perfil mais resistente e tipo Água;
- Charmander: Pokémon ativo inicial, cujo golpe de Fogo é pouco efetivo contra Squirtle.

A lista deve crescer por dados, sem alterar o loop de batalha a cada novo Pokémon.

## Separação técnica

- `GameLoopManager`: estado, estágio, combate e progressão do jogo;
- `ProgressionRules`: curva de XP e recompensas de ouro;
- `InventorySystem`: catálogo de itens, inventário e tabela de drops;
- `SaveService`: persistência do progresso e do inventário;
- `BattleArenaView`: apresentação compacta da faixa e da câmera;
- `MainWindowUI`: HUD mínima e menu de abertura;
- `MainWindowUI`: HUD mínima e primeira versão funcional do menu expandido de party, inventário e status;
- `CreatureDefinition`: dados editáveis de cada espécie no Inspector;
- `WildCreatureEncounter`: disponibilidade de cada espécie por estágio.

Durante o protótipo, os nomes, sprites e dados de Pokémon são placeholders. Eles devem ser substituídos por conteúdo original antes de qualquer distribuição pública.
