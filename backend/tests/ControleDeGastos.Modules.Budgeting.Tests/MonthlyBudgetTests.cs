using ControleDeGastos.Modules.Budgeting.Contracts;
using ControleDeGastos.Modules.Budgeting.Domain;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Budgeting.Tests;

/// <summary>
/// A regra que o usuario ve na tela inicial. Pura, sem I/O - o motivo de
/// MonthlyBudget ter sido isolado do servico.
/// </summary>
public sealed class MonthlyBudgetTests
{
    private static readonly YearMonth August = new(2026, 8);

    private static BudgetSettings SettingsWith(decimal salary, decimal reserveRate) =>
        BudgetSettings.Create(Guid.NewGuid(), salary, reserveRate).Value;

    [Fact]
    public void Reserva_sai_do_salario_antes_do_disponivel()
    {
        var settings = SettingsWith(5000m, 20m);

        var budget = MonthlyBudget.Calculate(August, settings, variableExpenses: 0m, fixedExpenses: 0m, income: 0m);

        Assert.Equal(1000m, budget.ReserveAmount);
        Assert.Equal(4000m, budget.Available);
        Assert.Equal(4000m, budget.Balance);
    }

    [Fact]
    public void Entradas_do_mes_abatem_o_total_gasto()
    {
        var settings = SettingsWith(5000m, 20m);

        var budget = MonthlyBudget.Calculate(August, settings, variableExpenses: 1200m, fixedExpenses: 800m, income: 500m);

        Assert.Equal(1500m, budget.NetSpent);
        Assert.Equal(2500m, budget.Balance);
    }

    [Theory]
    [InlineData(0, BudgetHealth.Healthy)]
    [InlineData(2000, BudgetHealth.Healthy)]      // 50% de 4000
    [InlineData(3000, BudgetHealth.Warning)]      // 75%
    [InlineData(3800, BudgetHealth.Critical)]     // 95%
    [InlineData(5000, BudgetHealth.Critical)]     // estourou
    public void Semaforo_acompanha_o_percentual_consumido(decimal spent, BudgetHealth expected)
    {
        var settings = SettingsWith(5000m, 20m);

        var budget = MonthlyBudget.Calculate(August, settings, variableExpenses: spent, fixedExpenses: 0m, income: 0m);

        Assert.Equal(expected, budget.Health);
    }

    [Fact]
    public void Percentual_exibido_nunca_passa_de_cem()
    {
        var settings = SettingsWith(1000m, 0m);

        var budget = MonthlyBudget.Calculate(August, settings, variableExpenses: 3000m, fixedExpenses: 0m, income: 0m);

        Assert.Equal(100m, budget.ConsumedPercentage);
        Assert.Equal(-2000m, budget.Balance);
        Assert.Equal(BudgetHealth.Critical, budget.Health);
    }

    [Fact]
    public void Sem_salario_configurado_nao_divide_por_zero()
    {
        var settings = BudgetSettings.Default(Guid.NewGuid());

        var budget = MonthlyBudget.Calculate(August, settings, variableExpenses: 300m, fixedExpenses: 0m, income: 0m);

        Assert.Equal(0m, budget.ConsumedPercentage);
        Assert.Equal(-300m, budget.Balance);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Taxa_de_reserva_fora_do_intervalo_e_rejeitada(decimal reserveRate)
    {
        var result = BudgetSettings.Create(Guid.NewGuid(), 5000m, reserveRate);

        Assert.True(result.IsFailure);
        Assert.Equal("budgeting.invalid_reserve_rate", result.Error.Code);
    }
}
