using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Budgeting.Domain;

public static class BudgetingErrors
{
    public static readonly Error InvalidSalary =
        Error.Validation("budgeting.invalid_salary", "Salario nao pode ser negativo.");

    public static readonly Error InvalidReserveRate =
        Error.Validation("budgeting.invalid_reserve_rate", "Taxa de reserva deve estar entre 0 e 100.");
}
