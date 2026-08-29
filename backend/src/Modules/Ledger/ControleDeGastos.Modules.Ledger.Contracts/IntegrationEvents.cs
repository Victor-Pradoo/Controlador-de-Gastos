using ControleDeGastos.SharedKernel.Messaging;

namespace ControleDeGastos.Modules.Ledger.Contracts;

public sealed record TransactionRegisteredIntegrationEvent(
    Guid TransactionId,
    Guid UserId,
    TransactionKind Kind,
    decimal Amount,
    string Category,
    DateOnly Date) : IntegrationEvent;

public sealed record TransactionRemovedIntegrationEvent(
    Guid TransactionId,
    Guid UserId,
    DateOnly Date) : IntegrationEvent;
