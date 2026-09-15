using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Messaging;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Tests;

/// <summary>
/// Dublês em memoria para os casos de uso. Repositorio e provider sao interfaces do
/// dominio, entao os handlers sao testaveis sem banco - o que o projeto ja faz com
/// MonthlyBudgetTests: exercitar a regra, nao a persistencia.
/// </summary>
internal sealed class FakeClock(DateOnly today) : IClock
{
    public DateOnly Today { get; set; } = today;

    public DateTimeOffset UtcNow => new(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}

internal sealed class FakeUnitOfWork : ILedgerUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class RecordingEventBus : IEventBus
{
    public List<IIntegrationEvent> Published { get; } = [];

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        Published.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Guarda regra e encerramentos e monta o calendario a partir deles, para o teste
/// enxergar o efeito de uma escrita na leitura seguinte - como no banco de verdade.
/// </summary>
internal sealed class InMemoryCompetenceStore
    : ICompetenceSettingsRepository, ICompetenceClosureRepository, ICompetenceCalendarProvider
{
    private readonly List<CompetenceClosure> _closures = [];
    private CompetenceSettings? _settings;

    // Explicito: ICompetenceSettingsRepository e ICompetenceCalendarProvider tem a
    // mesma assinatura de GetAsync, e o fake implementa os dois.
    Task<CompetenceSettings?> ICompetenceSettingsRepository.GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_settings?.Id == userId ? _settings : null);

    public void Add(CompetenceSettings settings) => _settings = settings;

    public Task<CompetenceClosure?> GetAsync(Guid userId, YearMonth competence, CancellationToken cancellationToken = default) =>
        Task.FromResult(_closures.FirstOrDefault(c => c.UserId == userId && c.Competence.Equals(competence)));

    public void Add(CompetenceClosure closure) => _closures.Add(closure);

    public void Remove(CompetenceClosure closure) => _closures.Remove(closure);

    Task<CompetenceCalendar> ICompetenceCalendarProvider.GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(new CompetenceCalendar(
            _settings?.Id == userId ? _settings.ClosingDay : null,
            _closures.Where(c => c.UserId == userId).ToDictionary(c => c.Competence, c => c.ClosedOn)));
}

internal sealed class InMemoryTransactionRepository : ITransactionRepository
{
    public List<Transaction> Items { get; } = [];

    public Task<Transaction?> GetAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.UserId == userId && t.Id == transactionId));

    public Task<IReadOnlyList<Transaction>> ListByMonthAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Transaction>>(Items.Where(t => t.UserId == userId).ToList());

    public Task<bool> ExistsByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(t => t.UserId == userId && t.ExternalId == externalId));

    public Task<Transaction?> GetByExternalIdAsync(Guid userId, string externalId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.UserId == userId && t.ExternalId == externalId));

    public Task<int> DeleteByRecurrenceAsync(Guid userId, Guid recurrenceId, CancellationToken cancellationToken = default)
    {
        var removed = Items.RemoveAll(t => t.UserId == userId && t.RecurrenceId == recurrenceId);
        return Task.FromResult(removed);
    }

    public void Add(Transaction transaction) => Items.Add(transaction);

    public void Remove(Transaction transaction) => Items.Remove(transaction);
}
