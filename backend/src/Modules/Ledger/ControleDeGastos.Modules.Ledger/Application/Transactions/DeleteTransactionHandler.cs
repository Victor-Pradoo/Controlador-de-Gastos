using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.SharedKernel.Messaging;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Application.Transactions;

public sealed class DeleteTransactionHandler(
    ITransactionRepository repository,
    ILedgerUnitOfWork unitOfWork,
    IEventBus eventBus)
{
    public async Task<Result> HandleAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await repository.GetAsync(userId, transactionId, cancellationToken);
        if (transaction is null)
        {
            return Result.Failure(LedgerErrors.NotFound);
        }

        if (!transaction.IsEditable)
        {
            return Result.Failure(LedgerErrors.NotEditable);
        }

        repository.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(
            new TransactionRemovedIntegrationEvent(transaction.Id, transaction.UserId, transaction.OccurredOn),
            cancellationToken);

        return Result.Success();
    }
}
