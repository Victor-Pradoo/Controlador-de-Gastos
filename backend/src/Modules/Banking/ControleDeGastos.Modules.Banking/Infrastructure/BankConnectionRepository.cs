using ControleDeGastos.Modules.Banking.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Banking.Infrastructure;

internal sealed class BankConnectionRepository(BankingDbContext context) : IBankConnectionRepository
{
    public Task<BankConnection?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        context.Connections.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<BankConnection>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Connections
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.InstitutionName)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByExternalItemAsync(Guid userId, string externalItemId, CancellationToken cancellationToken = default) =>
        context.Connections.AnyAsync(c => c.UserId == userId && c.ExternalItemId == externalItemId, cancellationToken);

    public void Add(BankConnection connection) => context.Connections.Add(connection);
}
