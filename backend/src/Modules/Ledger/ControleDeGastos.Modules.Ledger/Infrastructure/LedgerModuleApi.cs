using ControleDeGastos.Modules.Ledger.Application.Competence;
using ControleDeGastos.Modules.Ledger.Application.Transactions;
using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Infrastructure;

/// <summary>
/// Fachada que implementa a API publica do modulo. E o unico tipo do Ledger
/// que outros modulos resolvem do container.
/// </summary>
internal sealed class LedgerModuleApi(
    RegisterTransactionHandler registerHandler,
    SyncRecurrenceOccurrenceHandler syncRecurrenceHandler,
    CompetenceService competence,
    IClock clock,
    LedgerQueries queries,
    ITransactionRepository repository) : ILedgerModuleApi
{
    public Task<Result<Guid>> RegisterAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default) =>
        registerHandler.HandleAsync(request, cancellationToken);

    public Task<Result> SyncRecurrenceOccurrenceAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default) =>
        syncRecurrenceHandler.HandleAsync(request, cancellationToken);

    public async Task<CompetenceCalendarDto> GetCompetenceCalendarAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var calendar = await competence.GetCalendarAsync(userId, cancellationToken);
        var current = calendar.Current(clock.Today);

        return new CompetenceCalendarDto(
            calendar.ClosingDay,
            current,
            CompetenceService.ToDto(calendar.WindowOf(current)),
            calendar.Closures);
    }

    public Task<YearMonth> GetCurrentCompetenceAsync(Guid userId, CancellationToken cancellationToken = default) =>
        competence.GetCurrentAsync(userId, cancellationToken);

    public async Task<CompetenceWindowDto> GetCompetenceWindowAsync(
        Guid userId,
        YearMonth month,
        CancellationToken cancellationToken = default)
    {
        var calendar = await competence.GetCalendarAsync(userId, cancellationToken);
        return CompetenceService.ToDto(calendar.WindowOf(month));
    }

    public Task<IReadOnlyList<TransactionDto>> GetByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        queries.GetByMonthAsync(userId, month, cancellationToken);

    public Task<MonthlyTotalsDto> GetMonthlyTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        queries.GetMonthlyTotalsAsync(userId, month, cancellationToken);

    public Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        queries.GetCategoryTotalsAsync(userId, month, cancellationToken);

    public Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        repository.ExistsByExternalIdAsync(userId, externalId, cancellationToken);
}
