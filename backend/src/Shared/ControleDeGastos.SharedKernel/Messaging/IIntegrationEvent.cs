namespace ControleDeGastos.SharedKernel.Messaging;

/// <summary>
/// Contrato publico entre modulos. Vive sempre num projeto *.Contracts,
/// porque e a unica coisa que outro modulo pode enxergar.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOn { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
