using ControleDeGastos.Modules.Categorization.Domain;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Categorization.Infrastructure;

internal sealed class CategoryRuleRepository(CategorizationDbContext context) : ICategoryRuleRepository
{
    public async Task<IReadOnlyList<CategoryRule>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Rules
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

    public Task<CategoryRule?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default) =>
        context.Rules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

    public void Add(CategoryRule rule) => context.Rules.Add(rule);

    public void Remove(CategoryRule rule) => context.Rules.Remove(rule);
}
