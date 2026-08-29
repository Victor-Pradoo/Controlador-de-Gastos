# Back-end

Monólito modular em .NET 10. Ver [../docs/architecture.md](../docs/architecture.md)
para o desenho e [../docs/modules.md](../docs/modules.md) para o que cada módulo faz.

## Projetos

| Projeto | Papel |
| --- | --- |
| `src/Api/ControleDeGastos.Api` | Host: compõe os módulos, CORS, OpenAPI, tradução de erros |
| `src/Shared/ControleDeGastos.SharedKernel` | Primitivos puros (`Entity`, `Money`, `YearMonth`, `Result`). **Sem dependências.** |
| `src/Shared/ControleDeGastos.Infrastructure.Shared` | `IModule`, helpers de EF, event bus, relógio |
| `src/Modules/<Nome>/...Contracts` | API pública do módulo — a única coisa visível de fora |
| `src/Modules/<Nome>/...` | Domínio, aplicação, infraestrutura e endpoints do módulo |

## Comandos

```bash
dotnet build                 # compila a solution
dotnet test                  # unitários + arquitetura + integração
dotnet run --project src/Api/ControleDeGastos.Api
```

Documentação interativa em <http://localhost:5089/scalar/v1> (só em Development).

## Migrations

Cada módulo tem `DbContext` e schema próprios, e portanto histórico de migrations
próprio — em `src/Modules/<Nome>/.../Infrastructure/Migrations`. A migration
`Inicial` dos cinco módulos já está versionada.

O `dotnet-ef` está fixado num manifesto local (`.config/dotnet-tools.json`), então
todo mundo usa a mesma versão:

```bash
dotnet tool restore
```

Para criar uma migration nova nos cinco módulos de uma vez:

```bash
./scripts/add-migration.sh NomeDaMigration     # bash
./scripts/add-migration.ps1 NomeDaMigration    # PowerShell
```

Para aplicar manualmente um módulo específico:

```bash
dotnet dotnet-ef database update \
  --project src/Modules/Ledger/ControleDeGastos.Modules.Ledger \
  --startup-project src/Api/ControleDeGastos.Api \
  --context LedgerDbContext
```

Em `Development` a API aplica migrations pendentes ao subir (`Database:AutoMigrate`),
que é o caminho normal do dia a dia.

## Problemas conhecidos de ambiente

Dois problemas desta máquina que travam o back-end e **não** são do projeto:

**1. `Unable to load DLL 'Microsoft.Data.SqlClient.SNI.dll'`**

O antivírus põe em quarentena o SNI nativo dentro do cache do NuGet. O pacote fica
com as pastas `runtimes/win-*/native` vazias, e o `project.assets.json` congela
essa listagem. Correção:

```bash
rm -rf ~/.nuget/packages/microsoft.data.sqlclient.sni.runtime/6.0.2
find . -type d -name obj -exec rm -rf {} +      # o assets.json precisa ser refeito
dotnet restore
```

Vale adicionar uma exclusão do antivírus para `%USERPROFILE%\.nuget\packages`.

**2. `hostpolicy.dll` não encontrada em `Microsoft.NETCore.App\8.0.24`**

O runtime .NET 8.0.24 instalado está incompleto, e o `dotnet-ef` roda em `net8.0`.
Contorno enquanto não for reparado:

```bash
DOTNET_ROLL_FORWARD=LatestMajor dotnet dotnet-ef <comando>
```

A correção definitiva é reparar/reinstalar o runtime .NET 8.

## Segredos

Nunca em `appsettings.json`:

```bash
cd src/Api/ControleDeGastos.Api
dotnet user-secrets set "Banking:Pluggy:ClientId" "..."
dotnet user-secrets set "Banking:Pluggy:ClientSecret" "..."
dotnet user-secrets set "ConnectionStrings:Database" "..."
```

## Adicionando um módulo

1. `dotnet new classlib` para `ControleDeGastos.Modules.X` e `...X.Contracts`
2. `X.Contracts` referencia só o `SharedKernel`; `X` referencia
   `Infrastructure.Shared`, o próprio `.Contracts` e — se precisar — o `.Contracts`
   de outros módulos, **nunca** os projetos internos deles
3. Implemente `IModule` em `XModule.cs`
4. Registre no `Program.cs`: mais uma linha em `AddModules(...)`
5. Adicione `"X"` às listas dos testes de arquitetura

## Convenções

- Domínio devolve `Result`, não lança exceção para regra de negócio violada
- Repositórios e endpoints são `internal`; DTOs e interfaces de contrato são `public`
- Nada de `DateTime.Now` no domínio — use `IClock`
- Toda consulta é escopada por `UserId`
