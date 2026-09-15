using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Domain.Competence;

/// <summary>
/// Regra central do produto, isolada e pura: dada a data de um lancamento, a que
/// competencia ele pertence. Sem I/O e sem <c>DateTime.Now</c> - "hoje" entra por
/// parametro, como no resto do dominio.
///
/// Duas etapas, nesta ordem:
/// 1. Dia de virada: a partir dele o lancamento ja conta para a competencia seguinte.
/// 2. Encerramentos manuais: enquanto a competencia estiver encerrada em data menor
///    ou igual a do lancamento, avanca-se para a proxima.
///
/// <see cref="Resolve"/> e <see cref="WindowOf"/> sao duas leituras da MESMA regra e
/// precisam concordar: a janela de uma competencia contem exatamente as datas que
/// <see cref="Resolve"/> classifica nela. E o que o teste de propriedade garante.
/// </summary>
public sealed class CompetenceCalendar
{
    /// <summary>
    /// Virada no dia 1 nao tem ponto fixo: "dia >= 1" e sempre verdade, entao todo
    /// lancamento cairia na competencia seguinte, para sempre. Ausencia de virada e
    /// representada por <c>null</c>, nao por 1.
    /// </summary>
    public const int MinClosingDay = 2;

    public const int MaxClosingDay = 31;

    private readonly IReadOnlyDictionary<YearMonth, DateOnly> _closures;

    public CompetenceCalendar(int? closingDay = null, IReadOnlyDictionary<YearMonth, DateOnly>? closures = null)
    {
        ClosingDay = closingDay;
        _closures = Normalize(closingDay, closures);
    }

    /// <summary>
    /// Descarta encerramento cuja data cai fora da janela da propria competencia.
    /// A aplicacao nunca gera um desses - so a competencia corrente pode ser encerrada,
    /// e sempre na data de hoje - mas um dado assim (regra alterada depois de um
    /// encerramento antigo, importacao manual) faria <see cref="Resolve"/> e
    /// <see cref="WindowOf"/> discordarem, e lancamentos sumiriam de todas as
    /// competencias. Ignorar mantem as duas leituras coerentes por construcao.
    ///
    /// A validacao e sequencial, da competencia mais antiga para a mais nova: o
    /// encerramento de uma competencia desloca a janela da seguinte, entao encerrar
    /// duas competencias no mesmo dia continua sendo valido.
    /// </summary>
    private static Dictionary<YearMonth, DateOnly> Normalize(
        int? closingDay,
        IReadOnlyDictionary<YearMonth, DateOnly>? closures)
    {
        var accepted = new Dictionary<YearMonth, DateOnly>();

        if (closures is null)
        {
            return accepted;
        }

        foreach (var (competence, closedOn) in closures.OrderBy(c => c.Key))
        {
            if (WindowOf(competence, closingDay, accepted).Contains(closedOn))
            {
                accepted.Add(competence, closedOn);
            }
        }

        return accepted;
    }

    /// <summary>Dia a partir do qual o lancamento conta para a competencia seguinte. Nulo = mes do calendario.</summary>
    public int? ClosingDay { get; }

    /// <summary>Competencias encerradas manualmente e a data em que o encerramento passou a valer.</summary>
    public IReadOnlyDictionary<YearMonth, DateOnly> Closures => _closures;

    public static bool IsValidClosingDay(int? closingDay) =>
        closingDay is null || closingDay is >= MinClosingDay and <= MaxClosingDay;

    /// <summary>A que competencia pertence um lancamento ocorrido nesta data.</summary>
    public YearMonth Resolve(DateOnly date)
    {
        var competence = BaseCompetenceOf(date);

        // Encerrar o mes vale a partir da data do encerramento: um lancamento
        // deliberadamente retroativo continua na competencia encerrada.
        while (_closures.TryGetValue(competence, out var closedOn) && closedOn <= date)
        {
            competence = competence.AddMonths(1);
        }

        return competence;
    }

    /// <summary>Competencia corrente: a competencia de um lancamento feito hoje.</summary>
    public YearMonth Current(DateOnly today) => Resolve(today);

    /// <summary>Intervalo de datas desta competencia. Pode ser vazio - ver <see cref="CompetenceWindow.IsEmpty"/>.</summary>
    public CompetenceWindow WindowOf(YearMonth competence) => WindowOf(competence, ClosingDay, _closures);

    private static CompetenceWindow WindowOf(
        YearMonth competence,
        int? closingDay,
        IReadOnlyDictionary<YearMonth, DateOnly> closures)
    {
        // O encerramento da competencia anterior empurra o inicio desta para tras;
        // o encerramento desta antecipa o seu fim.
        var start = closures.TryGetValue(competence.AddMonths(-1), out var previousClosedOn)
            ? previousClosedOn
            : BaseStartOf(competence, closingDay);

        var end = closures.TryGetValue(competence, out var closedOn)
            ? closedOn.AddDays(-1)
            : BaseEndOf(competence, closingDay);

        return new CompetenceWindow(start, end);
    }

    private YearMonth BaseCompetenceOf(DateOnly date)
    {
        var calendarMonth = YearMonth.From(date);

        if (ClosingDay is null)
        {
            return calendarMonth;
        }

        return date.Day >= EffectiveClosingDayOf(calendarMonth, ClosingDay)
            ? calendarMonth.AddMonths(1)
            : calendarMonth;
    }

    private static DateOnly BaseStartOf(YearMonth competence, int? closingDay)
    {
        if (closingDay is null)
        {
            return competence.FirstDay;
        }

        var previous = competence.AddMonths(-1);
        return new DateOnly(previous.Year, previous.Month, EffectiveClosingDayOf(previous, closingDay));
    }

    private static DateOnly BaseEndOf(YearMonth competence, int? closingDay)
    {
        if (closingDay is null)
        {
            return competence.LastDay;
        }

        return new DateOnly(competence.Year, competence.Month, EffectiveClosingDayOf(competence, closingDay))
            .AddDays(-1);
    }

    /// <summary>Mes curto usa o ultimo dia disponivel - a mesma regra do dia de vencimento de um gasto fixo.</summary>
    private static int EffectiveClosingDayOf(YearMonth competence, int? closingDay) =>
        Math.Min(closingDay!.Value, DateTime.DaysInMonth(competence.Year, competence.Month));
}
