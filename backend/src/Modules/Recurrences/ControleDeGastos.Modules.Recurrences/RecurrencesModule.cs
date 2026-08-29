using ControleDeGastos.Infrastructure.Shared.Modules;
using ControleDeGastos.Infrastructure.Shared.Persistence;
using ControleDeGastos.Modules.Recurrences.Application;
using ControleDeGastos.Modules.Recurrences.Contracts;
using ControleDeGastos.Modules.Recurrences.Domain;
using ControleDeGastos.Modules.Recurrences.Infrastructure;
using ControleDeGastos.Modules.Recurrences.Presentation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Modules.Recurrences;

/// <summary>
/// Modulo Recurrences: cadastro de gastos fixos e geracao mensal dos lancamentos
/// correspondentes no Ledger.
/// </summary>
public sealed class RecurrencesModule : IModule
{
    public string Name => "recurrences";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<RecurrencesDbContext>(configuration, RecurrencesDbContext.Schema);
        services.AddScoped<IRecurrencesUnitOfWork>(sp => sp.GetRequiredService<RecurrencesDbContext>());

        services.AddScoped<IFixedExpenseRepository, FixedExpenseRepository>();
        services.AddScoped<FixedExpenseService>();
        services.AddScoped<IRecurrencesModuleApi>(sp => sp.GetRequiredService<FixedExpenseService>());

        services.AddHostedService<RecurrenceMaterializationWorker>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapFixedExpenseEndpoints();
}
