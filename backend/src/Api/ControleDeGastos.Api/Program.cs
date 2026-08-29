using System.Text.Json.Serialization;
using ControleDeGastos.Api.Authentication;
using ControleDeGastos.Api.Infrastructure;
using ControleDeGastos.Infrastructure.Shared.Messaging;
using ControleDeGastos.Infrastructure.Shared.Modules;
using ControleDeGastos.Infrastructure.Shared.Time;
using ControleDeGastos.Modules.Banking;
using ControleDeGastos.Modules.Budgeting;
using ControleDeGastos.Modules.Categorization;
using ControleDeGastos.Modules.Ledger;
using ControleDeGastos.Modules.Recurrences;
using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Messaging;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

// ---------------------------------------------------------------------------
// Blocos compartilhados por todos os modulos.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddHttpContextAccessor();

// TEMPORARIO: usuario fixo enquanto nao existe autenticacao. Ver docs/roadmap.md.
var devUserId = builder.Configuration.GetValue<Guid>("Auth:DevUserId");
builder.Services.AddScoped<ICurrentUser>(sp =>
    new DevCurrentUser(sp.GetRequiredService<IHttpContextAccessor>(), devUserId));

// ---------------------------------------------------------------------------
// Composicao do monolito modular. Esta e a UNICA lista de modulos da aplicacao:
// adicionar um modulo novo e uma linha aqui.
// ---------------------------------------------------------------------------
builder.Services.AddModules(
    builder.Configuration,
    new LedgerModule(),
    new BudgetingModule(),
    new RecurrencesModule(),
    new CategorizationModule(),
    new BankingModule());

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// Enums viajam como texto ("Expense", nao 1): o contrato fica legivel e o front
// pode usar union types de string em vez de numeros magicos.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Controle de Gastos API"));
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    modules = ModuleHostExtensions.RegisteredModules.Select(m => m.Name),
}))
.WithTags("Infra")
.WithName("HealthCheck");

app.MapModuleEndpoints();

await app.MigrateModulesAsync();

app.Run();

/// <summary>Exposto para os testes de integracao (WebApplicationFactory).</summary>
public partial class Program;
