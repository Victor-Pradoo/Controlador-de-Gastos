using ControleDeGastos.SharedKernel.Abstractions;

namespace ControleDeGastos.Modules.Budgeting.Domain;

public interface IBudgetSettingsRepository
{
    Task<BudgetSettings?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(BudgetSettings settings);
}

public interface IBudgetingUnitOfWork : IUnitOfWork;
