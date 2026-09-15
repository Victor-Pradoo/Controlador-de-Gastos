using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain.Competence;
using ControleDeGastos.Modules.Ledger.Infrastructure;
using ControleDeGastos.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ControleDeGastos.Modules.Ledger.Application.Transactions;

/// <summary>
/// Leituras do modulo. Consultas vao direto ao DbContext (sem repositorio):
/// leitura nao precisa de agregado, precisa de projecao enxuta.
///
/// O recorte de uma competencia e a janela da regra de virada, nao mais o primeiro
/// e o ultimo dia do mes - com virada no dia 25, a competencia atravessa dois meses
/// do calendario. O filtro em SQL continua sendo um intervalo sobre OccurredOn, entao
/// o indice (UserId, OccurredOn) segue servindo.
/// </summary>
public sealed class LedgerQueries(LedgerDbContext context, ICompetenceCalendarProvider calendars)
{
    public async Task<IReadOnlyList<TransactionDto>> GetByMonthAsync(
        Guid userId,
        YearMonth month,
        CancellationToken cancellationToken = default)
    {
        var window = await WindowOfAsync(userId, month, cancellationToken);

        if (window.IsEmpty)
        {
            return [];
        }

        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.OccurredOn >= window.Start && t.OccurredOn <= window.End)
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
        var window = await WindowOfAsync(userId, month, cancellationToken);

        var totals = window.IsEmpty
            ? []
            : await context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.OccurredOn >= window.Start && t.OccurredOn <= window.End)
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
        var window = await WindowOfAsync(userId, month, cancellationToken);

        if (window.IsEmpty)
        {
            return [];
        }

        // A ordenacao precisa vir ANTES da projecao no DTO: ordenar por uma
        // propriedade de um record ja construido nao traduz para SQL (o provedor
        // nao consegue mapear o parametro de construtor de volta para a coluna).
        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.OccurredOn >= window.Start
                && t.OccurredOn <= window.End
                && t.Kind != TransactionKind.Income)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount.Amount) })
            .OrderByDescending(x => x.Total)
            .Select(x => new CategoryTotalDto(x.Category, x.Total))
            .ToListAsync(cancellationToken);
    }

    private async Task<CompetenceWindow> WindowOfAsync(Guid userId, YearMonth month, CancellationToken cancellationToken)
    {
        var calendar = await calendars.GetAsync(userId, cancellationToken);
        return calendar.WindowOf(month);
    }
}
