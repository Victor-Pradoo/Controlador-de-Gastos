using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Banking.Application.Abstractions;

/// <summary>
/// Porta para o agregador de Open Finance. O dominio conhece SO esta interface:
/// trocar Pluggy por outro provedor e escrever outro adaptador, nada mais.
/// </summary>
public interface IBankDataProvider
{
    string Name { get; }

    /// <summary>
    /// Token de curta duracao que o front usa para abrir o widget de conexao.
    /// As credenciais do banco sao digitadas la, nunca aqui.
    /// </summary>
    Task<Result<string>> CreateConnectTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<BankItemSnapshot>> GetItemAsync(string externalItemId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<BankTransactionSnapshot>>> GetTransactionsAsync(
        string externalItemId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}

public sealed record BankItemSnapshot(string ExternalItemId, string InstitutionName, string Status);

public enum BankTransactionDirection
{
    Debit = 1,
    Credit = 2,
}

/// <summary>
/// Transacao como o provedor entrega. <paramref name="Amount"/> e sempre positivo;
/// o sinal vira <paramref name="Direction"/> para o resto do sistema nao adivinhar convencao.
/// </summary>
public sealed record BankTransactionSnapshot(
    string ExternalId,
    string Description,
    decimal Amount,
    DateOnly Date,
    BankTransactionDirection Direction);
