using ControleDeGastos.Modules.Budgeting.Contracts;
using ControleDeGastos.Modules.Budgeting.Domain;
using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Budgeting.Application;

/// <summary>
/// Junta a configuracao do usuario (deste modulo) com os totais do mes
/// (do Ledger, via contrato publico) e produz a visao de orcamento.
/// </summary>
public sealed class BudgetService(
    IBudgetSettingsRepository repository,
    IBudgetingUnitOfWork unitOfWork,
    ILedgerModuleApi ledger) : IBudgetingModuleApi
{
    public async Task<MonthlyBudgetDto> GetMonthlyBudgetAsync(
        Guid userId,
        YearMonth month,
        CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(userId, cancellationToken) ?? BudgetSettings.Default(userId);
        var totals = await ledger.GetMonthlyTotalsAsync(userId, month, cancellationToken);

        return MonthlyBudget.Calculate(
            month,
            settings,
            totals.VariableExpenses,
            totals.FixedExpenses,
            totals.Income);
    }

    public async Task<BudgetSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(userId, cancellationToken) ?? BudgetSettings.Default(userId);
        return new BudgetSettingsDto(settings.Salary.Amount, settings.ReserveRate);
    }

    public async Task<Result> UpdateSettingsAsync(
        Guid userId,
        decimal salary,
        decimal reserveRate,
        CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(userId, cancellationToken);

        if (settings is null)
        {
            var created = BudgetSettings.Create(userId, salary, reserveRate);
            if (created.IsFailure)
            {
                return Result.Failure(created.Error);
            }

            repository.Add(created.Value);
        }
        else
        {
            var updated = settings.Update(salary, reserveRate);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
