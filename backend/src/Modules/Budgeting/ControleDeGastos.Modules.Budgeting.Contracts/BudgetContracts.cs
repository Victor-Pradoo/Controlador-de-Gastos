using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Budgeting.Contracts;

/// <summary>
/// Visao de orcamento do mes - e exatamente o que a tela inicial mostra:
/// quanto sobra, quanto ja foi, e o quao perto do limite o usuario esta.
/// </summary>
public sealed record MonthlyBudgetDto(
    YearMonth Month,
    decimal Salary,
    decimal ReserveRate,
    decimal ReserveAmount,
    decimal Available,
    decimal FixedExpenses,
    decimal VariableExpenses,
    decimal Income,
    decimal NetSpent,
    decimal Balance,
    decimal ConsumedPercentage,
    BudgetHealth Health);

/// <summary>Semaforo do app legado: verde ate 70%, amarelo ate 90%, vermelho acima.</summary>
public enum BudgetHealth
{
    Healthy = 1,
    Warning = 2,
    Critical = 3,
}

public sealed record BudgetSettingsDto(decimal Salary, decimal ReserveRate);

public interface IBudgetingModuleApi
{
    Task<MonthlyBudgetDto> GetMonthlyBudgetAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);
}
