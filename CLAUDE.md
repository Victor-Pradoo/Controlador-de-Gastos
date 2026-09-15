# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Personal expense tracker evolving from a single `localStorage` HTML file (preserved
in `legacy/`) into an MVP that imports transactions directly from the user's bank via
Open Finance (Pluggy). Backend and frontend are separate deployables in one repo.

| Layer | Stack | Architecture |
| --- | --- | --- |
| Backend | .NET 10 / ASP.NET Core Minimal APIs / EF Core / SQL Server | Modular monolith |
| Frontend | Angular 20 (standalone components, signals) | Feature-driven |
| Open Finance | [Pluggy](https://docs.pluggy.ai) | Port + adapter (`IBankDataProvider`) |

Repo layout:

```
backend/    # .NET solution — src/Api, src/Shared, src/Modules, tests/
frontend/   # Angular workspace — src/app/{core,shared,features}
docs/       # architecture.md, modules.md, adr/, roadmap.md
legacy/     # original single-file HTML app, kept as UI reference only
```

Read `docs/architecture.md` and `docs/modules.md` before making structural changes —
they're short and describe the actual rules the code (and its tests) enforce.

## Commands

### Backend (run from `backend/`)

```bash
dotnet restore
dotnet build
dotnet test                                          # unit + architecture + integration
dotnet test --filter FullyQualifiedName~ClassName    # single test class
dotnet run --project src/Api/ControleDeGastos.Api    # http://localhost:5089, Scalar docs at /scalar/v1
```

Migrations (one `DbContext`/schema per module — `Development` auto-applies pending
migrations on startup via `Database:AutoMigrate`):

```bash
dotnet tool restore                    # pins dotnet-ef via .config/dotnet-tools.json
./scripts/add-migration.sh NomeDaMigration    # or .ps1 — creates migration in all 5 modules at once
dotnet dotnet-ef database update \
  --project src/Modules/Ledger/ControleDeGastos.Modules.Ledger \
  --startup-project src/Api/ControleDeGastos.Api \
  --context LedgerDbContext
```

Secrets — never in `appsettings.json`, always `dotnet user-secrets` from
`src/Api/ControleDeGastos.Api`: `Banking:Pluggy:ClientId`, `Banking:Pluggy:ClientSecret`,
`ConnectionStrings:Database`. `Banking:Pluggy:UseFakeProvider=true` (the default) uses a
deterministic synthetic-statement provider, so the app runs with zero bank credentials.

### Frontend (run from `frontend/`)

```bash
npm install
npm start          # ng serve on :4200, proxies /api to :5089 (frontend/proxy.conf.json)
npm run build
npx ng test        # unit tests, needs a browser (ChromeHeadless in CI)
```

The backend must be running for screens to load real data; without it the app still
boots and shows error toasts.

### Known environment issues (Windows, not project bugs — see `backend/README.md`)

- `Unable to load DLL 'Microsoft.Data.SqlClient.SNI.dll'`: antivirus quarantines the
  native SNI binaries inside the NuGet cache. Fix: delete
  `~/.nuget/packages/microsoft.data.sqlclient.sni.runtime/6.0.2`, delete all `obj/`
  dirs, `dotnet restore` again.
- `hostpolicy.dll` not found under `Microsoft.NETCore.App/8.0.24`: incomplete .NET 8
  runtime (needed because `dotnet-ef` targets net8.0). Workaround:
  `DOTNET_ROLL_FORWARD=LatestMajor dotnet dotnet-ef <command>`.

## Backend architecture (modular monolith)

One process, one deploy, real internal boundaries. The rule everything else enforces:

> **A module may only reference another module's `*.Contracts` project.**

Enforced three ways: (1) `.csproj` references only list other modules' `.Contracts`;
(2) repositories/endpoints/handlers are `internal`, only DTOs/interfaces in
`.Contracts` are `public`; (3) `backend/tests/ControleDeGastos.ArchitectureTests`
(`ModuleBoundaryTests`) fails the build if a module reaches into another's internals —
this test suite runs as part of `dotnet test`, so a boundary violation shows up as a
CI failure, not a runtime one.

Each module (`Ledger`, `Budgeting`, `Recurrences`, `Categorization`, `Banking`) follows
the same internal shape:

```
Modules/<Name>/
├── ControleDeGastos.Modules.<Name>.Contracts/   # public: DTOs, interfaces, integration events
└── ControleDeGastos.Modules.<Name>/             # internal
    ├── <Name>Module.cs   # implements IModule — what it registers, which routes it maps
    ├── Domain/            # entities, rules, errors — no EF, no ASP.NET
    ├── Application/       # use cases and queries
    ├── Infrastructure/    # DbContext, repositories, adapters
    └── Presentation/      # minimal API endpoints
```

The host (`src/Api/ControleDeGastos.Api`) only wires modules together — it has no
knowledge of what's inside one:

```csharp
builder.Services.AddModules(builder.Configuration,
    new LedgerModule(), new BudgetingModule(), /* ... */);
app.MapModuleEndpoints();
```

Adding a module: `dotnet new classlib` for `X` and `X.Contracts` → `X.Contracts`
depends only on `SharedKernel`; `X` depends on `Infrastructure.Shared`, its own
`.Contracts`, and (if needed) other modules' `.Contracts` only → implement `IModule`
in `XModule.cs` → register in `Program.cs`'s `AddModules(...)` → add `"X"` to the
architecture tests' module lists.

**Persistence**: one SQL Server database, one schema per module (`ledger`,
`budgeting`, …), each with its own `__EFMigrationsHistory`. No cross-module foreign
keys — a module holding a reference to another module's data stores its
`ExternalId` and talks through the contract, not the database. Each module defines
its own unit-of-work interface (`ILedgerUnitOfWork`, etc.) rather than a shared
generic one, to avoid one module accidentally saving to another's `DbContext`.

**Cross-module communication**: direct call via `I*ModuleApi` when the caller needs
the answer now (e.g. Budgeting asks Ledger for totals); `IEventBus` (today
`InMemoryEventBus` — synchronous, in-process, no delivery guarantee across a process
crash — see [ADR 0004](docs/adr/0004-eventos-in-process.md)) when notifying without
coupling.

**Errors**: domain methods return `Result` / `Result<T>` with a typed `Error` —
exceptions are reserved for unexpected failures, not business-rule violations.
`ResultExtensions` on the host is the single place that maps `ErrorType` to HTTP
status; endpoints never choose a status code themselves.

**Conventions**: no `DateTime.Now` in the domain — use `IClock`. Every query is
scoped by `UserId` (there's no login yet — `ICurrentUser` resolves a fixed dev user
via `Auth:DevUserId`, overridable with header `X-User-Id`; swapping in real auth
later only touches `ICurrentUser` and the frontend's `devUserInterceptor`).

### Module map

| Module | Owns | Depends on (`.Contracts` only) | Schema |
| --- | --- | --- | --- |
| Ledger | The transaction ledger (expenses, income, materialized fixed expenses) — every entry in the app lands here regardless of source — plus the competence cutoff rule that decides which month an entry belongs to | — | `ledger` |
| Budgeting | Salary + savings-rate settings and the "how much can I still spend" calculation | Ledger | `budgeting` |
| Recurrences | Fixed expenses and their monthly materialization into the ledger | Ledger | `recurrences` |
| Categorization | Category catalog + auto-categorization rules/heuristics | Ledger | `categorization` |
| Banking | Open Finance connections and statement import | Ledger, Categorization | `banking` |

Notable design points worth knowing before touching these:

- **Ledger** owns the **competence rule** (`CompetenceCalendar`, a pure function):
  a competência is no longer the calendar month. With a `ClosingDay` configured,
  entries from that day on belong to the *next* competência (`ClosingDay = 25` →
  25/08..24/09 is September), and the user can also close a month by hand. The
  competência is **derived, never stored** — so changing the rule reclassifies
  history, which is deliberate. Every query scopes by the rule's date *window*, not
  by first/last day of the month. Never re-derive this rule outside Ledger: ask
  `ILedgerModuleApi` for the calendar or the window.
- **Ledger** `Transaction.ExternalId` has a per-user unique index — it's the
  idempotency key for everything automatic (bank sync, fixed-expense
  materialization). `IsEditable` is true only for `Source = Manual`; imported/
  materialized entries aren't deleted by hand, the source of truth (bank statement
  or fixed-expense registration) is.
- **Budgeting** `MonthlyBudget.Calculate` is a pure function (no I/O) — it's the
  most user-visible rule in the app and the most densely tested
  (`MonthlyBudgetTests`).
- **Recurrences** materialization key is `recurrence:{id}:{yyyy-MM}` — safe to
  re-run. Deleting a recurrence deactivates it; it does not retroactively remove
  past ledger entries (the legacy app did, and that was a bug people hit). A
  `BackgroundService` materializes the current month on startup and once daily;
  multiple instances would need a distributed lock.
- **Categorization** applies user-defined rules first, then built-in heuristics for
  common Brazilian merchants (ifood, uber, posto, netflix, …); no match falls into
  `Outros` with zero confidence so the UI can prompt for confirmation. No ML here by
  design — post-MVP concern.
- **Banking** `IBankDataProvider` is the port; `PluggyBankDataProvider` (real,
  caches the API key ~2h) and `FakeBankDataProvider` (deterministic synthetic
  statement, default in dev) are the adapters. Sync re-fetches the last 3 already-
  synced days on purpose (card statements settle late) — safe only because of the
  `ExternalId` idempotency in Ledger. `BankConnection` stores only the provider's
  `ExternalItemId` — bank credentials never pass through this app, they stay in the
  Pluggy widget.

## Frontend architecture (feature-driven)

```
src/app/
├── core/       # one instance for the whole app: API tokens, interceptors, shell
├── shared/     # reusable, stateless: pipes, dumb components, models
└── features/
    └── <feature>/
        ├── <feature>.routes.ts   # lazy entry point
        ├── data-access/          # HTTP services + signal-based store
        ├── feature/              # routed pages (smart — know the store)
        └── ui/                   # presentational components (dumb — input()/output() only)
```

Rules that mirror the backend's module boundaries:

- A feature never imports from another feature — shared code moves up to `shared/`.
- A component never calls `HttpClient` directly — always through a `data-access/`
  service.
- Every component is standalone and `OnPush`; state is `signal`-based.
- Each feature is its own lazy chunk (`loadChildren` in its routes file) — screens
  you don't open don't get downloaded.
- `MonthService` lives in `core/competence/`, not in a feature: the whole app reasons
  in terms of the current "competência", and that competência now comes from the
  backend (it depends on the user's cutoff rule), so it is app-wide state that does
  I/O — which is what `core/` is for. It is loaded once via `provideAppInitializer`
  and falls back to the calendar month if the backend is unreachable.
- `src/app/shared/models/*` hand-mirror the backend's `*.Contracts` DTOs — when a
  contract changes, update both sides (no codegen from OpenAPI yet, though
  `/openapi/v1.json` is available if this starts hurting).

New feature scaffold: `npx ng generate component features/<name>/feature/<name>-page`,
then add `<name>.routes.ts` and wire it into `app.routes.ts` via `loadChildren`.

## CI

`.github/workflows/ci.yml` runs on push to `main` and on PRs: backend job does
`dotnet restore/build/test` (Release) from `backend/`, including the architecture
tests; frontend job does `npm ci`, `npm run build`, `npx ng test --watch=false
--browsers=ChromeHeadless` from `frontend/`.
