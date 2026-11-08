using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Mizan.Application.Auth;
using Mizan.Application.Profile;
using Mizan.Infrastructure.Background;

namespace Mizan.Api.Tests;

// UC-05 and UC-07: email codes for verifying the email and resetting the password.
[Collection(ApiCollection.Name)]
public sealed class AccountEmailTests(MizanApiFactory factory)
{
    private const string Password = "P@ssw0rd123";

    private Task SendOutboxAsync() =>
        factory.Services.GetRequiredService<BankingJobRunner>().ProcessOutboxAsync(CancellationToken.None);

    [Fact]
    public async Task SigningUp_SendsACode_AndTheRightCodeVerifiesTheEmail()
    {
        var api = await ApiClient.RegisterAsync(factory);
        Assert.False((await api.GetAsync<ProfileDto>("/api/me")).EmailVerified);

        await SendOutboxAsync();
        var email = Assert.Single(factory.Emails.To(api.Email));
        Assert.Contains(factory.Emails.LatestCode(api.Email), email.Body);

        var wrongCode = factory.Emails.LatestCode(api.Email) == "000000" ? "111111" : "000000";
        var wrong = await api.RawPostAsync("/api/me/email/verify", new { code = wrongCode });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        await api.VerifyEmailAsync(factory);
        Assert.True((await api.GetAsync<ProfileDto>("/api/me")).EmailVerified);
    }

    [Fact]
    public async Task LinkingABank_NeedsAVerifiedEmail()
    {
        var api = await ApiClient.RegisterAsync(factory);

        var response = await api.RawPostAsync("/api/bank-connections/link", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AskingAgainWithinAMinute_DoesNotSendAnotherEmail()
    {
        var api = await ApiClient.RegisterAsync(factory);

        await api.RawPostAsync("/api/me/email/send-code", new { });
        await SendOutboxAsync();

        Assert.Single(factory.Emails.To(api.Email));
    }

    [Fact]
    public async Task ACode_StopsWorking_AfterFiveWrongTries()
    {
        var api = await ApiClient.RegisterAsync(factory);
        await SendOutboxAsync();
        var code = factory.Emails.LatestCode(api.Email);
        var wrong = code == "123456" ? "654321" : "123456";

        for (var i = 0; i < 5; i++)
        {
            await api.RawPostAsync("/api/me/email/verify", new { code = wrong });
        }

        var response = await api.RawPostAsync("/api/me/email/verify", new { code });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ForAnUnknownEmail_LooksTheSame_AndSendsNothing()
    {
        var email = $"nobody-{Guid.NewGuid():N}@example.com";

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new { email });
        await SendOutboxAsync();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(factory.Emails.To(email));
    }

    [Fact]
    public async Task ResettingThePassword_ChangesIt_EndsOtherSessions_AndVerifiesTheEmail()
    {
        var client = new ApiClient(factory.CreateClient(), $"user-{Guid.NewGuid():N}@example.com");
        var old = await client.PostAsync<AuthTokens>("/api/auth/register", new
        {
            name = "Test User", email = client.Email, password = Password, monthlyIncome = 10_000, periodStartDay = 1,
        });

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.RawPostAsync("/api/auth/forgot-password", new { email = client.Email })).StatusCode);
        await SendOutboxAsync();
        var code = factory.Emails.LatestCode(client.Email);

        var tooShort = await client.RawPostAsync("/api/auth/reset-password", new { email = client.Email, code, newPassword = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        var tokens = await client.PostAsync<AuthTokens>("/api/auth/reset-password",
            new { email = client.Email, code, newPassword = "N3w-passw0rd" });

        // The old session is ended, the old password no longer works, the new one does.
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.RawPostAsync("/api/auth/refresh", new { refreshToken = old.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.RawPostAsync("/api/auth/login", new { email = client.Email, password = Password })).StatusCode);
        await client.PostAsync<AuthTokens>("/api/auth/login", new { email = client.Email, password = "N3w-passw0rd" });

        // The code worked once only.
        var again = await client.RawPostAsync("/api/auth/reset-password", new { email = client.Email, code, newPassword = "An0ther-one" });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        client.UseToken(tokens.AccessToken);
        Assert.True((await client.GetAsync<ProfileDto>("/api/me")).EmailVerified);
    }
}
