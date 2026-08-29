using ControleDeGastos.Modules.Budgeting.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Budgeting.Infrastructure;

public sealed class BudgetingDbContext(DbContextOptions<BudgetingDbContext> options)
    : DbContext(options), IBudgetingUnitOfWork
{
    public const string Schema = "budgeting";

    public DbSet<BudgetSettings> Settings => Set<BudgetSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetingDbContext).Assembly);
    }
}
