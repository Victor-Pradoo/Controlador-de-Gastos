using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Infrastructure.Shared.Modules;

/// <summary>
/// Ponto de entrada de um modulo do monolito. Cada modulo se auto-descreve:
/// o que registra no container e quais endpoints expoe. O host apenas compoe.
/// </summary>
public interface IModule
{
    /// <summary>Nome curto do modulo; usado tambem como schema no banco.</summary>
    string Name { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
