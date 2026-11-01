using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Mizan.Application.Abstractions;

namespace Mizan.Infrastructure.Banking.Lean;

// Lean's API token lasts about an hour, so it is reused until a minute before it expires.
// Customer tokens are made fresh for each link screen (they need at least 10 minutes left).
public sealed class LeanTokenCache(IHttpClientFactory httpClients, IOptions<LeanOptions> options, TimeProvider time)
{
    public const string HttpClientName = "lean-auth";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _apiToken;
    private DateTimeOffset _apiTokenExpiresAt;

    public async Task<string> GetApiTokenAsync(CancellationToken cancellationToken)
    {
        if (_apiToken is not null && time.GetUtcNow() < _apiTokenExpiresAt)
        {
            return _apiToken;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_apiToken is null || time.GetUtcNow() >= _apiTokenExpiresAt)
            {
                var token = await RequestAsync("api", cancellationToken);
                _apiToken = token.AccessToken;
                _apiTokenExpiresAt = time.GetUtcNow().AddSeconds(Math.Max(60, token.ExpiresIn - 60));
            }

            return _apiToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> GetCustomerTokenAsync(string customerId, CancellationToken cancellationToken) =>
        (await RequestAsync($"customer.{customerId}", cancellationToken)).AccessToken;

    private async Task<TokenResponse> RequestAsync(string scope, CancellationToken cancellationToken)
    {
        var lean = options.Value;
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = lean.AppToken,
            ["client_secret"] = lean.ClientSecret,
            ["grant_type"] = "client_credentials",
            ["scope"] = scope,
        });

        var http = httpClients.CreateClient(HttpClientName);
        try
        {
            using var response = await http.PostAsync($"{lean.AuthUrl.TrimEnd('/')}/oauth2/token", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new BankingProviderException($"Lean refused the token request ({(int)response.StatusCode}). Check Lean:AppToken and Lean:ClientSecret.");
            }

            return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new BankingProviderException("Lean returned an empty token.");
        }
        catch (HttpRequestException exception)
        {
            throw new BankingProviderException("Lean's auth server could not be reached.", exception);
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
