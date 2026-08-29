using ControleDeGastos.Infrastructure.Shared.Http;
using ControleDeGastos.Modules.Recurrences.Application;
using ControleDeGastos.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControleDeGastos.Modules.Recurrences.Presentation;

internal static class FixedExpenseEndpoints
{
    public static IEndpointRouteBuilder MapFixedExpenseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fixed-expenses").WithTags("Gastos fixos");

        group.MapGet("/", async (
            ICurrentUser currentUser,
            FixedExpenseService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(currentUser.UserId, cancellationToken)))
        .WithName("ListFixedExpenses");

        group.MapPost("/", async (
            CreateFixedExpenseRequest body,
            ICurrentUser currentUser,
            FixedExpenseService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AddAsync(
                currentUser.UserId,
                body.Description,
                body.Amount,
                body.Category,
                body.DayOfMonth,
                cancellationToken);

            return result.ToHttpResult(id => Results.Created($"/api/fixed-expenses/{id}", new { id }));
        })
        .WithName("CreateFixedExpense");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            FixedExpenseService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeactivateAsync(currentUser.UserId, id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeactivateFixedExpense")
        .WithSummary("Desativa o gasto fixo; lancamentos ja gerados permanecem no historico.");

        group.MapPost("/materialize", async (
            string? month,
            ICurrentUser currentUser,
            IClock clock,
            FixedExpenseService service,
            CancellationToken cancellationToken) =>
        {
            var competence = MonthParameter.Resolve(month, clock);
            var created = await service.MaterializeAsync(currentUser.UserId, competence, cancellationToken);
            return Results.Ok(new { month = competence.ToString(), created });
        })
        .WithName("MaterializeFixedExpenses")
        .WithSummary("Gera os lancamentos dos gastos fixos na competencia (idempotente).");

        return endpoints;
    }

    internal sealed record CreateFixedExpenseRequest(string Description, decimal Amount, string Category, int DayOfMonth);
}
