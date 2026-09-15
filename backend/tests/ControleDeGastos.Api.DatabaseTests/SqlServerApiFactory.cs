using ControleDeGastos.Modules.Ledger.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ControleDeGastos.Api.DatabaseTests;

/// <summary>
/// Sobe a API contra um SQL Server de verdade, num banco proprio de testes.
///
/// Banco separado de proposito: estes testes apagam dados entre execucoes, e fazer
/// isso no banco de desenvolvimento seria uma armadilha. A string vem de
/// <c>ConnectionStrings__Database</c> quando definida - e o que o CI injeta, apontando
/// para o container de SQL Server - e cai no LocalDB quando nao esta, que e o cenario
/// da maquina de desenvolvimento no Windows.
///
/// As migrations rodam no startup (<c>Database:AutoMigrate</c>), entao o esquema
/// exercitado aqui e exatamente o que elas produzem.
///
/// Este projeto existe separado de ControleDeGastos.Api.IntegrationTests porque
/// <c>ModuleHostExtensions</c> guarda os modulos registrados numa lista ESTATICA:
/// compor dois hosts no mesmo processo registra cada modulo duas vezes e o roteamento
/// quebra com "Duplicate endpoint name". Projetos diferentes = processos diferentes.
/// </summary>
public sealed class SqlServerApiFactory : WebApplicationFactory<Program>
{
    private const string LocalDbFallback =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ControleDeGastos_IntegrationTests;"
        + "Integrated Security=True;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? LocalDbFallback;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = ConnectionString,
                ["Database:AutoMigrate"] = "true",
                ["Banking:Pluggy:UseFakeProvider"] = "true",
            }));
    }

    /// <summary>
    /// Zera o que o Ledger guarda, para cada teste comecar do mesmo lugar. Nenhuma FK
    /// atravessa a fronteira de modulo, entao apagar aqui nao afeta outro schema.
    /// </summary>
    public async Task ResetLedgerAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();

        await context.Transactions.ExecuteDeleteAsync();
        await context.CompetenceClosures.ExecuteDeleteAsync();
        await context.CompetenceSettings.ExecuteDeleteAsync();
    }
}
