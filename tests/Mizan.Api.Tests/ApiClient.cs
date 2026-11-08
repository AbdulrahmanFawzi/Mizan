using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Mizan.Application.Auth;
using Mizan.Infrastructure.Background;

namespace Mizan.Api.Tests;

internal sealed class ApiClient(HttpClient http, string email = "")
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly TimeZoneInfo Riyadh = TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Riyadh).DateTime);

    public HttpClient Http => http;

    public string Email => email;

    public static async Task<ApiClient> RegisterAsync(MizanApiFactory factory, decimal income = 10_000, int periodStartDay = 1)
    {
        var client = new ApiClient(factory.CreateClient(), $"user-{Guid.NewGuid():N}@example.com");
        var tokens = await client.PostAsync<AuthTokens>("/api/auth/register", new
        {
            name = "Test User",
            email = client.Email,
            password = "P@ssw0rd123",
            monthlyIncome = income,
            periodStartDay,
        });

        client.UseToken(tokens.AccessToken);
        return client;
    }

    // Sends the outbox and types in the code from the email, as the user would.
    public async Task VerifyEmailAsync(MizanApiFactory factory)
    {
        await factory.Services.GetRequiredService<BankingJobRunner>().ProcessOutboxAsync(CancellationToken.None);
        var response = await RawPostAsync("/api/me/email/verify", new { code = factory.Emails.LatestCode(Email) });
        await EnsureAsync(response, HttpStatusCode.NoContent);
    }

    public void UseToken(string accessToken) =>
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public async Task<T> GetAsync<T>(string url)
    {
        var response = await http.GetAsync(url);
        await EnsureAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task<T> PostAsync<T>(string url, object body)
    {
        var response = await http.PostAsJsonAsync(url, body, Json);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task<T> PutAsync<T>(string url, object body)
    {
        var response = await http.PutAsJsonAsync(url, body, Json);
        await EnsureAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public Task<HttpResponseMessage> RawPostAsync(string url, object body) => http.PostAsJsonAsync(url, body, Json);

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }

    private static async Task EnsureAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            throw new HttpRequestException($"Expected {expected} but got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }
}
