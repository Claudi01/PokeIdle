# Direção visual e de interação

## Faixa de ação

O modo idle deve ocupar uma faixa muito pequena junto à taskbar:

- o Pokémon ativo fica no centro da ação;
- o inimigo em combate fica próximo, com investidas curtas;
- pequenos grupos entram pelas duas laterais para comunicar a próxima onda;
- a HUD mostra apenas rota, HP, ouro, derrotados e o último evento;
- nenhum painel grande deve competir com a animação da batalha.

## Menu expandido

Ao abrir o menu, a janela cresce para cima da taskbar sem interromper o progresso. O menu terá, por etapas:

1. Party: líder, ordem dos Pokémon e troca do time ativo;
2. Pokémon: nível, XP, HP, atributos, tipos, golpes e status;
3. Inventário: ouro, itens dropados, ovos e incubadora;
4. Rotas: região, rota atual, dificuldade e boss;
5. Pokédex/PC: criaturas descobertas, capturadas e armazenadas.

## Separação técnica

- `GameLoopManager`: estado e progressão do jogo;
- `BattleArenaView`: apresentação da batalha compacta;
- `MainWindowUI`: HUD fixa da taskbar;
- `ExpandedMenuUI` (próxima etapa): painéis de party, inventário e status;
- `CreatureDefinition`: dados editáveis de cada espécie no Inspector.

Durante o protótipo, as espécies de teste usam os nomes `Charmander` e `Caterpie`. Esses nomes, sprites e dados devem ser substituídos por conteúdo original antes de qualquer distribuição pública.
