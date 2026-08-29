using ControleDeGastos.Infrastructure.Shared.Http;
using ControleDeGastos.Modules.Budgeting.Application;
using ControleDeGastos.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControleDeGastos.Modules.Budgeting.Presentation;

internal static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/budget").WithTags("Budget");

        group.MapGet("/", async (
            string? month,
            ICurrentUser currentUser,
            IClock clock,
            BudgetService service,
            CancellationToken cancellationToken) =>
        {
            var competence = MonthParameter.Resolve(month, clock);
            var budget = await service.GetMonthlyBudgetAsync(currentUser.UserId, competence, cancellationToken);
            return Results.Ok(budget);
        })
        .WithName("GetMonthlyBudget")
        .WithSummary("Orcamento do mes: disponivel, gasto, saldo e nivel de alerta.");

        group.MapGet("/settings", async (
            ICurrentUser currentUser,
            BudgetService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetSettingsAsync(currentUser.UserId, cancellationToken)))
        .WithName("GetBudgetSettings");

        group.MapPut("/settings", async (
            UpdateSettingsRequest body,
            ICurrentUser currentUser,
            BudgetService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateSettingsAsync(currentUser.UserId, body.Salary, body.ReserveRate, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("UpdateBudgetSettings")
        .WithSummary("Define salario liquido e taxa de reserva.");

        return endpoints;
    }

    internal sealed record UpdateSettingsRequest(decimal Salary, decimal ReserveRate);
}
