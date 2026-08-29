# Roadmap

O que existe hoje, o que falta para o MVP fechar e o que fica para depois.

## Já funciona

- Solution .NET com 5 módulos, fronteiras impostas por testes de arquitetura
- Ledger, Budgeting, Recurrences, Categorization e Banking com domínio, persistência
  e endpoints
- Migrations dos 5 módulos versionadas e aplicadas (verificado em SQL Server LocalDB)
- Sincronização bancária de ponta a ponta contra o provedor falso, idempotente
- App Angular com as 6 telas, lazy loading por feature, consumindo a API
- 63 testes no back-end (unitários, arquitetura, integração) e 6 no front
- CI no GitHub Actions para as duas pontas

## Para fechar o MVP

### 1. Widget da Pluggy no front

`POST /api/banking/connect-token` já devolve o token. Falta plugar o SDK:

```bash
npm install pluggy-connect-sdk
```

Substituir o campo "Item ID" manual em
`features/bank-connections/feature/bank-connections-page.ts` por
`new PluggyConnect({ connectToken, onSuccess })`, registrando o `itemId` que o
callback devolve. O TODO está marcado no arquivo.

### 2. Autenticação

Hoje há um usuário fixo (`Auth:DevUserId`) com override por header `X-User-Id`.
Todo dado já é escopado por `UserId` e o acesso passa por `ICurrentUser`, então:

- adicionar um módulo `Identity` (ou um provedor externo — Auth0, Entra ID);
- trocar a implementação de `ICurrentUser` para ler o claim `sub`;
- trocar `devUserInterceptor` por um que anexe o Bearer token.

Nenhum módulo de negócio muda.

### 3. Sincronização automática

O `RecurrenceMaterializationWorker` já roda diariamente para gastos fixos. Falta o
equivalente para o Banking — hoje o sync é manual, pelo botão. Opções: worker
periódico ou webhook da Pluggy (`item/updated`), que é mais barato e mais rápido.

### 4. Node.js 22.22.3+

O ambiente tem Node 22.12, abaixo do mínimo do Angular 21 — por isso o workspace
foi criado no **Angular 20**. Depois de atualizar o Node:

```bash
cd frontend && npx ng update @angular/core @angular/cli
```

## Depois do MVP

| Tema | O que fazer | Por quê |
| --- | --- | --- |
| Outbox | Outbox transacional + broker no lugar do `InMemoryEventBus` | Hoje um evento se perde se o processo cair (ver [ADR 0004](adr/0004-eventos-in-process.md)) |
| Testes com banco | Fixture com Testcontainers.MsSql | Os testes de integração atuais não exercitam persistência |
| Categorização | Aprender com as correções do usuário | As regras manuais cansam depois de alguns meses |
| Multi-instância | Lock distribuído no worker de recorrências | Duas instâncias materializariam em paralelo (a idempotência salva, mas gera ruído) |
| Testes de front | Cobrir stores e páginas | Hoje só `MonthService` e `BrlPipe` têm testes |
| PWA | `ng add @angular/pwa` | O legado já era usado como app instalado no celular |
| Observabilidade | OpenTelemetry no host | Sync bancário falhando em silêncio é o pior modo de falha do produto |
