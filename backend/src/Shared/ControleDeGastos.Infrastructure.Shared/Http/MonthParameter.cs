using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Infrastructure.Shared.Http;

public static class MonthParameter
{
    /// <summary>
    /// Le o parametro de competencia ("2026-08"). Devolve null quando ausente ou
    /// invalido, e cabe ao endpoint cair na competencia corrente do usuario.
    ///
    /// O default NAO mora aqui de proposito: ele depende da regra de virada, que vive
    /// no Ledger, e este projeto e referenciado por todos os modulos - resolver o
    /// default aqui faria todo modulo depender de Ledger.Contracts por transitividade.
    /// </summary>
    public static YearMonth? TryParse(string? month)
    {
        if (string.IsNullOrWhiteSpace(month))
        {
            return null;
        }

        try
        {
            return YearMonth.Parse(month);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
