using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Api.IntegrationTests;

/// <summary>
/// Smoke test da composicao dos endpoints de competencia: confirma que as rotas foram
/// mapeadas e que os servicos novos do Ledger resolvem do container. Nao toca o banco -
/// falha aqui significa erro de DI ou de rota, nao de persistencia.
///
/// O fluxo com dados (configurar virada, lancar, conferir a competencia) esta coberto
/// por testes de unidade dos handlers; exercita-lo por HTTP depende de um SQL Server
/// (ver docs/roadmap.md, Testcontainers.MsSql).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CompetenceEndpointTests(ApiFactory factory)
{
    private static readonly (string Method, string Route)[] Expected =
    [
        ("GET", "/api/ledger/competence/"),
        ("PUT", "/api/ledger/competence/closing-day"),
        ("POST", "/api/ledger/competence/close"),
        ("DELETE", "/api/ledger/competence/close/{month}"),
    ];

    [Theory]
    [MemberData(nameof(ExpectedRoutes))]
    public void Rota_de_competencia_esta_mapeada(string method, string route)
    {
        // Forca a composicao do host antes de ler a tabela de rotas.
        _ = factory.CreateClient();

        var endpoints = factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => (
                Methods: e.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.IHttpMethodMetadata>()?.HttpMethods ?? [],
                Pattern: e.RoutePattern.RawText ?? string.Empty))
            .ToList();

        Assert.Contains(
            endpoints,
            e => e.Pattern.Equals(route, StringComparison.OrdinalIgnoreCase) && e.Methods.Contains(method));
    }

    public static TheoryData<string, string> ExpectedRoutes()
    {
        var data = new TheoryData<string, string>();

        foreach (var (method, route) in Expected)
        {
            data.Add(method, route);
        }

        return data;
    }
}
