using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Categorization.Domain;

public static class CategorizationErrors
{
    public static readonly Error InvalidKeyword =
        Error.Validation("categorization.invalid_keyword", "A palavra-chave precisa ter ao menos 2 caracteres.");

    public static readonly Error InvalidCategory =
        Error.Validation("categorization.invalid_category", "Categoria e obrigatoria.");

    public static readonly Error RuleNotFound =
        Error.NotFound("categorization.rule_not_found", "Regra nao encontrada.");
}
