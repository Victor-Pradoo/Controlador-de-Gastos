using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Contracts;

public sealed record TransactionDto(
    Guid Id,
    TransactionKind Kind,
    TransactionSource Source,
    string Description,
    decimal Amount,
    string Category,
    DateOnly OccurredOn,
    bool IsEditable);

/// <summary>Totais do mes. Budgeting consome isto para calcular o saldo disponivel.</summary>
public sealed record MonthlyTotalsDto(
    YearMonth Month,
    decimal VariableExpenses,
    decimal FixedExpenses,
    decimal Income)
{
    public decimal NetSpent => VariableExpenses + FixedExpenses - Income;
}

public sealed record CategoryTotalDto(string Category, decimal Total);

/// <summary>
/// Pedido de registro vindo de outro modulo (Recurrences materializando um fixo,
/// Banking importando uma transacao). <paramref name="ExternalId"/> garante idempotencia.
/// </summary>
public sealed record RegisterTransactionRequest(
    Guid UserId,
    TransactionKind Kind,
    TransactionSource Source,
    string Description,
    decimal Amount,
    string Category,
    DateOnly OccurredOn,
    string? ExternalId = null,
    Guid? RecurrenceId = null);
