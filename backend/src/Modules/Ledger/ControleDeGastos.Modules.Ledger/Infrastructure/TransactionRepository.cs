using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Infrastructure;

internal sealed class TransactionRepository(LedgerDbContext context) : ITransactionRepository
{
    public Task<Transaction?> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default) =>
        context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default)
    {
        var first = month.FirstDay;
        var last = month.LastDay;

        return await context.Transactions
            .Where(t => t.UserId == userId && t.OccurredOn >= first && t.OccurredOn <= last)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        context.Transactions.AnyAsync(t => t.UserId == userId && t.ExternalId == externalId, cancellationToken);

    public Task<int> DeleteByRecurrenceAsync(Guid userId, Guid recurrenceId, CancellationToken cancellationToken = default) =>
        context.Transactions
            .Where(t => t.UserId == userId && t.RecurrenceId == recurrenceId)
            .ExecuteDeleteAsync(cancellationToken);

    public void Add(Transaction transaction) => context.Transactions.Add(transaction);

    public void Remove(Transaction transaction) => context.Transactions.Remove(transaction);
}
