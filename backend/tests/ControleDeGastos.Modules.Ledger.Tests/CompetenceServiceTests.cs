using ControleDeGastos.Modules.Ledger.Application.Competence;
using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Tests;

/// <summary>
/// Configurar o dia de virada, encerrar o mes e reabrir. Os tres mudam como TODO
/// lancamento e classificado, entao os tres precisam avisar o resto do sistema.
/// </summary>
public sealed class CompetenceServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static (CompetenceService Service, InMemoryCompetenceStore Store, RecordingEventBus Bus, FakeClock Clock) Build(
        DateOnly? today = null)
    {
        var store = new InMemoryCompetenceStore();
        var bus = new RecordingEventBus();
        var clock = new FakeClock(today ?? new DateOnly(2026, 8, 18));

        var service = new CompetenceService(store, store, store, new FakeUnitOfWork(), bus, clock);

        return (service, store, bus, clock);
    }

    [Fact]
    public async Task Dia_de_virada_e_persistido()
    {
        var (service, _, _, _) = Build();

        var result = await service.UpdateClosingDayAsync(UserId, 25);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, (await service.GetSettingsAsync(UserId)).ClosingDay);
    }

    [Fact]
    public async Task Dia_de_virada_invalido_e_rejeitado()
    {
        var (service, _, bus, _) = Build();

        var result = await service.UpdateClosingDayAsync(UserId, 1);

        Assert.True(result.IsFailure);
        Assert.Equal("ledger.invalid_closing_day", result.Error.Code);
        Assert.Empty(bus.Published);
    }

    [Fact]
    public async Task Dia_de_virada_pode_ser_alterado_depois_de_definido()
    {
        var (service, _, _, _) = Build();

        await service.UpdateClosingDayAsync(UserId, 25);
        await service.UpdateClosingDayAsync(UserId, 10);

        Assert.Equal(10, (await service.GetSettingsAsync(UserId)).ClosingDay);
    }

    [Fact]
    public async Task Alterar_o_dia_de_virada_avisa_o_resto_do_sistema()
    {
        var (service, _, bus, _) = Build();

        await service.UpdateClosingDayAsync(UserId, 25);

        var published = Assert.Single(bus.Published);
        Assert.Equal(UserId, Assert.IsType<CompetenceRuleChangedIntegrationEvent>(published).UserId);
    }

    [Fact]
    public async Task Encerrar_o_mes_avanca_a_competencia_corrente()
    {
        var (service, _, _, _) = Build(today: new DateOnly(2026, 8, 18));
        await service.UpdateClosingDayAsync(UserId, 25);

        var result = await service.CloseAsync(UserId, new YearMonth(2026, 8));

        Assert.True(result.IsSuccess);
        Assert.Equal(new YearMonth(2026, 9), result.Value);
        Assert.Equal(new YearMonth(2026, 9), await service.GetCurrentAsync(UserId));
    }

    [Fact]
    public async Task Encerrar_registra_a_data_de_hoje()
    {
        var (service, store, _, _) = Build(today: new DateOnly(2026, 8, 18));

        await service.CloseAsync(UserId, new YearMonth(2026, 8));

        var closure = await store.GetAsync(UserId, new YearMonth(2026, 8));
        Assert.Equal(new DateOnly(2026, 8, 18), closure!.ClosedOn);
    }

    [Fact]
    public async Task So_a_competencia_corrente_pode_ser_encerrada()
    {
        var (service, _, _, _) = Build(today: new DateOnly(2026, 8, 18));

        var passada = await service.CloseAsync(UserId, new YearMonth(2026, 7));
        var futura = await service.CloseAsync(UserId, new YearMonth(2026, 9));

        Assert.Equal("ledger.competence_not_current", passada.Error.Code);
        Assert.Equal("ledger.competence_not_current", futura.Error.Code);
    }

    [Fact]
    public async Task Encerrar_duas_vezes_e_conflito_e_preserva_a_data_original()
    {
        var (service, store, _, clock) = Build(today: new DateOnly(2026, 8, 18));
        await service.CloseAsync(UserId, new YearMonth(2026, 8));

        // A competencia corrente ja avancou; encerrar 2026-08 de novo nao e mais possivel.
        clock.Today = new DateOnly(2026, 8, 20);
        var again = await service.CloseAsync(UserId, new YearMonth(2026, 8));

        Assert.True(again.IsFailure);
        Assert.Equal(new DateOnly(2026, 8, 18), (await store.GetAsync(UserId, new YearMonth(2026, 8)))!.ClosedOn);
    }

    [Fact]
    public async Task Encerrar_duas_competencias_no_mesmo_dia()
    {
        var (service, _, _, _) = Build(today: new DateOnly(2026, 8, 18));
        await service.UpdateClosingDayAsync(UserId, 25);

        await service.CloseAsync(UserId, new YearMonth(2026, 8));
        var second = await service.CloseAsync(UserId, new YearMonth(2026, 9));

        Assert.True(second.IsSuccess);
        Assert.Equal(new YearMonth(2026, 10), await service.GetCurrentAsync(UserId));
    }

    [Fact]
    public async Task Reabrir_desfaz_o_encerramento()
    {
        var (service, _, _, clock) = Build(today: new DateOnly(2026, 8, 18));
        await service.UpdateClosingDayAsync(UserId, 25);
        await service.CloseAsync(UserId, new YearMonth(2026, 8));

        clock.Today = new DateOnly(2026, 8, 20);
        var result = await service.ReopenAsync(UserId, new YearMonth(2026, 8));

        Assert.True(result.IsSuccess);
        Assert.Equal(new YearMonth(2026, 8), await service.GetCurrentAsync(UserId));
    }

    [Fact]
    public async Task Reabrir_competencia_nao_encerrada_e_not_found()
    {
        var (service, _, _, _) = Build();

        var result = await service.ReopenAsync(UserId, new YearMonth(2026, 8));

        Assert.True(result.IsFailure);
        Assert.Equal("ledger.competence_not_closed", result.Error.Code);
    }

    [Fact]
    public async Task Encerrar_e_reabrir_tambem_avisam_o_resto_do_sistema()
    {
        var (service, _, bus, _) = Build(today: new DateOnly(2026, 8, 18));

        await service.CloseAsync(UserId, new YearMonth(2026, 8));
        await service.ReopenAsync(UserId, new YearMonth(2026, 8));

        Assert.Equal(2, bus.Published.OfType<CompetenceRuleChangedIntegrationEvent>().Count());
    }

    [Fact]
    public async Task Configuracao_expoe_a_janela_da_competencia_corrente()
    {
        var (service, _, _, _) = Build(today: new DateOnly(2026, 8, 28));
        await service.UpdateClosingDayAsync(UserId, 25);

        var settings = await service.GetSettingsAsync(UserId);

        Assert.Equal(new YearMonth(2026, 9), settings.Current);
        Assert.Equal(new DateOnly(2026, 8, 25), settings.CurrentWindow.Start);
        Assert.Equal(new DateOnly(2026, 9, 24), settings.CurrentWindow.End);
    }

    [Fact]
    public async Task Configuracao_oferece_reabertura_so_quando_houve_encerramento()
    {
        var (service, _, _, clock) = Build(today: new DateOnly(2026, 8, 18));
        await service.UpdateClosingDayAsync(UserId, 25);

        Assert.Null((await service.GetSettingsAsync(UserId)).ReopenableCompetence);

        await service.CloseAsync(UserId, new YearMonth(2026, 8));
        clock.Today = new DateOnly(2026, 8, 20);

        var settings = await service.GetSettingsAsync(UserId);
        Assert.Equal(new YearMonth(2026, 8), settings.ReopenableCompetence);
        Assert.Equal(new DateOnly(2026, 8, 18), settings.ReopenableClosedOn);
    }

    [Fact]
    public async Task Usuario_sem_configuracao_usa_o_mes_do_calendario()
    {
        var (service, _, _, _) = Build(today: new DateOnly(2026, 8, 28));

        var settings = await service.GetSettingsAsync(UserId);

        Assert.Null(settings.ClosingDay);
        Assert.Equal(new YearMonth(2026, 8), settings.Current);
        Assert.Equal(new DateOnly(2026, 8, 1), settings.CurrentWindow.Start);
        Assert.Equal(new DateOnly(2026, 8, 31), settings.CurrentWindow.End);
    }
}
