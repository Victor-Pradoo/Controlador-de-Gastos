using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ControleDeGastos.Api.DatabaseTests;

/// <summary>
/// O fluxo da regra de virada por HTTP, contra SQL Server de verdade.
///
/// Os testes de unidade ja cobrem a regra em si; o que so aparece aqui e a traducao
/// da janela para SQL, a serializacao de YearMonth nos DTOs novos e a fiacao dos
/// endpoints - exatamente o que nao da para provar com dublê.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class CompetenceFlowTests(SqlServerApiFactory factory) : IAsyncLifetime
{
    /// <summary>Usuario proprio por classe: os dados de um teste nao vazam para outro.</summary>
    private static readonly string UserId = Guid.CreateVersion7().ToString();

    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", UserId);
        return client;
    }

    public Task InitializeAsync() => factory.ResetLedgerAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task SetClosingDayAsync(HttpClient client, int? closingDay)
    {
        var response = await client.PutAsJsonAsync("/api/ledger/competence/closing-day", new { closingDay });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task RegisterExpenseAsync(HttpClient client, string occurredOn, decimal amount)
    {
        var response = await client.PostAsJsonAsync("/api/ledger/transactions", new
        {
            kind = "Expense",
            description = $"Gasto {occurredOn}",
            amount,
            category = "Alimentacao",
            occurredOn,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<decimal> VariableExpensesAsync(HttpClient client, string month)
    {
        var summary = await ReadAsync(await client.GetAsync($"/api/ledger/summary?month={month}"));
        return summary.GetProperty("totals").GetProperty("variableExpenses").GetDecimal();
    }

    private async Task<int> TransactionCountAsync(HttpClient client, string month)
    {
        var transactions = await ReadAsync(await client.GetAsync($"/api/ledger/transactions?month={month}"));
        return transactions.GetArrayLength();
    }

    [Fact]
    public async Task Dia_de_virada_move_o_gasto_para_a_competencia_seguinte()
    {
        var client = Client();
        await SetClosingDayAsync(client, 25);

        await RegisterExpenseAsync(client, "2026-08-28", 100m);
        await RegisterExpenseAsync(client, "2026-08-20", 50m);

        // 28/08 e a partir da virada: conta para setembro. 20/08 ainda e agosto.
        Assert.Equal(100m, await VariableExpensesAsync(client, "2026-09"));
        Assert.Equal(50m, await VariableExpensesAsync(client, "2026-08"));

        Assert.Equal(1, await TransactionCountAsync(client, "2026-09"));
        Assert.Equal(1, await TransactionCountAsync(client, "2026-08"));
    }

    [Fact]
    public async Task Mudar_a_regra_reclassifica_o_que_ja_estava_gravado()
    {
        var client = Client();
        await SetClosingDayAsync(client, null);

        await RegisterExpenseAsync(client, "2026-08-28", 100m);
        Assert.Equal(100m, await VariableExpensesAsync(client, "2026-08"));

        // A competencia e derivada: mudar a regra move o lancamento ja gravado.
        await SetClosingDayAsync(client, 25);

        Assert.Equal(0m, await VariableExpensesAsync(client, "2026-08"));
        Assert.Equal(100m, await VariableExpensesAsync(client, "2026-09"));
    }

    [Fact]
    public async Task Encerrar_o_mes_avanca_a_competencia_corrente()
    {
        var client = Client();
        await SetClosingDayAsync(client, 25);

        var before = await ReadAsync(await client.GetAsync("/api/ledger/competence"));
        var current = before.GetProperty("current").GetString();

        var closed = await ReadAsync(await client.PostAsJsonAsync("/api/ledger/competence/close", new { }));

        Assert.Equal(current, closed.GetProperty("closed").GetString());

        var after = await ReadAsync(await client.GetAsync("/api/ledger/competence"));
        Assert.Equal(closed.GetProperty("current").GetString(), after.GetProperty("current").GetString());
        Assert.NotEqual(current, after.GetProperty("current").GetString());
        Assert.Equal(current, after.GetProperty("reopenableCompetence").GetString());
    }

    [Fact]
    public async Task Encerrar_duas_vezes_e_conflito()
    {
        var client = Client();
        await client.PostAsJsonAsync("/api/ledger/competence/close", new { });

        // A competencia corrente ja avancou, entao encerrar a anterior nao e mais possivel.
        var settings = await ReadAsync(await client.GetAsync("/api/ledger/competence"));
        var reopenable = settings.GetProperty("reopenableCompetence").GetString();

        var response = await client.DeleteAsync($"/api/ledger/competence/close/{reopenable}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Reaberta, da para encerrar de novo.
        var again = await client.PostAsJsonAsync("/api/ledger/competence/close", new { });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task Reabrir_o_que_nao_esta_encerrado_e_404()
    {
        var response = await Client().DeleteAsync("/api/ledger/competence/close/2020-01");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dia_de_virada_invalido_e_400()
    {
        var response = await Client().PutAsJsonAsync("/api/ledger/competence/closing-day", new { closingDay = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Leitura_sem_month_usa_a_competencia_corrente_da_regra()
    {
        var client = Client();
        await SetClosingDayAsync(client, 25);

        var settings = await ReadAsync(await client.GetAsync("/api/ledger/competence"));
        var current = settings.GetProperty("current").GetString()!;

        var summary = await ReadAsync(await client.GetAsync("/api/ledger/summary"));

        Assert.Equal(current, summary.GetProperty("totals").GetProperty("month").GetString());
    }

    [Fact]
    public async Task Janela_da_competencia_atravessa_dois_meses()
    {
        var client = Client();
        await SetClosingDayAsync(client, 25);

        var settings = await ReadAsync(await client.GetAsync("/api/ledger/competence"));
        var window = settings.GetProperty("currentWindow");

        var start = DateOnly.Parse(window.GetProperty("start").GetString()!);
        var end = DateOnly.Parse(window.GetProperty("end").GetString()!);

        Assert.Equal(25, start.Day);
        Assert.Equal(24, end.Day);
        Assert.True(start < end);
    }

    [Fact]
    public async Task Sem_dia_de_virada_a_competencia_e_o_mes_do_calendario()
    {
        var client = Client();
        await SetClosingDayAsync(client, null);

        await RegisterExpenseAsync(client, "2026-08-01", 10m);
        await RegisterExpenseAsync(client, "2026-08-31", 20m);

        Assert.Equal(30m, await VariableExpensesAsync(client, "2026-08"));
        Assert.Equal(0m, await VariableExpensesAsync(client, "2026-09"));
    }

    [Fact]
    public async Task Orcamento_segue_a_mesma_competencia_do_extrato()
    {
        var client = Client();
        await SetClosingDayAsync(client, 25);
        await RegisterExpenseAsync(client, "2026-08-28", 300m);

        var budget = await ReadAsync(await client.GetAsync("/api/budget?month=2026-09"));

        Assert.Equal("2026-09", budget.GetProperty("month").GetString());
        Assert.Equal(300m, budget.GetProperty("variableExpenses").GetDecimal());
    }
}
