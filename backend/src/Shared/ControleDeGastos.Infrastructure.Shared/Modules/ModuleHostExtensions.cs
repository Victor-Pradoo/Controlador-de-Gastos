using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Infrastructure.Shared.Modules;

public static class ModuleHostExtensions
{
    private static readonly List<IModule> Registered = [];

    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params IModule[] modules)
    {
        foreach (var module in modules)
        {
            module.RegisterServices(services, configuration);
            Registered.Add(module);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in Registered)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    public static IReadOnlyList<IModule> RegisteredModules => Registered;
}
