using System.Net;
using System.Net.Http.Json;
using Mizan.Application.Auth;
using Mizan.Application.Categories;
using Mizan.Application.Transactions;
using Mizan.Domain.Categories;

namespace Mizan.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class SecurityTests(MizanApiFactory factory)
{
    private const string Password = "P@ssw0rd123";

    [Fact]
    public async Task ProtectedEndpoints_RequireAToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Users_CannotSeeOrUseEachOthersData()
    {
        var alice = await ApiClient.RegisterAsync(factory);
        var bob = await ApiClient.RegisterAsync(factory);

        var aliceCategory = (await alice.GetAsync<List<CategoryDto>>("/api/categories")).First();
        var aliceTransaction = await alice.PostAsync<TransactionDto>("/api/transactions", new
        {
            amount = 10,
            categoryId = aliceCategory.Id,
            date = ApiClient.Today,
        });

        var read = await bob.Http.GetAsync($"/api/transactions/{aliceTransaction.Id}");
        var write = await bob.RawPostAsync("/api/transactions", new
        {
            amount = 10,
            categoryId = aliceCategory.Id,
            date = ApiClient.Today,
        });
        var delete = await bob.Http.DeleteAsync($"/api/transactions/{aliceTransaction.Id}");

        // 404, not 403: Bob must not even learn that the item exists.
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsRejected()
    {
        var email = await RegisterAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateEmail_IsAConflict()
    {
        var email = await RegisterAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", RegisterBody(email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ShortPassword_IsRejected()
    {
        var body = RegisterBody($"user-{Guid.NewGuid():N}@example.com") with { password = "short" };

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_RotatesAndDetectsReuse()
    {
        var client = new ApiClient(factory.CreateClient());
        var first = await client.PostAsync<AuthTokens>("/api/auth/register", RegisterBody($"user-{Guid.NewGuid():N}@example.com"));

        var second = await client.PostAsync<AuthTokens>("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        // Using the old token again looks like theft: it fails and also ends the newer session.
        var reuse = await client.RawPostAsync("/api/auth/refresh", new { refreshToken = first.RefreshToken });
        var afterReuse = await client.RawPostAsync("/api/auth/refresh", new { refreshToken = second.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var client = new ApiClient(factory.CreateClient());
        var tokens = await client.PostAsync<AuthTokens>("/api/auth/register", RegisterBody($"user-{Guid.NewGuid():N}@example.com"));

        await client.RawPostAsync("/api/auth/logout", new { refreshToken = tokens.RefreshToken });
        var refresh = await client.RawPostAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task DeletingAccount_RemovesTheUser()
    {
        var email = await RegisterAsync();
        var client = new ApiClient(factory.CreateClient());
        var tokens = await client.PostAsync<AuthTokens>("/api/auth/login", new { email, password = Password });
        client.UseToken(tokens.AccessToken);

        var delete = await client.Http.DeleteAsync("/api/me");
        var login = await client.RawPostAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task InvalidInput_ReturnsProblemDetails()
    {
        var api = await ApiClient.RegisterAsync(factory);
        var other = (await api.GetAsync<List<CategoryDto>>("/api/categories")).Single(c => c.Kind == CategoryKind.Other);

        var futureDate = await api.RawPostAsync("/api/transactions", new { amount = 10, categoryId = other.Id, date = ApiClient.Today.AddDays(1) });
        var missingField = await api.RawPostAsync("/api/transactions", new { amount = 10 });
        var duplicateCategory = await api.RawPostAsync("/api/categories", new { name = "أخرى", monthlyLimit = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, futureDate.StatusCode);
        Assert.Equal("application/problem+json", futureDate.Content.Headers.ContentType?.MediaType);
        Assert.Contains("المستقبل", await futureDate.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, missingField.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateCategory.StatusCode);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", RegisterBody(email));
        response.EnsureSuccessStatusCode();
        return email;
    }

    private static RegisterBodyModel RegisterBody(string email) => new("Test User", email, Password, 10_000, 1);

    private sealed record RegisterBodyModel(string name, string email, string password, decimal monthlyIncome, int periodStartDay);
}
