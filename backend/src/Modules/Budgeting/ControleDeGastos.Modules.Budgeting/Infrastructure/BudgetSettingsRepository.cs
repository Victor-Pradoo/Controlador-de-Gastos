using ControleDeGastos.Modules.Budgeting.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Budgeting.Infrastructure;

internal sealed class BudgetSettingsRepository(BudgetingDbContext context) : IBudgetSettingsRepository
{
    public Task<BudgetSettings?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Settings.FirstOrDefaultAsync(s => s.Id == userId, cancellationToken);

    public void Add(BudgetSettings settings) => context.Settings.Add(settings);
}
