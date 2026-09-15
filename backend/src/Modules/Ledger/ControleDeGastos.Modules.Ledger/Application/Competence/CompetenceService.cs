using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Messaging;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Application.Competence;

/// <summary>
/// Casos de uso da regra de virada: configurar o dia, encerrar o mes e reabrir.
///
/// Os tres alteram como TODO lancamento e classificado - a competencia e derivada,
/// nao gravada - por isso os tres publicam
/// <see cref="CompetenceRuleChangedIntegrationEvent"/>.
/// </summary>
public sealed class CompetenceService(
    ICompetenceSettingsRepository settingsRepository,
    ICompetenceClosureRepository closureRepository,
    ICompetenceCalendarProvider calendarProvider,
    ILedgerUnitOfWork unitOfWork,
    IEventBus eventBus,
    IClock clock)
{
    public async Task<CompetenceCalendar> GetCalendarAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await calendarProvider.GetAsync(userId, cancellationToken);

    public async Task<YearMonth> GetCurrentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var calendar = await calendarProvider.GetAsync(userId, cancellationToken);
        return calendar.Current(clock.Today);
    }

    public async Task<CompetenceSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var calendar = await calendarProvider.GetAsync(userId, cancellationToken);
        var current = calendar.Current(clock.Today);
        var previous = current.AddMonths(-1);

        // Se a competencia anterior foi encerrada na mao, e ela que o botao "reabrir" desfaz.
        var reopenable = calendar.Closures.TryGetValue(previous, out var closedOn)
            ? (Competence: (YearMonth?)previous, ClosedOn: (DateOnly?)closedOn)
            : (Competence: null, ClosedOn: null);

        return new CompetenceSettingsDto(
            calendar.ClosingDay,
            current,
            ToDto(calendar.WindowOf(current)),
            reopenable.Competence,
            reopenable.ClosedOn);
    }

    public async Task<Result> UpdateClosingDayAsync(
        Guid userId,
        int? closingDay,
        CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetAsync(userId, cancellationToken);

        if (settings is null)
        {
            var created = CompetenceSettings.Create(userId, closingDay);
            if (created.IsFailure)
            {
                return Result.Failure(created.Error);
            }

            settingsRepository.Add(created.Value);
        }
        else
        {
            var updated = settings.ChangeClosingDay(closingDay);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishRuleChangedAsync(userId, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<YearMonth>> CloseAsync(
        Guid userId,
        YearMonth competence,
        CancellationToken cancellationToken = default)
    {
        var calendar = await calendarProvider.GetAsync(userId, cancellationToken);
        var current = calendar.Current(clock.Today);

        // Encerrar uma competencia passada nao faria sentido (ela ja terminou) e
        // encerrar uma futura criaria um encerramento fora da propria janela.
        if (!competence.Equals(current))
        {
            return Result.Failure<YearMonth>(LedgerErrors.CompetenceNotCurrent);
        }

        if (await closureRepository.GetAsync(userId, competence, cancellationToken) is not null)
        {
            return Result.Failure<YearMonth>(LedgerErrors.CompetenceAlreadyClosed);
        }

        closureRepository.Add(CompetenceClosure.Create(userId, competence, clock.Today));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishRuleChangedAsync(userId, cancellationToken);

        return competence.AddMonths(1);
    }

    public async Task<Result> ReopenAsync(
        Guid userId,
        YearMonth competence,
        CancellationToken cancellationToken = default)
    {
        var closure = await closureRepository.GetAsync(userId, competence, cancellationToken);

        if (closure is null)
        {
            return Result.Failure(LedgerErrors.CompetenceNotClosed);
        }

        closureRepository.Remove(closure);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await PublishRuleChangedAsync(userId, cancellationToken);

        return Result.Success();
    }

    internal static CompetenceWindowDto ToDto(CompetenceWindow window) => new(window.Start, window.End);

    private Task PublishRuleChangedAsync(Guid userId, CancellationToken cancellationToken) =>
        eventBus.PublishAsync(new CompetenceRuleChangedIntegrationEvent(userId), cancellationToken);
}
