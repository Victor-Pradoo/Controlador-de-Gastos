using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Tests;

/// <summary>
/// A regra mais visivel do app depois do orcamento: em que competencia cai um gasto.
/// Cobre os cenarios de openspec/changes/add-competence-cutoff/specs/competence/cutoff-rule.
/// </summary>
public sealed class CompetenceCalendarTests
{
    private static CompetenceCalendar Calendar(int? closingDay = null, params (YearMonth Competence, DateOnly ClosedOn)[] closures) =>
        new(closingDay, closures.ToDictionary(c => c.Competence, c => c.ClosedOn));

    private static DateOnly Date(int year, int month, int day) => new(year, month, day);

    private static YearMonth Month(int year, int month) => new(year, month);

    [Fact]
    public void Data_antes_do_dia_de_virada_fica_na_competencia_do_mes()
    {
        Assert.Equal(Month(2026, 8), Calendar(closingDay: 25).Resolve(Date(2026, 8, 20)));
    }

    [Fact]
    public void Data_no_dia_de_virada_ja_conta_para_a_competencia_seguinte()
    {
        Assert.Equal(Month(2026, 9), Calendar(closingDay: 25).Resolve(Date(2026, 8, 25)));
    }

    [Fact]
    public void Data_depois_do_dia_de_virada_no_mes_seguinte()
    {
        Assert.Equal(Month(2026, 9), Calendar(closingDay: 25).Resolve(Date(2026, 9, 3)));
    }

    [Fact]
    public void Virada_em_dezembro_atravessa_o_ano()
    {
        Assert.Equal(Month(2027, 1), Calendar(closingDay: 25).Resolve(Date(2026, 12, 28)));
    }

    [Fact]
    public void Dia_de_virada_inexistente_no_mes_usa_o_ultimo_dia()
    {
        // Fevereiro de 2026 tem 28 dias: com virada no 31, o dia 28 ja e a virada.
        Assert.Equal(Month(2026, 3), Calendar(closingDay: 31).Resolve(Date(2026, 2, 28)));
        Assert.Equal(Month(2026, 2), Calendar(closingDay: 31).Resolve(Date(2026, 2, 27)));
    }

    [Fact]
    public void Sem_dia_de_virada_a_competencia_e_o_mes_do_calendario()
    {
        Assert.Equal(Month(2026, 8), Calendar().Resolve(Date(2026, 8, 28)));
        Assert.Equal(Month(2026, 8), Calendar().Resolve(Date(2026, 8, 1)));
        Assert.Equal(Month(2026, 8), Calendar().Resolve(Date(2026, 8, 31)));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(15)]
    [InlineData(28)]
    [InlineData(31)]
    public void Todo_dia_de_virada_valido_separa_o_mes_em_duas_metades(int closingDay)
    {
        var calendar = Calendar(closingDay);
        var effective = Math.Min(closingDay, DateTime.DaysInMonth(2026, 8));

        Assert.Equal(Month(2026, 8), calendar.Resolve(Date(2026, 8, effective - 1)));
        Assert.Equal(Month(2026, 9), calendar.Resolve(Date(2026, 8, effective)));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(2, true)]
    [InlineData(31, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(32, false)]
    public void Dia_de_virada_valido(int? closingDay, bool expected)
    {
        Assert.Equal(expected, CompetenceCalendar.IsValidClosingDay(closingDay));
    }

    [Fact]
    public void Lancamento_apos_o_encerramento_vai_para_a_competencia_seguinte()
    {
        var calendar = Calendar(25, (Month(2026, 8), Date(2026, 8, 18)));

        Assert.Equal(Month(2026, 9), calendar.Resolve(Date(2026, 8, 20)));
    }

    [Fact]
    public void Lancamento_retroativo_anterior_ao_encerramento_fica_na_competencia_encerrada()
    {
        var calendar = Calendar(25, (Month(2026, 8), Date(2026, 8, 18)));

        Assert.Equal(Month(2026, 8), calendar.Resolve(Date(2026, 8, 10)));
    }

    [Fact]
    public void Lancamento_na_propria_data_do_encerramento_ja_conta_para_a_seguinte()
    {
        var calendar = Calendar(25, (Month(2026, 8), Date(2026, 8, 18)));

        Assert.Equal(Month(2026, 9), calendar.Resolve(Date(2026, 8, 18)));
    }

    [Fact]
    public void Dois_encerramentos_no_mesmo_dia_avancam_duas_competencias()
    {
        var calendar = Calendar(
            25,
            (Month(2026, 8), Date(2026, 8, 18)),
            (Month(2026, 9), Date(2026, 8, 18)));

        Assert.Equal(Month(2026, 10), calendar.Resolve(Date(2026, 8, 20)));
    }

    [Fact]
    public void Encerramento_funciona_sem_dia_de_virada()
    {
        var calendar = Calendar(null, (Month(2026, 8), Date(2026, 8, 18)));

        Assert.Equal(Month(2026, 9), calendar.Resolve(Date(2026, 8, 20)));
        Assert.Equal(Month(2026, 8), calendar.Resolve(Date(2026, 8, 10)));
    }

    [Fact]
    public void Competencia_corrente_avanca_no_dia_de_virada()
    {
        var calendar = Calendar(closingDay: 25);

        Assert.Equal(Month(2026, 8), calendar.Current(Date(2026, 8, 24)));
        Assert.Equal(Month(2026, 9), calendar.Current(Date(2026, 8, 25)));
    }

    [Fact]
    public void Virada_do_calendario_nao_muda_a_competencia_corrente()
    {
        // O ponto da mudanca: com dia de virada, o dia 1 nao e mais um marco.
        // O ciclo ja tinha comecado no dia 25 e ja acumula gastos.
        var calendar = Calendar(closingDay: 25);

        Assert.Equal(Month(2026, 9), calendar.Current(Date(2026, 8, 31)));
        Assert.Equal(Month(2026, 9), calendar.Current(Date(2026, 9, 1)));
    }

    [Fact]
    public void Competencia_corrente_sem_dia_de_virada_segue_o_calendario()
    {
        Assert.Equal(Month(2026, 8), Calendar().Current(Date(2026, 8, 31)));
        Assert.Equal(Month(2026, 9), Calendar().Current(Date(2026, 9, 1)));
    }

    [Fact]
    public void Competencia_corrente_avanca_ao_encerrar_o_mes()
    {
        var calendar = Calendar(25, (Month(2026, 8), Date(2026, 8, 18)));

        Assert.Equal(Month(2026, 9), calendar.Current(Date(2026, 8, 18)));
    }

    [Fact]
    public void Janela_com_dia_de_virada_atravessa_dois_meses()
    {
        var window = Calendar(closingDay: 25).WindowOf(Month(2026, 9));

        Assert.Equal(Date(2026, 8, 25), window.Start);
        Assert.Equal(Date(2026, 9, 24), window.End);
    }

    [Fact]
    public void Janela_sem_dia_de_virada_e_o_mes_do_calendario()
    {
        var window = Calendar().WindowOf(Month(2026, 9));

        Assert.Equal(Date(2026, 9, 1), window.Start);
        Assert.Equal(Date(2026, 9, 30), window.End);
    }

    [Fact]
    public void Encerramento_encurta_a_janela_da_competencia_e_antecipa_a_seguinte()
    {
        var calendar = Calendar(25, (Month(2026, 8), Date(2026, 8, 18)));

        var closed = calendar.WindowOf(Month(2026, 8));
        var next = calendar.WindowOf(Month(2026, 9));

        Assert.Equal(Date(2026, 7, 25), closed.Start);
        Assert.Equal(Date(2026, 8, 17), closed.End);
        Assert.Equal(Date(2026, 8, 18), next.Start);
        Assert.Equal(Date(2026, 9, 24), next.End);
    }

    [Fact]
    public void Dois_encerramentos_no_mesmo_dia_produzem_janela_vazia()
    {
        var calendar = Calendar(
            25,
            (Month(2026, 8), Date(2026, 8, 18)),
            (Month(2026, 9), Date(2026, 8, 18)));

        var window = calendar.WindowOf(Month(2026, 9));

        Assert.True(window.IsEmpty);
        Assert.False(window.Contains(Date(2026, 8, 18)));
    }

    [Fact]
    public void Encerramento_com_data_fora_da_janela_e_ignorado()
    {
        // Com virada no dia 5, a janela de 2026-07 e 05/06..04/07: um encerramento
        // datado de 15/07 nao poderia ter sido criado pela aplicacao. Aceita-lo faria
        // Resolve e WindowOf discordarem e sumiria com os lancamentos de 05/07 a 14/07.
        var calendar = Calendar(5, (Month(2026, 7), Date(2026, 7, 15)));

        Assert.Empty(calendar.Closures);
        Assert.True(calendar.WindowOf(calendar.Resolve(Date(2026, 7, 5))).Contains(Date(2026, 7, 5)));
    }

    [Fact]
    public void Encerramentos_validos_em_sequencia_sao_preservados()
    {
        // O encerramento de 2026-08 desloca a janela de 2026-09 para comecar em 18/08,
        // entao encerrar 2026-09 no mesmo dia continua valido.
        var calendar = Calendar(
            25,
            (Month(2026, 8), Date(2026, 8, 18)),
            (Month(2026, 9), Date(2026, 8, 18)));

        Assert.Equal(2, calendar.Closures.Count);
    }

    [Fact]
    public void Janela_de_fevereiro_respeita_mes_curto()
    {
        var window = Calendar(closingDay: 31).WindowOf(Month(2026, 2));

        Assert.Equal(Date(2026, 1, 31), window.Start);
        Assert.Equal(Date(2026, 2, 27), window.End);
    }
}
