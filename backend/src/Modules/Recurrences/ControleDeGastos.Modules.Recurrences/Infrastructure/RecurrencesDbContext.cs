using ControleDeGastos.Modules.Recurrences.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Recurrences.Infrastructure;

public sealed class RecurrencesDbContext(DbContextOptions<RecurrencesDbContext> options)
    : DbContext(options), IRecurrencesUnitOfWork
{
    public const string Schema = "recurrences";

    public DbSet<FixedExpense> FixedExpenses => Set<FixedExpense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecurrencesDbContext).Assembly);
    }
}
