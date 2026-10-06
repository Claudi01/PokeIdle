# Revisão das milestones e interface — 6 de outubro de 2026

## Ordem de entrega

1. Revisar e validar a base de progressão/save (M0) e Party/Box (M1).
2. Validar o novo layout taskbar no executável Windows.
3. Só então implementar M2: captura automática/manual, bolas, novas espécies e chefes de território.

## Correções da base

- Reabrir o jogo numa fase antiga não remove níveis já comprados.
- Dindin zerado num save novo não é substituído pelo valor legado de Gold.
- Troca solicitada durante a aproximação também espera o fim do round.
- Trocas entre Box e slots ocupados são atômicas; o líder espera o fim do round.
- Uma mesma instância não pode ser duplicada entre Party/Box.
- Se o limite de Party for reduzido na configuração, membros excedentes vão para a Box.
- Save atômico mantém uma geração anterior em `.bak` e recupera `.tmp` completo.
- JSON sem criatura válida é rejeitado em vez de virar progresso vazio.
- O teste de dificuldade foi corrigido para respeitar o limite de nível do mundo.

## Layout

A faixa tem 720 × 160 pixels. A arena e o painel lateral de fase/Dindin/menu têm áreas próprias. O menu ocupa até 440 pixels acima da faixa; a câmera mantém o mesmo tamanho. No Editor, use uma Game View com aproximadamente 720 × 604 para visualizar o menu completo.

Party permanece à esquerda. Box, Pokémon, Golpes, Itens e Rota ficam em abas à direita. Clique num membro ou numa criatura da Box para editar aquele Pokémon. Arraste entre slots, da Box para a Party ou da Party para a área da Box. As ações de menu não pausam o combate. Pausa é um botão explícito.

No Windows, a janela inicia acima da área útil do monitor, sem borda. Arraste pela pequena alça pontilhada no alto da arena. Expandir/recolher preserva a base da janela, exceto quando é necessário limitá-la à área visível. A integração valida o processo proprietário da janela antes de modificá-la.

## Validação reproduzível

- `PokeIdle > Validate Milestones`: regressões de progressão, coleção, transferência, auto-swap, derrota coletiva, migração e recuperação do save.
- `PokeIdle > Build Windows Review`: valida e gera `Builds/Review/PokeIdle.exe`.
- Em Development Build, o argumento `-pokeidle-review` abre uma sessão em memória com Party/Box preenchidas e Dindin de teste. Não lê nem sobrescreve o save pessoal.

As validações usam objetos isolados e arquivos temporários exclusivos. Questionário e READMEs preexistentes permanecem com as alterações locais do usuário.

## Referências técnicas da correção da janela

- [Unity: Screen.SetResolution](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen.SetResolution.html): o tamanho é aplicado ao final do frame.
- [Microsoft: SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos): atualização do estilo da moldura e posicionamento topmost.
- [Microsoft: GetWindowThreadProcessId](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid): validação do processo proprietário.
