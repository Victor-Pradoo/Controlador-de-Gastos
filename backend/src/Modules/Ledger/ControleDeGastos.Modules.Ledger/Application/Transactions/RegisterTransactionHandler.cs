using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.SharedKernel.Messaging;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Application.Transactions;

/// <summary>
/// Caso de uso unico de registro: serve tanto o POST /ledger/transactions quanto
/// os modulos Recurrences e Banking (via <see cref="ILedgerModuleApi"/>).
/// </summary>
public sealed class RegisterTransactionHandler(
    ITransactionRepository repository,
    ILedgerUnitOfWork unitOfWork,
    IEventBus eventBus)
{
    public async Task<Result<Guid>> HandleAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.ExternalId)
            && await repository.ExistsByExternalIdAsync(request.UserId, request.ExternalId, cancellationToken))
        {
            return Result.Failure<Guid>(LedgerErrors.DuplicatedExternalId);
        }

        var result = Transaction.Register(
            request.UserId,
            request.Kind,
            request.Source,
            request.Description,
            request.Amount,
            request.Category,
            request.OccurredOn,
            request.ExternalId,
            request.RecurrenceId);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var transaction = result.Value;
        repository.Add(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(
            new TransactionRegisteredIntegrationEvent(
                transaction.Id,
                transaction.UserId,
                transaction.Kind,
                transaction.Amount.Amount,
                transaction.Category,
                transaction.OccurredOn),
            cancellationToken);

        transaction.ClearDomainEvents();

        return transaction.Id;
    }
}
