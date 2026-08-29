using ControleDeGastos.Modules.Recurrences.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Recurrences.Infrastructure;

internal sealed class FixedExpenseRepository(RecurrencesDbContext context) : IFixedExpenseRepository
{
    public Task<FixedExpense?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        context.FixedExpenses.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<FixedExpense>> ListAsync(
        Guid userId,
        bool onlyActive = true,
        CancellationToken cancellationToken = default) =>
        await context.FixedExpenses
            .Where(f => f.UserId == userId && (!onlyActive || f.IsActive))
            .OrderBy(f => f.DayOfMonth)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListUserIdsWithActiveExpensesAsync(CancellationToken cancellationToken = default) =>
        await context.FixedExpenses
            .Where(f => f.IsActive)
            .Select(f => f.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public void Add(FixedExpense fixedExpense) => context.FixedExpenses.Add(fixedExpense);
}
