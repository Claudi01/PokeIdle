# Direção visual e de interação

## Faixa de ação

O modo idle deve ser uma faixa muito pequena, posicionável em qualquer canto da tela:

- o jogador pode mover a janela para os cantos e laterais;
- o Pokémon ativo permanece visualmente no centro da faixa;
- a câmera acompanha o centro do jogador, sem deslocá-lo pela tela;
- o cenário se desloca horizontalmente para criar a sensação de corrida;
- um unico inimigo entra em linha reta pela direita;
- quando um inimigo chega perto, a corrida dá lugar ao combate;
- barras de HP ficam acima do jogador e de cada inimigo;
- a leitura principal é formada pelo cenário, criaturas, HP e feedback curto de combate.

## HUD compacta

O canto direito deve conter apenas informações essenciais:

- mundo, fase e progresso dos encontros;
- ouro e quantidade de inimigos derrotados;
- botão pequeno para abrir o menu;
- botão de pausa, quando necessário.

A HUD não deve cobrir a ação nem transformar a faixa em uma tela tradicional de RPG.

## Menu expandido

Ao abrir o menu, a janela cresce para cima da faixa sem interromper o progresso. O menu terá, por etapas:

1. Party: líder, ordem dos Pokémon e troca do time ativo;
2. Pokémon: nível comprado com moedas, HP, atributos, tipos, golpes e status;
3. Inventário: ouro, itens dropados, ovos e incubadora;
4. Rotas: região, estágio atual, dificuldade e boss;
5. Pokédex/PC: criaturas descobertas, capturadas e armazenadas.

## Encontros e estágios

Os encontros são dados de back-end com um estágio mínimo. O protótipo atual usa:

- Caterpie: disponível desde o estágio 1;
- Metapod: evolução de Caterpie e BOSS das fases em que Caterpie é dominante;
- Weedle, Pidgey e Rattata: variedade adicional das primeiras rotas;
- Squirtle: disponível a partir do mundo 2, com peso 4 (25% dos encontros normais iniciais desse mundo), perfil resistente e tipo Água;
- Charmander: Pokémon ativo inicial, cujo golpe de Fogo é pouco efetivo contra Squirtle.

Cada mundo é dividido em fases identificadas por `mundo-fase`: o mundo 1 começa em `1-0`, segue por `1-1` até `1-10` e então o mundo seguinte começa em `2-1`. Cada fase possui vários encontros e o último inimigo é sempre um BOSS. Ao derrotá-lo, a próxima fase é desbloqueada. Se o Pokémon ativo for derrotado, a tentativa termina e o jogador recua uma fase: de `1-4` para `1-3`, por exemplo. A fase perdida fica disponível em um botão para ser retomada diretamente. Ouro e itens obtidos antes da derrota permanecem salvos, e as moedas são usadas para comprar níveis entre tentativas.

Ao concluir uma fase, o Pokémon ativo é curado completamente antes de iniciar a próxima.

No balanceamento atual de teste, o Pokémon do jogador começa no nível 1. A dificuldade cresce por fase e considera 90% do nivel do jogador na primeira visita, usando o maior resultado. Essa dificuldade fica salva por fase para permitir farm e ganho de poder nas tentativas seguintes. O BOSS recebe dois níveis adicionais e mais HP.

As moedas podem comprar niveis, golpes e a evolucao para Charmeleon (nivel 16, custo inicial 400). A arvore de cada Pokemon possui requisitos de nivel, custo e golpes anteriores. Cada instancia guarda suas compras e equipa ate quatro golpes. A IA seleciona, dentre os equipados, o maior dano esperado contra o alvo atual, incluindo efetividade de tipos, atributos, poder, precisao e bonus de tipo. Trocar equipamentos nao custa moedas. A evolucao preserva o nivel e a arvore aprendida.

O menu atual tem abas `POKEMON`, `GOLPES` e `ITENS`, com rolagem para acessar todo o conteudo na janela compacta.

Os encontros normais usam peso de aparição. Caterpie tem o maior peso na primeira rota; por isso, quando a fase chega ao último encontro, o BOSS escolhido é Metapod, sua evolução, com dois níveis acima dos inimigos normais.

A lista deve crescer por dados, sem alterar o loop de batalha a cada novo Pokémon.

## Separação técnica

- `GameLoopManager`: estado, mundo/fase, combate e progressão do jogo;
- `ProgressionRules`: custo crescente de nivel por moedas e recompensas de ouro;
- `InventorySystem`: catálogo de itens, inventário e tabela de drops;
- `SaveService`: persistência do progresso e do inventário;
- `BattleArenaView`: apresentação compacta da faixa e da câmera;
- `MainWindowUI`: HUD mínima e menu de abertura;
- `MainWindowUI`: HUD mínima e primeira versão funcional do menu expandido de party, inventário e status;
- `CreatureDefinition`: dados editáveis de cada espécie no Inspector;
- `WildCreatureEncounter`: disponibilidade de cada espécie por estágio.

Durante o protótipo, os nomes, sprites e dados de Pokémon são placeholders. Eles devem ser substituídos por conteúdo original antes de qualquer distribuição pública.
