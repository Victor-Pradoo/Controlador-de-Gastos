using ControleDeGastos.Modules.Ledger.Application.Transactions;
using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Infrastructure;

/// <summary>
/// Fachada que implementa a API publica do modulo. E o unico tipo do Ledger
/// que outros modulos resolvem do container.
/// </summary>
internal sealed class LedgerModuleApi(
    RegisterTransactionHandler registerHandler,
    LedgerQueries queries,
    ITransactionRepository repository) : ILedgerModuleApi
{
    public Task<Result<Guid>> RegisterAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default) =>
        registerHandler.HandleAsync(request, cancellationToken);

    public Task<IReadOnlyList<TransactionDto>> GetByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        queries.GetByMonthAsync(userId, month, cancellationToken);

    public Task<MonthlyTotalsDto> GetMonthlyTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        queries.GetMonthlyTotalsAsync(userId, month, cancellationToken);

    public Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        queries.GetCategoryTotalsAsync(userId, month, cancellationToken);

    public Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        repository.ExistsByExternalIdAsync(userId, externalId, cancellationToken);
}
