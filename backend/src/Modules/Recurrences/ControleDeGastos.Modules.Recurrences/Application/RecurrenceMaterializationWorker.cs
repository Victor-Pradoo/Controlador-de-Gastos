using ControleDeGastos.SharedKernel.Abstractions;
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
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var month = YearMonthOf(clock);

        foreach (var userId in await service.ListUserIdsWithActiveExpensesAsync(cancellationToken))
        {
            var created = await service.MaterializeAsync(userId, month, cancellationToken);

            if (created > 0)
            {
                logger.LogInformation("Materializados {Count} gasto(s) fixo(s) de {UserId} em {Month}.", created, userId, month);
            }
        }
    }

    private static SharedKernel.Primitives.YearMonth YearMonthOf(IClock clock) =>
        SharedKernel.Primitives.YearMonth.From(clock.Today);
}
