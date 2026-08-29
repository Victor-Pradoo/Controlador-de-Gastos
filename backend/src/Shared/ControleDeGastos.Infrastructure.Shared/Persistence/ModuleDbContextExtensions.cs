using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Infrastructure.Shared.Persistence;

public static class ModuleDbContextExtensions
{
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// Registra o DbContext de um modulo isolado no seu proprio schema do SQL Server,
    /// com tabela de historico de migrations tambem separada. Um banco, N schemas:
    /// os modulos nao enxergam tabelas uns dos outros.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string schema)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' nao configurada. Veja backend/README.md.");

        services.AddDbContext<TContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.MigrationsHistoryTable("__EFMigrationsHistory", schema);
                sqlServer.EnableRetryOnFailure(3);
            }));

        return services;
    }
}
