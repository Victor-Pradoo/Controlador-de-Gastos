using ControleDeGastos.Infrastructure.Shared.Http;
using ControleDeGastos.Modules.Ledger.Application.Competence;
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
            CompetenceService competences,
            LedgerQueries queries,
            CancellationToken cancellationToken) =>
        {
            var competence = MonthParameter.TryParse(month)
                ?? await competences.GetCurrentAsync(currentUser.UserId, cancellationToken);
            var transactions = await queries.GetByMonthAsync(currentUser.UserId, competence, cancellationToken);
            return Results.Ok(transactions);
        })
        .WithName("ListTransactions")
        .WithSummary("Lancamentos da competencia (default: mes corrente).");

        group.MapGet("/summary", async (
            string? month,
            ICurrentUser currentUser,
            CompetenceService competences,
            LedgerQueries queries,
            CancellationToken cancellationToken) =>
        {
            var competence = MonthParameter.TryParse(month)
                ?? await competences.GetCurrentAsync(currentUser.UserId, cancellationToken);
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

        MapCompetenceEndpoints(endpoints);

        return endpoints;
    }

    /// <summary>
    /// A regra de virada de competencia. Fica sob /api/ledger e nao sob /api/budget:
    /// ela decide a que competencia um lancamento pertence, o que e assunto do Ledger,
    /// ainda que a tela de configuracoes mostre isto ao lado do orcamento.
    /// </summary>
    private static void MapCompetenceEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ledger/competence").WithTags("Competencia");

        group.MapGet("/", async (
            ICurrentUser currentUser,
            CompetenceService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetSettingsAsync(currentUser.UserId, cancellationToken)))
        .WithName("GetCompetenceSettings")
        .WithSummary("Dia de virada, competencia corrente, sua janela e o encerramento que da para desfazer.");

        group.MapPut("/closing-day", async (
            UpdateClosingDayRequest body,
            ICurrentUser currentUser,
            CompetenceService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateClosingDayAsync(currentUser.UserId, body.ClosingDay, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateClosingDay")
        .WithSummary("Define o dia a partir do qual o gasto conta para a competencia seguinte. Vazio = mes do calendario.");

        group.MapPost("/close", async (
            ICurrentUser currentUser,
            CompetenceService service,
            CancellationToken cancellationToken) =>
        {
            var current = await service.GetCurrentAsync(currentUser.UserId, cancellationToken);
            var result = await service.CloseAsync(currentUser.UserId, current, cancellationToken);

            return result.ToHttpResult(next => Results.Ok(new { closed = current, current = next }));
        })
        .WithName("CloseCurrentCompetence")
        .WithSummary("Encerra os gastos da competencia corrente: a partir de hoje os novos entram na seguinte.");

        group.MapDelete("/close/{month}", async (
            string month,
            ICurrentUser currentUser,
            CompetenceService service,
            CancellationToken cancellationToken) =>
        {
            if (MonthParameter.TryParse(month) is not { } competence)
            {
                return Results.Problem(
                    title: "ledger.invalid_month",
                    detail: $"Competencia invalida: '{month}'. Formato esperado: yyyy-MM.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await service.ReopenAsync(currentUser.UserId, competence, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("ReopenCompetence")
        .WithSummary("Desfaz um encerramento manual.");
    }

    internal sealed record UpdateClosingDayRequest(int? ClosingDay);

    /// <summary>Corpo do POST. O UserId nunca vem do cliente: sai do token.</summary>
    internal sealed record CreateTransactionRequest(
        TransactionKind Kind,
        string Description,
        decimal Amount,
        string Category,
        DateOnly OccurredOn);
}
