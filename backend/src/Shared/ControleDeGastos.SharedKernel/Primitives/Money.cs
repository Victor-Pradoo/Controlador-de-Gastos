namespace ControleDeGastos.SharedKernel.Primitives;

/// <summary>
/// Valor monetario em BRL. O MVP e single-currency de proposito;
/// quando houver multi-moeda, adicione o codigo da moeda aqui e nas migrations.
/// </summary>
public readonly record struct Money(decimal Amount) : IComparable<Money>
{
    public static readonly Money Zero = new(0m);

    public bool IsPositive => Amount > 0m;

    public static Money From(decimal amount) => new(decimal.Round(amount, 2, MidpointRounding.ToEven));

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator *(Money value, decimal factor) => From(value.Amount * factor);

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public override string ToString() => Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
}
