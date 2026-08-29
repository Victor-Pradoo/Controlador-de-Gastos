using ControleDeGastos.Infrastructure.Shared.Modules;
using ControleDeGastos.Infrastructure.Shared.Persistence;
using ControleDeGastos.Modules.Categorization.Application;
using ControleDeGastos.Modules.Categorization.Contracts;
using ControleDeGastos.Modules.Categorization.Domain;
using ControleDeGastos.Modules.Categorization.Infrastructure;
using ControleDeGastos.Modules.Categorization.Presentation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Modules.Categorization;

/// <summary>
/// Modulo Categorization: catalogo de categorias e as regras que transformam
/// a descricao crua do extrato em algo legivel.
/// </summary>
public sealed class CategorizationModule : IModule
{
    public string Name => "categorization";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CategorizationDbContext>(configuration, CategorizationDbContext.Schema);
        services.AddScoped<ICategorizationUnitOfWork>(sp => sp.GetRequiredService<CategorizationDbContext>());

        services.AddScoped<ICategoryRuleRepository, CategoryRuleRepository>();
        services.AddScoped<CategorizationService>();
        services.AddScoped<ICategorizationModuleApi>(sp => sp.GetRequiredService<CategorizationService>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapCategorizationEndpoints();
}
