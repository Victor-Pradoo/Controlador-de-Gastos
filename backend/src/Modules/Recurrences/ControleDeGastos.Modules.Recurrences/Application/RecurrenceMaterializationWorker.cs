using ControleDeGastos.Modules.Ledger.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ControleDeGastos.Modules.Recurrences.Application;

/// <summary>
/// Materializa os gastos fixos da competencia corrente ao subir e uma vez por dia.
/// MVP: roda dentro do processo da API. Se virar multi-instancia, mover para um
/// job com lock distribuido (ver docs/roadmap.md).
/// </summary>
public sealed class RecurrenceMaterializationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<RecurrenceMaterializationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await MaterializeCurrentMonthAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Falha na materializacao automatica de gastos fixos.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task MaterializeCurrentMonthAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<FixedExpenseService>();
        var ledger = scope.ServiceProvider.GetRequiredService<ILedgerModuleApi>();

        foreach (var userId in await service.ListUserIdsWithActiveExpensesAsync(cancellationToken))
        {
            // A competencia corrente e a da regra de virada do usuario, nao o mes do
            // calendario: com virada no dia 25, em 28/08 o fixo a materializar e o de setembro.
            var month = await ledger.GetCurrentCompetenceAsync(userId, cancellationToken);
            var materialized = await service.MaterializeAsync(userId, month, cancellationToken);

            if (materialized > 0)
            {
                logger.LogInformation("Materializados {Count} gasto(s) fixo(s) de {UserId} em {Month}.", materialized, userId, month);
            }
        }
    }
}
