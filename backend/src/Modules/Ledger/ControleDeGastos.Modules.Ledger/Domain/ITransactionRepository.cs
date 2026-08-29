using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Domain;

public interface ITransactionRepository
{
    Task<Transaction?> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> ListByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default);

    Task<int> DeleteByRecurrenceAsync(Guid userId, Guid recurrenceId, CancellationToken cancellationToken = default);

    void Add(Transaction transaction);

    void Remove(Transaction transaction);
}
