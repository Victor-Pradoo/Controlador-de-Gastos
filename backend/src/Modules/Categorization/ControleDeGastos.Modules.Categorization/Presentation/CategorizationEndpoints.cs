using ControleDeGastos.Infrastructure.Shared.Http;
using ControleDeGastos.Modules.Categorization.Application;
using ControleDeGastos.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControleDeGastos.Modules.Categorization.Presentation;

internal static class CategorizationEndpoints
{
    public static IEndpointRouteBuilder MapCategorizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories").WithTags("Categorias");

        group.MapGet("/", (CategorizationService service) => Results.Ok(service.GetCatalog()))
            .WithName("GetCategoryCatalog")
            .WithSummary("Catalogo de categorias com cores, consumido pelo front.");

        group.MapGet("/rules", async (
            ICurrentUser currentUser,
            CategorizationService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListRulesAsync(currentUser.UserId, cancellationToken)))
        .WithName("ListCategoryRules");

        group.MapPost("/rules", async (
            CreateRuleRequest body,
            ICurrentUser currentUser,
            CategorizationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AddRuleAsync(
                currentUser.UserId,
                body.Keyword,
                body.Category,
                body.Priority,
                cancellationToken);

            return result.ToHttpResult(id => Results.Created($"/api/categories/rules/{id}", new { id }));
        })
        .WithName("CreateCategoryRule")
        .WithSummary("Ensina o sistema a categorizar uma descricao recorrente do extrato.");

        group.MapDelete("/rules/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            CategorizationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RemoveRuleAsync(currentUser.UserId, id, cancellationToken);
            return result.ToHttpResult();
        })
        .WithName("DeleteCategoryRule");

        return endpoints;
    }

    internal sealed record CreateRuleRequest(string Keyword, string Category, int Priority = 0);
}
