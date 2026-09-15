using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Domain.Competence;

/// <summary>
/// Registro de "encerrei os gastos deste mes": a partir de <see cref="ClosedOn"/>,
/// lancamentos que cairiam nesta competencia passam a cair na seguinte. Lancamento
/// com data anterior continua onde estava - encerrar vale para o que vem, nao
/// reclassifica o que ja aconteceu.
///
/// Ano e mes ficam em colunas inteiras, e nao numa string "yyyy-MM", para comparar
/// e ordenar competencia em SQL sem conversao.
/// </summary>
public sealed class CompetenceClosure : Entity<Guid>
{
    private CompetenceClosure() : base(Guid.Empty)
    {
        // Construtor de materializacao do EF Core.
    }

    private CompetenceClosure(Guid id, Guid userId, YearMonth competence, DateOnly closedOn) : base(id)
    {
        UserId = userId;
        Year = competence.Year;
        Month = competence.Month;
        ClosedOn = closedOn;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public int Year { get; private set; }

    public int Month { get; private set; }

    /// <summary>Data em que o encerramento passou a valer.</summary>
    public DateOnly ClosedOn { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public YearMonth Competence => new(Year, Month);

    public static CompetenceClosure Create(Guid userId, YearMonth competence, DateOnly closedOn) =>
        new(Guid.CreateVersion7(), userId, competence, closedOn);
}
