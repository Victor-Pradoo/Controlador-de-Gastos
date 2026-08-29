using ControleDeGastos.Infrastructure.Shared.Http;
using ControleDeGastos.Modules.Ledger.Application.Transactions;
using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControleDeGastos.Modules.Ledger.Presentation;

internal static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ledger").WithTags("Ledger");

        group.MapGet("/transactions", async (
            string? month,
            ICurrentUser currentUser,
            IClock clock,
            LedgerQueries queries,
            CancellationToken cancellationToken) =>
        {
            var competence = MonthParameter.Resolve(month, clock);
            var transactions = await queries.GetByMonthAsync(currentUser.UserId, competence, cancellationToken);
            return Results.Ok(transactions);
        })
        .WithName("ListTransactions")
        .WithSummary("Lancamentos da competencia (default: mes corrente).");

        group.MapGet("/summary", async (
            string? month,
            ICurrentUser currentUser,
            IClock clock,
            LedgerQueries queries,
            CancellationToken cancellationToken) =>
        {
            var competence = MonthParameter.Resolve(month, clock);
            var totals = await queries.GetMonthlyTotalsAsync(currentUser.UserId, competence, cancellationToken);
            var categories = await queries.GetCategoryTotalsAsync(currentUser.UserId, competence, cancellationToken);
            return Results.Ok(new { totals, categories });
        })
        .WithName("GetLedgerSummary")
        .WithSummary("Totais e quebra por categoria da competencia.");

        group.MapPost("/transactions", async (
            CreateTransactionRequest body,
            ICurrentUser currentUser,
            RegisterTransactionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var request = new RegisterTransactionRequest(
                currentUser.UserId,
                body.Kind,
                TransactionSource.Manual,
                body.Description,
                body.Amount,
                body.Category,
                body.OccurredOn);

            var result = await handler.HandleAsync(request, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/api/ledger/transactions/{id}", new { id }));
        })
        .WithName("CreateTransaction")
        .WithSummary("Registra um lancamento manual (gasto, entrada ou fixo avulso).");

        group.MapDelete("/transactions/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            DeleteTransactionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(currentUser.UserId, id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteTransaction")
        .WithSummary("Remove um lancamento manual.");

        return endpoints;
    }

    /// <summary>Corpo do POST. O UserId nunca vem do cliente: sai do token.</summary>
    internal sealed record CreateTransactionRequest(
        TransactionKind Kind,
        string Description,
        decimal Amount,
        string Category,
        DateOnly OccurredOn);
}
