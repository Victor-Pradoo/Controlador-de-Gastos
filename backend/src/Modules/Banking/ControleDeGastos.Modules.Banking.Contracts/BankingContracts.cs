namespace ControleDeGastos.Modules.Banking.Contracts;

public enum BankConnectionStatus
{
    /// <summary>Criada no provedor, ainda sem o primeiro sync.</summary>
    Pending = 1,
    Active = 2,
    /// <summary>Precisa de acao do usuario: MFA, senha alterada, consentimento expirado.</summary>
    RequiresAction = 3,
    Error = 4,
    Disabled = 5,
}

public sealed record BankConnectionDto(
    Guid Id,
    string Provider,
    string InstitutionName,
    BankConnectionStatus Status,
    DateTimeOffset? LastSyncedAt);

public sealed record BankSyncResultDto(int Imported, int Skipped, int Failed, DateTimeOffset SyncedAt);

public interface IBankingModuleApi
{
    Task<IReadOnlyList<BankConnectionDto>> ListConnectionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<BankSyncResultDto> SyncAsync(Guid userId, Guid connectionId, int daysBack = 30, CancellationToken cancellationToken = default);
}
