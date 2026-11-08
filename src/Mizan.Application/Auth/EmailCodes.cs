using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Users;

namespace Mizan.Application.Auth;

// Issues and checks the 6-digit email codes (BR-U4, BR-U5). The email itself goes out through the
// outbox, so the request is fast and takes the same time whether or not the account exists.
public sealed class EmailCodes(IEmailCodeRepository codes, IOutbox outbox, Clock clock)
{
    // Asking again within a minute does nothing, so the button can't be used to flood someone's inbox.
    public async Task SendAsync(User user, EmailCodePurpose purpose, CancellationToken cancellationToken)
    {
        var latest = await codes.GetLatestAsync(user.Id, purpose, cancellationToken);
        if (latest is not null && !latest.CanResend(clock.Now))
        {
            return;
        }

        var (code, plainText) = EmailCode.Issue(user.Id, purpose, clock.Now);
        codes.Add(code);
        outbox.Enqueue(OutboxTypes.SendEmail, EmailText.For(purpose, user, plainText));
    }

    public async Task<bool> TryUseAsync(User user, EmailCodePurpose purpose, string? code, CancellationToken cancellationToken) =>
        await codes.GetLatestAsync(user.Id, purpose, cancellationToken) is { } latest && latest.TryUse(code, clock.Now);
}

// Arabic first, then English, until the user's language is stored.
internal static class EmailText
{
    public static EmailMessage For(EmailCodePurpose purpose, User user, string code)
    {
        var minutes = (int)EmailCode.Lifetime.TotalMinutes;
        var (subject, arabic, english) = purpose switch
        {
            EmailCodePurpose.VerifyEmail => (
                $"رمز تأكيد بريدك: {code} | Your verification code",
                "رمز تأكيد بريدك الإلكتروني",
                "Your email verification code"),
            _ => (
                $"رمز استعادة كلمة المرور: {code} | Password reset code",
                "رمز استعادة كلمة المرور",
                "Your password reset code"),
        };

        var body = $"""
            مرحبًا {user.Name.Value}،

            {arabic}: {code}
            ينتهي بعد {minutes} دقيقة. إذا ما طلبته، تجاهل هذي الرسالة.

            ---

            Hi {user.Name.Value},

            {english}: {code}
            It expires in {minutes} minutes. If you didn't ask for it, ignore this email.
            """;

        return new EmailMessage(user.Email.Value, subject, body);
    }
}
