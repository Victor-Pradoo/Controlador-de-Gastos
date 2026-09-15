using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.SharedKernel.Messaging;
using Microsoft.Extensions.Logging;

namespace ControleDeGastos.Modules.Recurrences.Application;

/// <summary>
/// A competencia e derivada da regra de virada, entao mudar a regra pode empurrar a
/// ocorrencia de um gasto fixo para fora da janela da competencia em que ela deveria
/// estar - deixando aquela competencia sem o fixo. Rematerializar reposiciona as
/// ocorrencias, e e idempotente.
///
/// A competencia anterior tambem entra porque uma mudanca de regra costuma mover a
/// fronteira entre as duas ultimas competencias.
/// </summary>
public sealed class CompetenceRuleChangedHandler(
    FixedExpenseService service,
    ILedgerModuleApi ledger,
    ILogger<CompetenceRuleChangedHandler> logger)
    : IIntegrationEventHandler<CompetenceRuleChangedIntegrationEvent>
{
    public async Task HandleAsync(
        CompetenceRuleChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var current = await ledger.GetCurrentCompetenceAsync(integrationEvent.UserId, cancellationToken);

        foreach (var month in new[] { current.AddMonths(-1), current })
        {
            var materialized = await service.MaterializeAsync(integrationEvent.UserId, month, cancellationToken);

            logger.LogInformation(
                "Regra de competencia mudou: {Count} gasto(s) fixo(s) de {UserId} reconciliado(s) em {Month}.",
                materialized,
                integrationEvent.UserId,
                month);
        }
    }
}
