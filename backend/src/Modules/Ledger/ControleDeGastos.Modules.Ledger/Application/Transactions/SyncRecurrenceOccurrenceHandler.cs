using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Application.Transactions;

/// <summary>
/// Upsert da ocorrencia mensal de um gasto fixo, identificada pelo ExternalId
/// (<c>recurrence:{id}:{yyyy-MM}</c>) - ou seja, pelo par (gasto fixo, competencia).
///
/// Existe separado de <see cref="RegisterTransactionHandler"/> porque a data da
/// ocorrencia depende da regra de virada, que o usuario pode mudar depois: quando isso
/// acontece, a ocorrencia precisa ser MOVIDA de volta para dentro da janela. Rejeitar
/// como duplicado (o comportamento de RegisterAsync, correto para banco e para
/// lancamento manual) deixaria a competencia sem o fixo, para sempre.
/// </summary>
public sealed class SyncRecurrenceOccurrenceHandler(
    ITransactionRepository repository,
    ILedgerUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(
        RegisterTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            return Result.Failure(LedgerErrors.ExternalIdRequired);
        }

        var existing = await repository.GetByExternalIdAsync(request.UserId, request.ExternalId, cancellationToken);

        if (existing is null)
        {
            var created = Transaction.Register(
                request.UserId,
                request.Kind,
                request.Source,
                request.Description,
                request.Amount,
                request.Category,
                request.OccurredOn,
                request.ExternalId,
                request.RecurrenceId);

            if (created.IsFailure)
            {
                return Result.Failure(created.Error);
            }

            repository.Add(created.Value);
            created.Value.ClearDomainEvents();

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        if (existing.OccurredOn == request.OccurredOn)
        {
            return Result.Success();
        }

        var moved = existing.MoveTo(request.OccurredOn);
        if (moved.IsFailure)
        {
            return moved;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
