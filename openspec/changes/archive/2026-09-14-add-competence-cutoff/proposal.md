## Why

Hoje a competencia de um lancamento e o mes do calendario da data em que ele
ocorreu: `Transaction.Competence => YearMonth.From(OccurredOn)`, e toda consulta
filtra `OccurredOn` entre o primeiro e o ultimo dia do mes. Isso nao corresponde
ao ciclo financeiro real do usuario: uma compra de 28/ago no cartao entra na
fatura que vence no inicio de outubro, mas o app a contabiliza em agosto — e o
orcamento de agosto aparece estourado enquanto o de setembro parece vazio.

Alem disso, a competencia corrente vira sozinha na virada do calendario
(`MonthService` e `MonthParameter.Resolve` chamam "hoje"), entao no dia 1 o
painel troca de mes e da a impressao de ter zerado, mesmo com o ciclo anterior
ainda aberto.

## What Changes

- **Nova regra de virada por dia do mes** (`ClosingDay`, por usuario): lancamentos
  a partir do dia configurado entram na competencia seguinte. `null` mantem o
  comportamento atual (mes do calendario) e e o padrao para quem ja usa o app.
- **Encerramento manual do mes**: botao "Encerrar os gastos deste mes" registra o
  fechamento da competencia corrente na data de hoje; a partir dali os novos
  lancamentos entram na competencia seguinte, mesmo antes do dia de virada. O
  encerramento e reversivel (reabrir).
- **Competencia deixa de ser o mes do calendario e passa a ser derivada** de
  `(OccurredOn, ClosingDay, fechamentos)` por uma funcao pura. Como e derivada,
  ajustar a regra reclassifica tambem o historico — decisao explicita do usuario.
- **A competencia corrente passa a seguir a regra**, nao o calendario: com virada
  no dia 25, ela avanca em 25/ago e nada acontece na passagem de 31/ago para
  01/set — o ciclo em curso ja acumula uma semana de gastos, entao o painel nao
  aparece zerado. Isso remove a "renovacao automatica" percebida hoje.
  **BREAKING** para o contrato de leitura:
  `GET /api/ledger/transactions` (e `/summary`, `/budget`) sem o parametro `month`
  passam a devolver a competencia resolvida pela regra, nao o mes do calendario.
- **Janela de competencia substitui o intervalo primeiro-dia/ultimo-dia** em todas
  as consultas do Ledger, no calculo do orcamento e na materializacao de gastos
  fixos. A data de ocorrencia de um gasto fixo passa a ser escolhida dentro da
  janela da competencia alvo, para que ele caia na competencia para a qual foi
  materializado.
- Lancamentos importados do banco seguem a mesma regra, sem mudanca no
  `BankSyncService` — a competencia deles ja sai da data da transacao.
- **BREAKING** (interno): `Transaction.Competence` deixa de existir como
  propriedade da entidade; a entidade nao conhece a regra do usuario.

Fora de escopo: mover um lancamento individual de competencia na mao; regra de
virada por conta/cartao (hoje e uma so por usuario); virada por dia da semana.

## Capabilities

### New Capabilities

- `competence/cutoff-rule`: como um lancamento e atribuido a uma competencia —
  configuracao do dia de virada, encerramento e reabertura manual do mes, funcao
  de resolucao data -> competencia e qual e a competencia corrente.
- `competence/scoping`: como o resto do sistema passa a enxergar a competencia —
  janela de datas de uma competencia, listagens e totais do Ledger, orcamento
  mensal e materializacao de gastos fixos.

### Modified Capabilities

<!-- Nenhuma: openspec/specs/ ainda esta vazio, entao todas as capacidades
     tocadas por esta mudanca sao introduzidas aqui. -->

## Impact

**Backend**

- `Modules/Ledger` (dono da regra, pois e quem ja define competencia e e a unica
  dependencia comum a todos os outros modulos): nova entidade
  `CompetenceSettings` e `CompetenceClosure`, resolvedor puro `CompetenceRule`,
  novos endpoints em `/api/ledger/competence`, ampliacao de `ILedgerModuleApi`,
  migration no schema `ledger`.
- `Modules/Ledger/Application/Transactions/LedgerQueries` e `TransactionRepository`:
  filtram pela janela da competencia.
- `Modules/Ledger/Domain/Transaction`: remove `Competence`.
- `Modules/Recurrences`: `FixedExpense.OccurrenceDate` e `FixedExpenseService.MaterializeAsync`
  passam a posicionar o lancamento dentro da janela; o worker materializa a
  competencia corrente resolvida pela regra, nao `YearMonth.From(clock.Today)`.
- `Modules/Budgeting`: nenhuma mudanca de codigo — ja consome os totais via
  `ILedgerModuleApi`; muda so o resultado.
- `Modules/Banking`: nenhuma mudanca.
- `Shared/Infrastructure.Shared/Http/MonthParameter`: resolver o default pela
  regra do usuario em vez de `clock.Today`.
- Testes: nova suite de testes da funcao pura de resolucao (no espirito de
  `MonthlyBudgetTests`); `TransactionTests` perde a asserção sobre `Competence`.

**Frontend**

- `shared/month.service.ts`: a competencia inicial vem do backend; deixa de ser
  calculada com `new Date()`.
- Novo `shared/data-access/competence.api.ts` (services nao podem chamar
  `HttpClient` direto de componente).
- `features/settings`: campo do dia de virada, com previa do efeito.
- `features/dashboard`: botao "Encerrar os gastos deste mes" e reabrir, com
  indicacao de competencia encerrada.
- `core/layout/shell`: rotulo do mes indica quando a competencia esta encerrada.

**Dados**

- Duas tabelas novas no schema `ledger`. Nenhuma coluna nova em `Transactions` e
  nenhum backfill: com `ClosingDay = null` o resultado e identico ao de hoje.
