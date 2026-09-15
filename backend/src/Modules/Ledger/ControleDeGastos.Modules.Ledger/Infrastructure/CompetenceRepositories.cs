using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Infrastructure;

internal sealed class CompetenceSettingsRepository(LedgerDbContext context) : ICompetenceSettingsRepository
{
    public Task<CompetenceSettings?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.CompetenceSettings.FirstOrDefaultAsync(s => s.Id == userId, cancellationToken);

    public void Add(CompetenceSettings settings) => context.CompetenceSettings.Add(settings);
}

internal sealed class CompetenceClosureRepository(LedgerDbContext context) : ICompetenceClosureRepository
{
    public Task<CompetenceClosure?> GetAsync(Guid userId, YearMonth competence, CancellationToken cancellationToken = default) =>
        context.CompetenceClosures.FirstOrDefaultAsync(
            c => c.UserId == userId && c.Year == competence.Year && c.Month == competence.Month,
            cancellationToken);

    public void Add(CompetenceClosure closure) => context.CompetenceClosures.Add(closure);

    public void Remove(CompetenceClosure closure) => context.CompetenceClosures.Remove(closure);
}

/// <summary>
/// Carrega a regra do usuario e devolve o calendario montado - uma leitura por
/// operacao, e nao uma por data resolvida.
///
/// Os encerramentos sao lidos por inteiro, e nao numa faixa em torno da competencia
/// consultada: <see cref="CompetenceCalendar.Resolve"/> pode encadear encerramentos
/// para a frente indefinidamente, entao um recorte classificaria lancamentos de um
/// jeito diferente do resto do app, em silencio. Sao poucas linhas por usuario (no
/// maximo uma por competencia encerrada) e a leitura sai pelo indice.
/// </summary>
internal sealed class CompetenceCalendarProvider(LedgerDbContext context) : ICompetenceCalendarProvider
{
    public async Task<CompetenceCalendar> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var closingDay = await context.CompetenceSettings
            .AsNoTracking()
            .Where(s => s.Id == userId)
            .Select(s => s.ClosingDay)
            .FirstOrDefaultAsync(cancellationToken);

        var closures = await context.CompetenceClosures
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Year, c.Month, c.ClosedOn })
            .ToListAsync(cancellationToken);

        return new CompetenceCalendar(
            closingDay,
            closures.ToDictionary(c => new YearMonth(c.Year, c.Month), c => c.ClosedOn));
    }
}
