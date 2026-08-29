using ControleDeGastos.Modules.Banking.Infrastructure;
using ControleDeGastos.Modules.Budgeting.Infrastructure;
using ControleDeGastos.Modules.Categorization.Infrastructure;
using ControleDeGastos.Modules.Ledger.Infrastructure;
using ControleDeGastos.Modules.Recurrences.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Api.Infrastructure;

/// <summary>
/// Aplica as migrations de cada modulo na subida. Ligado apenas em Development
/// (Database:AutoMigrate) - em producao migrations sao passo explicito de deploy.
/// </summary>
internal static class DatabaseMigrator
{
    public static async Task MigrateModulesAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:AutoMigrate", defaultValue: false))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = app.Logger;

        await MigrateAsync<LedgerDbContext>(scope.ServiceProvider, logger);
        await MigrateAsync<BudgetingDbContext>(scope.ServiceProvider, logger);
        await MigrateAsync<RecurrencesDbContext>(scope.ServiceProvider, logger);
        await MigrateAsync<CategorizationDbContext>(scope.ServiceProvider, logger);
        await MigrateAsync<BankingDbContext>(scope.ServiceProvider, logger);
    }

    private static async Task MigrateAsync<TContext>(IServiceProvider services, ILogger logger)
        where TContext : DbContext
    {
        var context = services.GetRequiredService<TContext>();

        // Sem migrations criadas ainda o contexto simplesmente nao tem o que aplicar;
        // avisar e melhor do que derrubar a API na primeira execucao.
        if (!(await context.Database.GetPendingMigrationsAsync()).Any())
        {
            return;
        }

        logger.LogInformation("Aplicando migrations de {Context}.", typeof(TContext).Name);
        await context.Database.MigrateAsync();
    }
}
