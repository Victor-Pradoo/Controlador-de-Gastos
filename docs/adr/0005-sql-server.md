# ADR 0005 — SQL Server como banco de dados

**Status:** aceito · **Data:** 2026-08-28
**Substitui:** a escolha inicial por PostgreSQL

## Contexto

O projeto nasceu apontando para PostgreSQL. O ambiente de desenvolvimento é
Windows com Visual Studio, que já traz **SQL Server LocalDB** instalado — enquanto
o PostgreSQL exigiria Docker (ausente na máquina) ou instalação manual.

## Decisão

Usar SQL Server, com `(localdb)\MSSQLLocalDB` como padrão de desenvolvimento.
O `docker-compose.yml` oferece um SQL Server 2022 em contêiner para quem preferir
isolamento ou não estiver no Windows.

## O que a troca custou

Praticamente nada, e isso não foi sorte — o provedor estava isolado atrás de
`ModuleDbContextExtensions.AddModuleDbContext`. A troca inteira foi:

- o pacote do provider (`Npgsql...` → `Microsoft.EntityFrameworkCore.SqlServer`);
- `UseNpgsql` → `UseSqlServer`, em **um** arquivo;
- o único filtro SQL cru do projeto, que usava aspas do Postgres:
  `"\"ExternalId\" IS NOT NULL"` → `"[ExternalId] IS NOT NULL"`;
- a connection string.

Domínio, casos de uso, endpoints e testes não mudaram uma linha.

## Consequências

**A favor:** zero setup no ambiente de desenvolvimento; ferramental (SSMS, perfis do
Visual Studio) já familiar; um obstáculo a menos entre clonar o repositório e rodar.

**Contra:**

- Licenciamento pesa se o app for para produção em nuvem — Azure SQL tem custo bem
  acima de um Postgres gerenciado equivalente. Reavaliar quando/se sair do uso pessoal.
- `Guid.CreateVersion7()` é ordenado no tempo, mas o SQL Server ordena
  `uniqueidentifier` pelos últimos bytes, então as chaves **não** ficam sequenciais
  no índice clusterizado. Irrelevante no volume de um app pessoal; se as tabelas
  crescerem, a saída é PK não-clusterizada com índice clusterizado em
  `(UserId, OccurredOn)` — que também é a consulta dominante do app.
