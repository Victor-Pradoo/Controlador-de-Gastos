using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Domain.Competence;

/// <summary>
/// Regra de virada de competencia do usuario. Ausencia de registro - ou
/// <see cref="ClosingDay"/> nulo - significa competencia igual ao mes do calendario,
/// que e o comportamento de quem nunca configurou nada.
/// </summary>
public sealed class CompetenceSettings : AggregateRoot<Guid>
{
    private CompetenceSettings() : base(Guid.Empty)
    {
        // Construtor de materializacao do EF Core.
    }

    private CompetenceSettings(Guid userId, int? closingDay) : base(userId)
    {
        ClosingDay = closingDay;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>O Id do agregado E o UserId: uma regra por usuario.</summary>
    public int? ClosingDay { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static CompetenceSettings Default(Guid userId) => new(userId, closingDay: null);

    public static Result<CompetenceSettings> Create(Guid userId, int? closingDay)
    {
        var settings = Default(userId);
        var result = settings.ChangeClosingDay(closingDay);

        return result.IsFailure ? Result.Failure<CompetenceSettings>(result.Error) : settings;
    }

    public Result ChangeClosingDay(int? closingDay)
    {
        if (!CompetenceCalendar.IsValidClosingDay(closingDay))
        {
            return Result.Failure(LedgerErrors.InvalidClosingDay);
        }

        ClosingDay = closingDay;
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
