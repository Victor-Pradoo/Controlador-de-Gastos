using ControleDeGastos.Modules.Banking.Contracts;
using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Banking.Domain;

/// <summary>
/// Vinculo entre o usuario e uma instituicao financeira no provedor de Open Finance.
/// Guarda apenas o identificador do item no provedor - credenciais do banco
/// NUNCA passam por esta aplicacao.
/// </summary>
public sealed class BankConnection : AggregateRoot<Guid>
{
    private BankConnection() : base(Guid.Empty)
    {
        // Construtor de materializacao do EF Core.
    }

    private BankConnection(Guid id, Guid userId, string provider, string externalItemId, string institutionName)
        : base(id)
    {
        UserId = userId;
        Provider = provider;
        ExternalItemId = externalItemId;
        InstitutionName = institutionName;
        Status = BankConnectionStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    /// <summary>Provedor de agregacao: "pluggy" hoje, outro amanha sem mudar o dominio.</summary>
    public string Provider { get; private set; } = null!;

    /// <summary>Id do item/conexao no provedor. E a chave para buscar transacoes.</summary>
    public string ExternalItemId { get; private set; } = null!;

    public string InstitutionName { get; private set; } = null!;

    public BankConnectionStatus Status { get; private set; }

    public DateTimeOffset? LastSyncedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? LastError { get; private set; }

    public static Result<BankConnection> Create(Guid userId, string provider, string externalItemId, string institutionName)
    {
        if (string.IsNullOrWhiteSpace(externalItemId))
        {
            return Result.Failure<BankConnection>(BankingErrors.InvalidExternalItemId);
        }

        return new BankConnection(
            Guid.CreateVersion7(),
            userId,
            provider.Trim().ToLowerInvariant(),
            externalItemId.Trim(),
            string.IsNullOrWhiteSpace(institutionName) ? "Instituicao" : institutionName.Trim());
    }

    public void MarkSynced(IClock clock)
    {
        Status = BankConnectionStatus.Active;
        LastSyncedAt = clock.UtcNow;
        LastError = null;
    }

    public void MarkRequiresAction(string reason)
    {
        Status = BankConnectionStatus.RequiresAction;
        LastError = Truncate(reason);
    }

    public void MarkError(string reason)
    {
        Status = BankConnectionStatus.Error;
        LastError = Truncate(reason);
    }

    public void Disable() => Status = BankConnectionStatus.Disabled;

    /// <summary>Janela de busca: do ultimo sync (com folga) ate hoje; na primeira vez, dias completos.</summary>
    public DateOnly SyncStartDate(IClock clock, int defaultDaysBack)
    {
        var fallback = clock.Today.AddDays(-defaultDaysBack);

        if (LastSyncedAt is null)
        {
            return fallback;
        }

        // Refaz alguns dias: extrato de cartao costuma consolidar lancamentos com atraso.
        var overlap = DateOnly.FromDateTime(LastSyncedAt.Value.UtcDateTime).AddDays(-3);
        return overlap < fallback ? fallback : overlap;
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400];
}
