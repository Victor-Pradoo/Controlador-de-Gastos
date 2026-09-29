## 1. Dominio da competencia (Ledger)

- [x] 1.1 Criar `CompetenceWindow` (Start/End inclusivos) e `CompetenceCalendar` em `Modules/Ledger/Domain/Competence/`, com `Resolve(DateOnly)`, `WindowOf(YearMonth)` e `Current(DateOnly today)` — puros, sem I/O e sem `DateTime.Now`. Verificar que o tipo nao referencia EF nem ASP.NET.
- [x] 1.2 Criar `ControleDeGastos.Modules.Ledger.Tests/CompetenceCalendarTests.cs` cobrindo os cenarios de `specs/competence/cutoff-rule` — dia de virada 2..31, dia inexistente no mes, virada de ano, dia de virada nulo, encerramento, lancamento retroativo anterior ao encerramento, dois encerramentos no mesmo dia. Verificar com `dotnet test --filter FullyQualifiedName~CompetenceCalendarTests`.
- [x] 1.3 Adicionar teste de propriedade que varre cada dia de um intervalo de 24 meses, para varias combinacoes de dia de virada e encerramentos, e exige que a data esteja dentro de `WindowOf(Resolve(data))` e que janelas consecutivas sejam contiguas e nao se sobreponham. Verificar que o teste passa.
- [x] 1.4 Criar `CompetenceSettings` (agregado com `ClosingDay` nulo ou 2..31, rejeitando 0, 1 e >31 via `Result`) e `CompetenceClosure` (UserId, Year, Month, ClosedOn), com erros tipados em `LedgerErrors`. Verificar com testes de unidade das validacoes.
- [x] 1.5 Remover `Transaction.Competence` e ajustar a asserção correspondente em `TransactionTests`. Verificar que `dotnet build` e `dotnet test` seguem verdes.

## 2. Persistencia (schema `ledger`)

- [x] 2.1 Adicionar `DbSet`s e configurations de `CompetenceSettings` (PK `UserId`) e `CompetenceClosure` (indice unico `UserId, Year, Month`) em `LedgerDbContext`. Verificar que `dotnet build` passa.
- [x] 2.2 Criar `ICompetenceSettingsRepository` e `ICompetenceClosureRepository` (`internal`) e registra-los em `LedgerModule`. Verificar via testes de unidade dos repositorios ou pelo teste de integracao da tarefa 8.1.
- [x] 2.3 Criar `ICompetenceCalendarProvider` que monta um `CompetenceCalendar` a partir do usuario, com uma leitura por operacao. Verificar que o snapshot devolvido reflete settings e encerramentos gravados.
- [x] 2.4 Rodar `./scripts/add-migration.ps1 AddCompetenceRule` e conferir que a migration do Ledger cria apenas as duas tabelas novas, sem alterar `ledger.Transactions`. Verificar aplicando a migration em base local.

## 3. Contrato e endpoints do Ledger

- [x] 3.1 Adicionar ao `Ledger.Contracts` os DTOs `CompetenceCalendarDto`, `CompetenceWindowDto` e `CompetenceSettingsDto`, e o evento `CompetenceRuleChangedIntegrationEvent`. Verificar que `ModuleBoundaryTests` continua passando.
- [x] 3.2 Ampliar `ILedgerModuleApi` com `GetCompetenceCalendarAsync`, `GetCurrentCompetenceAsync` e `SyncRecurrenceOccurrenceAsync`, implementando em `LedgerModuleApi`. Verificar com `dotnet build`.
- [x] 3.3 Implementar os casos de uso de configuracao (`UpdateClosingDayHandler`), encerramento (`CloseCompetenceHandler`, so a competencia corrente, `ClosedOn` vindo de `IClock`, conflito se ja encerrada) e reabertura (`ReopenCompetenceHandler`, not found se nao houver encerramento). Verificar com testes de unidade dos tres handlers.
- [x] 3.4 Publicar `CompetenceRuleChangedIntegrationEvent` no `IEventBus` ao alterar o dia de virada, encerrar e reabrir. Verificar com teste que captura a publicacao.
- [x] 3.5 Mapear `/api/ledger/competence` em `LedgerEndpoints`: GET (dia de virada, competencia corrente e sua janela, encerramento vigente), PUT do dia de virada, POST de encerramento, DELETE de reabertura — sem escolher status code na mao, usando `ResultExtensions`. Verificar pela documentacao em `/scalar/v1` e pelos testes da tarefa 8.1.
- [x] 3.6 Fazer `SyncRecurrenceOccurrenceAsync` criar a ocorrencia quando ela nao existe e reposicionar `OccurredOn` quando a existente saiu da janela, mantendo `RegisterAsync` com rejeicao estrita de `duplicated_external_id`. Verificar com testes cobrindo criar, reposicionar e nao-alterar.

## 4. Consultas do Ledger pela janela

- [x] 4.1 Substituir o filtro `FirstDay/LastDay` pela janela da competencia em `LedgerQueries.GetByMonthAsync`, `GetMonthlyTotalsAsync` e `GetCategoryTotalsAsync`. Verificar que a consulta gerada continua sendo um `BETWEEN` sobre `OccurredOn`.
- [x] 4.2 Substituir o mesmo filtro em `TransactionRepository.ListByMonthAsync`. Verificar com teste de unidade do repositorio.
- [x] 4.3 Garantir que janela vazia devolve lista vazia e totais zerados, sem excecao. Verificar com teste do cenario "Janela vazia" de `specs/competence/scoping`.
- [x] 4.4 Transformar `MonthParameter.Resolve` em `MonthParameter.TryParse(string?) -> YearMonth?` e resolver o default de cada endpoint do Ledger pela competencia corrente do usuario. Verificar que uma chamada sem `month` devolve a competencia da regra, nao o mes do calendario.

## 5. Gastos fixos (Recurrences)

- [x] 5.1 Trocar `FixedExpense.OccurrenceDate(YearMonth)` por `OccurrenceDate(CompetenceWindowDto)`: primeira data da janela cujo dia coincide com o vencimento (ajustado ao ultimo dia do mes) e, se nenhuma atender, o ultimo dia da janela. Verificar com testes cobrindo vencimento no primeiro mes da janela, no segundo, dia inexistente e janela encurtada.
- [x] 5.2 Fazer `FixedExpenseService.MaterializeAsync` obter a janela via `ILedgerModuleApi.GetCompetenceCalendarAsync` e registrar por `SyncRecurrenceOccurrenceAsync`, mantendo a chave `recurrence:{id}:{yyyy-MM}`. Verificar que materializar duas vezes deixa exatamente uma ocorrencia por gasto fixo ativo.
- [x] 5.3 Fazer `RecurrenceMaterializationWorker` materializar a competencia corrente resolvida pela regra, no lugar de `YearMonth.From(clock.Today)`. Verificar com teste em que o dia de virada 25 e hoje 28/08/2026 materializa 2026-09.
- [x] 5.4 Assinar `CompetenceRuleChangedIntegrationEvent` em Recurrences e rematerializar a competencia corrente e a anterior. Verificar com teste que altera o dia de virada e confere que a ocorrencia deslocada volta para dentro da janela.

## 6. Host e demais modulos

- [x] 6.1 Resolver o default de competencia em `BudgetEndpoints` e nos endpoints de Recurrences pela competencia corrente obtida do `ILedgerModuleApi`. Verificar que `/api/budget` sem `month` devolve a competencia da regra.
- [x] 6.2 Registrar os novos servicos do Ledger em `LedgerModule` e confirmar que nenhum modulo passou a referenciar internals de outro. Verificar com `dotnet test --filter FullyQualifiedName~ModuleBoundaryTests`.

## 7. Front-end

- [x] 7.1 Criar `core/competence/competence.api.ts` com as chamadas de GET, PUT do dia de virada, POST de encerramento e DELETE de reabertura, e os modelos correspondentes. Verificar que a feature compila com `npm run build`.
- [x] 7.2 Mover `MonthService` de `shared/` para `core/competence/`, atualizar os 6 imports e passar a inicializar a competencia corrente pelo backend, com fallback para o mes do calendario em caso de erro. Verificar com `npx ng test` e com o app subindo sem backend.
- [x] 7.3 Carregar a competencia corrente no bootstrap com `provideAppInitializer` em `app.config.ts`, para que nenhuma tela abra no mes errado e depois pule. Verificar abrindo o painel com o dia de virada 25 depois do dia 25 e conferindo que ele ja abre na competencia seguinte.
- [x] 7.4 Atualizar `month.service.spec.ts` para os novos cenarios (competencia vinda do backend, fallback, navegacao entre competencias). Verificar com `npx ng test --watch=false --browsers=ChromeHeadless`.
- [x] 7.5 Adicionar na tela de configuracoes o campo do dia de virada, com opcao "sem virada", previa do efeito ("gastos a partir do dia 25 entram na competencia seguinte") e aviso de que a mudanca reclassifica lancamentos ja registrados. Verificar salvando e conferindo a reclassificacao no painel.
- [x] 7.6 Adicionar no painel o botao "Encerrar os gastos deste mes" com confirmacao, e a acao de reabrir quando a competencia corrente decorre de um encerramento. Verificar que apos encerrar a competencia corrente avanca e um novo lancamento com a data de hoje cai nela.
- [x] 7.7 Indicar no rotulo de mes do shell quando a competencia esta encerrada ou vem de uma regra de virada. Verificar visualmente nos dois estados.

## 8. Verificacao de ponta a ponta

- [x] 8.1 Escrever testes de integracao em `ControleDeGastos.Api.DatabaseTests` (projeto separado: `ModuleHostExtensions` guarda os modulos numa lista estatica e nao suporta dois hosts no mesmo processo) para o fluxo completo: configurar dia de virada 25, registrar lancamento em 28/08, conferir que ele aparece em 2026-09 e nao em 2026-08; encerrar 2026-08 e conferir a competencia corrente e a classificacao de um lancamento posterior; reabrir e conferir a volta. Verificar com `dotnet test`.
- [x] 8.2 Confirmar que com `ClosingDay` nulo listagens, totais, orcamento e materializacao produzem exatamente o resultado de hoje. Verificar comparando com os cenarios existentes de `MonthlyBudgetTests` e dos testes do Ledger.
- [x] 8.3 Rodar a suite completa (`dotnet test` no backend e `npx ng test --watch=false --browsers=ChromeHeadless` no frontend) e conferir que o CI passaria.

## 9. Documentacao

- [x] 9.1 Atualizar `CLAUDE.md` e `docs/architecture.md`: competencia deixou de ser o mes do calendario, a regra mora no Ledger, e `MonthService` nao e mais a excecao deliberada em `shared/`. Verificar que nenhuma dos dois textos ainda descreve o comportamento antigo.
- [x] 9.2 Atualizar `docs/modules.md` com a nova responsabilidade do Ledger (regra de competencia) e a dependencia de Recurrences ao calendario de competencia. Verificar por leitura.
- [x] 9.3 Registrar um ADR sobre competencia derivada versus gravada, com a consequencia de reclassificacao retroativa. Verificar que o arquivo existe em `docs/adr/` seguindo a numeracao vigente.
