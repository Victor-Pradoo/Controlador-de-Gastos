using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Recurrences.Contracts;
using ControleDeGastos.Modules.Recurrences.Domain;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace ControleDeGastos.Modules.Recurrences.Application;

/// <summary>
/// Cadastro de gastos fixos e sua materializacao no Ledger.
/// Substitui o injectFixedThisMonth() do app legado, agora idempotente e por usuario.
/// </summary>
public sealed class FixedExpenseService(
    IFixedExpenseRepository repository,
    IRecurrencesUnitOfWork unitOfWork,
    ILedgerModuleApi ledger,
    ILogger<FixedExpenseService> logger) : IRecurrencesModuleApi
{
    public async Task<IReadOnlyList<FixedExpenseDto>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await repository.ListAsync(userId, onlyActive: true, cancellationToken);

        return items
            .Select(f => new FixedExpenseDto(f.Id, f.Description, f.Amount.Amount, f.Category, f.DayOfMonth, f.IsActive))
            .ToList();
    }

    public async Task<Result<Guid>> AddAsync(
        Guid userId,
        string description,
        decimal amount,
        string category,
        int dayOfMonth,
        CancellationToken cancellationToken = default)
    {
        var result = FixedExpense.Create(userId, description, amount, category, dayOfMonth);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        repository.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }

    public async Task<Result> DeactivateAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var fixedExpense = await repository.GetAsync(userId, id, cancellationToken);
        if (fixedExpense is null)
        {
            return Result.Failure(RecurrenceErrors.NotFound);
        }

        // Desativar preserva o historico ja lancado; o legado apagava tudo retroativamente.
        fixedExpense.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<int> MaterializeAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default)
    {
        // A janela vem pronta do Ledger, dono da regra: uma leitura para a competencia
        // inteira, e nao uma por gasto fixo - nem uma copia da regra deste lado.
        var window = await ledger.GetCompetenceWindowAsync(userId, month, cancellationToken);

        if (window.IsEmpty)
        {
            // Duas competencias encerradas no mesmo dia deixam a do meio sem nenhuma
            // data possivel - nao ha onde colocar o lancamento.
            return 0;
        }

        var items = await repository.ListAsync(userId, onlyActive: true, cancellationToken);
        var materialized = 0;

        foreach (var item in items)
        {
            var request = new RegisterTransactionRequest(
                userId,
                TransactionKind.FixedExpense,
                TransactionSource.Recurrence,
                item.Description,
                item.Amount.Amount,
                item.Category,
                item.OccurrenceDate(window),
                item.ExternalKeyFor(month),
                item.Id);

            // Upsert por (gasto fixo, competencia): cria se falta e reposiciona a data
            // se a regra de virada mudou. Re-executar e sempre seguro.
            var result = await ledger.SyncRecurrenceOccurrenceAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                materialized++;
            }
            else
            {
                logger.LogWarning(
                    "Falha ao materializar gasto fixo {FixedExpenseId} em {Month}: {Error}",
                    item.Id,
                    month,
                    result.Error.Message);
            }
        }

        return materialized;
    }

    public Task<IReadOnlyList<Guid>> ListUserIdsWithActiveExpensesAsync(CancellationToken cancellationToken = default) =>
        repository.ListUserIdsWithActiveExpensesAsync(cancellationToken);
}
