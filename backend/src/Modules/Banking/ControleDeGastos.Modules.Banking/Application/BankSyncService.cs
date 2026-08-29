using ControleDeGastos.Modules.Banking.Application.Abstractions;
using ControleDeGastos.Modules.Banking.Contracts;
using ControleDeGastos.Modules.Banking.Domain;
using ControleDeGastos.Modules.Categorization.Contracts;
using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace ControleDeGastos.Modules.Banking.Application;

/// <summary>
/// Caso de uso central do MVP: puxa o extrato no provedor, pede uma categoria ao
/// modulo Categorization e grava cada transacao no Ledger.
///
/// Idempotencia: o ExternalId enviado ao Ledger e unico por transacao do provedor,
/// entao re-sincronizar a mesma janela nao duplica lancamento.
/// </summary>
public sealed class BankSyncService(
    IBankConnectionRepository repository,
    IBankingUnitOfWork unitOfWork,
    IBankDataProvider provider,
    ILedgerModuleApi ledger,
    ICategorizationModuleApi categorization,
    IClock clock,
    ILogger<BankSyncService> logger) : IBankingModuleApi
{
    public async Task<IReadOnlyList<BankConnectionDto>> ListConnectionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connections = await repository.ListAsync(userId, cancellationToken);

        return connections
            .Select(c => new BankConnectionDto(c.Id, c.Provider, c.InstitutionName, c.Status, c.LastSyncedAt))
            .ToList();
    }

    public async Task<Result<Guid>> ConnectAsync(
        Guid userId,
        string externalItemId,
        CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsByExternalItemAsync(userId, externalItemId, cancellationToken))
        {
            return Result.Failure<Guid>(
                Error.Conflict("banking.already_connected", "Esta instituicao ja esta conectada."));
        }

        var item = await provider.GetItemAsync(externalItemId, cancellationToken);
        if (item.IsFailure)
        {
            return Result.Failure<Guid>(item.Error);
        }

        var connection = BankConnection.Create(userId, provider.Name, externalItemId, item.Value.InstitutionName);
        if (connection.IsFailure)
        {
            return Result.Failure<Guid>(connection.Error);
        }

        repository.Add(connection.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return connection.Value.Id;
    }

    public async Task<BankSyncResultDto> SyncAsync(
        Guid userId,
        Guid connectionId,
        int daysBack = 30,
        CancellationToken cancellationToken = default)
    {
        var connection = await repository.GetAsync(userId, connectionId, cancellationToken)
            ?? throw new InvalidOperationException($"Conexao {connectionId} nao encontrada para o usuario.");

        var from = connection.SyncStartDate(clock, daysBack);
        var to = clock.Today;

        var transactions = await provider.GetTransactionsAsync(connection.ExternalItemId, from, to, cancellationToken);

        if (transactions.IsFailure)
        {
            connection.MarkError(transactions.Error.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Sync da conexao {ConnectionId} falhou: {Error}", connectionId, transactions.Error.Message);

            return new BankSyncResultDto(0, 0, 0, clock.UtcNow);
        }

        var imported = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var transaction in transactions.Value)
        {
            var externalId = $"{connection.Provider}:{transaction.ExternalId}";

            if (await ledger.ExistsByExternalIdAsync(userId, externalId, cancellationToken))
            {
                skipped++;
                continue;
            }

            var isIncome = transaction.Direction == BankTransactionDirection.Credit;

            var suggestion = isIncome
                ? new CategorySuggestion("Pix recebido", 0.5m, "Entrada importada do banco.")
                : await categorization.SuggestAsync(userId, transaction.Description, transaction.Amount, cancellationToken);

            var request = new RegisterTransactionRequest(
                userId,
                isIncome ? TransactionKind.Income : TransactionKind.Expense,
                TransactionSource.BankSync,
                transaction.Description,
                transaction.Amount,
                suggestion.Category,
                transaction.Date,
                externalId);

            var result = await ledger.RegisterAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                imported++;
            }
            else if (result.Error.Code == "ledger.duplicated_external_id")
            {
                skipped++;
            }
            else
            {
                failed++;
                logger.LogWarning(
                    "Transacao {ExternalId} rejeitada pelo Ledger: {Error}",
                    externalId,
                    result.Error.Message);
            }
        }

        connection.MarkSynced(clock);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Sync {ConnectionId}: {Imported} importada(s), {Skipped} ja existente(s), {Failed} falha(s).",
            connectionId,
            imported,
            skipped,
            failed);

        return new BankSyncResultDto(imported, skipped, failed, clock.UtcNow);
    }

    public Task<Result<string>> CreateConnectTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
        provider.CreateConnectTokenAsync(userId, cancellationToken);
}
