# Arquitetura

## Visão geral

```
┌──────────────────────────────────────────────────────────────┐
│  Angular (feature-driven, standalone components)             │
│  dashboard · transactions · fixed-expenses · bank-connections · settings │
└───────────────────────────┬──────────────────────────────────┘
                            │ HTTP /api/*
┌───────────────────────────▼──────────────────────────────────┐
│  ControleDeGastos.Api  (host — compõe os módulos)            │
├──────────────────────────────────────────────────────────────┤
│  Ledger   Budgeting   Recurrences   Categorization   Banking │
│    │          │            │              │            │     │
│    └──────────┴────────────┴──────────────┴────────────┘     │
│         comunicação só via projetos *.Contracts              │
├──────────────────────────────────────────────────────────────┤
│  SharedKernel (primitivos puros) · Infrastructure.Shared     │
└───────────────────────────┬──────────────────────────────────┘
                            │
                   SQL Server — 1 banco, 1 schema por módulo
                            │
                   Pluggy (Open Finance) via porta IBankDataProvider
```

## Back-end: monólito modular

Um único processo e um único deploy, mas com fronteiras internas reais. A regra
que sustenta tudo:

> **Um módulo só pode referenciar o projeto `*.Contracts` de outro módulo.**

Isso é imposto em três níveis:

1. **Referências de projeto** — o `.csproj` de um módulo só lista `.Contracts` alheios.
2. **Visibilidade** — repositórios, endpoints e handlers são `internal`.
3. **Testes de arquitetura** — `ModuleBoundaryTests` quebra o build se alguém burlar.

### Anatomia de um módulo

```
Modules/Ledger/
├── ControleDeGastos.Modules.Ledger.Contracts/   # público: DTOs, interfaces, eventos
└── ControleDeGastos.Modules.Ledger/             # privado
    ├── LedgerModule.cs         # IModule: o que registra e quais rotas expõe
    ├── Domain/                 # entidades, regras, erros — zero EF, zero ASP.NET
    ├── Application/            # casos de uso e consultas
    ├── Infrastructure/         # DbContext, repositórios, adaptadores
    └── Presentation/           # endpoints minimal API
```

O host não sabe o que existe dentro de um módulo. Ele só faz:

```csharp
builder.Services.AddModules(
    builder.Configuration,
    new LedgerModule(), new BudgetingModule(), /* ... */);

app.MapModuleEndpoints();
```

Adicionar um módulo é uma linha. Extrair um módulo para um serviço próprio no
futuro é trocar as chamadas via `*.Contracts` por chamadas remotas — nada mais
depende das entranhas dele.

### Persistência

Um banco SQL Server, **um schema por módulo** (`ledger`, `budgeting`, …), cada um
com sua própria tabela `__EFMigrationsHistory`. Nenhuma foreign key cruza a
fronteira entre módulos — quando o Banking precisa referenciar um lançamento, ele
guarda o `ExternalId` e conversa pelo contrato, não pelo banco.

Cada módulo declara a sua própria interface de unit of work
(`ILedgerUnitOfWork`, `IBudgetingUnitOfWork`, …). Registrar um `IUnitOfWork`
genérico faria o último módulo registrado ganhar, e um módulo salvaria no
`DbContext` de outro.

### Comunicação entre módulos

Duas formas, ambas passando pelos contratos:

| Forma | Quando | Exemplo |
| --- | --- | --- |
| Chamada direta (`I*ModuleApi`) | Preciso da resposta agora | `Budgeting` pede totais ao `Ledger` |
| Evento de integração (`IEventBus`) | Notificar sem acoplar | `Ledger` publica `TransactionRegistered` |

Hoje o `IEventBus` entrega em processo e de forma síncrona (`InMemoryEventBus`).
Se a entrega precisar sobreviver a uma falha do processo, o caminho é outbox +
broker — a interface não muda.

### Erros

O domínio devolve `Result` / `Result<T>` com um `Error` tipado; exceções ficam
para falhas inesperadas. O host traduz `ErrorType` em status HTTP num único lugar
(`ResultExtensions`), então nenhum endpoint escolhe status code na mão.

## Front-end: feature-driven

```
src/app/
├── core/       # uma instância para o app: API, interceptors, shell
├── shared/     # reutilizável e burro: pipes, componentes de UI, modelos
└── features/
    └── <feature>/
        ├── <feature>.routes.ts   # entrada lazy da feature
        ├── data-access/          # serviços HTTP + store de signals
        ├── feature/              # páginas roteadas (smart)
        └── ui/                   # componentes de apresentação (dumb)
```

Regras que espelham as do back-end:

- Uma feature **não importa** de outra feature — o que for comum sobe para `shared/`.
- `feature/` (smart) conhece o store; `ui/` (dumb) só recebe `input()` e emite `output()`.
- Todo componente é standalone e `OnPush`; estado é `signal`.
- Cada feature é um chunk lazy próprio — quem não abre a tela não baixa o código.

O `MonthService` é a exceção deliberada em `shared/`: o app inteiro raciocina por
competência, e cada tela ter a sua noção de "mês atual" seria pior.

## Autenticação (pendente)

Não existe login ainda. Todo dado já é escopado por `UserId` desde o primeiro dia
e o acesso passa por `ICurrentUser` — hoje resolvido por um usuário fixo de
desenvolvimento (`Auth:DevUserId`, com override pelo header `X-User-Id`).

Quando o módulo de identidade entrar, muda-se a implementação de `ICurrentUser` e
o interceptor do front. Nenhum módulo de negócio é afetado.
