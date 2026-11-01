using Microsoft.Extensions.Logging;
using Mizan.Application.Abstractions;

namespace Mizan.Infrastructure.Emailing;

// shortcut: writes the email to the server log instead of sending it; replace with a real provider (Phase 3) before launch.
internal sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", message.To, message.Subject, message.Body);
        return Task.CompletedTask;
    }
}
