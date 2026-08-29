using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Recurrences.Contracts;

public sealed record FixedExpenseDto(
    Guid Id,
    string Description,
    decimal Amount,
    string Category,
    int DayOfMonth,
    bool IsActive);

public interface IRecurrencesModuleApi
{
    /// <summary>
    /// Garante que todo gasto fixo ativo do usuario tenha lancamento na competencia.
    /// E idempotente: rodar duas vezes no mesmo mes nao duplica nada.
    /// </summary>
    Task<int> MaterializeAsync(Guid userId, YearMonth month, CancellationToken cancellationToken = default);
}
