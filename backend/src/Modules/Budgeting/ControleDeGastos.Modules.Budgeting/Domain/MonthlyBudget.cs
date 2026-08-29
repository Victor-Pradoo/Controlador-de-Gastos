using ControleDeGastos.Modules.Budgeting.Contracts;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Budgeting.Domain;

/// <summary>
/// Regra central do produto, isolada e pura: dado o orcamento e os totais do mes,
/// quanto sobra e qual o nivel de alerta. Sem I/O - e o que os testes cobrem.
/// </summary>
public static class MonthlyBudget
{
    public const decimal WarningThreshold = 70m;
    public const decimal CriticalThreshold = 90m;

    public static MonthlyBudgetDto Calculate(
        YearMonth month,
        BudgetSettings settings,
        decimal variableExpenses,
        decimal fixedExpenses,
        decimal income)
    {
        var available = settings.Available.Amount;
        var netSpent = variableExpenses + fixedExpenses - income;
        var balance = available - netSpent;

        var consumed = available > 0m
            ? decimal.Round(netSpent / available * 100m, 1)
            : 0m;

        // Acima de 100% o semaforo ja esta vermelho; limitar mantem a barra da UI sa.
        var consumedForDisplay = Math.Clamp(consumed, 0m, 100m);

        var health = consumed switch
        {
            < WarningThreshold => BudgetHealth.Healthy,
            < CriticalThreshold => BudgetHealth.Warning,
            _ => BudgetHealth.Critical,
        };

        return new MonthlyBudgetDto(
            month,
            settings.Salary.Amount,
            settings.ReserveRate,
            settings.ReserveAmount.Amount,
            available,
            fixedExpenses,
            variableExpenses,
            income,
            netSpent,
            balance,
            consumedForDisplay,
            health);
    }
}
