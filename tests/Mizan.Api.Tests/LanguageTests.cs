using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mizan.Api.Infrastructure;

namespace Mizan.Api.Tests;

// Error messages, insights and notifications follow the app's language (Accept-Language).
[Collection(ApiCollection.Name)]
public sealed partial class LanguageTests(MizanApiFactory factory)
{
    private async Task<string> LoginErrorAsync(string? language)
    {
        var client = factory.CreateClient();
        if (language is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("detail").GetString()!;
    }

    [Theory]
    [InlineData(null, "الإيميل أو كلمة المرور غير صحيحة.")]
    [InlineData("ar", "الإيميل أو كلمة المرور غير صحيحة.")]
    [InlineData("ar-SA", "الإيميل أو كلمة المرور غير صحيحة.")]
    [InlineData("en", "Invalid credentials.")]
    [InlineData("en-US", "Invalid credentials.")]
    public async Task Errors_FollowTheRequestLanguage(string? language, string expected)
    {
        Assert.Equal(expected, await LoginErrorAsync(language));
    }

    [Fact]
    public void Messages_WithParameters_AreTranslated()
    {
        Assert.Equal("اسم الفئة مطلوب.", ErrorText.ToArabic("Category name is required."));
        Assert.Equal("'x@' مو إيميل صحيح.", ErrorText.ToArabic("'x@' is not a valid email address."));
        Assert.Equal("كلمة المرور لازم تكون بين 8 و128 حرف.", ErrorText.ToArabic("Password must be between 8 and 128 characters."));
    }

    // Internal checks that a user can't trigger from the app.
    private static readonly HashSet<string> Internal =
    [
        "A consent must expire after it is granted.",
        "A sync window must not end before it starts.",
    ];

    // A guard for new code: every fixed message thrown at the user has an Arabic version.
    [Fact]
    public void EveryFixedMessage_HasAnArabicVersion()
    {
        var src = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src"));
        var messages = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"))
            .SelectMany(f => ThrownMessage().Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value))
            .Concat(["Invalid credentials.", "The code is wrong or has expired.", "Verify your email first."])
            .Distinct()
            .Where(m => !Internal.Contains(m))
            .ToList();

        Assert.True(messages.Count > 30, $"Found only {messages.Count} messages; is the source path right? {src}");
        var untranslated = messages.Where(m => ErrorText.ToArabic(m) == m).ToList();
        Assert.True(untranslated.Count == 0, "Add to ErrorText: " + string.Join(" | ", untranslated));
    }

    [GeneratedRegex(@"throw new (?:DomainException|ValidationException|ConflictException|AuthenticationFailedException)\(""([^""{}]+)""\)")]
    private static partial Regex ThrownMessage();
}
