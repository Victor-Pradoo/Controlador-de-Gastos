namespace ControleDeGastos.Modules.Banking.Infrastructure.Pluggy;

public sealed class PluggyOptions
{
    public const string SectionName = "Banking:Pluggy";

    public string BaseUrl { get; set; } = "https://api.pluggy.ai";

    /// <summary>Use user-secrets / variavel de ambiente. Nunca commite isto.</summary>
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Quando true (padrao em Development), usa o provedor falso com dados sinteticos:
    /// da para desenvolver a UI inteira sem credencial de sandbox.
    /// </summary>
    public bool UseFakeProvider { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
