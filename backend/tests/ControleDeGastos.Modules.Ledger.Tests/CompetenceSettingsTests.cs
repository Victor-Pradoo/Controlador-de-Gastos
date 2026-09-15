using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Tests;

public sealed class CompetenceSettingsTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Usuario_sem_configuracao_nao_tem_dia_de_virada()
    {
        Assert.Null(CompetenceSettings.Default(UserId).ClosingDay);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(25)]
    [InlineData(31)]
    [InlineData(null)]
    public void Dia_de_virada_valido_e_aceito(int? closingDay)
    {
        var result = CompetenceSettings.Create(UserId, closingDay);

        Assert.True(result.IsSuccess);
        Assert.Equal(closingDay, result.Value.ClosingDay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(-5)]
    public void Dia_de_virada_fora_do_intervalo_e_rejeitado(int closingDay)
    {
        var result = CompetenceSettings.Create(UserId, closingDay);

        Assert.True(result.IsFailure);
        Assert.Equal("ledger.invalid_closing_day", result.Error.Code);
    }

    [Fact]
    public void Dia_de_virada_invalido_preserva_a_configuracao_anterior()
    {
        var settings = CompetenceSettings.Create(UserId, 25).Value;

        var result = settings.ChangeClosingDay(1);

        Assert.True(result.IsFailure);
        Assert.Equal(25, settings.ClosingDay);
    }

    [Fact]
    public void Desligar_a_virada_volta_para_o_mes_do_calendario()
    {
        var settings = CompetenceSettings.Create(UserId, 25).Value;

        Assert.True(settings.ChangeClosingDay(null).IsSuccess);
        Assert.Null(settings.ClosingDay);
    }

    [Fact]
    public void O_id_do_agregado_e_o_usuario()
    {
        Assert.Equal(UserId, CompetenceSettings.Default(UserId).Id);
    }

    [Fact]
    public void Encerramento_guarda_a_competencia_e_a_data()
    {
        var closure = CompetenceClosure.Create(UserId, new YearMonth(2026, 8), new DateOnly(2026, 8, 18));

        Assert.Equal(UserId, closure.UserId);
        Assert.Equal(new YearMonth(2026, 8), closure.Competence);
        Assert.Equal(2026, closure.Year);
        Assert.Equal(8, closure.Month);
        Assert.Equal(new DateOnly(2026, 8, 18), closure.ClosedOn);
    }
}
