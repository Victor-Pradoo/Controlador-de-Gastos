using ControleDeGastos.SharedKernel.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ControleDeGastos.Infrastructure.Shared.Messaging;

/// <summary>
/// Entrega sincrona e in-process de eventos de integracao.
/// Suficiente para o MVP; substituir por outbox + broker quando a entrega
/// precisar sobreviver a uma falha do processo (ver docs/adr/0004).
/// </summary>
public sealed class InMemoryEventBus(IServiceScopeFactory scopeFactory, ILogger<InMemoryEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToList();

        logger.LogInformation(
            "Publicando {Event} ({EventId}) para {HandlerCount} handler(s).",
            typeof(TEvent).Name,
            integrationEvent.EventId,
            handlers.Count);

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(integrationEvent, cancellationToken);
        }
    }
}
