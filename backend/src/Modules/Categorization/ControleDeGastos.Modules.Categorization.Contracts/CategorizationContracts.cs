namespace ControleDeGastos.Modules.Categorization.Contracts;

/// <summary>Onde a categoria aparece na UI. Espelha as abas do app legado.</summary>
public enum CategoryScope
{
    Variable = 1,
    Fixed = 2,
    Income = 3,
}

public sealed record CategoryDefinition(string Name, string Color, CategoryScope Scope);

/// <summary>
/// Sugestao para uma transacao importada do banco. Confidence baixa deve
/// aparecer na UI como "confirme a categoria".
/// </summary>
public sealed record CategorySuggestion(string Category, decimal Confidence, string Reason)
{
    public const string Fallback = "Outros";

    public static CategorySuggestion Unknown() => new(Fallback, 0m, "Nenhuma regra correspondeu.");
}

public sealed record CategoryRuleDto(Guid Id, string Keyword, string Category, int Priority);

public interface ICategorizationModuleApi
{
    Task<CategorySuggestion> SuggestAsync(
        Guid userId,
        string description,
        decimal amount,
        CancellationToken cancellationToken = default);

    IReadOnlyList<CategoryDefinition> GetCatalog();
}
