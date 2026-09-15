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

/// <summary>
/// A regra de virada do usuario mudou (dia de virada, encerramento ou reabertura).
/// Como a competencia e derivada, lancamentos ja gravados podem ter mudado de
/// competencia - quem depende disso precisa se reconciliar. Publicado em vez de o
/// Ledger chamar Recurrences: notificar sem acoplar e o que o barramento existe para fazer.
/// </summary>
public sealed record CompetenceRuleChangedIntegrationEvent(Guid UserId) : IntegrationEvent;
