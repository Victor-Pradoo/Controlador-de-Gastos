using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Recurrences.Domain;

/// <summary>
/// Gasto fixo: um modelo que gera um lancamento por mes no Ledger
/// (aluguel, financiamento, mensalidade). Nao e um lancamento - e a regra que os produz.
/// </summary>
public sealed class FixedExpense : AggregateRoot<Guid>
{
    public const int MaxDescriptionLength = 120;

    private FixedExpense() : base(Guid.Empty)
    {
        // Construtor de materializacao do EF Core.
    }

    private FixedExpense(Guid id, Guid userId, string description, Money amount, string category, int dayOfMonth)
        : base(id)
    {
        UserId = userId;
        Description = description;
        Amount = amount;
        Category = category;
        DayOfMonth = dayOfMonth;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public string Description { get; private set; } = null!;

    public Money Amount { get; private set; }

    public string Category { get; private set; } = null!;

    /// <summary>Dia de vencimento (1-31). Meses curtos usam o ultimo dia disponivel.</summary>
    public int DayOfMonth { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<FixedExpense> Create(
        Guid userId,
        string description,
        decimal amount,
        string category,
        int dayOfMonth)
    {
        description = description?.Trim() ?? string.Empty;
        if (description.Length is 0 or > MaxDescriptionLength)
        {
            return Result.Failure<FixedExpense>(RecurrenceErrors.InvalidDescription);
        }

        if (amount <= 0m)
        {
            return Result.Failure<FixedExpense>(RecurrenceErrors.InvalidAmount);
        }

        if (dayOfMonth is < 1 or > 31)
        {
            return Result.Failure<FixedExpense>(RecurrenceErrors.InvalidDayOfMonth);
        }

        category = category?.Trim() ?? string.Empty;
        if (category.Length == 0)
        {
            return Result.Failure<FixedExpense>(RecurrenceErrors.InvalidCategory);
        }

        return new FixedExpense(Guid.CreateVersion7(), userId, description, Money.From(amount), category, dayOfMonth);
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Data do lancamento dentro da janela desta competencia.
    ///
    /// A competencia deixou de ser o mes do calendario: com dia de virada 25, a janela
    /// de setembro vai de 25/08 a 24/09, e um fixo que vence no dia 28 tem que cair em
    /// 28/08 - dentro da janela - e nao em 28/09, que ja e outubro. Por isso a busca
    /// percorre os meses que a janela toca e devolve o primeiro vencimento que couber.
    ///
    /// Sem nenhum vencimento na janela (encurtada por um encerramento manual, por
    /// exemplo), usa o ultimo dia dela: o gasto fixo continua na competencia certa.
    /// </summary>
    public DateOnly OccurrenceDate(CompetenceWindowDto window)
    {
        var month = new YearMonth(window.Start.Year, window.Start.Month);
        var lastMonth = new YearMonth(window.End.Year, window.End.Month);

        while (month.CompareTo(lastMonth) <= 0)
        {
            // Mes curto usa o ultimo dia disponivel, como sempre fez.
            var day = Math.Min(DayOfMonth, DateTime.DaysInMonth(month.Year, month.Month));
            var candidate = new DateOnly(month.Year, month.Month, day);

            if (window.Contains(candidate))
            {
                return candidate;
            }

            month = month.AddMonths(1);
        }

        return window.End;
    }

    /// <summary>
    /// Chave de idempotencia enviada ao Ledger. O proprio indice unico do Ledger
    /// impede a duplicacao, entao materializar de novo e seguro.
    /// </summary>
    public string ExternalKeyFor(YearMonth month) => $"recurrence:{Id}:{month}";
}
