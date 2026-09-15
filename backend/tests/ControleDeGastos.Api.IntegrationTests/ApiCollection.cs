namespace ControleDeGastos.Api.IntegrationTests;

/// <summary>
/// Um unico host para toda a suite.
///
/// Nao e so economia de tempo: <c>ModuleHostExtensions</c> guarda os modulos
/// registrados numa lista ESTATICA, entao compor o host duas vezes no mesmo processo
/// registra cada modulo de novo e o roteamento quebra com "Duplicate endpoint name".
/// Enquanto essa lista for estatica, os testes precisam compartilhar a fabrica.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
