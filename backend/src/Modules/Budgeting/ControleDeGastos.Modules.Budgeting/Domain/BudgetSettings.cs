using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Budgeting.Domain;

/// <summary>
/// Configuracao de orcamento do usuario: salario liquido e o percentual que ele
/// quer guardar antes de gastar (a "taxa de reserva" do app legado).
/// </summary>
public sealed class BudgetSettings : AggregateRoot<Guid>
{
    private BudgetSettings() : base(Guid.Empty)
    {
        // Construtor de materializacao do EF Core.
    }

    private BudgetSettings(Guid userId, Money salary, decimal reserveRate) : base(userId)
    {
        Salary = salary;
        ReserveRate = reserveRate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>O Id do agregado E o UserId: um conjunto de configuracoes por usuario.</summary>
    public Money Salary { get; private set; }

    /// <summary>Percentual de 0 a 100.</summary>
    public decimal ReserveRate { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Money ReserveAmount => Money.From(Salary.Amount * (ReserveRate / 100m));

    /// <summary>Salario menos a reserva: o teto real de gastos do mes.</summary>
    public Money Available => Salary - ReserveAmount;

    public static BudgetSettings Default(Guid userId) => new(userId, Money.Zero, 20m);

    public static Result<BudgetSettings> Create(Guid userId, decimal salary, decimal reserveRate)
    {
        var settings = Default(userId);
        var result = settings.Update(salary, reserveRate);

        return result.IsFailure ? Result.Failure<BudgetSettings>(result.Error) : settings;
    }

    public Result Update(decimal salary, decimal reserveRate)
    {
        if (salary < 0m)
        {
            return Result.Failure(BudgetingErrors.InvalidSalary);
        }

        if (reserveRate is < 0m or > 100m)
        {
            return Result.Failure(BudgetingErrors.InvalidReserveRate);
        }

        Salary = Money.From(salary);
        ReserveRate = decimal.Round(reserveRate, 2);
        UpdatedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
