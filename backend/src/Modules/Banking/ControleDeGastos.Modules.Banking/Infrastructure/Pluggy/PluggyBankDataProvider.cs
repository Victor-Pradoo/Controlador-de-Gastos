using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ControleDeGastos.Modules.Banking.Application.Abstractions;
using ControleDeGastos.Modules.Banking.Domain;
using ControleDeGastos.SharedKernel.Abstractions;
using ControleDeGastos.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ControleDeGastos.Modules.Banking.Infrastructure.Pluggy;

/// <summary>
/// Adaptador da Pluggy (https://docs.pluggy.ai).
///
/// Fluxo: /auth devolve uma apiKey de ~2h -> /connect_token abre o widget no front ->
/// o widget devolve um itemId -> /accounts e /transactions trazem o extrato.
/// A senha do banco fica no widget da Pluggy; esta aplicacao nunca a ve.
/// </summary>
public sealed class PluggyBankDataProvider(
    HttpClient httpClient,
    IOptions<PluggyOptions> options,
    IClock clock,
    ILogger<PluggyBankDataProvider> logger) : IBankDataProvider
{
    private readonly PluggyOptions _options = options.Value;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private string? _apiKey;
    private DateTimeOffset _apiKeyExpiresAt = DateTimeOffset.MinValue;

    public string Name => "pluggy";

    public async Task<Result<string>> CreateConnectTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var authenticated = await EnsureAuthenticatedAsync(cancellationToken);
        if (authenticated.IsFailure)
        {
            return Result.Failure<string>(authenticated.Error);
        }

        // clientUserId amarra o item da Pluggy ao usuario daqui.
        var payload = new { clientUserId = userId.ToString() };

        return await SendAsync<PluggyConnectToken, string>(
            () => httpClient.PostAsJsonAsync("/connect_token", payload, cancellationToken),
            token => token.AccessToken,
            "criar connect token");
    }

    public async Task<Result<BankItemSnapshot>> GetItemAsync(string externalItemId, CancellationToken cancellationToken = default)
    {
        var authenticated = await EnsureAuthenticatedAsync(cancellationToken);
        if (authenticated.IsFailure)
        {
            return Result.Failure<BankItemSnapshot>(authenticated.Error);
        }

        return await SendAsync<PluggyItem, BankItemSnapshot>(
            () => httpClient.GetAsync($"/items/{externalItemId}", cancellationToken),
            item => new BankItemSnapshot(item.Id, item.Connector?.Name ?? "Instituicao", item.Status ?? "UNKNOWN"),
            "consultar item");
    }

    public async Task<Result<IReadOnlyList<BankTransactionSnapshot>>> GetTransactionsAsync(
        string externalItemId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var authenticated = await EnsureAuthenticatedAsync(cancellationToken);
        if (authenticated.IsFailure)
        {
            return Result.Failure<IReadOnlyList<BankTransactionSnapshot>>(authenticated.Error);
        }

        var accounts = await SendAsync<PluggyPage<PluggyAccount>, IReadOnlyList<PluggyAccount>>(
            () => httpClient.GetAsync($"/accounts?itemId={externalItemId}", cancellationToken),
            page => page.Results,
            "listar contas");

        if (accounts.IsFailure)
        {
            return Result.Failure<IReadOnlyList<BankTransactionSnapshot>>(accounts.Error);
        }

        var transactions = new List<BankTransactionSnapshot>();

        foreach (var account in accounts.Value)
        {
            var url = $"/transactions?accountId={account.Id}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&pageSize=500";

            var page = await SendAsync<PluggyPage<PluggyTransaction>, IReadOnlyList<PluggyTransaction>>(
                () => httpClient.GetAsync(url, cancellationToken),
                p => p.Results,
                "listar transacoes");

            if (page.IsFailure)
            {
                return Result.Failure<IReadOnlyList<BankTransactionSnapshot>>(page.Error);
            }

            transactions.AddRange(page.Value.Select(Map));
        }

        return Result.Success<IReadOnlyList<BankTransactionSnapshot>>(transactions);
    }

    /// <summary>
    /// A Pluggy usa sinal no valor (negativo = saida) e tambem manda o campo type.
    /// Normalizamos para valor positivo + direcao explicita.
    /// </summary>
    private static BankTransactionSnapshot Map(PluggyTransaction transaction)
    {
        var direction = string.Equals(transaction.Type, "CREDIT", StringComparison.OrdinalIgnoreCase)
            ? BankTransactionDirection.Credit
            : BankTransactionDirection.Debit;

        return new BankTransactionSnapshot(
            transaction.Id,
            string.IsNullOrWhiteSpace(transaction.Description) ? "Lancamento bancario" : transaction.Description,
            Math.Abs(transaction.Amount),
            DateOnly.FromDateTime(transaction.Date.UtcDateTime),
            direction);
    }

    private async Task<Result> EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            return Result.Failure(BankingErrors.Provider(
                "Credenciais da Pluggy ausentes. Defina Banking:Pluggy:ClientId e ClientSecret (user-secrets)."));
        }

        if (_apiKey is not null && clock.UtcNow < _apiKeyExpiresAt)
        {
            return Result.Success();
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (_apiKey is not null && clock.UtcNow < _apiKeyExpiresAt)
            {
                return Result.Success();
            }

            var response = await httpClient.PostAsJsonAsync(
                "/auth",
                new { clientId = _options.ClientId, clientSecret = _options.ClientSecret },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Autenticacao na Pluggy falhou com status {Status}.", (int)response.StatusCode);
                return Result.Failure(BankingErrors.Provider("Nao foi possivel autenticar no provedor bancario."));
            }

            var auth = await response.Content.ReadFromJsonAsync<PluggyAuth>(cancellationToken);
            if (auth is null || string.IsNullOrWhiteSpace(auth.ApiKey))
            {
                return Result.Failure(BankingErrors.Provider("Resposta de autenticacao invalida."));
            }

            _apiKey = auth.ApiKey;

            // A chave dura ~2h; renovamos antes para nao esbarrar no limite no meio de um sync.
            _apiKeyExpiresAt = clock.UtcNow.AddMinutes(100);

            httpClient.DefaultRequestHeaders.Remove("X-API-KEY");
            httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);

            return Result.Success();
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task<Result<TResult>> SendAsync<TResponse, TResult>(
        Func<Task<HttpResponseMessage>> send,
        Func<TResponse, TResult> map,
        string operation)
    {
        try
        {
            using var response = await send();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Pluggy respondeu {Status} ao {Operation}.", (int)response.StatusCode, operation);
                return Result.Failure<TResult>(BankingErrors.Provider($"Provedor bancario falhou ao {operation}."));
            }

            var payload = await response.Content.ReadFromJsonAsync<TResponse>();

            return payload is null
                ? Result.Failure<TResult>(BankingErrors.Provider($"Resposta vazia ao {operation}."))
                : Result.Success(map(payload));
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Erro de rede ao {Operation} na Pluggy.", operation);
            return Result.Failure<TResult>(BankingErrors.Provider($"Falha de comunicacao ao {operation}."));
        }
    }

    private sealed record PluggyAuth([property: JsonPropertyName("apiKey")] string ApiKey);

    private sealed record PluggyConnectToken([property: JsonPropertyName("accessToken")] string AccessToken);

    private sealed record PluggyPage<T>([property: JsonPropertyName("results")] IReadOnlyList<T> Results);

    private sealed record PluggyAccount([property: JsonPropertyName("id")] string Id);

    private sealed record PluggyConnector([property: JsonPropertyName("name")] string Name);

    private sealed record PluggyItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("connector")] PluggyConnector? Connector);

    private sealed record PluggyTransaction(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("date")] DateTimeOffset Date,
        [property: JsonPropertyName("type")] string? Type);
}
