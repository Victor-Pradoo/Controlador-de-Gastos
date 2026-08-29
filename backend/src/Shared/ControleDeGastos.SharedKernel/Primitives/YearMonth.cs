namespace ControleDeGastos.SharedKernel.Primitives;

/// <summary>
/// Competencia (ano/mes). O app inteiro raciocina por mes: orcamento, fixos e resumo.
/// Serializa como "2026-08".
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(YearMonthJsonConverter))]
public readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
{
    public static YearMonth From(DateOnly date) => new(date.Year, date.Month);

    public static YearMonth From(DateTimeOffset instant) => new(instant.Year, instant.Month);

    public static YearMonth Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
        {
            throw new FormatException($"Competencia invalida: '{value}'. Formato esperado: yyyy-MM.");
        }

        return new YearMonth(year, month);
    }

    public DateOnly FirstDay => new(Year, Month, 1);

    public DateOnly LastDay => new(Year, Month, DateTime.DaysInMonth(Year, Month));

    public YearMonth AddMonths(int months) => From(FirstDay.AddMonths(months));

    public bool Contains(DateOnly date) => date.Year == Year && date.Month == Month;

    public int CompareTo(YearMonth other) => (Year * 12 + Month).CompareTo(other.Year * 12 + other.Month);

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
