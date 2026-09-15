using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Domain.Competence;

public interface ICompetenceSettingsRepository
{
    Task<CompetenceSettings?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(CompetenceSettings settings);
}

public interface ICompetenceClosureRepository
{
    Task<CompetenceClosure?> GetAsync(Guid userId, YearMonth competence, CancellationToken cancellationToken = default);

    void Add(CompetenceClosure closure);

    void Remove(CompetenceClosure closure);
}

/// <summary>
/// Monta o <see cref="CompetenceCalendar"/> do usuario: uma leitura por operacao,
/// para que quem precisa resolver varias datas (materializacao de fixos, por exemplo)
/// nao bata no banco uma vez por data.
/// </summary>
public interface ICompetenceCalendarProvider
{
    Task<CompetenceCalendar> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}
