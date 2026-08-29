using ControleDeGastos.Infrastructure.Shared.Modules;
using ControleDeGastos.Infrastructure.Shared.Persistence;
using ControleDeGastos.Modules.Banking.Application;
using ControleDeGastos.Modules.Banking.Application.Abstractions;
using ControleDeGastos.Modules.Banking.Contracts;
using ControleDeGastos.Modules.Banking.Domain;
using ControleDeGastos.Modules.Banking.Infrastructure;
using ControleDeGastos.Modules.Banking.Infrastructure.Fake;
using ControleDeGastos.Modules.Banking.Infrastructure.Pluggy;
using ControleDeGastos.Modules.Banking.Presentation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleDeGastos.Modules.Banking;

/// <summary>
/// Modulo Banking: conexoes de Open Finance e importacao do extrato para o Ledger.
/// E a razao de ser deste MVP.
/// </summary>
public sealed class BankingModule : IModule
{
    public string Name => "banking";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<BankingDbContext>(configuration, BankingDbContext.Schema);
        services.AddScoped<IBankingUnitOfWork>(sp => sp.GetRequiredService<BankingDbContext>());

        services.AddScoped<IBankConnectionRepository, BankConnectionRepository>();
        services.AddScoped<BankSyncService>();
        services.AddScoped<IBankingModuleApi>(sp => sp.GetRequiredService<BankSyncService>());

        var options = configuration.GetSection(PluggyOptions.SectionName);
        services.Configure<PluggyOptions>(options);

        var useFake = options.GetValue("UseFakeProvider", defaultValue: true);

        if (useFake)
        {
            // Sem credenciais o app continua utilizavel de ponta a ponta com extrato sintetico.
            services.AddSingleton<IBankDataProvider, FakeBankDataProvider>();
        }
        else
        {
            services.AddHttpClient<IBankDataProvider, PluggyBankDataProvider>((sp, client) =>
            {
                var pluggy = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PluggyOptions>>().Value;
                client.BaseAddress = new Uri(pluggy.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints.MapBankingEndpoints();
}
