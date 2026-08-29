using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Recurrences.Domain;

public static class RecurrenceErrors
{
    public static readonly Error InvalidDescription =
        Error.Validation("recurrences.invalid_description", "Descricao do gasto fixo e obrigatoria.");

    public static readonly Error InvalidAmount =
        Error.Validation("recurrences.invalid_amount", "Valor deve ser maior que zero.");

    public static readonly Error InvalidCategory =
        Error.Validation("recurrences.invalid_category", "Categoria e obrigatoria.");

    public static readonly Error InvalidDayOfMonth =
        Error.Validation("recurrences.invalid_day", "Dia de vencimento deve estar entre 1 e 31.");

    public static readonly Error NotFound =
        Error.NotFound("recurrences.not_found", "Gasto fixo nao encontrado.");
}
