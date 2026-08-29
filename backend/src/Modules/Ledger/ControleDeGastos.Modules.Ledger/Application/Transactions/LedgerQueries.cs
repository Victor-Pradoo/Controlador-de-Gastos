using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Infrastructure;
using ControleDeGastos.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Application.Transactions;

/// <summary>
/// Leituras do modulo. Consultas vao direto ao DbContext (sem repositorio):
/// leitura nao precisa de agregado, precisa de projecao enxuta.
/// </summary>
public sealed class LedgerQueries(LedgerDbContext context)
{
    public async Task<IReadOnlyList<TransactionDto>> GetByMonthAsync(
        Guid userId,
        YearMonth month,
        CancellationToken cancellationToken = default)
    {
        var first = month.FirstDay;
        var last = month.LastDay;

        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.OccurredOn >= first && t.OccurredOn <= last)
            .OrderByDescending(t => t.OccurredOn)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto(
                t.Id,
                t.Kind,
                t.Source,
                t.Description,
                t.Amount.Amount,
                t.Category,
                t.OccurredOn,
                t.Source == TransactionSource.Manual))
            .ToListAsync(cancellationToken);
    }

    public async Task<MonthlyTotalsDto> GetMonthlyTotalsAsync(
        Guid userId,
        YearMonth month,
        CancellationToken cancellationToken = default)
    {
        var first = month.FirstDay;
        var last = month.LastDay;

        var totals = await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.OccurredOn >= first && t.OccurredOn <= last)
            .GroupBy(t => t.Kind)
            .Select(g => new { Kind = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .ToListAsync(cancellationToken);

        decimal TotalOf(TransactionKind kind) => totals.FirstOrDefault(t => t.Kind == kind)?.Total ?? 0m;

        return new MonthlyTotalsDto(
            month,
            TotalOf(TransactionKind.Expense),
            TotalOf(TransactionKind.FixedExpense),
            TotalOf(TransactionKind.Income));
    }

    public async Task<IReadOnlyList<CategoryTotalDto>> GetCategoryTotalsAsync(
        Guid userId,
        YearMonth month,
        CancellationToken cancellationToken = default)
    {
        var first = month.FirstDay;
        var last = month.LastDay;

        // A ordenacao precisa vir ANTES da projecao no DTO: ordenar por uma
        // propriedade de um record ja construido nao traduz para SQL (o provedor
        // nao consegue mapear o parametro de construtor de volta para a coluna).
        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.OccurredOn >= first
                && t.OccurredOn <= last
                && t.Kind != TransactionKind.Income)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .OrderByDescending(x => x.Total)
            .Select(x => new CategoryTotalDto(x.Category, x.Total))
            .ToListAsync(cancellationToken);
    }
}
