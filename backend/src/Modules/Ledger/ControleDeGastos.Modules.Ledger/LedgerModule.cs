using ControleDeGastos.Infrastructure.Shared.Modules;
using ControleDeGastos.Infrastructure.Shared.Persistence;
using ControleDeGastos.Modules.Ledger.Application.Transactions;
using ControleDeGastos.Modules.Ledger.Contracts;
using ControleDeGastos.Modules.Ledger.Domain;
using ControleDeGastos.Modules.Ledger.Infrastructure;
using ControleDeGastos.Modules.Ledger.Presentation;
using ControleDeGastos.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Modules.Ledger;

/// <summary>
/// Modulo Ledger: extrato do usuario (gastos, entradas e fixos materializados).
/// E o modulo central - os demais escrevem nele atraves de <see cref="ILedgerModuleApi"/>.
/// </summary>
public sealed class LedgerModule : IModule
{
    public string Name => "ledger";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<LedgerDbContext>(configuration, LedgerDbContext.Schema);
        services.AddScoped<ILedgerUnitOfWork>(sp => sp.GetRequiredService<LedgerDbContext>());

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<LedgerQueries>();
        services.AddScoped<RegisterTransactionHandler>();
        services.AddScoped<DeleteTransactionHandler>();

        services.AddScoped<ILedgerModuleApi, LedgerModuleApi>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapLedgerEndpoints();
}
