using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Recurrences.Domain;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Recurrences.Tests;

/// <summary>
/// Ledger de mentira que aplica a MESMA regra de virada do modulo real - dia de virada
/// mais encerramentos - e guarda as ocorrencias com a semantica de upsert por
/// ExternalId. Sem isso os testes de materializacao so provariam que o servico chama
/// alguem, nao que o gasto fixo cai na competencia certa.
/// </summary>
internal sealed class FakeLedger(DateOnly today, int? closingDay = null) : ILedgerModuleApi
{
    private readonly Dictionary<YearMonth, DateOnly> _closures = [];

    public DateOnly Today { get; set; } = today;

    public int? ClosingDay { get; set; } = closingDay;

    /// <summary>Ocorrencias gravadas, por ExternalId - o par (gasto fixo, competencia).</summary>
    public Dictionary<string, RegisterTransactionRequest> Occurrences { get; } = [];

    public void Close(YearMonth competence, DateOnly closedOn) => _closures[competence] = closedOn;

    public Task<Result> SyncRecurrenceOccurrenceAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default)
    {
        // Cria quando falta e reposiciona quando a data mudou; nunca duplica.
        Occurrences[request.ExternalId!] = request;
        return Task.FromResult(Result.Success());
    }

    public Task<YearMonth> GetCurrentCompetenceAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Resolve(Today));

    public Task<CompetenceWindowDto> GetCompetenceWindowAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        Task.FromResult(WindowOf(month));

    public Task<CompetenceCalendarDto> GetCompetenceCalendarAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var current = Resolve(Today);
        return Task.FromResult(new CompetenceCalendarDto(ClosingDay, current, WindowOf(current), _closures));
    }

    private YearMonth Resolve(DateOnly date)
    {
        var competence = ClosingDay is null || date.Day < EffectiveDay(YearMonth.From(date))
            ? YearMonth.From(date)
            : YearMonth.From(date).AddMonths(1);

        while (_closures.TryGetValue(competence, out var closedOn) && closedOn <= date)
        {
            competence = competence.AddMonths(1);
        }

        return competence;
    }

    private CompetenceWindowDto WindowOf(YearMonth month)
    {
        var previous = month.AddMonths(-1);

        var start = _closures.TryGetValue(previous, out var previousClosedOn)
            ? previousClosedOn
            : ClosingDay is null
                ? month.FirstDay
                : new DateOnly(previous.Year, previous.Month, EffectiveDay(previous));

        var end = _closures.TryGetValue(month, out var closedOn)
            ? closedOn.AddDays(-1)
            : ClosingDay is null
                ? month.LastDay
                : new DateOnly(month.Year, month.Month, EffectiveDay(month)).AddDays(-1);

        return new CompetenceWindowDto(start, end);
    }

    private int EffectiveDay(YearMonth month) =>
        Math.Min(ClosingDay!.Value, DateTime.DaysInMonth(month.Year, month.Month));

    public Task<Result<Guid>> RegisterAsync(RegisterTransactionRequest request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Materializacao de gasto fixo deve usar SyncRecurrenceOccurrenceAsync.");

    public Task<IReadOnlyList<TransactionDto>> GetByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TransactionDto>>([]);

    public Task<MonthlyTotalsDto> GetMonthlyTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MonthlyTotalsDto(month, 0m, 0m, 0m));

    public Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CategoryTotalDto>>([]);

    public Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Occurrences.ContainsKey(externalId));
}

internal sealed class InMemoryFixedExpenseRepository : IFixedExpenseRepository
{
    public List<FixedExpense> Items { get; } = [];

    public Task<IReadOnlyList<FixedExpense>> ListAsync(Guid userId, bool onlyActive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<FixedExpense>>(
            Items.Where(f => f.UserId == userId && (!onlyActive || f.IsActive)).ToList());

    public Task<FixedExpense?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(f => f.UserId == userId && f.Id == id));

    public Task<IReadOnlyList<Guid>> ListUserIdsWithActiveExpensesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>(Items.Where(f => f.IsActive).Select(f => f.UserId).Distinct().ToList());

    public void Add(FixedExpense fixedExpense) => Items.Add(fixedExpense);
}

internal sealed class FakeUnitOfWork : IRecurrencesUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
