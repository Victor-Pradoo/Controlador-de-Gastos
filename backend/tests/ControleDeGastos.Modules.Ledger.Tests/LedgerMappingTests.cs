using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.Modules.Ledger.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Tests;

/// <summary>
/// Valida o mapeamento sem precisar de banco: construir o modelo e gerar SQL
/// nao abre conexao. Pega erro de configuracao (e de traducao do Money) no CI.
/// </summary>
public sealed class LedgerMappingTests
{
    private static LedgerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=ControleDeGastosTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new LedgerDbContext(options);
    }

    [Fact]
    public void Transacao_vive_no_schema_do_modulo()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Transaction));

        Assert.NotNull(entity);
        Assert.Equal(LedgerDbContext.Schema, entity.GetSchema());
        Assert.Equal("transactions", entity.GetTableName());
    }

    [Fact]
    public void Money_e_persistido_como_coluna_decimal()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Transaction))!;
        var amount = entity.GetComplexProperties().Single(p => p.Name == nameof(Transaction.Amount));
        var column = amount.ComplexType.GetProperties().Single();

        Assert.Equal("amount", column.GetColumnName());
        Assert.Equal(18, column.GetPrecision());
        Assert.Equal(2, column.GetScale());
    }

    [Fact]
    public void Agregacao_por_valor_traduz_para_sql()
    {
        using var context = CreateContext();
        var userId = Guid.NewGuid();

        // Se o Money nao fosse traduzivel, isto lancaria em vez de gerar SQL.
        var sql = context.Transactions
            .Where(t => t.UserId == userId && t.Kind == TransactionKind.Expense)
            .GroupBy(t => t.Kind)
            .Select(g => new { Kind = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .ToQueryString();

        Assert.Contains("sum(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("amount", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Espelha a consulta de LedgerQueries.GetCategoryTotalsAsync.
    ///
    /// Regressao: ordenar por uma propriedade do DTO ja projetado (`.Select(...)`
    /// seguido de `.OrderByDescending(c => c.Total)`) compila, mas estoura em
    /// runtime porque nao traduz para SQL. Ordenar antes de projetar resolve.
    /// </summary>
    [Fact]
    public void Totais_por_categoria_traduzem_para_sql()
    {
        using var context = CreateContext();
        var userId = Guid.NewGuid();

        var sql = context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Kind != TransactionKind.Income)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .OrderByDescending(x => x.Total)
            .Select(x => new CategoryTotalDto(x.Category, x.Total))
            .ToQueryString();

        Assert.Contains("group by", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order by", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Id_externo_tem_indice_unico_por_usuario()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Transaction))!;

        var index = entity.GetIndexes().Single(i =>
            i.Properties.Any(p => p.Name == nameof(Transaction.ExternalId)));

        Assert.True(index.IsUnique);
    }
}
