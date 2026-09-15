# ADR 0006 — Competência derivada, não gravada

**Status:** aceito · **Data:** 2026-09-02

## Contexto

Até aqui a competência de um lançamento era o mês do calendário da data em que ele
ocorreu (`YearMonth.From(OccurredOn)`), e toda consulta filtrava entre o primeiro e o
último dia do mês.

Isso não corresponde ao ciclo financeiro real do usuário. Uma compra de 28/ago no
cartão entra na fatura que vence no início de outubro, mas o app a contabilizava em
agosto: o orçamento de agosto aparecia estourado e o de setembro, vazio.

Passamos a ter uma regra de virada por usuário — um dia do mês a partir do qual o
gasto conta para a competência seguinte — mais um encerramento manual ("Encerrar os
gastos deste mês"). A pergunta de projeto: onde a competência de um lançamento passa
a morar?

## Decisão

A competência é **derivada** de `(data de ocorrência, dia de virada, encerramentos)`
por uma função pura (`CompetenceCalendar`), e **nunca gravada** no lançamento.
`Transaction.Competence` deixou de existir.

Consequência aceita de propósito: **alterar a regra reclassifica também o histórico**.
Um lançamento de 28/08 que estava em agosto passa a estar em setembro quando o usuário
configura a virada no dia 25, e os totais dos dois meses mudam junto.

A regra vive no módulo **Ledger** — é ele que já definia competência, e é o único
módulo do qual todos os outros já dependem, então não surgiu nenhuma aresta nova no
grafo de dependências.

Como a competência não é uma coluna, o recorte de uma consulta é a **janela** de datas
da competência (`WindowOf`), e o filtro em SQL continua sendo um intervalo sobre
`OccurredOn` — sem coluna nova, sem índice novo, sem backfill.

## Alternativas consideradas

- **Gravar `Competence` como coluna no registro.** Daria consultas mais simples
  (`WHERE Competence = @c`, índice direto) e um histórico imutável: nada muda de lugar
  depois de gravado. Foi recusada porque o usuário quer justamente o contrário — ao
  corrigir um dia de virada configurado errado, ele espera que os meses anteriores se
  ajustem, não que fiquem com a classificação antiga. Também exigiria backfill.
- **Módulo `Competence` próprio.** Mais puro conceitualmente, mas custa um módulo, um
  schema, migrations, registro no host e nos testes de arquitetura, para um agregado
  minúsculo que só o Ledger consulta por dentro. Se a regra crescer (por cartão, por
  conta), extrair depois é barato: os consumidores já falam por contrato.
- **Regra no Budgeting**, junto das outras configurações do usuário. Budgeting já
  depende de Ledger; o inverso criaria acoplamento bidirecional entre os dois módulos
  mais centrais.

## Consequências

- Uma leitura a mais por operação para montar o calendário do usuário (poucas linhas,
  por índice). Cachear é a saída natural se aparecer — medir antes.
- `Resolve` (classifica uma data) e `WindowOf` (delimita uma competência) são duas
  leituras da mesma regra e **precisam concordar**. Discordarem é a falha mais difícil
  de perceber — lançamentos sumiriam de todas as competências. Um teste de propriedade
  varre 24 meses, para várias combinações de virada e encerramentos, exigindo que as
  janelas particionem o tempo.
- Encerrar o mês vale **a partir da data**, não a partir do registro: um lançamento
  deliberadamente retroativo continua na competência encerrada. É a única semântica
  compatível com competência derivada, e na prática é o comportamento desejado, porque
  o formulário já vem com a data de hoje.
- A ocorrência de um gasto fixo pode sair da janela quando a regra muda. Por isso a
  materialização virou upsert por (gasto fixo, competência): ela **reposiciona** a data
  em vez de criar uma segunda ocorrência ou recusar como duplicata.
- Com `ClosingDay` nulo — o padrão, e o estado de quem já usa o app — o resultado é
  idêntico ao de antes: a competência é o mês do calendário.
