namespace ControleDeGastos.SharedKernel.Messaging;

/// <summary>
/// Barramento de eventos de integracao. Hoje e in-process (ver InMemoryEventBus).
/// Quando um modulo virar servico proprio, so esta implementacao muda.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
