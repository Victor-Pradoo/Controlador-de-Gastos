namespace ControleDeGastos.SharedKernel.Primitives;

/// <summary>
/// Raiz de agregado: unica porta de entrada para alterar o grafo de objetos abaixo dela
/// e unico ponto que acumula eventos de dominio.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
