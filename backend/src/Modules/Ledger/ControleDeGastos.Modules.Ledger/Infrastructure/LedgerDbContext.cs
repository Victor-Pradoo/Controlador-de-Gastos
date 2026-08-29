using ControleDeGastos.Modules.Ledger.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Infrastructure;

/// <summary>
/// Contexto do modulo Ledger. Vive no schema "ledger" e enxerga apenas as tabelas
/// deste modulo - nenhuma FK atravessa a fronteira de modulo.
/// </summary>
public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options), ILedgerUnitOfWork
{
    public const string Schema = "ledger";

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
    }
}
