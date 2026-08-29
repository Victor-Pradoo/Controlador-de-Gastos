using ControleDeGastos.SharedKernel.Abstractions;

namespace ControleDeGastos.Infrastructure.Shared.Time;

public sealed class SystemClock : IClock
{
    /// <summary>Fuso do usuario do MVP. Vira preferencia por usuario quando houver multi-tenant.</summary>
    private static readonly TimeZoneInfo AppTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, AppTimeZone).Date);
}
