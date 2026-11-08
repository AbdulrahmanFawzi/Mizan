using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Mizan.Application.Abstractions;

namespace Mizan.Api.Tests;

public sealed partial class FakeEmailSender : IEmailSender
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> To(string email) => Sent.Where(m => m.To == email).ToList();

    public string LatestCode(string email) => SixDigits().Match(To(email).Last().Subject).Value;

    [GeneratedRegex(@"\d{6}")]
    private static partial Regex SixDigits();
}
