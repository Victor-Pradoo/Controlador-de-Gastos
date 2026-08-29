using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Infrastructure.Shared.Http;

public static class MonthParameter
{
    /// <summary>
    /// Le o parametro de competencia ("2026-08"). Ausente ou invalido cai no mes corrente,
    /// que e o comportamento esperado pela tela inicial.
    /// </summary>
    public static YearMonth Resolve(string? month, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(month))
        {
            return YearMonth.From(clock.Today);
        }

        try
        {
            return YearMonth.Parse(month);
        }
        catch (FormatException)
        {
            return YearMonth.From(clock.Today);
        }
    }
}
