using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Contracts;

/// <summary>
/// API publica do modulo Ledger. Outros modulos dependem SOMENTE desta interface,
/// nunca do DbContext nem das entidades do Ledger.
/// </summary>
public interface ILedgerModuleApi
{
    Task<Result<Guid>> RegisterAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDto>> GetByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    Task<MonthlyTotalsDto> GetMonthlyTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);

    /// <summary>Ja existe lancamento com este id externo? Evita duplicar na sincronizacao bancaria.</summary>
    Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default);
}
