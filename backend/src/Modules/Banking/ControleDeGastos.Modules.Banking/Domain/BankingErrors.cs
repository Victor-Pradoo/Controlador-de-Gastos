using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Banking.Domain;

public static class BankingErrors
{
    public static readonly Error InvalidExternalItemId =
        Error.Validation("banking.invalid_item_id", "Identificador da conexao no provedor e obrigatorio.");

    public static readonly Error ConnectionNotFound =
        Error.NotFound("banking.connection_not_found", "Conexao bancaria nao encontrada.");

    public static readonly Error ConnectionDisabled =
        Error.Conflict("banking.connection_disabled", "Esta conexao esta desativada.");

    public static Error Provider(string message) =>
        Error.External("banking.provider_error", message);
}
