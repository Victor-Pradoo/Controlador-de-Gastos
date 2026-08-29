using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Primitives;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Categorization.Domain;

/// <summary>
/// Regra "se a descricao contem X, a categoria e Y". Simples de proposito:
/// o usuario entende e corrige. Modelos estatisticos ficam para depois do MVP.
/// </summary>
public sealed class CategoryRule : AggregateRoot<Guid>
{
    private CategoryRule() : base(Guid.Empty)
    {
        // Construtor de materializacao do EF Core.
    }

    private CategoryRule(Guid id, Guid userId, string keyword, string category, int priority) : base(id)
    {
        UserId = userId;
        Keyword = keyword;
        Category = category;
        Priority = priority;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    /// <summary>Sempre normalizada em minusculas para casar sem depender de acentuacao do extrato.</summary>
    public string Keyword { get; private set; } = null!;

    public string Category { get; private set; } = null!;

    /// <summary>Maior vence quando mais de uma regra casa.</summary>
    public int Priority { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<CategoryRule> Create(Guid userId, string keyword, string category, int priority = 0)
    {
        keyword = keyword?.Trim().ToLowerInvariant() ?? string.Empty;
        if (keyword.Length < 2)
        {
            return Result.Failure<CategoryRule>(CategorizationErrors.InvalidKeyword);
        }

        category = category?.Trim() ?? string.Empty;
        if (category.Length == 0)
        {
            return Result.Failure<CategoryRule>(CategorizationErrors.InvalidCategory);
        }

        return new CategoryRule(Guid.CreateVersion7(), userId, keyword, category, priority);
    }

    public bool Matches(string description) =>
        description.Contains(Keyword, StringComparison.OrdinalIgnoreCase);
}

public interface ICategoryRuleRepository
{
    Task<IReadOnlyList<CategoryRule>> ListAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CategoryRule?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

    void Add(CategoryRule rule);

    void Remove(CategoryRule rule);
}

public interface ICategorizationUnitOfWork : IUnitOfWork;
