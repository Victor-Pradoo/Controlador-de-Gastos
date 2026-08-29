using ControleDeGastos.Modules.Banking.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Banking.Infrastructure;

public sealed class BankingDbContext(DbContextOptions<BankingDbContext> options)
    : DbContext(options), IBankingUnitOfWork
{
    public const string Schema = "banking";

    public DbSet<BankConnection> Connections => Set<BankConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
    }
}
