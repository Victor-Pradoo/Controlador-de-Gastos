using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.SharedKernel.Primitives;

namespace ControleDeGastos.Modules.Ledger.Domain.Events;

public sealed record TransactionRegisteredDomainEvent(
    Guid TransactionId,
    Guid UserId,
    TransactionKind Kind,
    Money Amount,
    string Category,
    DateOnly Date) : IDomainEvent;
