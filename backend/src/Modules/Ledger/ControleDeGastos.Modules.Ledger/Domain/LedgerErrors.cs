using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Ledger.Domain;

public static class LedgerErrors
{
    public static readonly Error UserRequired =
        Error.Validation("ledger.user_required", "Lancamento precisa estar associado a um usuario.");

    public static readonly Error InvalidDescription =
        Error.Validation("ledger.invalid_description", $"Descricao e obrigatoria e deve ter ate {Transaction.MaxDescriptionLength} caracteres.");

    public static readonly Error InvalidAmount =
        Error.Validation("ledger.invalid_amount", "Valor deve ser maior que zero.");

    public static readonly Error InvalidCategory =
        Error.Validation("ledger.invalid_category", "Categoria e obrigatoria.");

    public static readonly Error NotFound =
        Error.NotFound("ledger.transaction_not_found", "Lancamento nao encontrado.");

    public static readonly Error NotEditable =
        Error.Conflict("ledger.transaction_not_editable", "Lancamentos importados do banco ou gerados por gasto fixo nao podem ser removidos aqui.");

    public static readonly Error DuplicatedExternalId =
        Error.Conflict("ledger.duplicated_external_id", "Este lancamento ja foi importado.");
}
