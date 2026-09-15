namespace ControleDeGastos.Api.DatabaseTests;

/// <summary>
/// Um host para toda a suite: subir a API aplica migrations, e nao ha razao para
/// pagar isso por classe de teste. Tambem evita duas composicoes do host no mesmo
/// processo, que a lista estatica de modulos em <c>ModuleHostExtensions</c> nao suporta.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseCollection : ICollectionFixture<SqlServerApiFactory>
{
    public const string Name = "database";
}
