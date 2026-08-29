using ControleDeGastos.Infrastructure.Shared.Modules;
using ControleDeGastos.Infrastructure.Shared.Persistence;
using ControleDeGastos.Modules.Budgeting.Application;
using ControleDeGastos.Modules.Budgeting.Contracts;
using ControleDeGastos.Modules.Budgeting.Domain;
using ControleDeGastos.Modules.Budgeting.Infrastructure;
using ControleDeGastos.Modules.Budgeting.Presentation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Modules.Budgeting;

/// <summary>
/// Modulo Budgeting: salario, reserva e a leitura de quanto ainda da para gastar.
/// Le totais do Ledger pelo contrato publico, nunca as tabelas dele.
/// </summary>
public sealed class BudgetingModule : IModule
{
    public string Name => "budgeting";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<BudgetingDbContext>(configuration, BudgetingDbContext.Schema);
        services.AddScoped<IBudgetingUnitOfWork>(sp => sp.GetRequiredService<BudgetingDbContext>());

        services.AddScoped<IBudgetSettingsRepository, BudgetSettingsRepository>();
        services.AddScoped<BudgetService>();
        services.AddScoped<IBudgetingModuleApi>(sp => sp.GetRequiredService<BudgetService>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapBudgetEndpoints();
}
