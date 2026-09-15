## Context

Ver `proposal.md` — Why para a motivacao, e `specs/competence/**` para o
comportamento exigido.

Estado atual relevante para o desenho:

- `Transaction.Competence => YearMonth.From(OccurredOn)` — a competencia e
  derivada e nao existe coluna para ela. Nenhum backfill sera necessario.
- Todas as leituras filtram `t.OccurredOn >= month.FirstDay && <= month.LastDay`
  (`LedgerQueries`, `TransactionRepository.ListByMonthAsync`).
- `MonthParameter.Resolve(month, clock)` cai em `YearMonth.From(clock.Today)`
  quando o parametro nao vem. Ele vive em `Infrastructure.Shared`, projeto
  referenciado por **todos** os modulos.
- `FixedExpense.OccurrenceDate(month)` posiciona o fixo no dia de vencimento do
  proprio mes do calendario; a chave de idempotencia e `recurrence:{id}:{yyyy-MM}`.
- `MonthService` (Angular) calcula a competencia inicial com `new Date()` e e
  consumido por shell, dashboard, transactions e fixed-expenses.
- Regra de fronteira do monolito: um modulo so referencia o `*.Contracts` de
  outro, e `ModuleBoundaryTests` quebra o build se isso for violado.

## Goals / Non-Goals

**Goals:**

- Uma unica funcao pura, densamente testada, como fonte da verdade da competencia
  — no mesmo espirito de `MonthlyBudget.Calculate`.
- Nenhuma mudanca de schema em `ledger.Transactions` e nenhum backfill.
- Comportamento identico ao de hoje para quem nao configurar dia de virada.
- Nenhuma nova dependencia entre modulos alem das que ja existem.

**Non-Goals:**

- Cache da regra entre requisicoes (medir antes; ver Risks).
- Regra por conta/cartao, ou mais de uma regra por usuario.
- Mover um lancamento individual de competencia pela UI.
- Bloquear escrita em uma competencia encerrada (encerrar organiza, nao trava).

## Decisions

### A regra mora no modulo Ledger

O Ledger ja e o dono do conceito de competencia (`Transaction.Competence`) e e o
unico modulo do qual **todos** os outros ja dependem via `.Contracts`. Colocar a
regra la nao cria nenhuma aresta nova no grafo de dependencias.

Alternativas descartadas:

- **Budgeting** — e onde ficam as outras configuracoes do usuario e onde esta a
  tela de settings. Mas Budgeting ja depende de Ledger; Ledger passar a depender
  de Budgeting criaria acoplamento bidirecional entre os dois modulos mais
  centrais.
- **Modulo `Competence` novo** — mais puro conceitualmente, mas custa um modulo,
  um schema, migrations proprias, registro em `Program.cs` e nas listas dos testes
  de arquitetura, para um agregado minusculo que so o Ledger consulta de dentro.
  Se a regra crescer (por cartao, por conta), extrair depois e barato: os
  consumidores ja falam com ela por contrato.

### Competencia continua derivada; a regra e que e persistida

Decisao do usuario: mudar a regra deve reclassificar o historico. Isso so e
coerente se a competencia nunca for gravada no lancamento. Consequencia:
`Transaction.Competence` sai da entidade — a entidade nao tem como conhecer a
regra do usuario, e uma propriedade que mente e pior que uma propriedade ausente.

Alternativa descartada: gravar `Competence` como coluna no registro. Daria
consultas mais simples (`WHERE Competence = @c`, com indice direto) e um
historico imutavel, mas contraria a decisao de reclassificacao e exigiria
backfill.

### Modelo de dados: duas tabelas no schema `ledger`

```
ledger.CompetenceSettings   UserId (PK), ClosingDay int NULL, UpdatedAt
ledger.CompetenceClosures   Id (PK), UserId, Year, Month, ClosedOn, CreatedAt
                            UNIQUE (UserId, Year, Month)
```

`Year`/`Month` como colunas inteiras em vez de uma string `yyyy-MM`: ordenar e
comparar competencias em SQL fica trivial, e e como `YearMonth` ja e modelado.

Ausencia de linha em `CompetenceSettings` significa "sem dia de virada" — nao ha
seed nem migracao de dados, e usuarios existentes seguem com o comportamento de
hoje.

### Uma funcao pura sobre um snapshot, nao um servico chatty

O resolvedor e um tipo puro do dominio do Ledger:

```csharp
public sealed record CompetenceCalendar(int? ClosingDay, IReadOnlyDictionary<YearMonth, DateOnly> Closures)
{
    public YearMonth Resolve(DateOnly date);
    public CompetenceWindow WindowOf(YearMonth competence);   // (Start, End) inclusivo
    public YearMonth Current(DateOnly today) => Resolve(today);
}
```

Sem I/O, sem `DateTime.Now` — recebe `today` de fora, como o resto do dominio faz
com `IClock`. Um `ICompetenceCalendarProvider` interno carrega o snapshot
(settings + closures do usuario) uma vez por requisicao e devolve o record.

Alternativa descartada: metodos assincronos granulares no contrato
(`ResolveCompetenceAsync(userId, date)`), que fariam Recurrences bater no banco
uma vez por gasto fixo por mes. Com o snapshot, e uma leitura por operacao.

O algoritmo de `Resolve` e `WindowOf` esta descrito em
`specs/competence/cutoff-rule` e `specs/competence/scoping`; o ponto de desenho e
que os dois MUST concordar — a janela de uma competencia contem exatamente as
datas que `Resolve` classifica nela. Isso vira um teste de propriedade sobre
combinacoes de dia de virada e encerramentos.

### `ClosingDay` entre 2 e 31, nulo desliga

Com virada no dia 1 a condicao "dia >= virada" e sempre verdadeira e todo
lancamento cairia na competencia seguinte, para sempre — nao ha ponto fixo. Por
isso 1 e rejeitado, e "sem virada" e representado por `null`, nao por 1.

Dias 29-31 sao aceitos e ajustados ao ultimo dia do mes curto, exatamente como
`FixedExpense.DayOfMonth` ja faz — a mesma regra em dois lugares e um so conceito
para o usuario.

### Encerrar o mes vale a partir da data, nao a partir do registro

Literalmente, "todos os novos gastos entram na proxima competencia" incluiria um
lancamento retroativo digitado depois do encerramento. Isso e incompativel com a
competencia derivada: nada distingue um lancamento datado de 10/08 registrado
antes ou depois do encerramento, a nao ser gravando a competencia — o que a
decisao anterior exclui.

Por isso o encerramento e uma data de corte: vale para datas a partir dela. Na
pratica o efeito e o esperado, porque o formulario de lancamento ja vem com a
data de hoje. Um lancamento deliberadamente retroativo cair na competencia
encerrada e o comportamento desejavel — e a correcao de um gasto esquecido.

### Contrato do Ledger e comunicacao entre modulos

`ILedgerModuleApi` ganha:

```csharp
Task<CompetenceCalendarDto> GetCompetenceCalendarAsync(Guid userId, ...);
Task<YearMonth> GetCurrentCompetenceAsync(Guid userId, ...);
Task<Result> SyncRecurrenceOccurrenceAsync(RegisterTransactionRequest request, ...);
```

`CompetenceCalendarDto` carrega `ClosingDay`, os encerramentos e a janela pedida —
os consumidores (Budgeting, Recurrences) nao reimplementam a regra.

A configuracao e o encerramento sao expostos por endpoints proprios do Ledger em
`/api/ledger/competence` (GET, PUT do dia de virada, POST de encerramento, DELETE
de reabertura), fora de `/api/budget` — a regra nao e uma configuracao de
orcamento, ainda que a tela de settings mostre as duas juntas.

Quando a regra muda, o Ledger publica `CompetenceRuleChangedIntegrationEvent` no
`IEventBus`. Recurrences assina e rematerializa a competencia corrente e a
anterior. E o caso de uso classico do barramento (notificar sem acoplar) e evita
Ledger passar a depender de `Recurrences.Contracts`.

### Materializacao de fixo vira upsert por (gasto fixo, competencia)

A chave `recurrence:{id}:{yyyy-MM}` continua sendo a identidade, mas o registro
deixa de falhar com `duplicated_external_id` quando a data mudou: um metodo de
contrato separado (`SyncRecurrenceOccurrenceAsync`) cria a ocorrencia se ela nao
existe e reposiciona a data se ela saiu da janela. O `RegisterAsync` usado por
lancamento manual e por importacao bancaria mantem a rejeicao estrita de
duplicado.

Alternativa descartada: incluir a data na chave (`recurrence:{id}:{yyyy-MM-dd}`).
Uma mudanca de regra passaria a gerar uma ocorrencia nova e deixar a antiga para
tras, duplicando o fixo entre duas competencias.

### `MonthParameter` deixa de saber o default

`Infrastructure.Shared` e referenciado por todos os modulos; se `MonthParameter`
precisasse de `ILedgerModuleApi`, todo modulo passaria a depender de
`Ledger.Contracts` por transitividade. Entao ele vira so o parser
(`TryParse(string?) -> YearMonth?`), e cada endpoint resolve o default chamando a
competencia corrente — Ledger pelo servico interno, Budgeting e Recurrences pelo
contrato do Ledger, que eles ja consomem.

### Consulta continua sendo por intervalo de datas

A janela e resolvida em memoria e vira o mesmo `WHERE OccurredOn BETWEEN @start
AND @end` de hoje. Sem coluna nova, sem indice novo, sem mudanca no plano de
execucao — muda so o par de datas.

### Front-end: `MonthService` sai de `shared/` para `core/`

`MonthService` passa a fazer I/O (buscar a competencia corrente) e ja e um
singleton de app inteiro — e a definicao de `core/`, enquanto `shared/` e
"reutilizavel e stateless". Ele vai para `core/competence/`, junto de um
`competence.api.ts`, e os 6 pontos de import sao atualizados. `CLAUDE.md` e
`docs/architecture.md` descrevem hoje a permanencia dele em `shared/` como uma
excecao deliberada; os dois textos precisam ser corrigidos junto.

A competencia corrente e carregada uma vez no bootstrap
(`provideAppInitializer`), para que nenhuma tela abra com o mes errado e depois
pule. Se a chamada falhar, o servico cai no mes do calendario e a aplicacao segue
— o app ja e desenhado para subir sem backend.

## Risks / Trade-offs

- **Reclassificacao retroativa surpreende o usuario** (meses fechados mudam de
  total ao ajustar a regra) → aviso explicito na tela de configuracoes antes de
  salvar, e reabertura disponivel para desfazer um encerramento.
- **Uma leitura a mais por requisicao** (settings + encerramentos do usuario) →
  sao poucas linhas por usuario e a consulta e por chave primaria; medir antes de
  cachear. Um cache por usuario e a saida natural se aparecer.
- **`CompetenceClosures` cresce indefinidamente** (uma linha por competencia
  encerrada) → 12 linhas por usuario por ano; irrelevante. Recortar a leitura por
  faixa de competencia seria a otimizacao obvia, mas e uma armadilha: `Resolve`
  encadeia encerramentos para a frente sem limite, entao um recorte faria o
  snapshot classificar datas diferente do resto do app, em silencio. O provider le
  todos os encerramentos do usuario, pelo indice.
- **Janela vazia e um estado alcancavel** (duas competencias encerradas no mesmo
  dia) → especificado e testado; consultas devolvem vazio, nao erro.
- **Divergencia entre `Resolve` e `WindowOf`** e a falha mais provavel e mais
  dificil de perceber (lancamentos somem de todas as competencias) → teste de
  propriedade que varre datas ao longo de varios meses e exige que cada data
  esteja na janela da competencia que `Resolve` devolve.
- **Ordem de execucao na rematerializacao**: o evento de mudanca de regra dispara
  rematerializacao sincrona no `InMemoryEventBus`; uma falha ali nao tem retry
  (ADR 0004) → o worker diario reconcilia, e a operacao e idempotente.
- **`Transaction.Competence` some** — e `internal`, entao nao ha consumidor
  externo; so `TransactionTests` referencia.

## Migration Plan

1. Migration aditiva no schema `ledger` criando as duas tabelas. Nenhuma alteracao
   em `Transactions`, nenhum backfill.
2. Deploy do backend: sem linha em `CompetenceSettings`, `ClosingDay` e nulo e
   toda a resolucao devolve o mes do calendario — comportamento identico ao atual.
3. Deploy do front-end. Sem o backend novo, a chamada da competencia corrente
   falha e o `MonthService` cai no mes do calendario.
4. Rollback: reverter o codigo. As tabelas podem ficar — nada mais as le, e
   preserva-las mantem a configuracao do usuario caso o deploy seja refeito.

## Open Questions

- Qual o texto exato do aviso de reclassificacao na tela de configuracoes e a
  confirmacao do encerramento. Nao afeta specs, desenho nem tarefas.
- Se o painel deve mostrar a janela de datas da competencia ("25/08 a 24/09")
  junto do rotulo do mes. Bom para deixar a regra visivel, mas pode ser decidido
  ao implementar a tela.
