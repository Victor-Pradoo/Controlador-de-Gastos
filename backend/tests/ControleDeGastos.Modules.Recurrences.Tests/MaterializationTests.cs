using ControleDeGastos.Modules.Recurrences.Application;
using ControleDeGastos.Modules.Recurrences.Domain;
using ControleDeGastos.SharedKernel.Primitives;
using Microsoft.Extensions.Logging.Abstractions;

namespace ControleDeGastos.Modules.Recurrences.Tests;

/// <summary>
/// A materializacao tem que colocar o gasto fixo na competencia certa mesmo quando a
/// competencia nao e o mes do calendario, e tem que se reconciliar sozinha quando o
/// usuario muda a regra depois - senao a competencia fica sem o fixo.
/// </summary>
public sealed class MaterializationTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static (FixedExpenseService Service, FakeLedger Ledger, InMemoryFixedExpenseRepository Repository) Build(
        DateOnly today,
        int? closingDay = null,
        params int[] dueDays)
    {
        var repository = new InMemoryFixedExpenseRepository();

        foreach (var dueDay in dueDays)
        {
            repository.Add(FixedExpense.Create(UserId, $"Fixo dia {dueDay}", 100m, "Moradia", dueDay).Value);
        }

        var ledger = new FakeLedger(today, closingDay);

        var service = new FixedExpenseService(
            repository,
            new FakeUnitOfWork(),
            ledger,
            NullLogger<FixedExpenseService>.Instance);

        return (service, ledger, repository);
    }

    [Fact]
    public async Task Fixo_cai_na_competencia_para_a_qual_foi_materializado()
    {
        var (service, ledger, repository) = Build(new DateOnly(2026, 8, 28), closingDay: 25, dueDays: 28);

        var count = await service.MaterializeAsync(UserId, new YearMonth(2026, 9));

        Assert.Equal(1, count);
        var occurrence = ledger.Occurrences[$"recurrence:{repository.Items[0].Id}:2026-09"];
        Assert.Equal(new DateOnly(2026, 8, 28), occurrence.OccurredOn);
    }

    [Fact]
    public async Task Materializar_duas_vezes_nao_duplica()
    {
        var (service, ledger, _) = Build(new DateOnly(2026, 8, 28), closingDay: 25, dueDays: [28, 10]);

        await service.MaterializeAsync(UserId, new YearMonth(2026, 9));
        await service.MaterializeAsync(UserId, new YearMonth(2026, 9));

        Assert.Equal(2, ledger.Occurrences.Count);
    }

    [Fact]
    public async Task Sem_dia_de_virada_o_comportamento_e_o_de_sempre()
    {
        var (service, ledger, repository) = Build(new DateOnly(2026, 9, 15), closingDay: null, dueDays: 10);

        await service.MaterializeAsync(UserId, new YearMonth(2026, 9));

        var occurrence = ledger.Occurrences[$"recurrence:{repository.Items[0].Id}:2026-09"];
        Assert.Equal(new DateOnly(2026, 9, 10), occurrence.OccurredOn);
    }

    [Fact]
    public async Task Janela_vazia_nao_materializa_nada()
    {
        // Duas competencias encerradas no mesmo dia deixam a do meio sem data possivel.
        var (service, ledger, _) = Build(new DateOnly(2026, 8, 18), closingDay: 25, dueDays: 28);
        ledger.Close(new YearMonth(2026, 8), new DateOnly(2026, 8, 18));
        ledger.Close(new YearMonth(2026, 9), new DateOnly(2026, 8, 18));

        var count = await service.MaterializeAsync(UserId, new YearMonth(2026, 9));

        Assert.Equal(0, count);
        Assert.Empty(ledger.Occurrences);
    }

    [Fact]
    public async Task Mudanca_de_regra_reposiciona_a_ocorrencia_deslocada()
    {
        // Sem dia de virada, o fixo do dia 28 foi materializado em 28/08 para 2026-08.
        var (service, ledger, repository) = Build(new DateOnly(2026, 8, 28), closingDay: null, dueDays: 28);
        await service.MaterializeAsync(UserId, new YearMonth(2026, 8));

        var key = $"recurrence:{repository.Items[0].Id}:2026-08";
        Assert.Equal(new DateOnly(2026, 8, 28), ledger.Occurrences[key].OccurredOn);

        // Usuario passa a usar virada no dia 25: 28/08 agora pertence a 2026-09,
        // entao a ocorrencia de 2026-08 precisa recuar para dentro da janela 25/07..24/08.
        ledger.ClosingDay = 25;
        await service.MaterializeAsync(UserId, new YearMonth(2026, 8));

        var repositioned = ledger.Occurrences[key].OccurredOn;
        Assert.Equal(new DateOnly(2026, 7, 28), repositioned);
        Assert.Single(ledger.Occurrences);
    }

    [Fact]
    public async Task Handler_do_evento_reconcilia_a_competencia_corrente_e_a_anterior()
    {
        var (service, ledger, repository) = Build(new DateOnly(2026, 8, 28), closingDay: null, dueDays: 28);
        await service.MaterializeAsync(UserId, new YearMonth(2026, 8));

        ledger.ClosingDay = 25;
        var handler = new CompetenceRuleChangedHandler(service, ledger, NullLogger<CompetenceRuleChangedHandler>.Instance);

        await handler.HandleAsync(new Ledger.Contracts.CompetenceRuleChangedIntegrationEvent(UserId));

        // Corrente e 2026-09 (28/08 com virada 25); a anterior e 2026-08.
        var id = repository.Items[0].Id;
        Assert.Equal(new DateOnly(2026, 7, 28), ledger.Occurrences[$"recurrence:{id}:2026-08"].OccurredOn);
        Assert.Equal(new DateOnly(2026, 8, 28), ledger.Occurrences[$"recurrence:{id}:2026-09"].OccurredOn);
    }

    [Fact]
    public async Task Competencia_corrente_da_materializacao_automatica_segue_a_regra()
    {
        var (_, ledger, _) = Build(new DateOnly(2026, 8, 28), closingDay: 25, dueDays: 28);

        Assert.Equal(new YearMonth(2026, 9), await ledger.GetCurrentCompetenceAsync(UserId));
    }

    [Fact]
    public async Task Gasto_fixo_desativado_nao_e_materializado()
    {
        var (service, ledger, repository) = Build(new DateOnly(2026, 9, 15), dueDays: 10);
        repository.Items[0].Deactivate();

        var count = await service.MaterializeAsync(UserId, new YearMonth(2026, 9));

        Assert.Equal(0, count);
        Assert.Empty(ledger.Occurrences);
    }
}
