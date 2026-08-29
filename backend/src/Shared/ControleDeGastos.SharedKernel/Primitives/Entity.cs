namespace ControleDeGastos.SharedKernel.Primitives;

/// <summary>
/// Entidade de dominio identificada por <typeparamref name="TId"/>.
/// Igualdade e por identidade, nunca por valor.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; protected set; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
}
