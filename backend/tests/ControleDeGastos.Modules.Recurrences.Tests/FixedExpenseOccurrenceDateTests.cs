using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Recurrences.Domain;

namespace ControleDeGastos.Modules.Recurrences.Tests;

/// <summary>
/// Onde cai a ocorrencia mensal de um gasto fixo quando a competencia deixa de ser o
/// mes do calendario. Um fixo que vence no dia 28, com virada no dia 25, tem que cair
/// em 28/08 para pertencer a competencia de setembro - se caisse em 28/09 estaria em
/// outubro, e setembro ficaria sem ele.
/// </summary>
public sealed class FixedExpenseOccurrenceDateTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static FixedExpense Expense(int dayOfMonth) =>
        FixedExpense.Create(UserId, "Aluguel", 1500m, "Moradia", dayOfMonth).Value;

    private static CompetenceWindowDto Window(DateOnly start, DateOnly end) => new(start, end);

    private static DateOnly Date(int year, int month, int day) => new(year, month, day);

    [Fact]
    public void Vencimento_cai_no_primeiro_mes_da_janela()
    {
        // Competencia 2026-09 com virada no dia 25: 25/08 a 24/09.
        var date = Expense(28).OccurrenceDate(Window(Date(2026, 8, 25), Date(2026, 9, 24)));

        Assert.Equal(Date(2026, 8, 28), date);
    }

    [Fact]
    public void Vencimento_cai_no_segundo_mes_da_janela()
    {
        var date = Expense(10).OccurrenceDate(Window(Date(2026, 8, 25), Date(2026, 9, 24)));

        Assert.Equal(Date(2026, 9, 10), date);
    }

    [Fact]
    public void Vencimento_no_proprio_dia_de_virada_cai_no_inicio_da_janela()
    {
        var date = Expense(25).OccurrenceDate(Window(Date(2026, 8, 25), Date(2026, 9, 24)));

        Assert.Equal(Date(2026, 8, 25), date);
    }

    [Fact]
    public void Vencimento_no_ultimo_dia_da_janela()
    {
        var date = Expense(24).OccurrenceDate(Window(Date(2026, 8, 25), Date(2026, 9, 24)));

        Assert.Equal(Date(2026, 9, 24), date);
    }

    [Fact]
    public void Sem_dia_de_virada_o_comportamento_e_o_de_sempre()
    {
        // Janela = mes do calendario inteiro.
        var date = Expense(10).OccurrenceDate(Window(Date(2026, 9, 1), Date(2026, 9, 30)));

        Assert.Equal(Date(2026, 9, 10), date);
    }

    [Fact]
    public void Dia_inexistente_no_mes_usa_o_ultimo_dia()
    {
        var date = Expense(31).OccurrenceDate(Window(Date(2026, 2, 1), Date(2026, 2, 28)));

        Assert.Equal(Date(2026, 2, 28), date);
    }

    [Fact]
    public void Janela_encurtada_ainda_acomoda_o_vencimento_no_primeiro_mes()
    {
        // Competencia encerrada a mao em 18/08, janela 25/07..17/08:
        // o vencimento do dia 28 ainda cabe, em 28/07.
        var date = Expense(28).OccurrenceDate(Window(Date(2026, 7, 25), Date(2026, 8, 17)));

        Assert.Equal(Date(2026, 7, 28), date);
    }

    [Fact]
    public void Janela_encurtada_sem_o_vencimento_usa_o_ultimo_dia_da_janela()
    {
        // Janela 25/07..17/08: o dia 20 nao existe nela (20/07 e antes do inicio,
        // 20/08 e depois do fim). O fixo cai no ultimo dia, para nao mudar de competencia.
        var date = Expense(20).OccurrenceDate(Window(Date(2026, 7, 25), Date(2026, 8, 17)));

        Assert.Equal(Date(2026, 8, 17), date);
    }

    [Fact]
    public void A_data_escolhida_esta_sempre_dentro_da_janela()
    {
        var window = Window(Date(2026, 8, 25), Date(2026, 9, 24));

        for (var dayOfMonth = 1; dayOfMonth <= 31; dayOfMonth++)
        {
            var date = Expense(dayOfMonth).OccurrenceDate(window);

            Assert.True(
                window.Contains(date),
                $"Vencimento no dia {dayOfMonth} produziu {date:yyyy-MM-dd}, fora da janela.");
        }
    }

    [Fact]
    public void Chave_de_idempotencia_e_por_competencia()
    {
        var expense = Expense(28);

        Assert.Equal($"recurrence:{expense.Id}:2026-09", expense.ExternalKeyFor(new SharedKernel.Primitives.YearMonth(2026, 9)));
    }
}
