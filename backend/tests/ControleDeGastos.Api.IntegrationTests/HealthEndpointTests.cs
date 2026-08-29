using System.Net;
using System.Net.Http.Json;
using System.Text.Json;


namespace ControleDeGastos.Api.IntegrationTests;

/// <summary>
/// Smoke test da composicao: sobe o host de verdade e confirma que os cinco modulos
/// foram registrados. Nao toca o banco - falha aqui significa erro de DI ou de rota.
/// </summary>
public sealed class HealthEndpointTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_responde_ok_com_os_modulos_registrados()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var modules = payload.GetProperty("modules").EnumerateArray().Select(m => m.GetString()).ToArray();

        Assert.Equal("ok", payload.GetProperty("status").GetString());
        Assert.Contains("ledger", modules);
        Assert.Contains("budgeting", modules);
        Assert.Contains("recurrences", modules);
        Assert.Contains("categorization", modules);
        Assert.Contains("banking", modules);
    }

    [Fact]
    public async Task Catalogo_de_categorias_responde_sem_banco()
    {
        var client = factory.CreateClient();

        // Catalogo e estatico no modulo Categorization: bom canario para roteamento.
        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
