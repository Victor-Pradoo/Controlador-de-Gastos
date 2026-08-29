using ControleDeGastos.Modules.Categorization.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Categorization.Infrastructure;

public sealed class CategorizationDbContext(DbContextOptions<CategorizationDbContext> options)
    : DbContext(options), ICategorizationUnitOfWork
{
    public const string Schema = "categorization";

    public DbSet<CategoryRule> Rules => Set<CategoryRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CategorizationDbContext).Assembly);
    }
}
