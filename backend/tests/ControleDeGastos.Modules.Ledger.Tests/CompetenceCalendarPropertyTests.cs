using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Tests;

/// <summary>
/// <see cref="CompetenceCalendar.Resolve"/> classifica uma data; <see cref="CompetenceCalendar.WindowOf"/>
/// delimita uma competencia. Sao duas leituras da mesma regra e a falha mais dificil de
/// perceber e elas discordarem: um lancamento sumiria de todas as competencias, porque a
/// consulta filtra pela janela mas o usuario espera o resultado de Resolve.
///
/// Estes testes varrem 24 meses dia a dia e exigem que as janelas particionem o tempo.
/// </summary>
public sealed class CompetenceCalendarPropertyTests
{
    private static readonly DateOnly RangeStart = new(2026, 1, 1);
    private static readonly DateOnly RangeEnd = new(2027, 12, 31);

    public static TheoryData<int?, (int Year, int Month, int Day)[]> Calendars()
    {
        var data = new TheoryData<int?, (int, int, int)[]>();

        // Sem encerramento, cobrindo os extremos do intervalo permitido e um mes curto.
        foreach (int? closingDay in new int?[] { null, 2, 5, 15, 25, 28, 29, 30, 31 })
        {
            data.Add(closingDay, []);
        }

        // Com encerramentos: um isolado, dois consecutivos e dois no mesmo dia
        // (que produz uma competencia de janela vazia).
        data.Add(25, [(2026, 8, 18)]);
        data.Add(null, [(2026, 8, 18)]);
        data.Add(31, [(2026, 2, 10)]);
        data.Add(25, [(2026, 8, 18), (2026, 9, 20)]);
        data.Add(25, [(2026, 8, 18), (2026, 9, 18)]);
        data.Add(5, [(2026, 3, 2), (2026, 7, 15), (2026, 11, 30)]);

        return data;
    }

    [Theory]
    [MemberData(nameof(Calendars))]
    public void Cada_data_cai_na_janela_da_competencia_que_Resolve_devolve(
        int? closingDay,
        (int Year, int Month, int Day)[] closures)
    {
        var calendar = Build(closingDay, closures);

        for (var date = RangeStart; date <= RangeEnd; date = date.AddDays(1))
        {
            var competence = calendar.Resolve(date);
            var window = calendar.WindowOf(competence);

            Assert.True(
                window.Contains(date),
                $"{date:yyyy-MM-dd} foi classificada em {competence}, mas a janela dessa competencia e {window.Start:yyyy-MM-dd}..{window.End:yyyy-MM-dd}.");
        }
    }

    [Theory]
    [MemberData(nameof(Calendars))]
    public void Nenhuma_data_cai_em_duas_janelas(
        int? closingDay,
        (int Year, int Month, int Day)[] closures)
    {
        var calendar = Build(closingDay, closures);
        var windows = CompetencesIn(RangeStart, RangeEnd)
            .Select(competence => (Competence: competence, Window: calendar.WindowOf(competence)))
            .ToList();

        for (var date = RangeStart; date <= RangeEnd; date = date.AddDays(1))
        {
            var matches = windows.Where(w => w.Window.Contains(date)).ToList();

            Assert.True(
                matches.Count == 1,
                $"{date:yyyy-MM-dd} pertence a {matches.Count} janelas ({string.Join(", ", matches.Select(m => m.Competence))}); deveria pertencer a exatamente uma.");
        }
    }

    [Theory]
    [MemberData(nameof(Calendars))]
    public void Janelas_nao_vazias_sao_contiguas_e_cobrem_todo_o_intervalo(
        int? closingDay,
        (int Year, int Month, int Day)[] closures)
    {
        var calendar = Build(closingDay, closures);

        var windows = CompetencesIn(RangeStart, RangeEnd)
            .Select(calendar.WindowOf)
            .Where(window => !window.IsEmpty)
            .OrderBy(window => window.Start)
            .ToList();

        // Uma competencia de janela vazia e legitima (dois encerramentos no mesmo dia);
        // ela simplesmente nao participa da cobertura.
        for (var i = 1; i < windows.Count; i++)
        {
            Assert.Equal(windows[i - 1].End.AddDays(1), windows[i].Start);
        }

        Assert.True(windows[0].Start <= RangeStart);
        Assert.True(windows[^1].End >= RangeEnd);
    }

    /// <summary>
    /// Compatibilidade com quem ja usa o app: sem dia de virada configurado - o padrao,
    /// e o que toda conta existente tem - a competencia tem que continuar sendo
    /// exatamente o mes do calendario, do primeiro ao ultimo dia.
    /// </summary>
    [Fact]
    public void Sem_dia_de_virada_a_competencia_e_exatamente_o_mes_do_calendario()
    {
        var calendar = new CompetenceCalendar();

        foreach (var competence in CompetencesIn(RangeStart, RangeEnd))
        {
            var window = calendar.WindowOf(competence);

            Assert.Equal(competence.FirstDay, window.Start);
            Assert.Equal(competence.LastDay, window.End);
        }

        for (var date = RangeStart; date <= RangeEnd; date = date.AddDays(1))
        {
            Assert.Equal(YearMonth.From(date), calendar.Resolve(date));
        }
    }

    private static CompetenceCalendar Build(int? closingDay, (int Year, int Month, int Day)[] closures) =>
        new(closingDay, closures.ToDictionary(
            c => new YearMonth(c.Year, c.Month),
            c => new DateOnly(c.Year, c.Month, c.Day)));

    /// <summary>Competencias que podem tocar o intervalo, com uma folga de um mes em cada ponta.</summary>
    private static IEnumerable<YearMonth> CompetencesIn(DateOnly start, DateOnly end)
    {
        var first = YearMonth.From(start).AddMonths(-1);
        var last = YearMonth.From(end).AddMonths(1);

        for (var competence = first; competence.CompareTo(last) <= 0; competence = competence.AddMonths(1))
        {
            yield return competence;
        }
    }
}
