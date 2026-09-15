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

    public static readonly Error InvalidClosingDay =
        Error.Validation(
            "ledger.invalid_closing_day",
            $"Dia de virada deve estar entre {Competence.CompetenceCalendar.MinClosingDay} e {Competence.CompetenceCalendar.MaxClosingDay}, ou vazio para usar o mes do calendario.");

    public static readonly Error CompetenceNotCurrent =
        Error.Validation("ledger.competence_not_current", "So a competencia corrente pode ser encerrada.");

    public static readonly Error CompetenceAlreadyClosed =
        Error.Conflict("ledger.competence_already_closed", "Esta competencia ja foi encerrada.");

    public static readonly Error ExternalIdRequired =
        Error.Validation("ledger.external_id_required", "Ocorrencia de gasto fixo precisa de um id externo para ser identificada.");

    public static readonly Error NotRepositionable =
        Error.Conflict("ledger.transaction_not_repositionable", "So lancamento gerado por gasto fixo pode ser reposicionado.");

    public static readonly Error CompetenceNotClosed =
        Error.NotFound("ledger.competence_not_closed", "Esta competencia nao esta encerrada.");
}
