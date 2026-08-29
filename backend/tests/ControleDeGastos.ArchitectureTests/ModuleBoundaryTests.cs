using System.Reflection;
using ControleDeGastos.Modules.Banking;
using ControleDeGastos.Modules.Budgeting;
using ControleDeGastos.Modules.Categorization;
using ControleDeGastos.Modules.Ledger;
using ControleDeGastos.Modules.Recurrences;
using NetArchTest.Rules;

namespace ControleDeGastos.ArchitectureTests;

/// <summary>
/// A regra que sustenta o monolito modular: um modulo so pode enxergar o projeto
/// *.Contracts de outro. Estes testes falham no CI antes de a fronteira erodir.
/// </summary>
public sealed class ModuleBoundaryTests
{
    /// <summary>Camadas internas de um modulo - nada disso pode vazar para outro.</summary>
    private static readonly string[] InternalLayers = ["Domain", "Application", "Infrastructure", "Presentation"];

    private static readonly Dictionary<string, Assembly> ModuleAssemblies = new()
    {
        ["Ledger"] = typeof(LedgerModule).Assembly,
        ["Budgeting"] = typeof(BudgetingModule).Assembly,
        ["Recurrences"] = typeof(RecurrencesModule).Assembly,
        ["Categorization"] = typeof(CategorizationModule).Assembly,
        ["Banking"] = typeof(BankingModule).Assembly,
    };

    public static TheoryData<string, string> ModulePairs()
    {
        var data = new TheoryData<string, string>();

        foreach (var source in ModuleAssemblies.Keys)
        {
            foreach (var target in ModuleAssemblies.Keys.Where(t => t != source))
            {
                data.Add(source, target);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void Modulo_nao_deve_depender_das_camadas_internas_de_outro(string source, string target)
    {
        var forbidden = InternalLayers
            .Select(layer => $"ControleDeGastos.Modules.{target}.{layer}")
            .ToArray();

        var result = Types.InAssembly(ModuleAssemblies[source])
            .Should()
            .NotHaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"O modulo {source} acessa internals de {target}. Use ControleDeGastos.Modules.{target}.Contracts. " +
            $"Tipos infratores: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Theory]
    [InlineData("Ledger")]
    [InlineData("Budgeting")]
    [InlineData("Recurrences")]
    [InlineData("Categorization")]
    [InlineData("Banking")]
    public void Dominio_nao_deve_depender_do_EF_Core(string module)
    {
        var result = Types.InAssembly(ModuleAssemblies[module])
            .That()
            .ResideInNamespaceStartingWith($"ControleDeGastos.Modules.{module}.Domain")
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"O dominio de {module} vazou para infraestrutura. Tipos: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    /// <summary>
    /// As INTERFACES de repositorio sao publicas de proposito (sao a porta do dominio);
    /// as implementacoes e os endpoints nao devem escapar do assembly do modulo.
    /// </summary>
    [Theory]
    [InlineData("Ledger", "Repository")]
    [InlineData("Budgeting", "Repository")]
    [InlineData("Recurrences", "Repository")]
    [InlineData("Categorization", "Repository")]
    [InlineData("Banking", "Repository")]
    [InlineData("Ledger", "Endpoints")]
    [InlineData("Budgeting", "Endpoints")]
    [InlineData("Recurrences", "Endpoints")]
    [InlineData("Categorization", "Endpoints")]
    [InlineData("Banking", "Endpoints")]
    public void Implementacoes_internas_nao_devem_ser_publicas(string module, string suffix)
    {
        var result = Types.InAssembly(ModuleAssemblies[module])
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith(suffix)
            .Should()
            .NotBePublic()
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Detalhes internos de {module} estao publicos: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
