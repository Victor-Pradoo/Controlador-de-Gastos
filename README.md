# Controle de Gastos

Aplicativo de controle de gastos pessoais evoluindo de um HTML único com `localStorage`
para um MVP que **importa os lançamentos direto do banco** via Open Finance.

| Camada | Stack | Arquitetura |
| --- | --- | --- |
| Back-end | .NET 10 / ASP.NET Core Minimal APIs / EF Core / SQL Server | Monólito modular |
| Front-end | Angular 20 (standalone components, signals) | Feature-driven |
| Open Finance | [Pluggy](https://docs.pluggy.ai) (adaptador trocável) | Porta + adaptador |

---

## Estrutura do repositório

```
.
├── backend/          # Solution .NET (monólito modular)
│   ├── src/
│   │   ├── Api/                    # Host: compõe os módulos, expõe HTTP
│   │   ├── Shared/                 # SharedKernel (puro) + Infrastructure.Shared
│   │   └── Modules/                # Ledger, Budgeting, Recurrences, Categorization, Banking
│   └── tests/                      # Unitários, arquitetura e integração
├── frontend/         # Workspace Angular (feature-driven)
│   └── src/app/
│       ├── core/                   # API, interceptors, layout — instanciado uma vez
│       ├── shared/                 # Componentes burros, pipes, modelos
│       └── features/               # Uma pasta por feature, com lazy loading
├── docs/             # Arquitetura, decisões (ADRs) e roadmap
├── legacy/           # App HTML original, preservado como referência de UI
└── docker-compose.yml
```

---

## Rodando localmente

### Pré-requisitos

- .NET SDK 10.0.400+
- Node.js **22.22.3+** (o instalado hoje é 22.12 — ver [docs/roadmap.md](docs/roadmap.md))
- SQL Server — o **LocalDB** que acompanha o Visual Studio já basta

### 1. Banco de dados

Nada a fazer no Windows: a connection string padrão aponta para
`(localdb)\MSSQLLocalDB` e o banco `ControleDeGastos` é criado na primeira execução.

Se quiser um SQL Server em contêiner (ou está em Linux/macOS):

```bash
docker compose up -d sqlserver
```

e ajuste `ConnectionStrings:Database` em
`backend/src/Api/ControleDeGastos.Api/appsettings.json` — ou, melhor,
via `dotnet user-secrets`.

### 2. Back-end

```bash
cd backend
dotnet run --project src/Api/ControleDeGastos.Api
```

As migrations dos cinco módulos já estão versionadas e são aplicadas
automaticamente ao subir em `Development` (`Database:AutoMigrate`). Em produção,
aplicar migrations é passo explícito de deploy — ver
[backend/README.md](backend/README.md).

- API: <http://localhost:5089>
- Documentação interativa (Scalar): <http://localhost:5089/scalar/v1>
- Health + módulos carregados: <http://localhost:5089/health>

### 3. Front-end

```bash
cd frontend
npm install
npm start
```

App em <http://localhost:4200>, com `/api` fazendo proxy para a API
(`frontend/proxy.conf.json`).

---

## Conexão bancária

O MVP funciona **sem credenciais**: `Banking:Pluggy:UseFakeProvider` vem `true` e o
`FakeBankDataProvider` gera um extrato sintético determinístico — dá para construir
a UI inteira antes de tocar no provedor real.

Para usar a Pluggy de verdade:

```bash
cd backend/src/Api/ControleDeGastos.Api
dotnet user-secrets set "Banking:Pluggy:ClientId" "<seu-client-id>"
dotnet user-secrets set "Banking:Pluggy:ClientSecret" "<seu-client-secret>"
dotnet user-secrets set "Banking:Pluggy:UseFakeProvider" "false"
```

Credenciais **nunca** vão para `appsettings.json`. A senha do banco do usuário fica
no widget da Pluggy e não passa por esta aplicação.

---

## Testes

```bash
cd backend  && dotnet test      # unitários + arquitetura + integração
cd frontend && npx ng test      # unitários do Angular
```

Os testes de arquitetura (`backend/tests/ControleDeGastos.ArchitectureTests`) falham
o build se um módulo acessar as entranhas de outro — é o que mantém o monólito
modular de fato modular.

---

## Documentação

- [docs/architecture.md](docs/architecture.md) — como as peças se encaixam
- [docs/modules.md](docs/modules.md) — o que cada módulo faz e o que expõe
- [docs/adr/](docs/adr/) — decisões arquiteturais e o porquê de cada uma
- [docs/roadmap.md](docs/roadmap.md) — o que falta para o MVP e o que vem depois
