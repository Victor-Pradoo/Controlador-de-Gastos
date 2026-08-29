using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain.Events;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Domain;

/// <summary>
/// Um lancamento no extrato do usuario: gasto variavel, entrada ou a ocorrencia
/// mensal de um gasto fixo. E a raiz de agregado do modulo Ledger.
/// </summary>
public sealed class Transaction : AggregateRoot<Guid>
{
    public const int MaxDescriptionLength = 120;
    public const int MaxCategoryLength = 60;

    private Transaction() : base(Guid.Empty)
    {
        // Construtor de materializacao do EF Core.
    }

    private Transaction(
        Guid id,
        Guid userId,
        TransactionKind kind,
        TransactionSource source,
        string description,
        Money amount,
        string category,
        DateOnly occurredOn,
        string? externalId,
        Guid? recurrenceId)
        : base(id)
    {
        UserId = userId;
        Kind = kind;
        Source = source;
        Description = description;
        Amount = amount;
        Category = category;
        OccurredOn = occurredOn;
        ExternalId = externalId;
        RecurrenceId = recurrenceId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    public TransactionKind Kind { get; private set; }

    public TransactionSource Source { get; private set; }

    public string Description { get; private set; } = null!;

    public Money Amount { get; private set; }

    public string Category { get; private set; } = null!;

    public DateOnly OccurredOn { get; private set; }

    /// <summary>Id da transacao no provedor bancario (Pluggy). Chave de idempotencia da sincronizacao.</summary>
    public string? ExternalId { get; private set; }

    /// <summary>Gasto fixo que gerou este lancamento, quando <see cref="Source"/> e Recurrence.</summary>
    public Guid? RecurrenceId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public YearMonth Competence => YearMonth.From(OccurredOn);

    /// <summary>
    /// Lancamento importado do banco ou gerado por recorrencia nao e apagado a mao:
    /// a fonte da verdade e o extrato / o cadastro do fixo.
    /// </summary>
    public bool IsEditable => Source == TransactionSource.Manual;

    public static Result<Transaction> Register(
        Guid userId,
        TransactionKind kind,
        TransactionSource source,
        string description,
        decimal amount,
        string category,
        DateOnly occurredOn,
        string? externalId = null,
        Guid? recurrenceId = null)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<Transaction>(LedgerErrors.UserRequired);
        }

        description = description?.Trim() ?? string.Empty;
        if (description.Length is 0 or > MaxDescriptionLength)
        {
            return Result.Failure<Transaction>(LedgerErrors.InvalidDescription);
        }

        if (amount <= 0m)
        {
            return Result.Failure<Transaction>(LedgerErrors.InvalidAmount);
        }

        category = category?.Trim() ?? string.Empty;
        if (category.Length is 0 or > MaxCategoryLength)
        {
            return Result.Failure<Transaction>(LedgerErrors.InvalidCategory);
        }

        var transaction = new Transaction(
            Guid.CreateVersion7(),
            userId,
            kind,
            source,
            description,
            Money.From(amount),
            category,
            occurredOn,
            externalId,
            recurrenceId);

        transaction.Raise(new TransactionRegisteredDomainEvent(transaction.Id, userId, kind, transaction.Amount, category, occurredOn));

        return transaction;
    }

    public Result Recategorize(string category)
    {
        category = category?.Trim() ?? string.Empty;
        if (category.Length is 0 or > MaxCategoryLength)
        {
            return Result.Failure(LedgerErrors.InvalidCategory);
        }

        Category = category;
        return Result.Success();
    }
}
