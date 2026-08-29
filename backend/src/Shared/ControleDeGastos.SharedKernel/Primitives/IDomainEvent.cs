namespace ControleDeGastos.SharedKernel.Primitives;

/// <summary>
/// Evento interno ao modulo. Para comunicacao ENTRE modulos use
/// <see cref="ControleDeGastos.SharedKernel.Messaging.IIntegrationEvent"/>.
/// </summary>
public interface IDomainEvent
{
    Guid EventId => Guid.CreateVersion7();

    DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
