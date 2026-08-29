using ControleDeGastos.Modules.Categorization.Contracts;

namespace ControleDeGastos.Modules.Categorization.Domain;

/// <summary>
/// Catalogo padrao, portado do app legado (as cores vinham do objeto CAT_COLORS).
/// O front consome isto em vez de manter a lista duplicada em TypeScript.
/// </summary>
public static class CategoryCatalog
{
    public static readonly IReadOnlyList<CategoryDefinition> All =
    [
        new("Alimentacao", "#c8f060", CategoryScope.Variable),
        new("Transporte", "#60b4f0", CategoryScope.Variable),
        new("Saude", "#f06090", CategoryScope.Variable),
        new("Lazer", "#b060f0", CategoryScope.Variable),
        new("Moradia", "#f09060", CategoryScope.Variable),
        new("Educacao", "#60f090", CategoryScope.Variable),
        new("Roupas", "#f0d060", CategoryScope.Variable),
        new("Assinaturas", "#60d0f0", CategoryScope.Variable),
        new("Outros", "#a0a0a0", CategoryScope.Variable),

        new("Financiamento", "#f0a060", CategoryScope.Fixed),
        new("Faculdade", "#f0c060", CategoryScope.Fixed),
        new("Aluguel", "#f09060", CategoryScope.Fixed),
        new("Plano de saude", "#f06090", CategoryScope.Fixed),
        new("Seguro", "#d0a060", CategoryScope.Fixed),
        new("Assinatura fixa", "#60d0f0", CategoryScope.Fixed),
        new("Condominio", "#d09060", CategoryScope.Fixed),
        new("Mensalidade", "#e0c060", CategoryScope.Fixed),
        new("Outros fixos", "#a0a0a0", CategoryScope.Fixed),

        new("Investimentos", "#60f0c0", CategoryScope.Income),
        new("Venda", "#60f0a0", CategoryScope.Income),
        new("Pix recebido", "#60f0c0", CategoryScope.Income),
        new("Freelance", "#a0f0a0", CategoryScope.Income),
        new("Reembolso", "#80f0c0", CategoryScope.Income),
        new("Presente", "#c0f0a0", CategoryScope.Income),
    ];

    public static bool Exists(string category) =>
        All.Any(c => string.Equals(c.Name, category, StringComparison.OrdinalIgnoreCase));
}
