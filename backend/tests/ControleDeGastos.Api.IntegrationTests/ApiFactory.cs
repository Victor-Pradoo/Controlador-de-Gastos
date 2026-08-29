using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ControleDeGastos.Api.IntegrationTests;

/// <summary>
/// Sobe a API no ambiente "Testing": sem migrations automaticas e com provedor
/// bancario falso, para os testes de fumaca rodarem sem banco nem credenciais.
///
/// Testes que exercitam persistencia de verdade precisam de um banco - use
/// Testcontainers.MsSql numa fixture propria (ver docs/roadmap.md).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:AutoMigrate"] = "false",
                ["Banking:Pluggy:UseFakeProvider"] = "true",
            }));
    }
}
