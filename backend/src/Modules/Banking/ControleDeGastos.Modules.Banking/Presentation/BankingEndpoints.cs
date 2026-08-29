using ControleDeGastos.Infrastructure.Shared.Http;
using ControleDeGastos.Modules.Banking.Application;
using ControleDeGastos.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ControleDeGastos.Modules.Banking.Presentation;

internal static class BankingEndpoints
{
    public static IEndpointRouteBuilder MapBankingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/banking").WithTags("Banking");

        group.MapPost("/connect-token", async (
            ICurrentUser currentUser,
            BankSyncService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateConnectTokenAsync(currentUser.UserId, cancellationToken);
            return result.ToHttpResult(token => Results.Ok(new { token }));
        })
        .WithName("CreateConnectToken")
        .WithSummary("Token de curta duracao para o widget de conexao do provedor.");

        group.MapGet("/connections", async (
            ICurrentUser currentUser,
            BankSyncService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListConnectionsAsync(currentUser.UserId, cancellationToken)))
        .WithName("ListBankConnections");

        group.MapPost("/connections", async (
            ConnectRequest body,
            ICurrentUser currentUser,
            BankSyncService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ConnectAsync(currentUser.UserId, body.ItemId, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/api/banking/connections/{id}", new { id }));
        })
        .WithName("CreateBankConnection")
        .WithSummary("Registra o itemId devolvido pelo widget do provedor.");

        group.MapPost("/connections/{id:guid}/sync", async (
            Guid id,
            int? daysBack,
            ICurrentUser currentUser,
            BankSyncService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.SyncAsync(currentUser.UserId, id, daysBack ?? 30, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("SyncBankConnection")
        .WithSummary("Importa o extrato para o Ledger. Idempotente por transacao.");

        return endpoints;
    }

    internal sealed record ConnectRequest(string ItemId);
}
