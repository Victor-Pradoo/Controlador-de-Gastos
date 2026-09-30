using ControleDeGastos.Modules.Ledger.Application.Transactions;
using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.Modules.Ledger.Infrastructure;
using ControleDeGastos.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Tests;

/// <summary>
/// Janela vazia acontece quando duas competencias sao encerradas no mesmo dia: nenhuma
/// data cai na do meio. Consultar essa competencia tem que devolver vazio, e nao
/// estourar nem devolver o mes inteiro por engano.
///
/// O contexto aqui nunca abre conexao - o caminho da janela vazia retorna antes de
/// tocar o banco, e e exatamente isso que este teste fixa. As consultas com janela
/// preenchida sao exercitadas contra banco de verdade nos testes de integracao.
/// </summary>
public sealed class LedgerQueriesTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static LedgerQueries Build(ICompetenceCalendarProvider calendars)
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseNpgsql("Host=nao-conecta;Database=nao-usado;Username=nao-usado")
            .Options;

        return new LedgerQueries(new LedgerDbContext(options), calendars);
    }

    /// <summary>2026-08 e 2026-09 encerradas no mesmo dia deixam 2026-09 sem nenhuma data.</summary>
    private static LedgerQueries WithEmptyWindow()
    {
        var store = new InMemoryCompetenceStore();
        store.Add(CompetenceSettings.Create(UserId, 25).Value);
        store.Add(CompetenceClosure.Create(UserId, new YearMonth(2026, 8), new DateOnly(2026, 8, 18)));
        store.Add(CompetenceClosure.Create(UserId, new YearMonth(2026, 9), new DateOnly(2026, 8, 18)));

        return Build(store);
    }

    [Fact]
    public async Task Janela_vazia_devolve_lista_vazia()
    {
        Assert.Empty(await WithEmptyWindow().GetByMonthAsync(UserId, new YearMonth(2026, 9)));
    }

    [Fact]
    public async Task Janela_vazia_devolve_totais_zerados()
    {
        var totals = await WithEmptyWindow().GetMonthlyTotalsAsync(UserId, new YearMonth(2026, 9));

        Assert.Equal(new YearMonth(2026, 9), totals.Month);
        Assert.Equal(0m, totals.VariableExpenses);
        Assert.Equal(0m, totals.FixedExpenses);
        Assert.Equal(0m, totals.Income);
        Assert.Equal(0m, totals.NetSpent);
    }

    [Fact]
    public async Task Janela_vazia_devolve_categorias_vazias()
    {
        Assert.Empty(await WithEmptyWindow().GetCategoryTotalsAsync(UserId, new YearMonth(2026, 9)));
    }
}
