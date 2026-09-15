using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Infrastructure;

internal sealed class TransactionRepository(LedgerDbContext context, ICompetenceCalendarProvider calendars) : ITransactionRepository
{
    public Task<Transaction?> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default) =>
        context.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> ListByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default)
    {
        // A competencia e a janela da regra de virada, nao o mes do calendario.
        var calendar = await calendars.GetAsync(userId, cancellationToken);
        var window = calendar.WindowOf(month);

        if (window.IsEmpty)
        {
            return [];
        }

        return await context.Transactions
            .Where(t => t.UserId == userId && t.OccurredOn >= window.Start && t.OccurredOn <= window.End)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        context.Transactions.AnyAsync(t => t.UserId == userId && t.ExternalId == externalId, cancellationToken);

    public Task<Transaction?> GetByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        context.Transactions.FirstOrDefaultAsync(t => t.UserId == userId && t.ExternalId == externalId, cancellationToken);

    public Task<int> DeleteByRecurrenceAsync(Guid userId, Guid recurrenceId, CancellationToken cancellationToken = default) =>
        context.Transactions
            .Where(t => t.UserId == userId && t.RecurrenceId == recurrenceId)
            .ExecuteDeleteAsync(cancellationToken);

    public void Add(Transaction transaction) => context.Transactions.Add(transaction);

    public void Remove(Transaction transaction) => context.Transactions.Remove(transaction);
}
