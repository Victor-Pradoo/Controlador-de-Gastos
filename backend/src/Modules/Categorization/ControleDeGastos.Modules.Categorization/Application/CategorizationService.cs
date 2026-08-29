using ControleDeGastos.Modules.Categorization.Contracts;
using ControleDeGastos.Modules.Categorization.Domain;
using ControleDeGastos.SharedKernel.Results;

namespace ControleDeGastos.Modules.Categorization.Application;

/// <summary>
/// Sugere categoria para uma transacao importada. Ordem: regras do usuario primeiro,
/// depois heuristicas embutidas; nada casou, cai em "Outros" com confianca zero
/// para a UI pedir confirmacao.
/// </summary>
public sealed class CategorizationService(
    ICategoryRuleRepository repository,
    ICategorizationUnitOfWork unitOfWork) : ICategorizationModuleApi
{
    /// <summary>Palavras que aparecem no extrato de praticamente todo banco brasileiro.</summary>
    private static readonly (string Keyword, string Category)[] BuiltInHeuristics =
    [
        ("ifood", "Alimentacao"),
        ("rappi", "Alimentacao"),
        ("supermercado", "Alimentacao"),
        ("mercado", "Alimentacao"),
        ("restaurante", "Alimentacao"),
        ("padaria", "Alimentacao"),
        ("uber", "Transporte"),
        ("99app", "Transporte"),
        ("posto", "Transporte"),
        ("combustivel", "Transporte"),
        ("estacionamento", "Transporte"),
        ("farmacia", "Saude"),
        ("drogaria", "Saude"),
        ("laboratorio", "Saude"),
        ("netflix", "Assinaturas"),
        ("spotify", "Assinaturas"),
        ("amazon prime", "Assinaturas"),
        ("aluguel", "Aluguel"),
        ("condominio", "Condominio"),
        ("energia", "Moradia"),
        ("agua", "Moradia"),
        ("internet", "Moradia"),
        ("cinema", "Lazer"),
        ("faculdade", "Faculdade"),
        ("mensalidade", "Mensalidade"),
    ];

    public async Task<CategorySuggestion> SuggestAsync(
        Guid userId,
        string description,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return CategorySuggestion.Unknown();
        }

        var rules = await repository.ListAsync(userId, cancellationToken);

        var match = rules
            .Where(rule => rule.Matches(description))
            .OrderByDescending(rule => rule.Priority)
            .ThenByDescending(rule => rule.Keyword.Length)
            .FirstOrDefault();

        if (match is not null)
        {
            return new CategorySuggestion(match.Category, 1.0m, $"Regra do usuario: '{match.Keyword}'.");
        }

        foreach (var (keyword, category) in BuiltInHeuristics)
        {
            if (description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return new CategorySuggestion(category, 0.7m, $"Heuristica padrao: '{keyword}'.");
            }
        }

        return CategorySuggestion.Unknown();
    }

    public IReadOnlyList<CategoryDefinition> GetCatalog() => CategoryCatalog.All;

    public async Task<IReadOnlyList<CategoryRuleDto>> ListRulesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rules = await repository.ListAsync(userId, cancellationToken);
        return rules.Select(r => new CategoryRuleDto(r.Id, r.Keyword, r.Category, r.Priority)).ToList();
    }

    public async Task<Result<Guid>> AddRuleAsync(
        Guid userId,
        string keyword,
        string category,
        int priority,
        CancellationToken cancellationToken = default)
    {
        var result = CategoryRule.Create(userId, keyword, category, priority);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        repository.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }

    public async Task<Result> RemoveRuleAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetAsync(userId, id, cancellationToken);
        if (rule is null)
        {
            return Result.Failure(CategorizationErrors.RuleNotFound);
        }

        repository.Remove(rule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
